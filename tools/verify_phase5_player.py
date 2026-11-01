"""Real Phase5 services/restart, independent owned-HWND input, and hard-kill verification.
No execution without an actual exact-source native build receipt. Never runs older candidates.
"""
from pathlib import Path
import argparse,json,os,re,subprocess,sys,time
sys.dont_write_bytecode=True
from run_phase5_build_policy import ROOT,APPROVED,CANDIDATE,absolute,no_reparse,sha,compiler_manifest,player_manifest,preserved_players,shared_base_pins,originals_manifest
from phase5_owned_window_input import OwnedWindowInput
from verify_tilt_player import read_eventually,validate_png,validate_binder_controls
from verify_phase3_player import owned_window
EXE=CANDIDATE/'VintageCardCollector.exe'

def reconcile_count(report,count,key):
 if type(report.get(count)) is not int or report[count]!=len(report.get(key,[])):raise ValueError('Declared '+count+' differs from actual '+key)

def validate_report(report,mode):
 if report.get('passed') is not True or report.get('mode')!=mode:raise ValueError('Actual native report pass/mode mismatch')
 for count,key in [('checkCount','checks'),('snapshotCount','snapshots'),('seedCount','seeds'),('screenshotCount','screenshots'),('wrapperCount','wrappers')]:reconcile_count(report,count,key)
 checks=report['checks']
 if not checks or any(c.get('passed') is not True for c in checks) or len({c['name'] for c in checks})!=len(checks):raise ValueError('Native check pass/nonzero/unique-name mismatch')
 if report['snapshotCount']!=2 or report['snapshots']!=[report['initial'],report['final']]:raise ValueError('Initial/final snapshot reconciliation failed')
 initial,final=report['initial'],report['final'];before=json.loads(initial['sessionJson']);after=json.loads(final['sessionJson'])
 if initial['generatedResultCount']!=0:raise ValueError('Startup generation forbidden')
 new=after['packs'][len(before['packs']):]
 if final['generatedResultCount']!=len(new) or report['seeds']!=[p['seed'] for p in new] or any(type(v) is not int or not 0<v<=0xffffffff for v in report['seeds']):raise ValueError('Process generations/seeds/history disagree')
 if final['totalVirtualCards']!=len(after['packs'])*11 or initial['mediaBound'] is not True or (mode!='input' and final['mediaBound'] is not True):raise ValueError('Award total or stable media mismatch')
 if mode.endswith('reopen') or mode.startswith('after-'):
  if initial['sessionJson']!=final['sessionJson'] or new:raise ValueError('Reload changed/regenerated history')
 if mode=='services':
  names={c['name'] for c in checks}
  if len(new)!=9 or not {'global-backup','global-review','uncertain-import-retained','restart-required','global-history-no-reroll'}<=names:raise ValueError('Complete three-set service proof missing')
  for printing in ('base-en-unlimited1999','jungle-en-unlimited1999','fossil-en-unlimited1999'):
   subset=[p for p in new if p['printingId']==printing]
   if len(subset)!=3 or {wrapper_variant(p['transactionId']) for p in subset}!={0,1,2}:raise ValueError('All deterministic actual transaction wrapper variants required')
 return report

def validate_audio(report,clip_names):
 from verify_reveal_audio_native import validate_playback
 records=report.get('playback',[])
 if not records or len(clip_names)!=14 or len(set(clip_names))!=14:raise ValueError('Fourteen actual assigned clips and native observations required')
 for row in records:validate_playback(row)
 if set(report.get('rareBranches',[]))!={'non-pokemon','pokemon','holo'}:raise ValueError('All three actual rare classifications required')
 if not set(clip_names)<=set(r['actualClip'] for r in records):raise ValueError('All fourteen clips must actually play, not only appear in a profile')
 for printing in ('base-en-unlimited1999','jungle-en-unlimited1999','fossil-en-unlimited1999'):
  positions={int(m[1]) for r in records if r['printingId']==printing and (m:=re.match(r'position:(\d+):seed:',r['name']))}
  if positions!=set(range(1,12)):raise ValueError('Every set must actually play all eleven positions')
 required={'all-three-rare-categories','skip-no-music-catchup','focus-return-no-replay','unmute-no-replay','pause-stops-audio','resume-no-catchup','binder-back-no-catchup','audio-media-reference-restored'}
 if not required<={c['name'] for c in report['checks']}:raise ValueError('Native audio suppression/lifecycle checks incomplete')
 return report

