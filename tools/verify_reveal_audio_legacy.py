"""Actual partial reveal/restart/failed-save and owned native-click hard-kill recovery.
Uses unchanged Phase2SmokeRunner only after new probe's exact scoped legacy admission.
"""
from pathlib import Path
import argparse,json,os,re,subprocess,sys
sys.dont_write_bytecode=True
from verify_reveal_audio_native import admitted_build
from run_reveal_audio_build import ROOT,CANDIDATE,tree_manifest,source_manifest,preserved_players,sha,no_reparse
from verify_tilt_player import OwnedWindowInput,validate_png,read_eventually
from verify_phase3_player import owned_window

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--label',required=True);parser.add_argument('--build-label',required=True);parser.add_argument('--mode',choices=['smoke','crash'],default='smoke');args=parser.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9_-]+',args.label):parser.error('Unsafe label')
    build,pins,manifest,_=admitted_build(args.build_label)
    out=ROOT/'.artifacts/reveal-audio-legacy'/args.label;no_reparse(out);out.mkdir(parents=True,exist_ok=False);tmp=out/'tmp';tmp.mkdir()
    env=dict(os.environ,TMP=str(tmp),TEMP=str(tmp),TMPDIR=str(tmp),PYTHONDONTWRITEBYTECODE='1')
    protected=preserved_players();(out/'protected-before.json').write_text(json.dumps(protected,indent=2));runners=['tools/verify_reveal_audio_legacy.py','tools/verify_reveal_audio_native.py','tools/verify_tilt_player.py','tools/verify_phase3_player.py','tools/run_reveal_audio_build.py','tools/run_tilt_build.py'];runner_pins={n:sha(ROOT/n) for n in runners};(out/'runner-pins.json').write_text(json.dumps(runner_pins,indent=2))
    checks=[];cases={};images=[];failure=None;process=None;stream=None
    def check(name,ok,detail=''):
        checks.append({'name':name,'passed':bool(ok),'detail':detail})
        if not ok:raise AssertionError(name+': '+str(detail))
    def launch(case,scenario,save):
        folder=out/case;folder.mkdir();(folder/'reveal-audio-legacy-fixture.json').write_text('{"schemaVersion":1,"owner":"reveal-audio-legacy-fixture","case":"'+case+'"}')
        command=[str(CANDIDATE/'VintageCardCollector.exe'),'-screen-width','1280','-screen-height','720','-screen-fullscreen','0','-logFile',str(folder/'player.log'),'--vcc-smoke='+scenario,'--vcc-save-root='+str(out/save),'--vcc-artifacts='+str(folder)]
        (folder/'command.json').write_text(json.dumps(command,indent=2));handle=(folder/'process.log').open('w')
        return subprocess.Popen(command,cwd=CANDIDATE,env=env,stdout=handle,stderr=subprocess.STDOUT),handle,folder
    def finish(process,stream,folder):
        code=process.wait(timeout=120);stream.close();check(folder.name+':exit-zero',code==0,code)
        report=json.loads((folder/'report.json').read_text());check(folder.name+':actual-report-count',report['passed'] and report['checkCount']==len(report['checks'])>0 and all(c['passed'] for c in report['checks']))
        for name in report['screenshots']:
            if Path(name).name!=name or not name.endswith('.png'):raise ValueError('Unsafe image path')
            check(folder.name+':complete-png:'+name,validate_png(folder/name)==(1280,720));images.append({'path':(folder/name).relative_to(ROOT).as_posix(),'sha256':sha(folder/name)})
        cases[folder.name]=report;return report
    def verify_state(save,expected_total,expected_packs):
        raw=(out/save/'state.save').read_bytes();header,digest,payload=raw.split(b'\n',2)
        import hashlib
        check(save+':save-envelope',header==b'VTCG-SAVE-1' and hashlib.sha256(payload).hexdigest().encode()==digest)
        state=json.loads(payload);check(save+':once-only-authoritative-awards',len(state['packs'])==expected_packs and sum(len(p['orderedCardIds']) for p in state['packs'])==expected_total)
        check(save+':mirrored-acknowledged-bytes',raw==(out/save/'state.backup').read_bytes() and not (out/save/'state.pending').exists())
    try:
        if args.mode=='smoke':
            for case in ['prepare','resume','error']:
                process,stream,folder=launch(case,case,'error-session' if case=='error' else 'session');finish(process,stream,folder);process=None;stream=None
            a,b=cases['prepare']['final'],cases['resume']['initial'];keys=['transactionId','cardIds','newToVirtual','revealedCount','revision']
            check('actual-next-process-exact-partial-reveal-history',all(a[k]==b[k] for k in keys) and b['revealedCount']==3 and b['generatedResultCount']==0 and b['totalVirtualCards']==11)
            check('real-failed-save-retry-no-reroll',cases['error']['final']['generatedResultCount']==1 and cases['error']['final']['totalVirtualCards']==11 and cases['error']['final']['revealedCount']==1)
            verify_state('session',22,2);verify_state('error-session',11,1)
        else:
            def click(name,predicate):
                nonlocal state
                check('native-click-needs-real-transition:'+name+':'+str(len(checks)),not predicate(state))
                native.click(hwnd,state,name);state=read_eventually(probe,predicate,20);return state
            process,stream,folder=launch('before-kill','input','crash-session');probe=folder/'probe.json';native=OwnedWindowInput(process);state=read_eventually(probe,lambda s:s['stage']=='ready',45);hwnd=owned_window(process.pid,15)
            if state['paused']:click('resume',lambda s:not s['paused'])
            click('primary',lambda s:s['stage']=='sealed');click('primary',lambda s:s['stage']=='revealing')
            for target in range(1,4):click('primary',lambda s,target=target:s['revealedCount']==target)
            before=state;process.kill();code=process.wait(timeout=10);stream.close();stream=None;process=None;check('owned-player-actually-hard-killed',code!=0,code);cases['before-kill']={'kind':'actual-hard-kill-observation','processExitCode':code,'probe':before}
            process,stream,folder=launch('after-kill','input','crash-session');probe=folder/'probe.json';native=OwnedWindowInput(process);state=read_eventually(probe,lambda s:s['stage']=='revealing' and s['revealedCount']==3,45);hwnd=owned_window(process.pid,15)
            check('hard-kill-recovery-exact-pack-order-flags-revision',all(state[k]==before[k] for k in ['transactionId','cardIds','newToVirtual','revealedCount','revision']) and state['generatedResultCount']==0 and state['totalVirtualCards']==11)
            if state['paused']:click('resume',lambda s:not s['paused'])
            click('primary',lambda s:s['revealedCount']==4);click('secondary',lambda s:s['stage']=='results');check('actual-native-input-continues-without-awarding-again',state['totalVirtualCards']==11 and state['generatedResultCount']==0)
            native.click(hwnd,state,'quit');finish(process,stream,folder);process=None;stream=None;verify_state('crash-session',11,1)
    except Exception as error:failure=repr(error);checks.append({'name':'legacy-verification-failure','passed':False,'detail':failure})
    finally:
        if process is not None and process.poll() is None:process.kill();process.wait(timeout=10)
        if stream is not None:stream.close()
        after=preserved_players();(out/'protected-after.json').write_text(json.dumps(after,indent=2))
        checks.extend([{'name':'all-seven-accepted-players-preserved','passed':after==protected},{'name':'candidate-immutable','passed':tree_manifest(CANDIDATE,{'saves','logs'})==manifest},{'name':'exact-built-source-unchanged','passed':source_manifest()==pins},{'name':'runners-unchanged','passed':all(sha(ROOT/n)==d for n,d in runner_pins.items())}])
        report={'passed':failure is None and all(c['passed'] for c in checks),'checkCount':len(checks),'checks':checks,'cases':cases,'images':images,'imageCount':len(images),'failure':failure,'mode':args.mode}
        (out/'verification.json').write_text(json.dumps(report,indent=2));print(json.dumps({k:report[k] for k in ['passed','checkCount','imageCount','failure']},indent=2))
    return 0 if report['passed'] else 1
if __name__=='__main__':sys.exit(main())
