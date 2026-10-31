using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
namespace VintageCollector.Media
{
    public static class MediaFiles
    {
        public const int MediaByteLimit = 16 * 1024 * 1024;
        public const int ManifestByteLimit = 1024 * 1024;
        static void Invalid() { throw new InvalidDataException("Unsafe media path or content"); }
        public static string LocalRoot(string root)
        {
            // Windows desktop contract: reject remote/device roots lexically before any metadata access.
            // A source URL can never become a project root or implicit UNC filesystem fetch.
            if(string.IsNullOrWhiteSpace(root) || root.Length<3 || !((root[0]>='A'&&root[0]<='Z')||(root[0]>='a'&&root[0]<='z')) || root[1]!=':' || (root[2]!='/' && root[2]!='\\')) Invalid();
            if(root.IndexOf(':',2)>=0 || root.IndexOfAny(new[]{'?','*','"','<','>','|'})>=0) Invalid();
            foreach(char c in root) if(char.IsControl(c)) Invalid();
            string drive=Path.GetPathRoot(root);
            if(new DriveInfo(drive).DriveType==DriveType.Network) Invalid();
            string full=Path.GetFullPath(root);
            return full.Length==3?full:full.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
        }
        public static string Resolve(string root, string relative, bool test)
        {
            // Lexical gate runs before metadata access. No URI decoding or silent normalization.
            if(string.IsNullOrWhiteSpace(root) || string.IsNullOrEmpty(relative)) Invalid();
            if(!(relative.StartsWith("Assets/Supplied/",StringComparison.Ordinal) || (test && relative.StartsWith(".artifacts/",StringComparison.Ordinal)))) Invalid();
            if(relative.IndexOfAny(new[]{'\\',':','%','?','*','"','<','>','|'})>=0 || Path.IsPathRooted(relative)) Invalid();
            foreach(char c in relative) if(char.IsControl(c)) Invalid();
            foreach(string part in relative.Split('/')) {
                if(part.Length==0 || part=="." || part==".." || part.EndsWith(".",StringComparison.Ordinal) || part.EndsWith(" ",StringComparison.Ordinal)) Invalid();
                string stem=part.Split('.')[0].ToUpperInvariant();
                if(stem=="CON" || stem=="PRN" || stem=="AUX" || stem=="NUL" || stem=="CONIN$" || stem=="CONOUT$" || (stem.Length==4 && (stem.StartsWith("COM",StringComparison.Ordinal)||stem.StartsWith("LPT",StringComparison.Ordinal)) && stem[3]>='0' && stem[3]<='9')) Invalid();
            }
            string fullRoot=LocalRoot(root);
            string full=Path.GetFullPath(Path.Combine(fullRoot,relative));
            string prefix=fullRoot.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(!full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)) Invalid();
            CheckAncestors(full);
            return full;
        }
        public static string[] AncestorsRootFirst(string path)
        {
            var paths=new System.Collections.Generic.List<string>();
            for(string current=path; !string.IsNullOrEmpty(current); current=Path.GetDirectoryName(current)) paths.Add(current);
            paths.Reverse(); return paths.ToArray();
        }
        static void CheckAncestors(string path)
        {
            // Never stat a descendant through an unchecked ancestor (which might redirect remotely).
            foreach(string current in AncestorsRootFirst(path)) {
                try { if((File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0) Invalid(); }
                catch(FileNotFoundException) { }
                catch(DirectoryNotFoundException) { }
            }
        }
        // Verify actual opened handle before reading bytes, including a junction swapped at open time.
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
        static extern uint GetFinalPathNameByHandle(IntPtr handle, StringBuilder path, uint length, uint flags);
        static void CheckHandle(FileStream stream, string expected)
        {
            if(Environment.OSVersion.Platform!=PlatformID.Win32NT) return;
            var path=new StringBuilder(32768);
            uint count=GetFinalPathNameByHandle(stream.SafeFileHandle.DangerousGetHandle(),path,(uint)path.Capacity,0);
            if(count==0 || count>=path.Capacity) Invalid();
            string actual=path.ToString();
            if(actual.StartsWith("\\\\?\\",StringComparison.Ordinal)) actual=actual.Substring(4);
            if(!string.Equals(actual,expected,StringComparison.OrdinalIgnoreCase)) Invalid();
        }
        public static byte[] Read(string root, string relative, bool test, int limit)
        {
            if(limit<=0 || limit>MediaByteLimit) throw new ArgumentException("Invalid read limit");
            string path=Resolve(root,relative,test);
            using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)) {
                CheckHandle(stream,path); CheckAncestors(path);
                if(stream.Length<1 || stream.Length>limit) throw new InvalidDataException("Media byte limit exceeded or empty file");
                byte[] result=new byte[(int)stream.Length];
                int count=0;
                while(count<result.Length) { int n=stream.Read(result,count,result.Length-count); if(n==0) throw new InvalidDataException("Truncated media"); count+=n; }
                return result;
            }
        }
        public static string Sha256(byte[] bytes)
        {
            using(var sha=SHA256.Create()) { return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(); }
        }
        public static bool IsHash(string hash)
        {
            if(hash==null || hash.Length!=64) return false;
            foreach(char c in hash) if(!((c>='0'&&c<='9')||(c>='a'&&c<='f'))) return false;
            return true;
        }
        public static void VerifyHash(byte[] bytes, string hash)
        {
            if(!IsHash(hash) || Sha256(bytes)!=hash) throw new InvalidDataException("Missing, noncanonical or mismatched SHA256");
        }
    }
}
