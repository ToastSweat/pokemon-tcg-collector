using System;
using System.Globalization;
using System.Text.RegularExpressions;
namespace VintageCollector.Shopping {
 public static class ShoppingLinks {
  public static string Query(WantedCard w){if(w==null)throw new ArgumentNullException("w");return w.DisplayName+" Pokemon English "+w.SetName+" Unlimited "+w.CollectorNumber+"/"+w.CollectorTotal.ToString(CultureInfo.InvariantCulture)+(w.Finish=="holo"?" Holo ":" Non-Holo ")+"Near Mint raw ungraded -\"1st Edition\" -\"First Edition\" -Shadowless -PSA -BGS -CGC -graded";}
  public static string Encode(string text){if(text==null)throw new ArgumentNullException("text");return Uri.EscapeDataString(text);}
  static string TcgplayerQuery(WantedCard w){if(w==null)throw new ArgumentNullException("w");return w.DisplayName+" Pokemon English "+w.SetName+" Unlimited"+(w.Finish=="holo"?" Holo":"");}
  public static string Search(WantedCard wanted,string provider){string prefix;switch(provider){case "ebay":prefix="https://www.ebay.com/sch/i.html?_nkw=";break;case "tcgplayer":return "https://www.tcgplayer.com/search/pokemon/product?productLineName=pokemon&q="+Encode(TcgplayerQuery(wanted));default:throw new ArgumentException("Unknown provider");}return prefix+Encode(Query(wanted));}
  public static string ValidateListing(string raw,string provider){
   if(string.IsNullOrEmpty(raw)||raw.Length>2048||raw!=raw.Trim()||raw.IndexOf('\\')>=0||raw.IndexOf('#')>=0)throw new ArgumentException("Unsafe URL");
   foreach(char c in raw)if(char.IsControl(c)||char.IsWhiteSpace(c)||c>127)throw new ArgumentException("URL must be escaped ASCII");
   for(int i=0;i<raw.Length;i++)if(raw[i]=='%'){if(i+2>=raw.Length||!Uri.IsHexDigit(raw[i+1])||!Uri.IsHexDigit(raw[i+2]))throw new ArgumentException("Malformed escape");int b=Convert.ToInt32(raw.Substring(i+1,2),16);if(b<32||b==127||b==92)throw new ArgumentException("Unsafe escaped character");i+=2;}
   Uri uri;if(!Uri.TryCreate(raw,UriKind.Absolute,out uri)||uri.Scheme!="https"||!uri.IsDefaultPort||uri.UserInfo!=""||uri.Fragment!="")throw new ArgumentException("Unsafe authority");
   string authority=raw.Substring(raw.IndexOf("://",StringComparison.Ordinal)+3).Split('/','?')[0];if(authority.IndexOf('@')>=0||authority.IndexOf('%')>=0||authority.IndexOf(':')>=0&&!authority.EndsWith(":443",StringComparison.Ordinal))throw new ArgumentException("Unsafe authority");
   bool ebay=provider=="ebay";if(!ebay&&provider!="tcgplayer")throw new ArgumentException("Unknown provider");string host=uri.Host;if(ebay?host!="ebay.com"&&host!="www.ebay.com":host!="tcgplayer.com"&&host!="www.tcgplayer.com")throw new ArgumentException("Untrusted host");
   int start=raw.IndexOf('/',raw.IndexOf("://",StringComparison.Ordinal)+3);if(start<0)throw new ArgumentException("Missing path");string path=raw.Substring(start).Split('?')[0];
   string pattern=ebay?@"\A/itm/(?:[A-Za-z0-9_-]+/)?[1-9][0-9]{0,19}/?\z":@"\A/product/[1-9][0-9]{0,19}(?:/[A-Za-z0-9_-]+)?/?\z";if(!Regex.IsMatch(path,pattern,RegexOptions.CultureInvariant))throw new ArgumentException("Not a listing path");
   if(uri.Query.Length>0)foreach(string pair in uri.Query.Substring(1).Split('&')){var parts=pair.Split('=');if(parts.Length!=2||!Regex.IsMatch(parts[1],@"\A[A-Za-z0-9_-]{1,128}\z"))throw new ArgumentException("Unsafe parameter");bool allowed=ebay?(parts[0]=="var"||parts[0]=="hash"||parts[0]=="itmmeta"||parts[0]=="itmprp"):parts[0]=="Language"||parts[0]=="Printing"||parts[0]=="Condition";if(!allowed)throw new ArgumentException("Unsupported parameter");}
   return raw;
  }
 }
}
