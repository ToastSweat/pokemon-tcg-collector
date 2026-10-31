"""Real materials candidate services/restart or owned-window input verification. No foreground cursor/input."""
from pathlib import Path
import argparse
import ctypes
from ctypes import wintypes
import hashlib
import json
import os
import re
import struct
import subprocess
import sys
import time
import zlib
ROOT=Path(__file__).resolve().parents[1]
EXE=ROOT/'Builds/Phase3MaterialsWindows/VintageCardCollector.exe'

def validate_png(path):
    raw=path.read_bytes()
    if raw[:8]!=b'\x89PNG\r\n\x1a\n':raise ValueError('PNG signature')
    at=8;dimensions=None;compressed=[];ended=False;channels=0
    while at+12<=len(raw):
        length=struct.unpack('>I',raw[at:at+4])[0];kind=raw[at+4:at+8]
        if at+12+length>len(raw):raise ValueError('Truncated PNG')
        data=raw[at+8:at+8+length];crc=struct.unpack('>I',raw[at+8+length:at+12+length])[0]
        if zlib.crc32(kind+data)&0xffffffff!=crc:raise ValueError('PNG CRC')
        if kind==b'IHDR':
            if dimensions is not None or at!=8 or length!=13:raise ValueError('PNG IHDR')
            dimensions=struct.unpack('>II',data[:8])
            if min(dimensions)<1:raise ValueError('PNG dimensions')
            depth,color,compression,filter_method,interlace=struct.unpack('>BBBBB',data[8:])
            if depth!=8 or color not in (2,6) or compression or filter_method or interlace:raise ValueError('PNG expected noninterlaced RGB/RGBA8')
            channels=3 if color==2 else 4
        if kind==b'IDAT':compressed.append(data)
        at+=12+length
        if kind==b'IEND':
            if length or at!=len(raw):raise ValueError('PNG trailing bytes or IEND size')
            ended=True;break
    if not ended or dimensions is None or not compressed:raise ValueError('PNG incomplete')
    try:decoded=zlib.decompress(b''.join(compressed))
    except zlib.error as error:raise ValueError('PNG compressed pixels') from error
    stride=1+dimensions[0]*channels
    if len(decoded)!=dimensions[1]*stride or any(decoded[y*stride]>4 for y in range(dimensions[1])):raise ValueError('PNG scanline size/filter')
    return dimensions

