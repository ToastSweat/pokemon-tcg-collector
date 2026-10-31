using System;
using System.IO;
using System.Threading;
// The runner injects the EXACT production probe method; this is not a second implementation.
internal static class PlayerAtomicTests {
    /*ATOMIC_SOURCE*/
    private static int Main(string[] args) {
        string root=args[0];Directory.CreateDirectory(root);string path=Path.Combine(root,"probe.json");
        File.WriteAllText(path,"old");
        using(var reader=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)) {
            var release=new Thread(()=>{Thread.Sleep(180);reader.Dispose();});release.Start();
            try {AtomicText(path,"new");} catch(Exception e) {release.Join();Console.Error.WriteLine(e);return 1;}
            release.Join();
        }
        if(File.ReadAllText(path)!="new")return 2;
        Console.WriteLine("PASS temporary-reader-share-lock-retried-with-exact-source");
        bool refused=false;var started=DateTime.UtcNow;
        using(var reader=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)) {
            try {AtomicText(path,"must-not-publish");} catch(IOException){refused=true;}
        }
        if(!refused || (DateTime.UtcNow-started).TotalSeconds>4 || File.ReadAllText(path)!="new")return 3;
        Console.WriteLine("PASS persistent-lock-fails-bounded-preserves-published-bytes");
        AtomicText(path,"recovered");if(File.ReadAllText(path)!="recovered" || File.Exists(path+".writing"))return 4;
        Console.WriteLine("PASS subsequent-write-recovers-with-no-stale-work-file");
        Console.WriteLine("TEST_COUNT 3");return 0;
    }
}
