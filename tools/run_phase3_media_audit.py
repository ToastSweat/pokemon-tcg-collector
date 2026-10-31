"""Audit retained media TDD reports and final source pins; no Unity or main-project writes."""
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'.artifacts/phase3-media'
PAIRS=['01-foley','02-wav','03-path-hash','04-png','05-manifest','06-empty-native','07-manifest-fallback','08-bindings','09-duplicate-mask','10-dispose','11-schema-exact','12-total-pixels','14-partial-types','16-local-root','18-root-first']

def report(label):
    folder=OUT/label
    path=folder/'results.json'
    if not path.exists(): path=folder/'native-results.json'
    value=json.loads(path.read_text(encoding='utf-8'))
    assert value['testCount']>0 and value['testCount']==len(value['checks']), str(path)+' count mismatch'
    assert value['passed']==all(c['passed'] for c in value['checks']), str(path)+' passed mismatch'
    protected=json.loads((folder/'protected-inputs.json').read_text(encoding='utf-8'))
    assert protected['unchanged'] and protected['before']==protected['after'],str(path)+' protected mutations'
    log=(folder/'execution.log').read_text(encoding='utf-8')
    assert ('EXIT 0' if value['passed'] else 'EXIT 1') in log,str(path)+' exit disagreement'
    return {'label':label,'report':str(path.relative_to(ROOT)),'testCount':value['testCount'],'passed':value['passed'],'failedChecks':[c['name'] for c in value['checks'] if not c['passed']],'protectedFileCount':len(protected['before'])}

def main():
    pairs=[]
    for name in PAIRS:
        red=report(name+'-red'); green=report(name+'-green')
        assert not red['passed'] and red['failedChecks'] and green['passed'],name+' lacks real RED/GREEN'
        pairs.append({'slice':name,'red':red,'green':green})
    finals=[]
    for label in ['19-pure-final','19-native-final']:
        data=report(label)
        assert data['passed']
        pins=json.loads((OUT/label/'compiled-inputs.json').read_text(encoding='utf-8'))
        assert pins and all(hashlib.sha256((ROOT/f).read_bytes()).hexdigest()==digest for f,digest in pins.items()),label+' stale source evidence'
        data['currentSourcePinsMatch']=True
        finals.append(data)
    result={'passed':True,'pairCount':len(pairs),'pairs':pairs,'finalSuiteCount':len(finals),'finalSuites':finals,'boundary':'Pure tests and isolated native Unity Editor checks, not core-connected player acceptance or physical hearing. No tracked work closed.'}
    (OUT/'evidence-index.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'passed':True,'pairCount':len(pairs),'finalSuites':finals,'index':str((OUT/'evidence-index.json').relative_to(ROOT))}))

if __name__=='__main__': main()