def validate_phase5_binder_controls(state):
 view=state.get('binder')
 if not view or not view.get('open'):return True
 extras=[c for c in state['controls'] if c['name'].startswith('binder-set:')];legacy=[c for c in state['controls'] if not c['name'].startswith('binder-set:')]
 validate_binder_controls(dict(state,controls=legacy))
 ids={'base-en-unlimited1999','jungle-en-unlimited1999','fossil-en-unlimited1999'}
 # SetChoiceView is an in-memory projection, not a JsonUtility transport. Use the
 # complete actual repaint controls as the set witness; do not invent a serialized field.
 if {c['name'] for c in extras}!={'binder-set:'+s for s in ids} or len(extras)!=3:raise ValueError('Complete current Phase5 binder selector controls required')
 if view.get('supportedSets') is not None and {s['printingId'] for s in view['supportedSets']}!=ids:raise ValueError('Serialized binder choices disagree with canonical controls')
 enabled=view.get('canSelectSet') is True and not any(view.get(k) for k in ('busy','restartRequired','pendingPhysicalEdit','pendingImportConfirm','error','importSummary'))
 if any(c['enabled'] is not enabled or c['width']<=0 or c['height']<=0 for c in extras):raise ValueError('Binder selector command/capability witness mismatch')
 return True

def validate_selector_targets(receipt,source_pins,size):
 key='Assets/VintageCollector/Presentation/SetSelectorPresentation.cs'
 if type(receipt)!=dict or set(receipt)!={'schemaVersion','owner','sourcePath','sourceSha256','width','height','targets'} or type(receipt['schemaVersion']) is not int or receipt['schemaVersion']!=1 or receipt['owner']!='phase5-reviewed-selector-layout' or receipt['sourcePath']!=key or source_pins.get(key)!=receipt['sourceSha256'] or not source_pins.get(key) or (receipt['width'],receipt['height'])!=size:raise ValueError('Reviewed exact-built-source selector layout receipt required')
 entries=receipt['targets'];ids={'base-en-unlimited1999','jungle-en-unlimited1999','fossil-en-unlimited1999'}
 if type(entries)!=list or len(entries)!=3 or {e.get('printingId') for e in entries}!=ids:raise ValueError('Exactly three canonical source-pinned selector targets required')
 controls=[]
 for e in entries:
  if set(e)!={'printingId','x','y','width','height'} or any(type(e[k]) is not int for k in ('x','y','width','height')) or min(e['x'],e['y'])<0 or min(e['width'],e['height'])<=0 or e['x']+e['width']>size[0] or e['y']+e['height']>size[1]:raise ValueError('Selector target outside actual current client')
  controls.append(dict(name='set:'+e['printingId'],enabled=True,**{k:e[k] for k in ('x','y','width','height')}))
 return controls

def wrapper_variant(tx):
 # Independent oracle, not runtime selection authority. The player invokes the frozen production helper.
 h=2166136261
 for b in tx.encode('utf-8'):h=((h^b)*16777619)&0xffffffff
 return h%3

