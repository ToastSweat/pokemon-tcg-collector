"""Extract the exact legacy reflection helper; prove overload-safe binding without Unity doubles being called native."""
from pathlib import Path
import hashlib
import os
import subprocess
import sys
import unittest
ROOT=Path(__file__).resolve().parents[1]
MONO=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge')
ORIGINAL=ROOT/'Assets/VintageCollector/Editor/Phase2RuntimeChecks.cs'
STAGED=ROOT/'.artifacts/phase5-parent-test-adapters/Assets/VintageCollector/Editor/Phase2RuntimeChecks.cs'

class ReflectionAdapter(unittest.TestCase):
    def test_seed_only_reflection_selects_the_existing_uint_api(self):
        source=STAGED if STAGED.exists() else ORIGINAL
        text=source.read_text(encoding='utf-8')
        start=text.index('public void Invoke(string method, params object[] args)')
        brace=text.index('{',start);end=brace+1;depth=1
        while depth:
            if text[end]=='{':depth+=1
            elif text[end]=='}':depth-=1
            end+=1
        method=text[start:end]
        out=Path(os.environ['PHASE5_REFLECTION_TEST_OUT'])
        self.assertTrue(out.resolve().is_relative_to((ROOT/'.artifacts').resolve()))
        out.mkdir(parents=True,exist_ok=False)
        body='''using System;
public sealed class FakeGame {
 public uint seed;public string identity;
 public void GenerateForSmoke(uint value) {seed=value;identity=null;}
 public void GenerateForSmoke(uint value,string id) {seed=value;identity=id;}
 public void DispatchCommand(string command) {identity=command;}
}
public sealed class Fixture {
 public readonly FakeGame game=new FakeGame();
 static void Require(bool pass,string message) {if(!pass)throw new Exception(message);}
 // EXTRACTED
}
public static class Checks {
 public static int Main() {
  try {var f=new Fixture();f.Invoke("GenerateForSmoke",(uint)5);if(f.game.seed!=5||f.game.identity!=null)throw new Exception("Wrong overload selected");f.Invoke("DispatchCommand","unchanged");if(f.game.identity!="unchanged")throw new Exception("Other reflection dispatch changed");Console.WriteLine("PASS exact seed-only overload and unchanged other method");return 0;}
  catch(Exception e) {Console.WriteLine("FAIL "+e);return 1;}
 }
}
'''.replace('// EXTRACTED',method)
        file=out/'ExactReflection.cs';file.write_text(body,encoding='utf-8')
        env=dict(os.environ,TEMP=str(out),TMP=str(out),TMPDIR=str(out),PYTHONDONTWRITEBYTECODE='1')
        commands=[
            [str(MONO/'bin/mono.exe'),str(MONO/'lib/mono/4.5/mcs.exe'),'-warnaserror+','-out:'+str(out/'Checks.exe'),str(file)],
            [str(MONO/'bin/mono.exe'),str(out/'Checks.exe')]
        ]
        logs=[]
        for command in commands:
            result=subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True,timeout=120)
            logs.extend([repr(command),result.stdout,result.stderr,'EXIT '+str(result.returncode)])
            (out/'execution.log').write_text('\n'.join(logs),encoding='utf-8')
            self.assertEqual(result.returncode,0,result.stdout+result.stderr)
        (out/'source-sha256.txt').write_text(hashlib.sha256(source.read_bytes()).hexdigest(),encoding='utf-8')

if __name__=='__main__':unittest.main(verbosity=2)
