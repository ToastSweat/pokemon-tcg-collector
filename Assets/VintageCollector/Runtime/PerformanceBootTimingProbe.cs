using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Text;
using UnityEngine;
using VintageCollector.Runtime;
namespace VintageCollector.Verification {
 // Attach only to the asset-free boot root. String configuration; no heavy asset references.
 [DefaultExecutionOrder(-9000)]
 public sealed class PerformanceBootTimingProbe:MonoBehaviour {
  public const string Root="F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG";
  public string allowedProjectRoot=Root;
  internal GamePaths paths;internal string mode;
  void OnDestroy(){if(admitted)Debug.LogError("Boot timing owner destroyed before readiness; attach probe to a SEPARATE asset-free root, not coordinator root.");}
  bool admitted,first,progress,capturing;int width,height;
  [Serializable] public sealed class Timing {public int pid,firstActualRepaintFrame,width,height;public double firstActualRepaintSeconds,readySeconds;public float observedSceneProgress;public bool progressFrameObserved;}
  Timing timing=new Timing();
  void Awake(){try{paths=Admit(allowedProjectRoot,out mode);if(paths==null)return;admitted=true;timing.pid=System.Diagnostics.Process.GetCurrentProcess().Id;DontDestroyOnLoad(gameObject);}catch(Exception e){Debug.LogError("Performance boot admission refused: "+e.Message);enabled=false;}}
  public static bool ExactMarker(string text,string mode){return (mode=="performance"||mode=="reopen")&&text=="{\"schemaVersion\":1,\"owner\":\"performance-player\",\"mode\":\""+mode+"\"}";}
  internal static GamePaths Admit(string root,out string mode){
   mode=null;if(Path.GetFullPath(root)!=Path.GetFullPath(Root))throw new ArgumentException("Wrong approved root");
   var p=GamePaths.ResolvePhase5(root,Application.dataPath,Application.isEditor,Environment.GetCommandLineArgs());
   if(!p.IsSmokeTest)return null;
   if(p.SmokeScenario!="input")return null;
   string area=Path.GetFullPath(Path.Combine(Root,".artifacts/performance-player")),dest=Path.GetFullPath(p.ArtifactDirectory);
   if(!dest.StartsWith(area+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))return null;
   string[] parts=dest.Substring(area.Length+1).Split(Path.DirectorySeparatorChar);
   if(parts.Length!=2||!System.Text.RegularExpressions.Regex.IsMatch(parts[0],"^[A-Za-z0-9_-]+$")||(parts[1]!="performance"&&parts[1]!="reopen"))throw new ArgumentException("Fixture directory");
   if(!string.Equals(Path.GetFullPath(p.BaseSaveDirectory),Path.Combine(area,parts[0],"session"),StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Sibling session required");
   mode=parts[1];string file=Path.Combine(dest,"performance-fixture.json");NoReparse(file);
   if(!ExactMarker(ReadBounded(file,512),mode))throw new ArgumentException("Exact marker required");return p;
  }
  internal static string ReadBounded(string file,int cap){byte[] b=new byte[cap+1];int n=0;NoReparse(file);using(var f=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read)){while(n<b.Length){int k=f.Read(b,n,b.Length-n);if(k==0)break;n+=k;}}if(n>cap)throw new ArgumentException("Bound exceeded");return new UTF8Encoding(false,true).GetString(b,0,n);}
  internal static void NoReparse(string file){string parent=Path.GetDirectoryName(file);if(!string.IsNullOrEmpty(parent)&&parent!=file)NoReparse(parent);try{if((File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("Reparse refused");}catch(FileNotFoundException){}catch(DirectoryNotFoundException){}}
  internal static void Write(string file,string text){NoReparse(file);string tmp=file+".writing";NoReparse(tmp);using(var f=new FileStream(tmp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){byte[] b=new UTF8Encoding(false).GetBytes(text);f.Write(b,0,b.Length);f.Flush(true);}if(File.Exists(file))File.Replace(tmp,file,null);else File.Move(tmp,file);}
  void OnGUI(){if(!admitted||first||Event.current.type!=EventType.Repaint)return;first=true;timing.firstActualRepaintFrame=Time.frameCount;timing.firstActualRepaintSeconds=Time.realtimeSinceStartupAsDouble;StartCoroutine(CaptureBoot("startup-first"));}
  void Update(){
   if(!admitted)return;
   var c=UnityEngine.Object.FindFirstObjectByType<StartupLoadingCoordinator>();
   var field=typeof(StartupLoadingCoordinator).GetField("operation",BindingFlags.Instance|BindingFlags.NonPublic);
   var op=c==null?null:field.GetValue(c) as AsyncOperation;
   if(!progress&&!capturing&&op!=null&&!op.isDone&&op.progress>0){progress=true;timing.observedSceneProgress=op.progress;timing.progressFrameObserved=true;StartCoroutine(CaptureBoot("startup-progress"));}
   if(StartupLoadingCoordinator.IsReady&&!capturing){timing.readySeconds=Time.realtimeSinceStartupAsDouble;timing.width=Screen.width;timing.height=Screen.height;Write(Path.Combine(paths.ArtifactDirectory,"ready.json"),JsonUtility.ToJson(timing,true));admitted=false;}
  }
  IEnumerator CaptureBoot(string name){capturing=true;yield return new WaitForEndOfFrame();width=Screen.width;height=Screen.height;string file=Path.Combine(paths.ArtifactDirectory,name+".png");NoReparse(file);ScreenCapture.CaptureScreenshot(file);double until=Time.realtimeSinceStartupAsDouble+8;while(!CompletePng(file,width,height)&&Time.realtimeSinceStartupAsDouble<until)yield return null;bool ok=CompletePng(file,width,height);capturing=false;if(!ok){Debug.LogError("Boot framebuffer incomplete: "+name);admitted=false;}Write(Path.Combine(paths.ArtifactDirectory,"boot-trace.json"),JsonUtility.ToJson(timing,true));}
  static uint U32(byte[] b,int p){return ((uint)b[p]<<24)|((uint)b[p+1]<<16)|((uint)b[p+2]<<8)|b[p+3];}
  public static bool CompletePng(string file,int width,int height){try{NoReparse(file);var info=new FileInfo(file);if(info.Length>64*1024*1024)return false;byte[] b=File.ReadAllBytes(file),sig={137,80,78,71,13,10,26,10};if(b.Length<45)return false;for(int i=0;i<8;i++)if(b[i]!=sig[i])return false;int p=8;bool header=false,data=false;while(p+12<=b.Length){uint len=U32(b,p);if(len>int.MaxValue||(long)p+12+len>b.Length)return false;int n=(int)len;uint crc=0xffffffff;for(int i=p+4;i<p+8+n;i++){crc^=b[i];for(int bit=0;bit<8;bit++)crc=(crc&1)!=0?0xedb88320^(crc>>1):crc>>1;}if((crc^0xffffffff)!=U32(b,p+8+n))return false;string kind=Encoding.ASCII.GetString(b,p+4,4);if(kind=="IHDR"){if(header||p!=8||n!=13||U32(b,p+8)!=width||U32(b,p+12)!=height)return false;header=true;}if(kind=="IDAT")data=true;p+=12+n;if(kind=="IEND")return n==0&&p==b.Length&&header&&data;}return false;}catch(IOException){return false;}}
 }
}
