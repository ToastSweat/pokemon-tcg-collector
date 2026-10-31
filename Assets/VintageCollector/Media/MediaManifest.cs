using System;
using System.Collections.Generic;
using VintageCollector.Domain;
namespace VintageCollector.Media
{
    public sealed class MediaEntry
    {
        public string Role { get; internal set; }
        public string CardId { get; internal set; }
        public string RelativePath { get; internal set; }
        public string Source { get; internal set; }
        public string Rights { get; internal set; }
        public string Restrictions { get; internal set; }
        public string Sha256 { get; internal set; }
        public string Binding { get { return Role+"|"+CardId; } }
    }
    public static class MediaManifest
    {
        static void Require(bool value) { if(!value) throw new System.IO.InvalidDataException("Invalid media manifest schema or binding"); }
        static string Text(Dictionary<string,object> d,string key,bool nonempty=true)
        {
            object value; Require(d.TryGetValue(key,out value) && value is string);
            string text=(string)value; Require(!nonempty || !string.IsNullOrWhiteSpace(text)); return text;
        }
        static void Fields(Dictionary<string,object> d,string fields)
        {
            string[] allowed=fields.Split(',');
            foreach(string key in d.Keys) Require(Array.IndexOf(allowed,key)>=0);
        }
        public static List<object> Parse(string json)
        {
            Require(json!=null && json.Length<=MediaFiles.ManifestByteLimit);
            var reader=new Reader(json);
            var root=reader.Read(0) as Dictionary<string,object>;
            Require(root!=null && reader.Finished);
            Fields(root,"schemaVersion,status,intendedUse,redistributionPermission,rightsDefault,notes,assets");
            object version; Require(root.TryGetValue("schemaVersion",out version) && version is int && (int)version==1);
            foreach(string name in new[]{"status","intendedUse","redistributionPermission","rightsDefault","notes"}) Text(root,name,name!="notes");
            object raw; Require(root.TryGetValue("assets",out raw) && raw is List<object>);
            var assets=(List<object>)raw; Require(assets.Count<=512); return assets;
        }
        public static MediaEntry Validate(object entry, Catalog catalog)
        {
            var d=entry as Dictionary<string,object>; Require(d!=null);
            Fields(d,"role,cardId,relativePath,source,rights,restrictions,sha256");
            string role=Text(d,"role"), id="";
            Require(Array.IndexOf(new[]{"card-face","holo-mask","card-back","wrapper-front","wrapper-back","sound-generate","sound-open","sound-next","sound-skip"},role)>=0);
            object raw; if(d.TryGetValue("cardId",out raw)) { Require(raw is string || raw==null); id=(string)raw??""; }
            if(role=="card-face" || role=="holo-mask") {
                Require(catalog!=null && !string.IsNullOrEmpty(id));
                CardPrinting card=null;
                foreach(var candidate in catalog.Cards) if(candidate.Id==id) { card=candidate; break; }
                Require(card!=null && (role!="holo-mask" || card.Finish=="holo"));
            } else Require(id=="");
            string hash=Text(d,"sha256"); Require(MediaFiles.IsHash(hash));
            return new MediaEntry { Role=role, CardId=id, RelativePath=Text(d,"relativePath"), Source=Text(d,"source"), Rights=Text(d,"rights"), Restrictions=Text(d,"restrictions",false), Sha256=hash };
        }
        // Bounded strict JSON reader. Values of the wrong schema type still parse so one bad
        // entry does not discard unrelated valid entries; validation remains exact afterward.
        // Duplicate keys, excessive nesting/strings/nodes and trailing tokens fail closed.
        sealed class Reader
        {
            readonly string text; int p, nodes;
            public Reader(string value) { text=value; }
            void Space() { while(p<text.Length && (text[p]==' ' || text[p]=='\r' || text[p]=='\n' || text[p]=='\t')) p++; }
            public bool Finished { get { Space(); return p==text.Length; } }
            bool Take(char c) { Space(); if(p<text.Length && text[p]==c) { p++; return true; } return false; }
            void Need(char c) { Require(Take(c)); }
            public object Read(int depth)
            {
                Space(); Require(depth<=8 && ++nodes<=12000 && p<text.Length);
                if(text[p]=='{') {
                    p++; var d=new Dictionary<string,object>(StringComparer.Ordinal);
                    if(Take('}')) return d;
                    do { string key=String(); Need(':'); Require(!d.ContainsKey(key)); d.Add(key,Read(depth+1)); } while(Take(','));
                    Need('}'); return d;
                }
                if(text[p]=='[') {
                    p++; var a=new List<object>(); if(Take(']')) return a;
                    do { a.Add(Read(depth+1)); } while(Take(',')); Need(']'); return a;
                }
                if(text[p]=='"') return String();
                if(p+4<=text.Length && text.Substring(p,4)=="null") { p+=4; return null; }
                if(p+4<=text.Length && text.Substring(p,4)=="true") { p+=4; return true; }
                if(p+5<=text.Length && text.Substring(p,5)=="false") { p+=5; return false; }
                int start=p; if(text[p]=='-') p++;
                Require(p<text.Length && text[p]>='0' && text[p]<='9');
                if(text[p]=='0') p++; else while(p<text.Length && text[p]>='0' && text[p]<='9') p++;
                bool fractional=false;
                if(p<text.Length && text[p]=='.') {
                    fractional=true; p++; int digits=p;
                    while(p<text.Length && text[p]>='0' && text[p]<='9') p++;
                    Require(p>digits);
                }
                if(p<text.Length && (text[p]=='e' || text[p]=='E')) {
                    fractional=true; p++; if(p<text.Length && (text[p]=='+' || text[p]=='-')) p++;
                    int digits=p; while(p<text.Length && text[p]>='0' && text[p]<='9') p++;
                    Require(p>digits);
                }
                string token=text.Substring(start,p-start);
                int number;
                if(!fractional && int.TryParse(token,System.Globalization.NumberStyles.AllowLeadingSign,System.Globalization.CultureInfo.InvariantCulture,out number)) return number;
                double value; Require(double.TryParse(token,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out value) && !double.IsNaN(value) && !double.IsInfinity(value)); return value;
            }
            string String()
            {
                Need('"'); var result=new System.Text.StringBuilder(); bool closed=false;
                while(p<text.Length) {
                    char c=text[p++]; if(c=='"') { closed=true; break; }
                    Require(c>=32);
                    if(c=='\\') {
                        Require(p<text.Length); c=text[p++];
                        switch(c) {
                            case '"': case '\\': case '/': break;
                            case 'b': c='\b'; break; case 'f': c='\f'; break; case 'n': c='\n'; break; case 'r': c='\r'; break; case 't': c='\t'; break;
                            case 'u':
                                Require(p+4<=text.Length); ushort hex; Require(ushort.TryParse(text.Substring(p,4),System.Globalization.NumberStyles.AllowHexSpecifier,System.Globalization.CultureInfo.InvariantCulture,out hex)); c=(char)hex; p+=4; break;
                            default: Require(false); break;
                        }
                    }
                    result.Append(c); Require(result.Length<=8192);
                }
                Require(closed);
                string value=result.ToString();
                for(int i=0;i<value.Length;i++) {
                    if(char.IsHighSurrogate(value[i])) { Require(i+1<value.Length && char.IsLowSurrogate(value[i+1])); i++; }
                    else Require(!char.IsLowSurrogate(value[i]));
                }
                return value;
            }
        }
    }
}
