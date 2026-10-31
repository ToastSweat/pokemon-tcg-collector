"""Graphics-enabled native assignable-audio playback plus separate-process reload. No foreground/global input."""
from pathlib import Path
import argparse
import json
import os
import re
import subprocess
import sys
sys.dont_write_bytecode=True
from run_reveal_audio_build import ROOT,CANDIDATE,tree_manifest,sha,no_reparse,source_manifest,preserved_players
from verify_tilt_player import validate_png,OwnedWindowInput
from verify_phase3_player import owned_window

def validate_playback(record):
    import math
    if not record['expectedClip'] or record['actualClip']!=record['expectedClip']:raise ValueError('Native expected/actual clip mismatch')
    if not record['childSourcePlaying'] or record['samples']<=0 or record['frequency']<=0:raise ValueError('Native child source was not playing imported samples')
    if not all(math.isfinite(record[k]) and record[k]>.00001 for k in ['sourcePeak','listenerPeak']):raise ValueError('Native source/listener signal was silent or nonfinite')

def validate_audio_report(report,clip_names):
    if not report.get('passed') or report.get('checkCount')!=len(report.get('checks',[])) or not report['checkCount'] or not all(c['passed'] for c in report['checks']):raise ValueError('Native report pass/count mismatch')
    names=[c['name'] for c in report['checks']]
    if len(set(names))!=len(names):raise ValueError('Duplicate native check names')
    records=report.get('playback',[])
    for record in records:validate_playback(record)
    required={'startup-zero-generations','startup-child-sources-silent','fourteen-distinct-imported-profile-clips','rare-position-eight','actual-provider-bound'}
    if report['mode']=='audio':
        required.update({'all-three-real-rare-branches','failed-save-no-cue','retry-not-replayed','muted-reveal-silent-consumed','unmute-does-not-replay','pause-stops-audio','paused-next-does-not-reveal-or-cue','resume-no-replay','binder-silent','binder-back-no-replay','duplicate-skip-no-replay','once-only-eleven-awards-per-real-generated-pack'})
        if set(report['rareBranches'])!={'non-pokemon','pokemon','holo'}:raise ValueError('All three actual rare branches required')
        if not set(clip_names)<=set(r['actualClip'] for r in records):raise ValueError('All fourteen imported assigned clips must actually play, including position-eight rare-missing fallback')
        positions={int(m[1]) for r in records if (m:=re.match(r'position:(\d+):',r['name']))}
        if positions!=set(range(1,12)):raise ValueError('All eleven actual reveal positions required')
        if report['totalVirtualCards']!=report['generatedResultCount']*11:raise ValueError('Once-only award total mismatch')
    else:
        required.add('reload-no-generation-no-cue')
        if report['generatedResultCount'] or records:raise ValueError('Reload playback/generation forbidden')
    if not required<=set(names):raise ValueError('Required native checks missing: '+repr(sorted(required-set(names))))
    return report

