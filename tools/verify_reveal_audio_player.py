"""Assignable-audio candidate: inherited complete materials/collections gameplay regression, owned saves only.

New candidate/evidence admission; read-only imports reuse proven per-message HWND guards and PNG/oracle logic.
The regression algorithm is an owned adaptation of tilt's services/input driver; old tools are never edited/run.
"""
from pathlib import Path
import argparse, hashlib, json, os, re, subprocess, sys
sys.dont_write_bytecode=True
from verify_tilt_player import OwnedWindowInput, read_eventually, validate_png, validate_binder_controls, actual_oracle, regression_report
ROOT=Path(__file__).resolve().parents[1]
EXE=ROOT/'Builds/Phase3RevealAudioWindows/VintageCardCollector.exe'

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--label',required=True);parser.add_argument('--build-label',required=True);parser.add_argument('--mode',choices=['services','input','demo'],default='services');parser.add_argument('--size',choices=['1280x720','1600x900','2560x1080'],default='1600x900');args=parser.parse_args()
    for label in [args.label,args.build_label]:
        if not re.fullmatch(r'[A-Za-z0-9_-]+',label):parser.error('Unsafe label')
    # Imports existing read-only helpers only; their main methods never execute.
    sys.path.insert(0,str(ROOT/'tools'))
    from run_reveal_audio_build import tree_manifest,no_reparse,source_manifest,preserved_players
    from verify_phase3_player import owned_window
    buildfile=ROOT/'.artifacts/reveal-audio-build'/args.build_label/'results.json'
    if not buildfile.is_file():parser.error('A successful admitted real native reveal-audio build is required')
    build=json.loads(buildfile.read_text())
    if not build.get('passed') or not build.get('built'):parser.error('A successful admitted real native reveal-audio build is required')
    before=tree_manifest(EXE.parent,{'saves','logs'})
    if before!=build['candidateManifest']:parser.error('Candidate membership/bytes differ from pinned build')
    pins=json.loads((ROOT/'.artifacts/reveal-audio-build'/args.build_label/'source-pins.json').read_text())
    if source_manifest()!=pins:parser.error('Current source differs from built source pins')
    out=ROOT/'.artifacts/phase4-player'/args.label;no_reparse(out);out.mkdir(parents=True,exist_ok=False)
    tmp=out/'tmp';tmp.mkdir();env=dict(os.environ,TMP=str(tmp),TEMP=str(tmp),TMPDIR=str(tmp),PYTHONDONTWRITEBYTECODE='1')
    checks=[];cases={};material_cases={};images=[];material_images=[];failure=None;process=None;stream=None;acknowledgements=[]
    runner_before={name:hashlib.sha256((ROOT/name).read_bytes()).hexdigest() for name in ['tools/verify_reveal_audio_player.py','tools/verify_tilt_player.py','tools/verify_phase4_player.py','tools/verify_phase3_player.py','tools/run_reveal_audio_build.py','tools/run_tilt_build.py']}
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
            process,stream,folder=launch('input');probe=folder/'probe.json';native=OwnedWindowInput(process)
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
                native.click(hwnd,state,control);state=acknowledge(control,predicate);return state
            def ensure(control,predicate):
                # Setup already in its desired state is NOT an input test or action acknowledgement.
                return state if predicate(state) else click(control,predicate)
            def quantity(value):
                nonlocal state
                if state['binder']['quantityText']==value:return
                check('current-quantity-field-registered',any(c['name']=='binder-quantity' and c['enabled'] for c in state['controls']))
                native.click(hwnd,state,'binder-quantity');native.key(hwnd,0x23)
                for _ in range(len(state['binder']['quantityText'])+2):native.key(hwnd,8)
                for char in value:native.message(hwnd,0x102,ord(char),1)
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
            native.click(hwnd,state,'binder-search')
            for char in row['name']:native.message(hwnd,0x102,ord(char),1)
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
            native.key(hwnd,0x1b);state=acknowledge('escape-cancel',lambda s:not s['binder']['importSummary'] and s['binder']['open'])
            check('native-cancel-keeps-draft',state['binder']['quantityText']=='7')
            click('import-preview',lambda s:bool(s['binder']['importSummary']));click('import-cancel',lambda s:not s['binder']['importSummary'])
            click('import-preview',lambda s:bool(s['binder']['importSummary']));arm_fault('import')
            click('import-confirm',lambda s:s['binder']['pendingImportConfirm'] and bool(s['binder']['error']))
            check('native-pending-import-retry-enabled-cancel-disabled',any(c['name']=='import-confirm' and c['enabled'] for c in state['controls']) and not any(c['name']=='import-cancel' and c['enabled'] for c in state['controls']))
            click('binder-close',lambda s:not s['binder']['open']);click('binder',lambda s:s['binder']['open'])
            click('import-confirm',lambda s:s['binder']['restartRequired'] and not s['binder']['pendingImportConfirm'])
            check('native-confirm-old-profile-preserved',old==filepins(out/'session'))
            check('native-quit-current-registered',validate_binder_controls(state) and any(c['name']=='quit' and c['enabled'] for c in state['controls']))
            native.click(hwnd,state,'quit');first=finish(process,stream,folder);process=None
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