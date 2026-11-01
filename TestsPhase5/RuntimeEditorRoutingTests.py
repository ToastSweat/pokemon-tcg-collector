"""Execute exact controller/host path-assignment statements with the real path policy; no native/store claim."""
from pathlib import Path
import os,subprocess,unittest
ROOT=Path(__file__).resolve().parents[1]
WORK=ROOT/'.artifacts/phase5-parent-paths'
BASE=ROOT/'.artifacts/phase5-integration-work/Assets/VintageCollector/Runtime'
MONO=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge')

def statement(name,start):
    path=WORK/'Assets/VintageCollector/Runtime'/name
    if not path.exists():path=BASE/name
    text=path.read_text();at=text.index(start);end=text.index(';',at)+1
    return text[at:end]

class Routing(unittest.TestCase):
    def test_exact_phase5_controller_host_media_routing(self):
        out=Path(os.environ['PHASE5_PATH_ROUTING_OUT']);self.assertTrue(out.resolve().is_relative_to((ROOT/'.artifacts').resolve()));out.mkdir(parents=True,exist_ok=False)
        game=statement('OpeningGame.cs','if (Paths == null) Paths =')
        host=statement('Phase5CollectionHost.cs','var paths=GamePaths.Resolve')
        media=statement('Phase5MediaHost.cs','var validated=GamePaths.Resolve')
        text='''using System;using System.IO;using VintageCollector.Runtime;
public static class Application {public static string dataPath;public static bool isEditor=true;}
public sealed class Owner {public string allowedProjectRoot;public GamePaths Paths;}
public static class Checks {
 public static object ApprovedSets;public static GamePaths Paths;public static string initialRoot;public static string[] initialArgs=new string[0];public static string initialProfileId="";
 static GamePaths Open(string root,bool phase5) {ApprovedSets=phase5?new object():null;Paths=null;initialRoot=root; // GAME
 return Paths;}
 static GamePaths Host(string root) {var Game=new Owner{allowedProjectRoot=root};var args=new string[0]; // HOST
 return paths;}
 static GamePaths Media(string root) {var owner=new Owner{Paths=GamePaths.ResolvePhase5(root,Application.dataPath,true,new string[0])};var args=new string[0]; // MEDIA
 return validated;}
 static void Require(bool v,string message){if(!v)throw new Exception(message);}
 public static int Main(string[] args){try{string root=Path.Combine(args[0],"project");Application.dataPath=Path.Combine(root,"Assets");Directory.CreateDirectory(Application.dataPath);string expected=Path.Combine(root,"Saves","Phase5");Require(Open(root,true).SaveDirectory==expected,"Phase5 controller selects legacy Editor saves");Require(Host(root).SaveDirectory==expected,"Phase5 collection lease selects legacy Editor saves");Require(Media(root).SaveDirectory==expected,"Phase5 media validation selects legacy Editor saves");Require(Open(root,false).SaveDirectory==Path.Combine(root,"Saves","Phase2"),"Legacy controller path changed");Require(!Directory.Exists(Path.Combine(root,"Saves")),"Routing created save directories");Console.WriteLine("PASS exact real-policy controller/host/media routing");return 0;}catch(Exception e){Console.WriteLine("FAIL "+e);return 1;}}
}'''.replace('// GAME',game).replace('// HOST',host).replace('// MEDIA',media)
        source=out/'ExactRouting.cs';source.write_text(text)
        env=dict(os.environ,TEMP=str(out),TMP=str(out),TMPDIR=str(out))
        commands=[[str(MONO/'bin/mono.exe'),str(MONO/'lib/mono/4.5/mcs.exe'),'-warnaserror+','-out:'+str(out/'Checks.exe'),str(WORK/'Assets/VintageCollector/Runtime/GamePaths.cs'),str(source)],[str(MONO/'bin/mono.exe'),str(out/'Checks.exe'),str(out)]]
        log=[]
        for command in commands:
            run=subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True,timeout=120);log.extend([repr(command),run.stdout,run.stderr,'EXIT '+str(run.returncode)]);(out/'execution.log').write_text('\n'.join(log));self.assertEqual(run.returncode,0,run.stdout+run.stderr)
if __name__=='__main__':unittest.main(verbosity=2)
