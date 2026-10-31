"""Actual WM_MOUSEMOVE to an owned native HWND; no global cursor, foreground, or injected pointer seam."""
import argparse, ctypes, json, os, subprocess, sys, time
from ctypes import wintypes
from pathlib import Path
sys.dont_write_bytecode=True
from run_tilt_presentation_native import ROOT, PROJECT, digest, validate_png
U=ctypes.WinDLL('user32',use_last_error=True)
U.PostMessageW.argtypes=[wintypes.HWND,wintypes.UINT,wintypes.WPARAM,wintypes.LPARAM];U.PostMessageW.restype=wintypes.BOOL
U.GetWindowThreadProcessId.argtypes=[wintypes.HWND,ctypes.POINTER(wintypes.DWORD)]
U.IsWindowVisible.argtypes=[wintypes.HWND];U.IsWindowVisible.restype=wintypes.BOOL
U.GetClientRect.argtypes=[wintypes.HWND,ctypes.POINTER(wintypes.RECT)]
def wait(fn,seconds=15):
    end=time.monotonic()+seconds;last=None
    while time.monotonic()<end:
        try:
            last=fn()
            if last:return last
        except (OSError,ValueError):pass
        time.sleep(.05)
    raise TimeoutError('Native condition not met: '+repr(last))
def window(pid):
    found=[];callback=ctypes.WINFUNCTYPE(wintypes.BOOL,wintypes.HWND,wintypes.LPARAM)
    def visit(h,l):
        p=wintypes.DWORD();U.GetWindowThreadProcessId(h,ctypes.byref(p))
        if p.value==pid and U.IsWindowVisible(h):
            r=wintypes.RECT();U.GetClientRect(h,ctypes.byref(r))
            if r.right>500 and r.bottom>400:found.append(h)
        return True
    U.EnumWindows(callback(visit),0)
    return found[0] if found else None
def message(h,m,w=0,l=0):
    if not U.PostMessageW(h,m,w,l):raise ctypes.WinError(ctypes.get_last_error())