def validate_binder_controls(state):
    """Independent source-pinned safe DTO/page contract. Empty means wait, never fabricate hitboxes."""
    view=state.get('binder')
    if not view or not view['open']:return True
    controls=state['controls']
    if not controls:return False
    if state.get('controlsSource')!='binder-repaint' or not state.get('controlsViewJson') or json.loads(state['controlsViewJson'])!=view:raise ValueError('Controls not tied to current observed binder DTO')
    rows=sorted((r for r in view['rows'] if r and r['id']),key=lambda r:(int(r['collectorNumber']) if r['collectorNumber'].isdigit() else 2147483647,r['id']))
    page_count=max(1,(len(rows)+11)//12);page=max(0,min(view['page'],page_count-1))
    selected=next((r for r in rows if r['id']==view['selectedCardId']),None)
    review=bool((view.get('importSummary') or '').strip())
    conditions=['unknown','near-mint','lightly-played','moderately-played','heavily-played','damaged']
    expected={'mode:virtual','mode:physical','binder-search','page:'+str(page-1),'page:'+str(page+1),'backup','import-preview','binder-close','quit'}
    expected.update('filter:'+f for f in ['all','owned','missing','duplicates','holo'])
    expected.update('select:'+r['id'] for r in rows[page*12:(page+1)*12])
    expected.update('backup-select:'+str(i) for i in range(len(view['backups'])))
    if selected and view['mode']=='physical':expected.update(['binder-quantity','save-physical']+['condition:'+c for c in conditions])
    if review:expected.update(['import-confirm','import-cancel'])
    names=[c['name'] for c in controls]
    if len(set(names))!=len(names) or set(names)!=expected:raise ValueError('Ghost/missing current-render controls: '+repr(sorted(set(names)^expected)))
    def allowed(name):
        if name in ('binder-close','quit'):return True
        if view.get('busy') or view['restartRequired']:return False
        if view['pendingPhysicalEdit']:return name=='save-physical'
        if view['pendingImportConfirm']:return name=='import-confirm' and review
        if name=='import-cancel':return review
        if name=='import-confirm':return review and not view['error']
        if review:return False
        if name.startswith('page:'):return 0<=int(name[5:])<page_count
        if name=='backup' or name=='save-physical':return not view['error']
        if name=='import-preview':return 0<=view['selectedBackupIndex']<len(view['backups']) and bool(view['backups'][view['selectedBackupIndex']])
        return True
    if any(c['enabled']!=allowed(c['name']) for c in controls):raise ValueError('Control enabled state disagrees with safe current DTO')
    if any(c['width']<=0 or c['height']<=0 for c in controls):raise ValueError('Invalid observed bounds')
    return True

def read_eventually(path,predicate,seconds=15):
    deadline=time.monotonic()+seconds;last=None
    while time.monotonic()<deadline:
        try:
            last=json.loads(path.read_text(encoding='utf-8'))
            if predicate(last):return last
        except (OSError,ValueError):pass
        time.sleep(.07)
    raise TimeoutError('No acknowledged state at '+str(path)+'; last='+repr(last))

def message(hwnd,message,wparam,lparam):
    user32=ctypes.WinDLL('user32',use_last_error=True);user32.PostMessageW.argtypes=[wintypes.HWND,wintypes.UINT,wintypes.WPARAM,wintypes.LPARAM];user32.PostMessageW.restype=wintypes.BOOL
    if not user32.PostMessageW(hwnd,message,wparam,lparam):raise ctypes.WinError(ctypes.get_last_error())
    time.sleep(.08)

def key(hwnd,code):
    message(hwnd,0x100,code,1);message(hwnd,0x101,code,0xc0000001)

def actual_oracle():
    from PIL import Image
    from run_materials_build import no_reparse,sha
    manifest=ROOT/'Assets/Supplied/BaseSetMaterials/Manifest.json';no_reparse(manifest)
    data=json.loads(manifest.read_text(encoding='utf-8'));bindings=[]
    if len(data['assets'])!=121:raise ValueError('Actual manifest must contain 121 bindings')
    for entry in data['assets']:
        relative=entry['relativePath']
        if not relative.startswith('Assets/Supplied/BaseSetMaterials/') or '\\' in relative or any(p in ('','.','..') for p in relative.split('/')):raise ValueError('Unsafe actual image path')
        path=ROOT/relative;no_reparse(path)
        if sha(path)!=entry['sha256']:raise ValueError('Actual image hash mismatch')
        with Image.open(path) as image:
            image.load();rgba=image.convert('RGBA');width,height=rgba.size
            bottom_up=rgba.transpose(Image.Transpose.FLIP_TOP_BOTTOM).tobytes()
        bindings.append({'role':entry['role'],'cardId':entry.get('cardId') or '', 'width':width,'height':height,'rgbaSha256':hashlib.sha256(bottom_up).hexdigest()})
    return {'synthetic':False,'manifestSha256':sha(manifest),'bindings':bindings}

def regression_report(folder, mode):
    # Demo is solely the explicitly opted-in MaterialsSmokeProbe. Its complete
    # native report remains mandatory below; never fabricate a legacy report.
    return None if mode=='demo' else json.loads((folder/'report.json').read_text())

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--label',required=True);parser.add_argument('--build-label',required=True);parser.add_argument('--mode',choices=['services','input','demo'],default='services');args=parser.parse_args()
    for label in [args.label,args.build_label]:
        if not re.fullmatch(r'[A-Za-z0-9_-]+',label):parser.error('Unsafe label')
    # Imports existing read-only helpers only; their main methods never execute.
    sys.path.insert(0,str(ROOT/'tools'))
    from run_materials_build import tree_manifest,no_reparse,source_manifest,preserved_players
    from verify_phase3_player import owned_window,post_click
    build=json.loads((ROOT/'.artifacts/materials-build'/args.build_label/'results.json').read_text())
    if not build.get('passed') or not build.get('built'):parser.error('A successful admitted real native materials build is required')
    before=tree_manifest(EXE.parent,{'saves','logs'})
    if before!=build['candidateManifest']:parser.error('Candidate membership/bytes differ from pinned build')
    pins=json.loads((ROOT/'.artifacts/materials-build'/args.build_label/'source-pins.json').read_text())
    if source_manifest()!=pins:parser.error('Current source differs from built source pins')
    out=ROOT/'.artifacts/phase4-player'/args.label;no_reparse(out);out.mkdir(parents=True,exist_ok=False)
    tmp=out/'tmp';tmp.mkdir();env=dict(os.environ,TMP=str(tmp),TEMP=str(tmp),TMPDIR=str(tmp),PYTHONDONTWRITEBYTECODE='1')
    checks=[];cases={};material_cases={};images=[];material_images=[];failure=None;process=None;stream=None;acknowledgements=[]
    runner_before={name:hashlib.sha256((ROOT/name).read_bytes()).hexdigest() for name in ['tools/verify_materials_player.py','tools/verify_phase4_player.py','tools/verify_phase3_player.py','tools/run_materials_build.py']}
    protected_before=preserved_players();(out/'protected-before.json').write_text(json.dumps(protected_before,indent=2))
    (out/'runner-pins.json').write_text(json.dumps(runner_before,indent=2))
    def check(name,ok,detail=''):
        checks.append({'name':name,'passed':bool(ok),'detail':detail})
        if not ok:raise AssertionError(name+': '+detail)
    oracle=actual_oracle()
    (out/'actual-image-oracle.json').write_text(json.dumps(oracle,indent=2))
    def launch(mode):
        folder=out/mode;folder.mkdir();(folder/'phase4-fixture.json').write_text('{"schemaVersion":1,"owner":"phase4-player-fixture","mode":"'+mode+'"}')
        (folder/'materials-fixture.json').write_text('{"schemaVersion":1,"owner":"materials-player-fixture","mode":"'+('demo' if args.mode=='demo' else 'observe')+'"}')
        (folder/'materials-oracle.json').write_text(json.dumps(oracle,separators=(',',':')))
        command=[str(EXE),'-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-logFile',str(folder/'player.log'),'--vcc-smoke=input','--vcc-save-root='+str(out/'session'),'--vcc-artifacts='+str(folder)]
        (folder/'command.json').write_text(json.dumps(command,indent=2));stream=(folder/'process.log').open('w')
        return subprocess.Popen(command,cwd=EXE.parent,env=env,stdout=stream,stderr=subprocess.STDOUT),stream,folder
    def finish(process,stream,folder):
        code=process.wait(timeout=120);stream.close();check(folder.name+':native-process-exit',code==0,str(code))
        report=regression_report(folder,args.mode)
        if report is not None:check(folder.name+':count-matched-real-report',report['passed'] and report['checkCount']==len(report['checks'])>0 and all(c['passed'] for c in report['checks']))
        for image in report['screenshots'] if report is not None else []:
            if Path(image).name!=image or not image.endswith('.png'):raise ValueError('Invalid screenshot member')
            check(folder.name+':png:'+image,validate_png(folder/image)==(1600,900));images.append(str((folder/image).relative_to(ROOT)))
        materials=json.loads((folder/'materials-report.json').read_text())
        check(folder.name+':actual-material-native-report',materials['passed'] and not materials['synthetic'] and materials['checkCount']==len(materials['checks'])>0 and all(c['passed'] for c in materials['checks']))
        check(folder.name+':full-quality-no-fallback',materials['suppliedBindings']==121 and materials['uniqueTextures']==107 and materials['texturePixels']==51479000 and materials['manifestSha256']==oracle['manifestSha256'])
        for image in materials['images']:
            if Path(image).name!=image or not image.endswith('.png'):raise ValueError('Unsafe material image member')
            expected=(600,825) if image=='materials-shader-holo.png' else (260,475) if image.startswith('materials-shader-wrapper-') else (1600,900)
            check(folder.name+':material-png:'+image,validate_png(folder/image)==expected);material_images.append((folder/image).relative_to(ROOT).as_posix())
        material_cases[folder.name]=materials
        if report is not None:cases[folder.name]=report
        return report
    def filepins(directory):
        return {name:hashlib.sha256((directory/name).read_bytes()).hexdigest() for name in ['state.save','state.backup','state.anchor','physical.save','physical.backup','physical.anchor'] if (directory/name).is_file()}
    try:
        check('candidate-native-dependencies',EXE.is_file() and (EXE.parent/'UnityPlayer.dll').is_file() and (EXE.parent/'VintageCardCollector_Data').is_dir() and (EXE.parent/'MonoBleedingEdge').is_dir())
        if args.mode=='demo':
            process,stream,folder=launch('input');finish(process,stream,folder);process=None
        elif args.mode=='services':
            process,stream,folder=launch('services');first=finish(process,stream,folder);process=None
            old=filepins(out/'session');check('default-profile-six-authoritative-files',len(old)==6)
            process,stream,folder=launch('reopen');second=finish(process,stream,folder);process=None
            assert first is not None and second is not None, 'Regression modes require their real gameplay reports'
            check('actual-next-process-identical-session-history-order-flags-revision',first['final']['sessionJson']==second['initial']['sessionJson'] and second['initial']['generatedResultCount']==0)
            profile=second['initial']['profileId'];check('new-immutable-profile-selected',bool(re.fullmatch(r'[a-f0-9]{32}',profile)))
            check('restored-exact-original-authoritative-bytes',old==filepins(out/'session'/'Profiles'/profile))
            check('old-profile-bytes-unchanged-after-restart',old==filepins(out/'session'))
            check('physical-revision-and-buckets-reloaded',first['final']['physicalRevision']==second['initial']['physicalRevision']==2)
        else:
            process,stream,folder=launch('input');probe=folder/'probe.json'
            state=read_eventually(probe,lambda s:s['ready'] and s['stage']=='ready',45);hwnd=owned_window(process.pid,15)
            def acknowledge(name,predicate):
                state=read_eventually(probe,lambda s:predicate(s) and validate_binder_controls(s),18);check('current-render-safe-controls:'+name,validate_binder_controls(state));check('native-ack:'+name,bool(state['capture']) and validate_png(folder/state['capture'])==(1600,900))
                acknowledgements.append({'action':name,'state':state,'pngSha256':hashlib.sha256((folder/state['capture']).read_bytes()).hexdigest()})
                (out/'native-acknowledgements.json').write_text(json.dumps(acknowledgements,indent=2));return state
            def click(control,predicate):
                nonlocal state
                check('native-action-needs-state-transition:'+control,not predicate(state))
                check('current-render-before-click:'+control,validate_binder_controls(state))
                bounds=next((b for b in state['controls'] if b['name']==control and b.get('enabled',True)),None)
                check('native-reachable:'+control,bounds is not None and 0<=bounds['y']<state['screenHeight'] and bounds['y']+bounds['height']<=state['screenHeight']+1)
                post_click(hwnd,state,control);state=acknowledge(control,predicate);return state
            def ensure(control,predicate):
                # Setup already in its desired state is NOT an input test or action acknowledgement.
                return state if predicate(state) else click(control,predicate)
            def quantity(value):
                nonlocal state
                if state['binder']['quantityText']==value:return
                check('current-quantity-field-registered',any(c['name']=='binder-quantity' and c['enabled'] for c in state['controls']))
                post_click(hwnd,state,'binder-quantity');key(hwnd,0x23)
                for _ in range(len(state['binder']['quantityText'])+2):key(hwnd,8)
                for char in value:message(hwnd,0x102,ord(char),1)
                state=acknowledge('quantity-keyboard:'+value,lambda s:s['binder']['quantityText']==value)
            def arm_fault(value):
                nonlocal state
                (folder/'arm-fault.txt').write_text(value)
                state=acknowledge('fault-armed:'+value,lambda s:s['fixtureAck']==value)
            check('native-startup-no-generation',state['generatedResultCount']==0 and state['totalVirtualCards']==0 and state['mediaBound'])
            if state['paused']:click('resume',lambda s:not s['paused'])
            click('binder',lambda s:s['binder']['open']);ensure('mode:physical',lambda s:s['binder']['mode']=='physical')
            click('page:1',lambda s:s['binder']['page']==1);click('page:0',lambda s:s['binder']['page']==0)
            selected=next(r['id'] for r in state['binder']['rows'] if r['id']!=state['binder']['selectedCardId']);click('select:'+selected,lambda s:s['binder']['selectedCardId']==selected)
            ensure('condition:near-mint',lambda s:s['binder']['selectedCondition']=='near-mint');quantity('2')
            arm_fault('physical');click('save-physical',lambda s:s['binder']['pendingPhysicalEdit'] and bool(s['binder']['error']))
            check('native-pending-save-retry-enabled',any(c['name']=='save-physical' and c['enabled'] for c in state['controls']))
            click('binder-close',lambda s:not s['binder']['open']);click('binder',lambda s:s['binder']['open'])
            check('native-pending-save-survives-back',state['binder']['pendingPhysicalEdit'] and state['generatedResultCount']==0)
            click('save-physical',lambda s:not s['binder']['pendingPhysicalEdit'] and s['physicalRevision']==1 and s['binder']['totalQuantity']==2)
            click('condition:lightly-played',lambda s:s['binder']['selectedCondition']=='lightly-played');quantity('1')
            click('save-physical',lambda s:s['physicalRevision']==2 and s['binder']['totalQuantity']==3)
            check('native-manual-before-virtual-pulls',state['generatedResultCount']==0 and state['totalVirtualCards']==0 and state['binder']['uniqueOwned']==1)
            click('binder-close',lambda s:not s['binder']['open']);click('primary',lambda s:s['stage']=='sealed')
            click('binder',lambda s:s['binder']['open']);ensure('mode:virtual',lambda s:s['binder']['mode']=='virtual')
            check('native-sealed-binder-no-spoilers',state['binder']['totalQuantity']==0 and all(r['quantity']==0 for r in state['binder']['rows']))
            click('binder-close',lambda s:not s['binder']['open']);click('primary',lambda s:s['stage']=='revealing')
            for target in range(1,4):click('primary',lambda s,target=target:s['revealedCount']==target)
            click('binder',lambda s:s['binder']['open']);check('native-three-reveals-only',state['binder']['totalQuantity']==3)
            click('binder-close',lambda s:not s['binder']['open'])
            for target in range(4,12):click('primary',lambda s,target=target:s['revealedCount']==target)
            revision=state['revision'];history=state['sessionJson'];check('native-normal-card11-hold',state['stage']=='revealing' and state['inspectIndex']==-1)
            click('binder',lambda s:s['binder']['open']);click('binder-close',lambda s:not s['binder']['open'])
            check('native-binder-back-keeps-normal-final-table',state['stage']=='revealing' and state['revision']==revision and state['sessionJson']==history)
            click('primary',lambda s:s['stage']=='results')
            check('native-results-missing-label-cumulative-observation-plus-current-projection','PHYSICAL MISSING' in state['cumulativeRenderedLabels'] and 'PHYSICAL MISSING' in state['physicalLabels'])
            # Edit a genuinely revealed printing through the real physical GUI to render BOTH badge types.
            ids=json.loads(history)['packs'][0]['orderedCardIds'];owned_id=next(i for i in ids if i!=selected)
            click('binder',lambda s:s['binder']['open']);ensure('mode:physical',lambda s:s['binder']['mode']=='physical')
            row=next(r for r in state['binder']['rows'] if r['id']==owned_id)
            check('native-search-current-registered-and-change-needed',any(c['name']=='binder-search' and c['enabled'] for c in state['controls']) and state['binder']['search']!=row['name'])
            post_click(hwnd,state,'binder-search')
            for char in row['name']:message(hwnd,0x102,ord(char),1)
            state=acknowledge('search-revealed-printing',lambda s:s['binder']['search']==row['name'])
            click('select:'+owned_id,lambda s:s['binder']['selectedCardId']==owned_id)
            ensure('condition:near-mint',lambda s:s['binder']['selectedCondition']=='near-mint');quantity('1')
            click('save-physical',lambda s:s['physicalRevision']==3)
            click('binder-close',lambda s:not s['binder']['open'])
            check('native-results-both-physical-labels-cumulative-observation-plus-current-projection',all(t in state['cumulativeRenderedLabels'] and t in state['physicalLabels'] for t in ['PHYSICAL OWNED','PHYSICAL MISSING']))
            for index in range(11):
                click('result:'+str(index),lambda s,index=index:s['inspectIndex']==index)
                click('back',lambda s:s['inspectIndex']==-1 and s['stage']=='results')
            check('native-results-inspect-hitboxes-preserve-history',state['sessionJson']==history and state['revision']==revision)
            click('binder',lambda s:s['binder']['open']);click('backup',lambda s:len(s['binder']['backups'])==1)
            click('condition:damaged',lambda s:s['binder']['selectedCondition']=='damaged');quantity('7')
            click('backup-select:0',lambda s:s['binder']['selectedBackupIndex']==0);old=filepins(out/'session')
            click('import-preview',lambda s:bool(s['binder']['importSummary']))
            check('native-preview-retained-draft-and-authoritative-bytes',state['binder']['quantityText']=='7' and state['binder']['selectedCondition']=='damaged' and old==filepins(out/'session'))
            key(hwnd,0x1b);state=acknowledge('escape-cancel',lambda s:not s['binder']['importSummary'] and s['binder']['open'])
            check('native-cancel-keeps-draft',state['binder']['quantityText']=='7')
            click('import-preview',lambda s:bool(s['binder']['importSummary']));click('import-cancel',lambda s:not s['binder']['importSummary'])
            click('import-preview',lambda s:bool(s['binder']['importSummary']));arm_fault('import')
            click('import-confirm',lambda s:s['binder']['pendingImportConfirm'] and bool(s['binder']['error']))
            check('native-pending-import-retry-enabled-cancel-disabled',any(c['name']=='import-confirm' and c['enabled'] for c in state['controls']) and not any(c['name']=='import-cancel' and c['enabled'] for c in state['controls']))
            click('binder-close',lambda s:not s['binder']['open']);click('binder',lambda s:s['binder']['open'])
            click('import-confirm',lambda s:s['binder']['restartRequired'] and not s['binder']['pendingImportConfirm'])
            check('native-confirm-old-profile-preserved',old==filepins(out/'session'))
            check('native-quit-current-registered',validate_binder_controls(state) and any(c['name']=='quit' and c['enabled'] for c in state['controls']))
            post_click(hwnd,state,'quit');first=finish(process,stream,folder);process=None
            process,stream,folder=launch('reopen');second=finish(process,stream,folder);process=None
            assert first is not None and second is not None, 'Regression modes require their real gameplay reports'
            profile=second['initial']['profileId']
            check('native-input-next-process-selects-reviewed-clone',bool(re.fullmatch(r'[a-f0-9]{32}',profile)) and second['initial']['generatedResultCount']==0 and first['final']['sessionJson']==second['initial']['sessionJson'])
            check('native-input-clone-exact-old-profile-preserved',old==filepins(out/'session')==filepins(out/'session'/'Profiles'/profile))
            check('native-input-physical-reload',second['initial']['physicalRevision']==3)
    except Exception as error:
        failure=repr(error);checks.append({'name':'verification-failure','passed':False,'detail':failure})
    finally:
        if process is not None and process.poll() is None:process.kill();process.wait(timeout=10)
        if stream is not None:stream.close()
        after=tree_manifest(EXE.parent,{'saves','logs'});checks.append({'name':'candidate-immutable-membership-and-bytes-unchanged','passed':before==after})
        protected_after=preserved_players();(out/'protected-after.json').write_text(json.dumps(protected_after,indent=2))
        checks.append({'name':'older-accepted-players-immutable-membership-bytes-unchanged','passed':protected_before==protected_after})
        checks.append({'name':'current-source-still-exact-built-pins','passed':source_manifest()==pins})
        checks.append({'name':'verification-runner-inputs-unchanged','passed':all(hashlib.sha256((ROOT/name).read_bytes()).hexdigest()==digest for name,digest in runner_before.items())})
        report={'passed':failure is None and all(c['passed'] for c in checks),'checkCount':len(checks),'checks':checks,'cases':cases,'materialCases':material_cases,'materialImageCount':len(material_images),'materialImages':material_images,'screenshotCount':len(images),'screenshots':images,'failure':failure}
        (out/'verification.json').write_text(json.dumps(report,indent=2));print(json.dumps({k:report[k] for k in ['passed','checkCount','screenshotCount','failure']},indent=2))
    return 0 if report['passed'] else 1
if __name__=='__main__':sys.exit(main())
