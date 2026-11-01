using System;
using VintageCollector.EditorTools;
// Pure production-policy admission only; caller owns and snapshots every fixture.
#if BOUNDARY_ATTRIBUTES
// Test-only OS attribute seam for special nodes Windows NTFS cannot create as POSIX FIFOs.
public static class BoundaryAttributes {
 public static System.IO.FileAttributes GetAttributes(string path) {
  string target=Environment.GetEnvironmentVariable("PHASE5_TEST_DEVICE");
  if(target!=null && string.Equals(System.IO.Path.GetFullPath(path),System.IO.Path.GetFullPath(target),StringComparison.OrdinalIgnoreCase))return System.IO.FileAttributes.Device;
  return System.IO.File.GetAttributes(path);
 }
}
#endif
public static class RuntimeBuildBoundaryChecks {
 public static int Main(string[] args) {
  bool refused=false;int writes=0,launches=0;IDisposable locked=null;
  try {
   if(args.Length>3)locked=new System.IO.FileStream(args[3],System.IO.FileMode.Open,System.IO.FileAccess.Read,System.IO.FileShare.None);
   if(args[1]=="candidate")Phase5BuildPolicy.Resolve(args[0]);
   else if(args[1]=="stage")Phase5BuildPolicy.RequireOwnedStage(System.IO.Path.Combine(args[0],".artifacts","phase5-build-project"),args[0]);
   else throw new Exception("Unknown check mode");
   // Mock downstream sinks: a refused admission must never reach either sink.
   if(args[2]=="refuse"){writes++;launches++;}
  } catch(ArgumentException e) {refused=true;Console.WriteLine(e.Message);}
    catch(InvalidOperationException e) {refused=true;Console.WriteLine(e.Message);}
    finally{if(locked!=null)locked.Dispose();}
  Console.WriteLine("WRITE_CALLS "+writes+" LAUNCH_CALLS "+launches);
  bool expected=args[2]=="refuse";
  Console.WriteLine((refused==expected?"PASS ":"FAIL ")+args[1]+"-"+args[2]);
  Console.WriteLine("CHECKS 1");return refused==expected?0:1;
 }
}
