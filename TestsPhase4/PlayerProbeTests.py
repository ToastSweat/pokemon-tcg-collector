"""Compile exact probe observer/publication methods under native Mono, not a model of the fix."""
from pathlib import Path
import hashlib,json,os,re,subprocess,sys
ROOT=Path(__file__).resolve().parents[1]
MONO=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge')
def main():
    label=sys.argv[1]
    if not re.fullmatch('[A-Za-z0-9_-]+',label):raise ValueError('Unsafe label')
    out=ROOT/'.artifacts/phase4-player-tools'/label;out.mkdir(parents=True,exist_ok=False)
    tmp=out/'tmp';tmp.mkdir();env=dict(os.environ,TMP=str(tmp),TEMP=str(tmp),TMPDIR=str(tmp),PYTHONDONTWRITEBYTECODE='1')
    probe=ROOT/'Assets/VintageCollector/Runtime/Phase4SmokeProbe.cs';template=ROOT/'TestsPhase4/PlayerProbeTests.cs';text=probe.read_text()
    observer=text[text.index('private void ObserveControl('):text.index('private Probe Snapshot(')]
    fields=text[text.index('private readonly Dictionary<string,Control> binderControls='):text.index('private readonly string[] authoritative=')]
    # The pre-fix branch is extracted verbatim too: RED executes the defective real publication statement.
    if 'CurrentBinderControls()' in text:
        publication='return CurrentBinderControls();'
    else:
        publication='var controls=new List<Control>();'+text[text.index('if(host!=null && host.CurrentBinderView!=null && host.CurrentBinderView.open)'):text.index('\n            else {',text.index('private Probe Snapshot('))]+'return controls.ToArray();'
    bounded=''
    if 'private static string ReadExactFixture(' in text:
        bounded=text[text.index('private static string ReadExactFixture('):text.index('private static void CheckAncestors(')]
    else:
        bounded='private static string ReadExactFixture(string path,int cap,params string[] expected) { string value=File.ReadAllText(path);foreach(string item in expected)if(value==item)return value;throw new ArgumentException("Unknown fixture"); }'
    source=out/'ExactProbeTests.cs';source.write_text(template.read_text().replace('/*FIELDS*/',fields).replace('/*OBSERVER*/',observer).replace('/*PUBLICATION*/',publication).replace('/*BOUNDED*/',bounded))
    (out/'source-pins.json').write_text(json.dumps({str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in [probe,template,Path(__file__)]},indent=2))
    logs=[];checks=[];code=-1
    for command in [[str(MONO/'bin/mono.exe'),str(MONO/'lib/mono/4.5/mcs.exe'),'-warnaserror+','-out:'+str(out/'tests.exe'),str(source)], [str(MONO/'bin/mono.exe'),str(out/'tests.exe'),str(out/'fixtures')]]:
        result=subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True,timeout=40);code=result.returncode;logs += [json.dumps(command),result.stdout,result.stderr,'EXIT '+str(code)]
        checks=re.findall('^PASS (.+)$',result.stdout,re.M)
        if code:break
    passed=code==0 and len(checks)>0
    (out/'execution.log').write_text('\n'.join(logs));report={'passed':passed,'checkCount':len(checks),'checks':checks,'exit':code};(out/'results.json').write_text(json.dumps(report,indent=2));print(json.dumps(report));return 0 if passed else 1
if __name__=='__main__':sys.exit(main())
