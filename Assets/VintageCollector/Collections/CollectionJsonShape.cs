using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
namespace VintageCollector.Collections
{
 // JSON shape gate only: production StateValidation/PhysicalValidation still own semantics.
 // No permissive DTO defaults, duplicate properties, ignored fields or numeric coercion.
 internal sealed class CollectionJsonShape
 {
  readonly string text;int pos; CollectionJsonShape(string s) {text=s;}
  sealed class Number {internal string Text;internal Number(string t) {Text=t;} }
  static void Bad() {throw new InvalidDataException("Invalid collection JSON shape");}
  void Space() {while(pos<text.Length && (text[pos]==' '||text[pos]=='\t'||text[pos]=='\r'||text[pos]=='\n')) pos++;}
  bool Take(char c) {Space();if(pos<text.Length&&text[pos]==c) {pos++;return true;}return false;}
  string Str() {
   if(!Take('"')) Bad();var b=new StringBuilder();bool ended=false;
   while(pos<text.Length) {char c=text[pos++];if(c=='"') {ended=true;break;}if(c<32) Bad();
    if(c=='\\') {if(pos>=text.Length) Bad();c=text[pos++];switch(c) {
     case '"':case '\\':case '/':break;
     case 'b':c='\b';break;case 'f':c='\f';break;case 'n':c='\n';break;case 'r':c='\r';break;case 't':c='\t';break;
     case 'u':int code=0;for(int i=0;i<4;i++) {if(pos>=text.Length) Bad();char h=text[pos++];int n=h>='0'&&h<='9'?h-'0':h>='a'&&h<='f'?h-'a'+10:h>='A'&&h<='F'?h-'A'+10:-1;if(n<0) Bad();code=code*16+n;}c=(char)code;break;
     default:Bad();break;
    }}b.Append(c);
   }
   if(!ended) Bad();string s=b.ToString();for(int i=0;i<s.Length;i++) if(char.IsSurrogate(s[i])) {if(!char.IsHighSurrogate(s[i])||i+1>=s.Length||!char.IsLowSurrogate(s[++i])) Bad();}return s;
  }
  object Value(int depth) {
   Space();if(depth>8||pos>=text.Length) {Bad();return null;}
   char c=text[pos];
   if(c=='{') {pos++;var d=new Dictionary<string,object>(StringComparer.Ordinal);if(Take('}')) return d;
    do {string k=Str();if(!Take(':')||d.ContainsKey(k)) Bad();d.Add(k,Value(depth+1));if(Take('}')) return d;}while(Take(','));Bad();
   }
   if(c=='[') {pos++;var a=new List<object>();if(Take(']')) return a;do {a.Add(Value(depth+1));if(Take(']')) return a;}while(Take(','));Bad();}
   if(c=='"') return Str();
   foreach(string literal in new[]{"true","false"}) if(pos+literal.Length<=text.Length&&text.Substring(pos,literal.Length)==literal) {pos+=literal.Length;return literal=="true";}
   int start=pos;if(c>='0'&&c<='9') {pos++;if(c!='0') while(pos<text.Length&&text[pos]>='0'&&text[pos]<='9') pos++;return new Number(text.Substring(start,pos-start));}
   Bad();return null;
  }
  static Dictionary<string,object> Object(object o,string fields) {
   var d=o as Dictionary<string,object>;if(d==null) {Bad();return null;}string[] names=fields.Split(',');if(d.Count!=names.Length) Bad();foreach(string n in names) if(!d.ContainsKey(n)) Bad();return d;
  }
  static void Type(Dictionary<string,object> d,string fields,Type type) {foreach(string n in fields.Split(',')) if(d[n]==null||d[n].GetType()!=type) Bad();}
  static List<object> ArrayOf(object o,Type type) {var a=o as List<object>;if(a==null) {Bad();return null;}foreach(object x in a) if(x==null||x.GetType()!=type) Bad();return a;}
  static object Parse(string s) {if(s==null) {Bad();return null;}var p=new CollectionJsonShape(s);var v=p.Value(0);p.Space();if(p.pos!=s.Length) Bad();return v;}
  static void Range(Dictionary<string,object> d,string field,ulong maximum) {
   ulong value;var number=d[field] as Number;
   if(number==null || !ulong.TryParse(number.Text,NumberStyles.None,CultureInfo.InvariantCulture,out value) || value>maximum) Bad();
  }
  internal static void Session(string s) {
   var d=Object(Parse(s),"schemaVersion,revision,packs");Type(d,"schemaVersion,revision",typeof(Number));
   Range(d,"schemaVersion",int.MaxValue);Range(d,"revision",(ulong)long.MaxValue);
   foreach(object o in ArrayOf(d["packs"],typeof(Dictionary<string,object>))) {
    var p=Object(o,"transactionId,createdUtc,modelId,modelVersion,catalogVersion,printingId,rngAlgorithm,status,seed,orderedCardIds,newToVirtual,revealedCount,wrapperOpened");
    Type(p,"transactionId,createdUtc,modelId,modelVersion,catalogVersion,printingId,rngAlgorithm,status",typeof(string));Type(p,"seed,revealedCount",typeof(Number));Range(p,"seed",uint.MaxValue);Range(p,"revealedCount",int.MaxValue);Type(p,"wrapperOpened",typeof(bool));ArrayOf(p["orderedCardIds"],typeof(string));ArrayOf(p["newToVirtual"],typeof(bool));
   }
  }
  internal static void Physical(string s) {
   var d=Object(Parse(s),"schemaVersion,revision,edits");Type(d,"schemaVersion,revision",typeof(Number));
   foreach(object o in ArrayOf(d["edits"],typeof(Dictionary<string,object>))) {var e=Object(o,"transactionId,createdUtc,cardId,condition,quantity");Type(e,"transactionId,createdUtc,cardId,condition",typeof(string));Type(e,"quantity",typeof(Number));}
  }
 }
}
