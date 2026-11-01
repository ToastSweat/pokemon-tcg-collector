using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
namespace VintageCollector.Runtime {
 // Compiled source authority, never scene/caller claims. Semantic authority is independently enforced by ApprovedSetRegistry.
 public static class Phase5InputPolicy {
  public const string BasePrintingId="base-en-unlimited1999";
  public const string JunglePrintingId="jungle-en-unlimited1999";
  public const string FossilPrintingId="fossil-en-unlimited1999";
  public const string ApprovalSha256="014260958e10bc3c2d9b28018b66026ca73490732e6f618c7b045233101fed9d";
  public const string WeightSupplementSha256="7ceb3d2b4a429c927301e77be965efcb506d41228a0324bc2ff43e3d012713ef";
  public const string BaseCatalogSha256="0a72cd298d70a2039a9e25b092f76d8d13929117cbe7a2948c2286279122b680";
  public const string BaseModelSha256="058c2027dde6633a576997359c688e461796cc5ec9689515d43d2a2880b1dbec";
  // Canonical authoring handoff independently rehashed before these compiled pins were frozen.
  public const string JungleCatalogSha256="60fc385e944778008e284c96d0038411238c0b44607b370537c825b9c88a0355";
  public const string JungleModelSha256="0d58b66ee91099bf6ad47845d62751d523404bd0dc866a62f1897041972bff12";
  public const string FossilCatalogSha256="e724f63927dd66a819c02ab4dd14408ccd0affd2bee6d5a8caa7908ed5f69860";
  public const string FossilModelSha256="eff5082e6f11efbe638d62cfbd314bda1e2fb8d54b5c7534d3a77956cd10353e";
  public static string VerifySource(string printingId,bool model,byte[] source) {
   string pin;
   switch(printingId) {
    case BasePrintingId:pin=model?BaseModelSha256:BaseCatalogSha256;break;
    case JunglePrintingId:pin=model?JungleModelSha256:JungleCatalogSha256;break;
    case FossilPrintingId:pin=model?FossilModelSha256:FossilCatalogSha256;break;
    default:throw new ArgumentException("Unknown approved printing ID");
   }
   if(source==null || source.Length==0 || source.Length>4*1024*1024) throw new InvalidDataException("Missing or oversized approved source");
   byte[] owned=(byte[])source.Clone();
   using(var sha=SHA256.Create()) {
    string actual=BitConverter.ToString(sha.ComputeHash(owned)).Replace("-","").ToLowerInvariant();
    if(!string.Equals(actual,pin,StringComparison.Ordinal)) throw new InvalidDataException("Approved source bytes differ from the frozen input");
   }
   return new UTF8Encoding(false,true).GetString(owned);
  }
 }
}