def admitted_build(label):
 if not re.fullmatch('[A-Za-z0-9_-]+',label):raise ValueError('Unsafe build label')
 path=ROOT/'.artifacts/phase5-build'/label/'results.json';no_reparse(path);b=json.loads(path.read_text(encoding='utf-8'))
 if b.get('passed') is not True or b.get('built') is not True or b.get('action')!='BuildWindows':raise ValueError('Actual successful native Phase5 build required')
 if not EXE.is_file() or player_manifest(CANDIDATE)!=b['candidateManifest'] or len(b['candidateManifest'])!=b['candidateMemberCount']:raise ValueError('Complete immutable candidate member count/bytes mismatch')
 if compiler_manifest()!=b['sourcePins']:raise ValueError('Current Root differs from actually built source')
 if preserved_players()!=b['protectedPlayers'] or shared_base_pins()!=b['sharedBasePins'] or originals_manifest()!=b['originals']:raise ValueError('Protected old players/Base/original assets changed')
 if any(sha(ROOT/n)!=d for n,d in b['toolPins'].items()):raise ValueError('Built tooling changed')
 return b

def wait_png(path,size,seconds=10):
 deadline=time.monotonic()+seconds;last=None
 while time.monotonic()<deadline:
  try:
   no_reparse(path)
   if validate_png(path)==size:return sha(path)
  except (ValueError,OSError) as e:last=e
  time.sleep(.05)
 raise TimeoutError('Complete IEND/CRC/dimensions PNG deadline: '+str(last))

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--label',required=True);p.add_argument('--build-label',required=True);p.add_argument('--mode',choices=['services','input','crash','audio'],default='services');p.add_argument('--size',choices=['1280x720','1600x900'],default='1600x900');p.add_argument('--selector-targets',type=Path);a=p.parse_args()
 if not re.fullmatch('[A-Za-z0-9_-]+',a.label):p.error('Unsafe label')
 if absolute(ROOT)!=absolute(APPROVED):p.error('Exact canonical Root only')
 build=admitted_build(a.build_label);size=tuple(map(int,a.size.split('x')));selector_controls=[];selector_receipt=None
 if a.mode=='input' and a.selector_targets is not None:
  path=absolute(a.selector_targets)
  if not path.is_relative_to(ROOT/'.artifacts') or path.stat().st_size>8192:p.error('Bounded project-local selector receipt required')
  selector_receipt=json.loads(path.read_text(encoding='utf-8'));selector_controls=validate_selector_targets(selector_receipt,build['sourcePins'],size)
 out=ROOT/'.artifacts/phase5-player'/a.label;no_reparse(out);out.mkdir(parents=True,exist_ok=False);tmp=out/'tmp';tmp.mkdir()
 env=dict(os.environ,TMP=str(tmp),TEMP=str(tmp),TMPDIR=str(tmp),PYTHONDONTWRITEBYTECODE='1')
 runners=('tools/verify_phase5_player.py','tools/phase5_owned_window_input.py','tools/verify_tilt_player.py','tools/verify_phase3_player.py','tools/run_phase5_build.py','tools/run_phase5_build_policy.py');toolpins={n:sha(ROOT/n) for n in runners}
 checks=[];cases={};processes=[];images=[];failure=None;process=None;stream=None;external=[]
 def save(name,value):(out/name).write_text(json.dumps(value,indent=2),encoding='utf-8')
 save('runner-pins.json',toolpins)
 def check(name,ok,detail=''):
  checks.append({'name':name,'passed':bool(ok),'detail':detail})
  if not ok:raise AssertionError(name+': '+detail)
 def launch(mode):
  nonlocal process,stream
  folder=out/mode;folder.mkdir();(folder/'phase5-fixture.json').write_text('{"schemaVersion":1,"owner":"phase5-player-fixture","mode":"'+mode+'"}',encoding='utf-8')
  cmd=[str(EXE),'-screen-width',str(size[0]),'-screen-height',str(size[1]),'-screen-fullscreen','0','-logFile',str(folder/'player.log'),'--vcc-smoke=input','--vcc-save-root='+str(out/'session'),'--vcc-artifacts='+str(folder)]
  (folder/'command.json').write_text(json.dumps(cmd,indent=2));stream=(folder/'process.log').open('w',encoding='utf-8');process=subprocess.Popen(cmd,cwd=CANDIDATE,env=env,stdout=stream,stderr=subprocess.STDOUT);entry={'pid':process.pid,'mode':mode,'command':cmd,'exitCode':None,'killed':False};processes.append(entry)
  hwnd=owned_window(process.pid,25);native=OwnedWindowInput(process)
  for message,wparam in ((0x6,1),(0x1c,1),(0x7,0)):native.message(hwnd,message,wparam,0)
  (folder/'phase5-driver-ready.json').write_text('{"owner":"phase5-owned-window","pid":'+str(process.pid)+'}',encoding='utf-8')
  return folder,native,hwnd,entry
 def finish(folder,entry):
  nonlocal process,stream
  code=process.wait(timeout=360);entry['exitCode']=code;stream.close();stream=None;process=None;check(folder.name+':actual-exit-zero',code==0,str(code));r=validate_report(json.loads((folder/'report.json').read_text(encoding='utf-8')),folder.name);cases[folder.name]=r
  for n in r['screenshots']:
   if Path(n).name!=n or not n.endswith('.png'):raise ValueError('Unsafe screenshot member')
   images.append({'path':(folder/n).relative_to(ROOT).as_posix(),'sha256':wait_png(folder/n,size)})
  return r
 def state(folder,predicate):return read_eventually(folder/'probe.json',predicate,30)
 def click(folder,native,hwnd,current,control,predicate):
  check('external-needed:'+str(len(external))+':'+control,not predicate(current));check('external-current-dimensions:'+str(len(external)),(current['screenWidth'],current['screenHeight'])==size)
  if current['controlsSource']=='binder-repaint':validate_phase5_binder_controls(current)
  native.click(hwnd,current,control);nextstate=state(folder,predicate);capture=nextstate['capture'];check('external-safe-capture:'+str(len(external)),Path(capture).name==capture and capture.endswith('.png'));pin=wait_png(folder/capture,size);external.append({'control':control,'before':current,'after':nextstate,'artifactDirectory':folder.relative_to(ROOT).as_posix(),'pngPath':(folder/capture).relative_to(ROOT).as_posix(),'pngSha256':pin,'inputKind':'owned-HWND-messages','physicalHumanInput':False});save('external-input.json',external);return nextstate
 try:
  if a.mode=='services':
   f,n,h,e=launch('services');first=finish(f,e);f,n,h,e=launch('reopen');second=finish(f,e);check('independent-process-global-history',first['final']['sessionJson']==second['initial']['sessionJson']);check('independent-process-zero-generation',second['final']['generatedResultCount']==0)
  elif a.mode=='audio':
   f,n,h,e=launch('audio');first=finish(f,e);clips=[m['name'] for m in build['nativeChecks']['media'] if m['kind']=='clip-set-0'];validate_audio(first,clips);f,n,h,e=launch('audio-reopen');second=finish(f,e);check('audio-independent-process-exact-history',first['final']['sessionJson']==second['initial']['sessionJson']);check('audio-reopen-no-generation-or-playback',second['final']['generatedResultCount']==0 and not second['playback'])
  elif a.mode=='input':
   f,n,h,e=launch('input');s=state(f,lambda s:s['ready'] and s['stage']=='ready');check('external-startup-no-generation',s['generatedResultCount']==0)
   if s['paused']:s=click(f,n,h,s,'resume',lambda s:not s['paused'])
   for printing in ('jungle-en-unlimited1999','fossil-en-unlimited1999','base-en-unlimited1999'):
    history=s['sessionJson'];generated=s['generatedResultCount'];prior_active=s['activePrintingId']
    if selector_controls:pinned=dict(s,controls=s['controls']+selector_controls,controlsSource='independently-reviewed-source-pinned-selector-layout')
    else:
     s=state(f,lambda s:s.get('selectorControlsObserved') is True);pinned=s
     check('external-real-current-selector-repaint:'+printing,all(c.get('source')=='selector-repaint' for c in s['controls'] if c['name'].startswith('set:')))
    s=click(f,n,h,pinned,'set:'+printing,lambda s,p=printing:s['selectedPrintingId']==p)
    check('external-selector-no-generation:'+printing,s['sessionJson']==history and s['generatedResultCount']==generated and s['activePrintingId']==prior_active)
    s=click(f,n,h,s,'primary',lambda s:s['stage']=='sealed');check('external-selected-set-generated:'+printing,s['activePrintingId']==printing);s=click(f,n,h,s,'primary',lambda s:s['stage']=='revealing')
    for target in range(1,12):s=click(f,n,h,s,'primary',lambda s,t=target:s['revealedCount']==t)
    check('external-normal-eleven-hold:'+printing,s['stage']=='revealing');s=click(f,n,h,s,'primary',lambda s:s['stage']=='results')
   s=click(f,n,h,s,'result:0',lambda s:s['inspectIndex']==0);s=click(f,n,h,s,'back',lambda s:s['inspectIndex']==-1);s=click(f,n,h,s,'binder',lambda s:s['binder']['open']);s=click(f,n,h,s,'binder-close',lambda s:not s['binder']['open'])
   # Use the source-pinned footer Quit while unpaused; the probe's footer bounds are not paused-dialog geometry.
   check('external-footer-quit-unpaused',not s['paused']);n.click(h,s,'quit');finish(f,e)
  else:
   for mode,after,target in [('sealed','after-sealed',0),('mid','after-mid',3)]:
    f,n,h,e=launch(mode);s=state(f,lambda s:s['ready'])
    if s['paused']:s=click(f,n,h,s,'resume',lambda s:not s['paused'])
    if mode=='sealed':s=click(f,n,h,s,'primary',lambda s:s['stage']=='sealed')
    else:
     check('resumed-sealed-before-mid',s['stage']=='sealed');s=click(f,n,h,s,'primary',lambda s:s['stage']=='revealing')
     for t in range(1,4):s=click(f,n,h,s,'primary',lambda s,t=t:s['revealedCount']==t)
    check(mode+':acknowledged-before-hard-kill',s['revealedCount']==target and s['stage']==('sealed' if target==0 else 'revealing'));save(mode+'-observed-before-kill.json',s)
    process.kill();e['killed']=True;e['exitCode']=process.wait(timeout=15);stream.close();stream=None;process=None
    check(mode+':no-fabricated-normal-exit-report',not (f/'report.json').exists())
    f,n,h,e=launch(after);r=finish(f,e);check(after+':exact-acknowledged-history',s['sessionJson']==r['initial']['sessionJson']);check(after+':same-wrapper-index',s['wrapperIndex']==r['initial']['wrapperIndex'])
 except Exception as e:failure=repr(e);checks.append({'name':'verification-failure','passed':False,'detail':failure})
 finally:
  if process is not None:
   if process.poll() is None:process.kill()
   processes[-1]['exitCode']=process.wait(timeout=15);processes[-1]['cleanup']=True
  if stream is not None:stream.close()
  for name,ok in [('candidate-immutable',player_manifest(CANDIDATE)==build['candidateManifest']),('eight-old-players-preserved',preserved_players()==build['protectedPlayers']),('originals-preserved',originals_manifest()==build['originals']),('shared-Base-preserved',shared_base_pins()==build['sharedBasePins']),('current-source-exact',compiler_manifest()==build['sourcePins']),('tooling-unchanged',all(sha(ROOT/n)==d for n,d in toolpins.items()))]:checks.append({'name':name,'passed':ok})
  result={'passed':failure is None and all(c['passed'] for c in checks),'checkCount':len(checks),'checks':checks,'failure':failure,'cases':cases,'caseCount':len(cases),'processes':processes,'processCount':len(processes),'images':images,'imageCount':len(images),'externalInput':external,'externalInputCount':len(external),'selectorReceipt':selector_receipt,'humanAcceptance':False};save('verification.json',result)
 print(json.dumps({k:result[k] for k in ('passed','checkCount','processCount','imageCount','failure')},indent=2));return 0 if result['passed'] else 1
if __name__=='__main__':sys.exit(main())
