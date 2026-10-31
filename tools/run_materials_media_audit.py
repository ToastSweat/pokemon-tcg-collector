"""Reconcile retained materials media reports, RED/GREEN evidence and final source pins."""
import hashlib
import json
from pathlib import Path
import sys
sys.dont_write_bytecode=True
from run_phase3_media import ROOT

def main():
    base=ROOT/'.artifacts/materials-media'
    def report(label,filename='native-results.json',passed=True):
        out=base/label
        data=json.loads((out/filename).read_text(encoding='utf-8'))
        assert data['testCount']>0 and data['testCount']==len(data['checks'])
        assert data['passed']==passed and (all(c['passed'] for c in data['checks']) if passed else any(not c['passed'] for c in data['checks']))
        assert ('EXIT 0' if passed else 'EXIT 1') in (out/'execution.log').read_text(encoding='utf-8')
        assert json.loads((out/'protected-inputs.json').read_text(encoding='utf-8'))['unchanged']
        return data
    pairs=[]
    for red,green,message,kind in [
        ('02-wrapper-red','03-wrapper-green','Add separate IWrapperMetallicMedia','feature'),
        ('04-budget-red','05-budget-green','Exposed full set limit constant','feature'),
        ('06-cache-red','07-cache-green','Verified identical bytes share','feature'),
        ('11-cache-adversarial-mutant-red','12-cache-adversarial-green','Cache hit cannot authorize','isolated-stage mutation')]:
        rd=report(red,passed=False); report(green)
        assert any(message in c.get('error','') for c in rd['checks'] if not c['passed'])
        pairs.append(dict(kind=kind,red=red,green=green,expectedFailure=message))
    suites=[]
    for label,filename in [('16-pure-final','results.json'),('17-native-regression-final','native-results.json'),('15-synthetic-full-set-final','native-results.json')]:
        data=report(label,filename)
        inputs=json.loads((base/label/'compiled-inputs.json').read_text(encoding='utf-8'))
        assert all(hashlib.sha256((ROOT/key).read_bytes()).hexdigest()==digest for key,digest in inputs.items())
        suites.append(dict(label=label,testCount=data['testCount'],passed=True,fullSet=data.get('fullSet',[])))
    media=[ROOT/('Assets/VintageCollector/Media/'+name+'.cs') for name in ['PresentationMediaContract','MediaManifest','PresentationMediaLibrary']]
    pins={f.relative_to(ROOT).as_posix():hashlib.sha256(f.read_bytes()).hexdigest() for f in media}
    native=json.loads((base/'15-synthetic-full-set-final/native-library-pins.json').read_text(encoding='utf-8'))
    index=dict(passed=True,evidence='Actual isolated Unity Editor native decoding/readback/lifecycle; full-set content is explicitly synthetic. Not player or human acceptance.',finalSuites=suites,redGreenPairs=pairs,mediaSourcePins=pins,nativeLibraries=native,actualPreparedFullSet='Not ready at the single discovery check; no waiting or polling. Run the actual-material entrypoint after authoring publishes.',actualFullSetCommand='python -B tools/run_materials_media_native.py --label actual-materials-full-set --full-set',retainedFailedSetupRuns={'01-wrapper-red':'Test fixture used an unavailable Unity property; corrected before proper RED 02.','14-synthetic-full-set-final':'Native 30 passed but strict overall verification failed because Unity added ProjectVersion revision. Fixed exact revision and newline; complete rerun 15 passed.'})
    (base/'evidence-index.json').write_text(json.dumps(index,indent=2),encoding='utf-8')
    print(json.dumps(index))
    return 0

if __name__=='__main__': sys.exit(main())