def admitted_build(label):
    if not re.fullmatch(r'[A-Za-z0-9_-]+',label):raise ValueError('Unsafe build label')
    folder=ROOT/'.artifacts/reveal-audio-build'/label
    report=json.loads((folder/'results.json').read_text())
    if not report.get('passed') or not report.get('built'):raise ValueError('Successful exact-source native reveal-audio build required')
    manifest=tree_manifest(CANDIDATE,{'saves','logs'})
    if manifest!=report['candidateManifest']:raise ValueError('Candidate immutable membership/bytes changed')
    pins=json.loads((folder/'source-pins.json').read_text())
    if source_manifest()!=pins:raise ValueError('Current source differs from built source')
    if any(sha(ROOT/n)!=d for n,d in report['toolPins'].items()):raise ValueError('Build runner/helper changed')
    profile=json.loads((folder/'audio-profile.json').read_text())
    if not profile['passed'] or profile['clipCount']!=len(profile['clips']) or profile['clipCount']!=14:raise ValueError('Native imported profile report count mismatch')
    if len(profile['clips'])!=14 or len({c['name'] for c in profile['clips']})!=14:raise ValueError('Fourteen actual imported clips required')
    return report,pins,manifest,[c['name'] for c in profile['clips']]

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--label',required=True);parser.add_argument('--build-label',required=True);args=parser.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9_-]+',args.label):parser.error('Unsafe label')
    build,pins,manifest,clip_names=admitted_build(args.build_label)
    out=ROOT/'.artifacts/reveal-audio-player'/args.label;no_reparse(out);out.mkdir(parents=True,exist_ok=False);tmp=out/'tmp';tmp.mkdir()
    protected=preserved_players();(out/'protected-before.json').write_text(json.dumps(protected,indent=2))
    runners=['tools/verify_reveal_audio_native.py','tools/run_reveal_audio_build.py','tools/run_tilt_build.py','tools/verify_tilt_player.py']
    runner_pins={n:sha(ROOT/n) for n in runners};(out/'runner-pins.json').write_text(json.dumps(runner_pins,indent=2))
    env=dict(os.environ,TMP=str(tmp),TEMP=str(tmp),TMPDIR=str(tmp),PYTHONDONTWRITEBYTECODE='1')
    checks=[];cases={};images=[];failure=None;process=None;stream=None
    def check(name,ok):
        checks.append({'name':name,'passed':bool(ok)})
        if not ok:raise AssertionError(name)
    try:
        for mode in ['audio','reopen']:
            folder=out/mode;folder.mkdir();(folder/'reveal-audio-fixture.json').write_text('{"schemaVersion":1,"owner":"reveal-audio-player-fixture","mode":"'+mode+'"}')
            command=[str(CANDIDATE/'VintageCardCollector.exe'),'-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-logFile',str(folder/'player.log'),'--vcc-smoke=input','--vcc-save-root='+str(out/'session'),'--vcc-artifacts='+str(folder)]
            (folder/'command.json').write_text(json.dumps(command,indent=2));stream=(folder/'process.log').open('w')
            process=subprocess.Popen(command,cwd=CANDIDATE,env=env,stdout=stream,stderr=subprocess.STDOUT)
            hwnd=owned_window(process.pid,20);native=OwnedWindowInput(process)
            for message,wparam in [(0x6,1),(0x1c,1),(0x7,0)]:native.message(hwnd,message,wparam,0)
            (folder/'owned-window.json').write_text(json.dumps({'pid':process.pid,'hwnd':int(hwnd),'foregroundInput':False,'globalCursor':False},indent=2))
            (folder/'audio-driver-ready.json').write_text('{"owner":"reveal-audio-owned-window","pid":'+str(process.pid)+'}')
            code=process.wait(timeout=300);stream.close();stream=None;process=None;check(mode+':real-player-exit-zero',code==0)
            report=validate_audio_report(json.loads((folder/'audio-report.json').read_text()),clip_names);cases[mode]=report
            for name in report['screenshots']:
                if Path(name).name!=name or not name.endswith('.png'):raise ValueError('Unsafe native screenshot name')
                check(mode+':complete-framebuffer:'+name,validate_png(folder/name)==(1600,900));images.append({'path':(folder/name).relative_to(ROOT).as_posix(),'sha256':sha(folder/name)})
        check('independent-process-exact-history-order-flags-revision-and-awards',cases['audio']['finalSession']==cases['reopen']['initialSession']==cases['reopen']['finalSession'])
        check('independent-process-zero-generation',cases['reopen']['generatedResultCount']==0)
    except Exception as error:failure=repr(error);checks.append({'name':'native-verification-failure','passed':False,'detail':failure})
    finally:
        if process is not None and process.poll() is None:process.kill();process.wait(timeout=10)
        if stream is not None:stream.close()
        after=preserved_players();(out/'protected-after.json').write_text(json.dumps(after,indent=2))
        for name,ok in [('candidate-immutable',tree_manifest(CANDIDATE,{'saves','logs'})==manifest),('all-seven-accepted-players-preserved',after==protected),('source-exact-built-inputs',source_manifest()==pins),('runner-inputs-unchanged',all(sha(ROOT/n)==d for n,d in runner_pins.items()))]:checks.append({'name':name,'passed':ok})
        report={'passed':failure is None and all(c['passed'] for c in checks),'checkCount':len(checks),'checks':checks,'failure':failure,'cases':cases,'images':images,'imageCount':len(images),'humanHearingAcceptance':False}
        (out/'verification.json').write_text(json.dumps(report,indent=2));print(json.dumps({k:report[k] for k in ['passed','checkCount','imageCount','failure']},indent=2))
    return 0 if report['passed'] else 1
if __name__=='__main__':sys.exit(main())