def main():
    ap=argparse.ArgumentParser(__doc__);ap.add_argument('--label',required=True);ap.add_argument('--build-label');ap.add_argument('--slice',choices=['wrapper','all','focus','hint','fallback','bounds'],default='wrapper');a=ap.parse_args()
    assert a.label and all(c.isalnum() or c in '-_' for c in a.label)
    out=ROOT/'.artifacts/tilt-presentation'/a.label;out.mkdir(parents=True,exist_ok=False);tmp=out/'tmp';tmp.mkdir()
    env=dict(os.environ,TEMP=str(tmp),TMP=str(tmp),TMPDIR=str(tmp),TILT_PRESENTATION_OUT=str(out),TILT_PRESENTATION_ROOT=str(PROJECT))
    exe=PROJECT/'.artifacts/player/Integration.exe'
    build=None
    if a.build_label:
        assert all(c.isalnum() or c in '-_' for c in a.build_label)
        build=json.loads((ROOT/'.artifacts/tilt-presentation'/a.build_label/'build.json').read_text(encoding='utf-8'))
        assert build['built'] and build['nativeExit']==0 and build['exeSha256']==digest(exe)
        assert all(digest(ROOT/k)==v['source'] for k,v in build['sourcePins'].items())
        assert bool(build['withoutBackend'])==(a.slice=='fallback')
        (out/'built-source-pins.json').write_text(json.dumps(build,indent=2))
    (out/'runner-pins.json').write_text(json.dumps({Path(__file__).name:digest(Path(__file__)), 'run_tilt_presentation_native.py':digest(ROOT/'tools/run_tilt_presentation_native.py')},indent=2))
    cmd=[str(exe),'-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-logFile',str(out/'player.log')]
    (out/'command.json').write_text(json.dumps(cmd,indent=2))
    checks=[];states={};failure=None;proc=None;hwnd=None;request_id=0;pngs={};events=[]
    def check(name,ok,detail=None):
        checks.append({'name':name,'passed':bool(ok),'detail':detail})
        if not ok:raise AssertionError(name+': '+repr(detail))
    def state():
        s=wait(lambda:json.loads((out/'state.json').read_text(encoding='utf-8')))
        if s.get('failure'):raise AssertionError(s['failure'])
        return s
    current={'stage':'sealed','transaction':'fixture-A','count':0,'inspect':-1,'enabled':False,'paused':False,'binder':False,'focus':True,'fixedClock':True,'mutate':False,'quit':False,'width':0,'height':0}
    def request(**kw):
        nonlocal request_id
        current.update(kw);request_id+=1;current['id']=request_id
        work=out/'request.writing';work.write_text(json.dumps(current));wait(lambda:work.replace(out/'request.json'))
        s=wait(lambda:(s if (s:=state())['id']==request_id else None))
        time.sleep(.35);return state()
    def move(x,y):
        assert proc is not None
        message(hwnd,0x200,0,(int(y)&65535)<<16|(int(x)&65535));events.append({'message':'WM_MOUSEMOVE','x':int(x),'y':int(y),'pid':proc.pid,'hwnd':hwnd});time.sleep(.75);return state()
    def capture(name):
        request(capture=name+'.png');p=out/(name+'.png');wait(lambda:p.exists());pngs[p.name]=validate_png(p);states[name]=state();return states[name]
    def surface(s,role):
        return next(x for x in s['surfaces'] if role in x['key'])
    def point(s,b,u,v):
        scale=min(s['width']/1600,s['height']/900)
        return (b['x']+u*b['width'])*scale,(b['y']+v*b['height'])*scale
    def exercise(name,role,finish):
        from PIL import Image,ImageChops,ImageStat
        before=state();b=surface(before,role)['bounds'];baseline_commands=before['commands']
        neutral=move(*point(before,b,.5,.5));n=surface(neutral,role)
        check(name+':center-neutral',abs(n['pitchDegrees'])<.12 and abs(n['yawDegrees'])<.12,n)
        capture(name+'-neutral')
        with Image.open(out/(name+'-neutral.png')) as im:reference=im.convert('RGB')
        for direction,u,v,axis,sign in [('left',.08,.5,'yawDegrees',-1),('right',.92,.5,'yawDegrees',1),('up',.5,.08,'pitchDegrees',1),('down',.5,.92,'pitchDegrees',-1)]:
            xy=point(before,b,u,v);s=move(*xy);selected=surface(s,role)
            check(name+':'+direction+':actual-pose',selected['hovered'] and selected[axis]*sign>8 and selected['finish']==finish and sum(x['hovered'] for x in s['surfaces'])==1,selected)
            scale=min(s['width']/1600,s['height']/900)
            check(name+':'+direction+':client-coordinate-parity',abs(s['pointer']['x']-int(xy[0])/scale)<1.5 and abs(s['pointer']['y']-int(xy[1])/scale)<1.5,s['pointer'])
            check(name+':'+direction+':fixed-clock',s['clock']==3,s['clock'])
            capture(name+'-'+direction)
            with Image.open(out/(name+'-'+direction+'.png')) as im:changed=im.convert('RGB')
            crop=(int((b['x']-4)*scale),int((b['y']-4)*scale),int((b['x']+b['width']+4)*scale),int((b['y']+b['height']+4)*scale))
            delta=ImageChops.difference(reference,changed).crop(crop)
            check(name+':'+direction+':actual-framebuffer-change',sum(ImageStat.Stat(delta).mean)>2,ImageStat.Stat(delta).mean)
            check(name+':'+direction+':hover-passive',s['commands']==baseline_commands and s['cues']==0 and s['audioSources']==0)
        s=move(1100*min(before['width']/1600,before['height']/900),810*min(before['width']/1600,before['height']/900))
        check(name+':leave-centers',all(abs(x['pitchDegrees'])<.12 and abs(x['yawDegrees'])<.12 and not x['hovered'] for x in s['surfaces']),s['surfaces'])
        capture(name+'-leave')
        return s
    def frozen_check(name,**kw):
        request(fixedClock=False,capture=None,**kw);frozen=state();s=move(50,50)
        states[name+'-before']=frozen;states[name+'-after']=s
        check(name+':exact-pose-target-clock-context-pointer-freeze',s['frozen'] and all(s[k]==frozen[k] for k in ['clock','controllers','key','pointer']),{'before':frozen,'after':s})
    def click_logical(x,y,expected):
        before=state();scale=min(before['width']/1600,before['height']/900);x=int(x*scale);y=int(y*scale)
        hovered=move(x,y);check('button-hover-not-command:'+expected,hovered['commands']==before['commands'])
        lp=(y&65535)<<16|(x&65535);message(hwnd,0x201,1,lp);time.sleep(.12);message(hwnd,0x202,0,lp)
        events.append({'message':'WM_LBUTTONDOWN/WM_LBUTTONUP','x':x,'y':y,'expected':expected})
        after=wait(lambda:(s if (s:=state())['commands']>before['commands'] else None))
        check('accepted-native-hitbox:'+expected,after['lastCommand']==expected and after['commands']==before['commands']+1,after['lastCommand'])
    try:
        proc=subprocess.Popen(cmd,cwd=exe.parent,env=env,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
        hwnd=wait(lambda:window(proc.pid));s=wait(lambda:state())
        check('actual-supplied-library',s['bindings']==121,s['bindings'])
        check('default-disabled-no-surface-registry',not s['enabled'] and s['surfaces']==[],s)
        request(enabled=True)
        s=move(393,300)
        check('actual-visible-wrapper-registered',len(s['surfaces'])==1,s)
        check('actual-rendered-hover-hint',s['hint'],s)
        b=s['surfaces'][0]['bounds'];scale=min(s['width']/1600,s['height']/900)
        s=move((b['x']+.08*b['width'])*scale,(b['y']+.5*b['height'])*scale)
        check('actual-WM_MOUSEMOVE-drives-left-yaw',s['surfaces'][0]['hovered'] and s['surfaces'][0]['yawDegrees'] < -4,s)
        capture('wrapper-left')
        if a.slice in ('all','bounds'):
            s=request(stage='revealing',count=2,inspect=1)
            check('inspection-conservative-clip-never-overlaps-info-or-Back',len(s['drawBounds'])==1 and s['drawBounds'][0]['x']+s['drawBounds'][0]['width']<=852,s['drawBounds'])
            s=request(inspect=-1)
            check('stack-conservative-clip-clears-unrevealed-label',all(d['y']+d['height']<=b['bounds']['y']+b['bounds']['height']+18 for b,d in zip(s['surfaces'],s['drawBounds']) if '/stack-back:' in b['key']),s['drawBounds'])
            s=request(stage='results',count=11)
            check('results-conservative-clips-clear-footer-labels',all(d['y']+d['height']<749 for d in s['drawBounds']),s['drawBounds'])
            s=request(stage='sealed',count=0)
        if a.slice=='fallback':
            from PIL import Image,ImageChops
            left=state();s=move(*point(left,b,.92,.5));capture('wrapper-fallback-right')
            check('backend-unavailable-still-tracks-real-hover',surface(s,'/wrapper:')['yawDegrees']>8,s)
            with Image.open(out/'wrapper-left.png') as left_image,Image.open(out/'wrapper-fallback-right.png') as right_image:
                check('backend-unavailable-falls-back-to-stable-old-2D-face',ImageChops.difference(left_image.convert('RGB'),right_image.convert('RGB')).getbbox() is None)
        if a.slice in ('all','hint'):
            from PIL import Image,ImageChops
            move(1100,650);request(enabled=False,stage='revealing',count=2)
            capture('hint-disabled')
            request(enabled=True);capture('hint-enabled')
            scale=min(state()['width']/1600,state()['height']/900)
            secondary=(int(1170*scale),int(694*scale),int(1526*scale),int(740*scale))
            with Image.open(out/'hint-disabled.png') as off,Image.open(out/'hint-enabled.png') as on:
                delta=ImageChops.difference(off.convert('RGB').crop(secondary),on.convert('RGB').crop(secondary))
                check('hover-hint-does-not-overpaint-accepted-Skip-button',delta.getbbox() is None,delta.getbbox())
            s=request(stage='sealed',count=0)
        if a.slice=='focus':
            request(fixedClock=False)
            message(hwnd,0x6,1,0);message(hwnd,0x1c,1,0);message(hwnd,0x7,0,0);time.sleep(.5)
            states['focus-active']=state()
            message(hwnd,0x6,0,0);message(hwnd,0x1c,0,0);message(hwnd,0x8,0,0);time.sleep(.5)
            states['focus-inactive']=state()
            check('native-window-messages-focus-gate',state()['frozen'],state())
        if a.slice=='all':
            exercise('wrapper-1280','/wrapper:',2)
            request(stage='revealing',count=1,capture=None)
            s=state()
            check('visible-hero-and-backs-registered',len(s['surfaces'])==5 and any('/hero:0' in x['key'] and x['finish']==0 for x in s['surfaces']),s)
            exercise('nonholo','/hero:0',0)
            b=surface(state(),'/stack-back:0')['bounds'];s=move(*point(state(),b,.5,.5))
            check('stack-overlap-only-topmost-back-hovered',sum(x['hovered'] for x in s['surfaces'])==1 and surface(s,'/stack-back:0')['hovered'],s['surfaces'])
            capture('back-overlap')
            move(1100,650);s=request(count=2)
            check('new-card-context-is-neutral-no-pose-bleed',all(abs(x['pitchDegrees'])<.01 and abs(x['yawDegrees'])<.01 for x in s['surfaces']) and '/2/table' in s['key'],s)
            exercise('holo','/hero:1',1)
            request(inspect=1)
            check('inspection-only-current-surface',len(state()['surfaces'])==1 and '/inspection:1' in state()['surfaces'][0]['key'],state())
            exercise('inspection','/inspection:1',1)
            request(inspect=10)
            check('unrevealed-inspection-exposes-no-surface',state()['surfaces']==[],state())
            request(inspect=-1,count=11)
            check('final-eleventh-card-still-held',len(state()['surfaces'])==1 and '/hero:10' in state()['surfaces'][0]['key'] and state()['view']=='revealing',state())
            request(stage='results')
            check('results-current-set-exactly-eleven',len(state()['surfaces'])==11 and len({x['key'] for x in state()['surfaces']})==11,state())
            for i in [0,1,6,10]:
                s=state();b=surface(s,'/result:'+str(i))['bounds'];s=move(*point(s,b,.8,.3))
                check('results-selection-'+str(i),sum(x['hovered'] for x in s['surfaces'])==1 and surface(s,'/result:'+str(i))['hovered'],s['surfaces'])
            capture('results-hover')
            request(inspect=1)
            s=state();b=surface(s,'/inspection:1')['bounds'];s=move(*point(s,b,.8,.5))
            check('results-modal-priority-no-background-surface',len(s['surfaces'])==1 and s['surfaces'][0]['hovered'],s['surfaces'])
            request(mutate=True)
            check('diagnostic-mutation-does-not-control-renderer',state()['detached'] and all(x['key']!='forged' and abs(x['pitchDegrees'])<=12 for x in state()['surfaces']),state())
            for width,height in [(1600,900),(2560,1080)]:
                request(stage='sealed',count=0,inspect=-1,width=width,height=height,capture=None)
                s=wait(lambda:(s if (s:=state())['width']==width and s['height']==height else None))
                check('actual-resize-'+str(width),s['width']==width and s['height']==height)
                exercise('wrapper-'+str(width),'/wrapper:',2)
            request(width=1280,height=720,stage='revealing',count=2,inspect=-1,overlayOnly=False,binder=False,fixedClock=False)
            s=state();b=surface(s,'/hero:1')['bounds'];s=move(*point(s,b,.1,.4));first_clock=s['clock'];capture('holo-running-left')
            s=move(*point(s,b,.9,.4));capture('holo-running-right')
            check('actual-pointer-and-moving-presentation-clock',s['clock']>first_clock and surface(s,'/hero:1')['yawDegrees']>8,s)
            frozen_check('pause',paused=True)
            request(paused=False)
            frozen_check('Binder',binder=True)
            check('hidden-Binder-has-no-current-visible-surfaces',state()['surfaces']==[],state())
            request(binder=False)
            frozen_check('hidden-overlay',overlayOnly=True)
            request(overlayOnly=False)
            frozen_check('error',stage='error')
            request(stage='revealing')
            message(hwnd,0x6,1,0);message(hwnd,0x1c,1,0);message(hwnd,0x7,0,0);time.sleep(.35)
            before_focus=state();message(hwnd,0x6,0,0);message(hwnd,0x1c,0,0);message(hwnd,0x8,0,0)
            focused=wait(lambda:(s if (s:=state())['frozen'] else None))
            s=move(100,200)
            check('actual-owned-window-focus-loss-freezes',all(s[k]==focused[k] for k in ['clock','controllers','key','pointer']),{'before':focused,'after':s})
            check('focus-preserves-accepted-explicit-pause-request',s['commands']==before_focus['commands']+1,{'before':before_focus['commands'],'after':s['commands']})
            message(hwnd,0x6,1,0);message(hwnd,0x1c,1,0);message(hwnd,0x7,0,0)
            wait(lambda:not state()['frozen'])
            s=request(fixedClock=True)
            baseline_commands=s['commands'];move(1100,650)
            for i in range(12):
                s=request(transaction='fixture-context-'+str(i),count=1 if i%2 else 2)
                check('bounded-context-reset-'+str(i),s['cache']<=5 and all(abs(x['pitchDegrees'])<.01 and abs(x['yawDegrees'])<.01 for x in s['surfaces']),s['cache'])
            check('all-native-hover-passive-after-contexts',s['commands']==baseline_commands and s['cues']==0 and s['audioSources']==0,s)
            s=request(destroyUi=True,mutate=False)
            check('actual-UI-teardown-disposes-material-not-external-textures',s['teardownPassed'] and s['bindings']==121,s)
            request(destroyUi=False)
            request(stage='revealing',count=2,inspect=-1,width=1280,height=720)
            click_logical(410,239,'inspect:1')
            click_logical(830,378,'inspect:0')
            click_logical(1348,717,'skip')
            click_logical(1348,651,'next')
            request(count=11)
            click_logical(1348,651,'results')
            request(stage='results',inspect=1)
            click_logical(1058,725,'close-inspect')
            check('native-UI-requests-never-local-reveal-or-audio',state()['view']=='results' and state()['cues']==0 and state()['audioSources']==0,state())
        else:
            check('hover-no-commands-audio',s['commands']==0 and s['cues']==0 and s['audioSources']==0,s)
        request(quit=True);check('native-exit',proc.wait(timeout=30)==0);proc=None
        if build:
            after={k:digest(ROOT/k) for k in build['sourcePins']}
            (out/'source-after.json').write_text(json.dumps(after,indent=2))
            check('actual-built-source-pins-unchanged-after-input',all(v['source']==after[k] for k,v in build['sourcePins'].items()))
        check('no-fixture-save-authority-or-save-files',not list(out.rglob('*.save')))
    except Exception as e:failure=repr(e)
    finally:
        if proc is not None:
            try:request(quit=True);code=proc.wait(timeout=20)
            except Exception:proc.kill();code=proc.wait(timeout=20)
            (out/'cleanup-exit.json').write_text(json.dumps({'exit':code}))
        assert len(pngs)==len(list(out.glob('*.png')))
        (out/'results.json').write_text(json.dumps({'passed':failure is None,'checkCount':len(checks),'checks':checks,'failure':failure,'states':states,'screenshotCount':len(pngs),'pngs':pngs,'eventCount':len(events),'events':events},indent=2))
    print(json.dumps({'passed':failure is None,'checkCount':len(checks),'failure':failure,'out':str(out)}));return 0 if failure is None else 1
if __name__=='__main__':sys.exit(main())
