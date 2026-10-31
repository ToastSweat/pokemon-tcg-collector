"""Reconcile retained material-render evidence; no accepted-build or live-save traversal."""
import hashlib,json,re,sys
from pathlib import Path
sys.dont_write_bytecode=True
from run_materials_render_native import ROOT,validate_png
OUT=ROOT/'.artifacts/materials-render'
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def report(label):
    p=OUT/label/'results.json';d=json.loads(p.read_text(encoding='utf-8'))
    assert d['checkCount']==len(d['checks'])
    return p,d
def exit_code(label,graphics=False):
    p=OUT/label/('player-execution.log' if graphics else 'execution.log')
    return int(re.findall(r'EXIT (-?\d+)',p.read_text(encoding='utf-8'))[-1])
def main():
    labels=['10-final-host-restore','12-corrected-oracle-green','10-final-legacy-native','10-final-legacy-phase2','10-final-legacy-host','10-final-legacy-host-play','11-final-legacy-graphics']
    result={'scope':'Isolated native renderer/host proof only; not candidate or human acceptance','suites':[],'redGreen':[],'pngs':{}}
    for label in labels:
        p,d=report(label);graphics=d.get('screenshotCount',0)>0
        assert d['passed'] and d['checkCount']>0 and exit_code(label,graphics)==0
        for c in d['checks']:
            assert c.get('passed',True) if isinstance(c,dict) else not c.startswith('FAIL ')
        pins=json.loads((p.parent/'source-pins.json').read_text(encoding='utf-8'))
        assert pins and all(v['source']==v['staged']==digest(ROOT/k) for k,v in pins.items())
        supplied=json.loads((p.parent/'supplied-pins.json').read_text(encoding='utf-8'))
        assert supplied and all(v['source']==v['staged']==digest(ROOT/k) for k,v in supplied.items())
        result['suites'].append({'label':label,'checkCount':d['checkCount'],'reportSha256':digest(p),'sourcePinsSha256':digest(p.parent/'source-pins.json'),'suppliedPinsSha256':digest(p.parent/'supplied-pins.json')})
        pngs={f.relative_to(OUT).as_posix():validate_png(f) for f in p.parent.rglob('*.png')}
        if graphics:assert len(pngs)==d['screenshotCount']==len(d['screenshots'])
        result['pngs'].update(pngs)
    pure=ROOT/'.artifacts/materials-render-project/.artifacts/pure/08-final-pure/results.json'
    d=json.loads(pure.read_text(encoding='utf-8'));assert d['passed'] and d['checkCount']==len(d['checks'])>0
    result['suites'].append({'label':'08-final-pure','checkCount':d['checkCount'],'reportSha256':digest(pure)})
    pairs=[('01-host-red','01-host-green','Trusted serialized manifest field exists',False),('02-wrapper-red','02-wrapper-green-corrected-oracle','visible masked movement pixels=0',True),('03-host-selection-red','03-host-selection-green-profile','Trusted configured manifest rather than old empty manifest selected',False),('04-host-budget-red','04-host-budget-green','full=True count=2',False)]
    for red,green,needle,graphics in pairs:
        rp,rd=report(red);gp,gd=report(green)
        assert not rd['passed'] and needle in rd['failure'] and exit_code(red,graphics)!=0
        assert gd['passed'] and gd['checkCount']>0 and exit_code(green,graphics)==0
        result['redGreen'].append({'red':red,'green':green,'redExit':exit_code(red,graphics),'greenExit':exit_code(green,graphics),'expectedFailure':needle,'redReportSha256':digest(rp),'greenReportSha256':digest(gp),'redSourcePinsSha256':digest(rp.parent/'source-pins.json'),'greenSourcePinsSha256':digest(gp.parent/'source-pins.json')})
    red='12-corrected-oracle-defect-red';green='12-corrected-oracle-green'
    rp,rd=report(red);gp,gd=report(green)
    assert not rd['passed'] and 'visible masked movement pixels=0' in rd['failure'] and exit_code(red,True)==1
    assert gd['passed'] and exit_code(green,True)==0
    defect=json.loads((rp.parent/'defect-injection.json').read_text(encoding='utf-8'))
    mutant_pins=json.loads((rp.parent/'source-pins.json').read_text(encoding='utf-8'))
    assert [k for k,v in mutant_pins.items() if v['source']!=v['staged']]==[defect['target']]
    assert all(v['source']==digest(ROOT/k) for k,v in mutant_pins.items())
    result['correctedOracleDefectControl']={'red':red,'green':green,'redExit':1,'greenExit':0,'redReportSha256':digest(rp),'greenReportSha256':digest(gp),'stagedDefect':defect}
    manifest=ROOT/'Assets/Supplied/AssetManifest.json'
    old_hash=json.loads((OUT/'01-host-red/protected-manifest.json').read_text(encoding='utf-8'))['sha256']
    assert digest(manifest)==old_hash and json.loads(manifest.read_text(encoding='utf-8'))['assets']==[]
    actual=ROOT/'Assets/Supplied/BaseSetMaterials/Manifest.json'
    assert digest(actual)=='7fef4c77cb34195b4195715fa99c6c01c400ee31bf73f4a5ede963d86eee93be'
    result['oldManifestSha256']=old_hash;result['actualManifestSha256']=digest(actual)
    result['actualBindingCount']=len(json.loads(actual.read_text(encoding='utf-8'))['assets'])
    result['totalFinalChecks']=sum(d['checkCount'] for d in result['suites']);result['pngCount']=len(result['pngs'])
    result['productionPins']={p:digest(ROOT/p) for p in ['Assets/VintageCollector/Runtime/Phase3MediaHost.cs','Assets/VintageCollector/Presentation/TabletopPresentation.cs','Assets/VintageCollector/Resources/Presentation/WrapperMetallic.shader','Assets/VintageCollector/Resources/Presentation/Phase3Foil.shader','Assets/VintageCollector/Media/PresentationMediaLibrary.cs']}
    (OUT/'evidence-index.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k!='pngs'},indent=2))
if __name__=='__main__':main()
