using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
namespace VintageCollector.Funds {
 // Closed DTO codec: no Unity JsonUtility or permissive number coercion.
 internal static class LedgerJsonCodec {
  internal const int MaxBytes=8*1024*1024;
  static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
  static bool Closed(Type t){return t==typeof(LedgerData)||t==typeof(LedgerEntryData)||t==typeof(SettingsData)||t==typeof(LedgerOperationData);}
  static FieldInfo[] Fields(Type t){if(!Closed(t))throw new InvalidDataException("Unknown DTO");var f=t.GetFields(BindingFlags.Public|BindingFlags.Instance);Array.Sort(f,(a,b)=>StringComparer.Ordinal.Compare(a.Name,b.Name));return f;}
  internal static byte[] Encode(object value){var b=new StringBuilder();Write(b,value);byte[] bytes=Utf8.GetBytes(b.ToString());if(bytes.Length>MaxBytes)throw new InvalidDataException("Fund byte limit");return bytes;}
  static void Write(StringBuilder b,object o){
   if(o==null){b.Append("null");return;}var t=o.GetType();if(t==typeof(string)){b.Append('"');foreach(char c in (string)o){if(char.IsControl(c)||char.IsSurrogate(c))throw new InvalidDataException("Unsupported text");if(c=='"'||c=='\\')b.Append('\\');b.Append(c);}b.Append('"');return;}
   if(t==typeof(bool)){b.Append((bool)o?"true":"false");return;}if(t==typeof(int)||t==typeof(long)){b.Append(Convert.ToString(o,CultureInfo.InvariantCulture));return;}
   var a=o as Array;if(a!=null){b.Append('[');for(int i=0;i<a.Length;i++){if(i>0)b.Append(',');Write(b,a.GetValue(i));}b.Append(']');return;}
   b.Append('{');var f=Fields(t);for(int i=0;i<f.Length;i++){if(i>0)b.Append(',');Write(b,f[i].Name);b.Append(':');Write(b,f[i].GetValue(o));}b.Append('}');
  }
  internal static T Decode<T>(byte[] bytes){if(bytes==null||bytes.Length==0||bytes.Length>MaxBytes||bytes.Length>=3&&bytes[0]==239&&bytes[1]==187&&bytes[2]==191)throw new InvalidDataException("Invalid fund bytes");string text=Utf8.GetString(bytes);var p=new Parser(text);object v=p.Read(typeof(T),0);p.End();return (T)v;}
  internal static string Hash(byte[] bytes){using(var h=SHA256.Create()){var b=h.ComputeHash(bytes);var s=new StringBuilder(64);foreach(byte v in b)s.Append(v.ToString("x2",CultureInfo.InvariantCulture));return s.ToString();}}
  sealed class Parser {
   readonly string s;int p,tokens;internal Parser(string value){s=value;}
   void Bad(){throw new InvalidDataException("Malformed or unsupported fund JSON at "+p);}
   void Ws(){while(p<s.Length&&(s[p]==' '||s[p]=='\r'||s[p]=='\n'||s[p]=='\t'))p++;}
   bool Take(char c){Ws();if(p<s.Length&&s[p]==c){p++;return true;}return false;}
   void Need(char c){if(!Take(c))Bad();}
   bool Literal(string x){Ws();if(p+x.Length<=s.Length&&string.CompareOrdinal(s,p,x,0,x.Length)==0){p+=x.Length;return true;}return false;}
   internal void End(){Ws();if(p!=s.Length)Bad();}
   string Str(){Need('"');var b=new StringBuilder();while(p<s.Length){char c=s[p++];if(c=='"')return b.ToString();if(c=='\\'){if(p>=s.Length)Bad();c=s[p++];if(c=='u'){if(p+4>s.Length)Bad();int n=0;for(int i=0;i<4;i++){char h=s[p++];int v=h>='0'&&h<='9'?h-'0':h>='a'&&h<='f'?h-'a'+10:h>='A'&&h<='F'?h-'A'+10:-1;if(v<0)Bad();n=n*16+v;}c=(char)n;}else if(c!='"'&&c!='\\'&&c!='/')Bad();}if(char.IsControl(c)||char.IsSurrogate(c)||b.Length>=2048)Bad();b.Append(c);}Bad();return null;}
   internal object Read(Type t,int depth){if(depth>8||++tokens>1500000)Bad();Ws();if(t==typeof(string)){if(Literal("null"))return null;return Str();}
    if(t==typeof(bool)){if(Literal("true"))return true;if(Literal("false"))return false;Bad();}
    if(t==typeof(long)||t==typeof(int)){int start=p;if(p<s.Length&&s[p]=='-')p++;if(p>=s.Length||s[p]<'0'||s[p]>'9')Bad();if(s[p]=='0'){p++;if(p<s.Length&&char.IsDigit(s[p]))Bad();}else while(p<s.Length&&s[p]>='0'&&s[p]<='9'){p++;if(p-start>20)Bad();}long v;if(!long.TryParse(s.Substring(start,p-start),NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out v)||t==typeof(int)&&(v>int.MaxValue||v<int.MinValue))Bad();if(t==typeof(int))return (int)v;return v;}
    if(t.IsArray){Need('[');Type element=t.GetElementType();int max=10000;var list=new List<object>();if(!Take(']')){do{if(list.Count>=max)Bad();list.Add(Read(element,depth+1));}while(Take(','));Need(']');}var a=Array.CreateInstance(element,list.Count);for(int i=0;i<list.Count;i++)a.SetValue(list[i],i);return a;}
    var fields=Fields(t);Need('{');var obj=Activator.CreateInstance(t);var seen=new HashSet<string>(StringComparer.Ordinal);if(!Take('}')){do{string name=Str();if(!seen.Add(name))Bad();FieldInfo f=Array.Find(fields,x=>x.Name==name);if(f==null)Bad();Need(':');f.SetValue(obj,Read(f.FieldType,depth+1));}while(Take(','));Need('}');}if(seen.Count!=fields.Length)Bad();return obj;
   }
  }
 }
}
