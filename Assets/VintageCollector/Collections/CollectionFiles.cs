using System;
using System.IO;
namespace VintageCollector.Collections
{
 internal static class CollectionFiles
 {
  [System.Runtime.InteropServices.DllImport("kernel32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode,SetLastError=true)]
  static extern bool CreateDirectoryW(string path,IntPtr security);
  internal static void ExclusiveDirectory(string p) {Check(p);if(!CreateDirectoryW(p,IntPtr.Zero)) throw new IOException("Import directory could not be exclusively created");}

  internal static string Local(string path) {
   if(string.IsNullOrEmpty(path) || path.Length<3 || !char.IsLetter(path[0]) || path[1]!=':' || path[2]!='/'&&path[2]!='\\' || path.IndexOf(':',2)>=0 || path.StartsWith("\\\\",StringComparison.Ordinal)) throw new ArgumentException("A local absolute directory is required");
   foreach(string s in path.Substring(3).Split(new[]{'/', '\\'},StringSplitOptions.RemoveEmptyEntries)) {
    if(s=="." || s==".." || s.TrimEnd(' ','.')!=s || s.IndexOfAny(Path.GetInvalidFileNameChars())>=0) throw new ArgumentException("Invalid collection path");
    string device=s.Split('.')[0].ToUpperInvariant();
    if(device=="CON"||device=="PRN"||device=="AUX"||device=="NUL"||device=="CLOCK$"||device.Length==4&&(device.StartsWith("COM",StringComparison.Ordinal)||device.StartsWith("LPT",StringComparison.Ordinal))&&("123456789¹²³".IndexOf(device[3])>=0)) throw new ArgumentException("Device paths are not collection storage");
   }
   string full=Path.GetFullPath(path);if(full.Length<=3) throw new ArgumentException("Select a collection directory, not a drive root");full=full.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar); Check(full); return full;
  }
  internal static void Check(string path) {
   string full=Path.GetFullPath(path),r=Path.GetPathRoot(full),current=r;
   CheckOne(current); foreach(string part in full.Substring(r.Length).Split(new[]{'/', '\\'},StringSplitOptions.RemoveEmptyEntries)) {current=Path.Combine(current,part); CheckOne(current);}
  }
  static void CheckOne(string p) {
   try {if((File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0) throw new InvalidDataException("Reparse paths are not collection storage");}
   catch(FileNotFoundException) {} catch(DirectoryNotFoundException) {}
  }
  internal static bool Exists(string p) {Check(p); try {File.GetAttributes(p);return true;} catch(FileNotFoundException) {return false;} catch(DirectoryNotFoundException) {return false;}}
 }
}
