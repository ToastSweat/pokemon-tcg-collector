"""Reconcile real host RED/GREEN and final native reports without touching live saves."""
import hashlib
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'.artifacts/phase3-host'
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def run(label, passed):
    folder=OUT/label
    data=json.loads((folder/'results.json').read_text(encoding='utf-8'))
    checks=data['checks']
    assert checks and data['checkCount']==len(checks)
    assert data['passed'] is passed
    assert all(c['passed'] for c in checks) if passed else any(not c['passed'] for c in checks)
    exit_code=int((folder/'execution.log').read_text(encoding='utf-8').rsplit('EXIT ',1)[1].strip())
    assert (exit_code==0) is passed
    protected=json.loads((folder/'protected-inputs.json').read_text(encoding='utf-8'))
    assert protected['unchanged'] and protected['before']==protected['after']
    return {'label':label,'passed':passed,'exit':exit_code,'checkCount':len(checks),'protectedFileCount':len(protected['before']),'reportSha256':sha(folder/'results.json'),'unityLogSha256':sha(folder/'unity.log'),'failedChecks':[c['name'] for c in checks if not c['passed']]}
def main():
    pairs=[('01-binding-red-native','01-binding-green'),('02-override-red','02-override-green'),('03-lifecycle-red','03-lifecycle-green-native-callback'),('06-initial-recovery-red','06-initial-recovery-green')]
    evidence={'pairs':[{'red':run(r,False),'green':run(g,True)} for r,g in pairs], 'final':[run('final-native-integration',True),run('final-native-play',True)]}
    for label in ['final-native-integration','final-native-play']:
        stage=json.loads((OUT/label/'staged-inputs.json').read_text(encoding='utf-8'))
        assert stage['exact'] and stage['files']
        for rel,pins in stage['files'].items(): assert pins['source']==pins['staged']==sha(ROOT/rel)
    owned=['Assets/VintageCollector/Runtime/Phase3MediaHost.cs','Assets/VintageCollector/Editor/Phase3MediaHostChecks.cs','TestsPhase3/HostRun.py','TestsPhase3/HostAudit.py','docs/technical/Phase_3_Host.md']
    evidence['sourcePins']={p:sha(ROOT/p) for p in owned if (ROOT/p).exists()}
    evidence['totalFinalAssertions']=sum(v['checkCount'] for v in evidence['final'])
    evidence['scope']='Real Unity 6000.2.14f1 exact-source staged Editor integration and Play Mode; no player or physical-hearing acceptance. Live saves never read/written.'
    (OUT/'evidence-index.json').write_text(json.dumps(evidence,indent=2),encoding='utf-8')
    print(json.dumps({'pairs':len(pairs),'finalAssertions':evidence['totalFinalAssertions'],'protectedFiles':evidence['final'][0]['protectedFileCount'],'exactStagedSources':True,'sourcePins':evidence['sourcePins']},indent=2))
if __name__=='__main__': main()
