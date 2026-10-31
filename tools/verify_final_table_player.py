"""Actual owned-window mouse verification of the eleventh-card preview fix."""
from pathlib import Path
import json
import os
import subprocess
import sys
from verify_phase3_player import owned_window, native_click, post_click, read_json_eventually, png_size

ROOT=Path(__file__).resolve().parents[1]
EXE=ROOT/'Builds/Phase3TableFlowWindows/VintageCardCollector.exe'


def main():
    out=ROOT/'.artifacts/final-table-player'/sys.argv[1]
    if not sys.argv[1].replace('-','').isalnum(): raise ValueError('Unsafe label')
    out.mkdir(parents=True,exist_ok=False); (out/'tmp').mkdir()
    env=dict(os.environ,TMP=str(out/'tmp'),TEMP=str(out/'tmp'),TMPDIR=str(out/'tmp'))
    checks=[]; records={}
    def check(name,condition):
        checks.append({'name':name,'passed':bool(condition)})
        if not condition: raise RuntimeError(name)
    def launch(name,scenario,save):
        folder=out/name; folder.mkdir(); stream=(folder/'process.log').open('w')
        cmd=[str(EXE),'-screen-width','1280','-screen-height','720','-screen-fullscreen','0','-logFile',str(folder/'player.log'),
             '--vcc-smoke='+scenario,'--vcc-save-root='+str(save),'--vcc-artifacts='+str(folder)]
        return subprocess.Popen(cmd,cwd=EXE.parent,env=env,stdout=stream,stderr=subprocess.STDOUT),stream,folder
    def finish(process,stream,folder):
        code=process.wait(timeout=90); stream.close(); check(folder.name+':exit',code==0)
        report=json.loads((folder/'report.json').read_text()); check(folder.name+':report',report['passed'] and report['checkCount']==len(report['checks']))
        for image in report['screenshots']: check(folder.name+':png:'+image,png_size(folder/image)==(1280,720))
        records[folder.name]=report
    failure=None
    try:
        save=out/'session'
        process,stream,folder=launch('reveal-eleven','input',save)
        try:
            path=folder/'probe.json'; mp=folder/'media-probe.json'
            state=read_json_eventually(path,lambda s:s['stage']=='ready',35); window=owned_window(process.pid,15)
            if state['paused']: state=native_click(window,path,state,'resume',lambda s:not s['paused'])
            state=native_click(window,path,state,'primary',lambda s:s['stage']=='sealed')
            state=native_click(window,path,state,'primary',lambda s:s['stage']=='revealing')
            for target in range(1,11):
                state=native_click(window,path,state,'primary',lambda s,target=target:s['revealedCount']==target)
                check('reveal-normal-'+str(target),state['stage']=='revealing' and state['inspectIndex']==-1)
            before=read_json_eventually(mp,lambda m:m['revealedCount']==10,8)
            state=native_click(window,path,state,'primary',lambda s:s['revealedCount']==11 and s['inspectIndex']==-1 and s['stage']=='revealing')
            check('real-mouse-final-next-keeps-normal-table-eleventh',state['stage']=='revealing' and state['cardIds'][10] and state['totalVirtualCards']==11)
            after=read_json_eventually(mp,lambda m:m['revealedCount']==11,8)
            if before['rendererFocused'] and after['rendererFocused']:
                check('final-next-one-audio-cue',after['cues']==before['cues']+1)
            revision=state['revision']
            state=native_click(window,path,state,'hero',lambda s:s['inspectIndex']==10)
            state=native_click(window,path,state,'back',lambda s:s['inspectIndex']==-1 and s['stage']=='revealing')
            check('optional-inspection-back-keeps-table',state['revision']==revision)
            state=native_click(window,path,state,'primary',lambda s:s['stage']=='results')
            check('explicit-results-no-save-award',state['stage']=='results' and state['revision']==revision and state['totalVirtualCards']==11 and state['generatedResultCount']==1)
            post_click(window,state,'quit'); finish(process,stream,folder)
        finally:
            if process.poll() is None: process.kill(); process.wait(timeout=10)
            stream.close()
        process,stream,folder=launch('completed-resume','input',save)
        try:
            path=folder/'probe.json'; state=read_json_eventually(path,lambda s:s['stage']=='results',35)
            check('completed-resume-no-modal-or-reroll',state['inspectIndex']==-1 and state['totalVirtualCards']==11 and state['generatedResultCount']==0)
            window=owned_window(process.pid,15); post_click(window,state,'quit'); finish(process,stream,folder)
        finally:
            if process.poll() is None: process.kill(); process.wait(timeout=10)
            stream.close()
        process,stream,folder=launch('skip','layout',out/'skip-session')
        try:
            finish(process,stream,folder)
            check('skip-direct-results-remains-compatible',records['skip']['final']['stage']=='results' and records['skip']['final']['inspectIndex']==-1)
        finally:
            if process.poll() is None: process.kill(); process.wait(timeout=10)
            stream.close()
    except Exception as error:
        failure=repr(error); checks.append({'name':'verification-failure','passed':False,'detail':failure})
    result={'passed':failure is None and all(c['passed'] for c in checks),'checkCount':len(checks),'checks':checks,'cases':records}
    (out/'verification.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({'passed':result['passed'],'checkCount':len(checks),'failure':failure,'report':str(out/'verification.json')},indent=2))
    return 0 if result['passed'] else 1


if __name__=='__main__': sys.exit(main())
