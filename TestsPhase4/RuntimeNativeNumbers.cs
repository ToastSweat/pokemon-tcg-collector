using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using UnityEngine;
using VintageCollector.Domain;
using VintageCollector.Persistence;
using VintageCollector.Collections;
using VintageCollector.Runtime;
namespace VintageCollector.EditorChecks {
 public static class RuntimeNativeNumbers {
  [Serializable] public sealed class Observation {public string input,physicalResult,sessionResult;}
  [Serializable] public sealed class Observations {public Observation[] values;}
  static string Hash(string s) {using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-","").ToLowerInvariant();}
  static byte[] Envelope(string header,string s) {return Encoding.UTF8.GetBytes(header+"\n"+Hash(s)+"\n"+s);}
  static void Rejected(Action a) {bool rejected=false;try{a();}catch(InvalidDataException){rejected=true;}catch(ArgumentException){rejected=true;}if(!rejected)throw new Exception("Malformed native JSON accepted");}
  public static void Run(string output,Action<string,Action> test) {
   string root=Directory.GetParent(Directory.GetParent(Directory.GetParent(output).FullName).FullName).FullName;
   var catalog=Catalog.FromData(JsonUtility.FromJson<CatalogData>(File.ReadAllText(Path.Combine(Application.dataPath,"Data/base-en-unlimited1999.catalog.json"))));
   string card=catalog.Cards[0].Id;
   string physical=new UnityPhysicalSerializer().Serialize(new PhysicalStateData {schemaVersion=1,revision=1,edits=new[]{new PhysicalEditData {transactionId=Guid.NewGuid().ToString("N"),createdUtc=DateTime.UtcNow.ToString("O"),cardId=card,condition="unknown",quantity=1}}});
   var observations=new List<Observation>();
   foreach(string token in new[]{"4294967297","9223372036854775808","18446744073709551617","1e0","1.0","01","-0"}) {
    string sample=physical.Replace("\"quantity\":1","\"quantity\":"+token);string result;
    try{var p=JsonUtility.FromJson<PhysicalStateData>(sample);result="quantity="+p.edits[0].quantity+", revision="+p.revision;}catch(Exception e){result=e.GetType().Name;}
    observations.Add(new Observation {input=token,physicalResult=result});
   }
   File.WriteAllText(Path.Combine(output,"native-json-observations.json"),JsonUtility.ToJson(new Observations {values=observations.ToArray()},true));
   var mutations=new Dictionary<string,string> {
    {"schema int alias",physical.Replace("\"schemaVersion\":1","\"schemaVersion\":4294967297")},
    {"revision long overflow",physical.Replace("\"revision\":1","\"revision\":18446744073709551617")},
    {"quantity int alias",physical.Replace("\"quantity\":1","\"quantity\":4294967297")},
    {"quantity exponent",physical.Replace("\"quantity\":1","\"quantity\":1e0")},
    {"quantity decimal",physical.Replace("\"quantity\":1","\"quantity\":1.0")},
    {"quantity leading zero",physical.Replace("\"quantity\":1","\"quantity\":01")},
    {"duplicate quantity",physical.Replace("\"quantity\":1","\"quantity\":1,\"quantity\":1")},
    {"unknown property",physical.Replace("\"quantity\":1","\"quantity\":1,\"unknown\":false")}
   };
   foreach(var m in mutations) {var entry=m;test("native physical adapter rejects "+m.Key,()=>Rejected(()=>new UnityPhysicalSerializer().Deserialize(entry.Value)));}
   int n=0;foreach(var m in mutations) {var entry=m;string directory=Path.Combine(output,"native-number-physical-"+(++n));test("native physical store rejects "+m.Key,()=> {
    Directory.CreateDirectory(directory);File.WriteAllBytes(Path.Combine(directory,"physical.save"),Envelope("VTCG-PHYSICAL-1",entry.Value));Rejected(()=>{using(var p=new PhysicalStore(directory,new UnityPhysicalSerializer(),catalog)) {}});
   });}
   string source=Path.Combine(output,"native-number-session-source");var generator=new PackGenerator(catalog,JsonUtility.FromJson<RuleData>(File.ReadAllText(Path.Combine(Application.dataPath,"Data/base-en-unlimited1999.model-a-v0.1.json"))));
   using(var store=new SessionStore(source,new UnityStateSerializer(),catalog)) {store.OpenPack(Guid.NewGuid().ToString("N"),DateTime.UtcNow.ToString("O"),123,generator.Generate(new SeededRandom(123)));}
   string envelope=File.ReadAllText(Path.Combine(source,"state.save"));string session=envelope.Substring(envelope.IndexOf('\n',envelope.IndexOf('\n')+1)+1);
   var sessionMutations=new Dictionary<string,string> {
    {"schema int alias",session.Replace("\"schemaVersion\":1","\"schemaVersion\":4294967297")},
    {"revision long overflow",session.Replace("\"revision\":1","\"revision\":18446744073709551617")},
    {"seed uint alias",session.Replace("\"seed\":123","\"seed\":"+(4294967296UL+123).ToString())},
    {"reveal int alias",session.Replace("\"revealedCount\":0","\"revealedCount\":4294967296")},
    {"seed exponent",session.Replace("\"seed\":123","\"seed\":123e0")},
    {"seed leading zero",session.Replace("\"seed\":123","\"seed\":0123")}
   };
   foreach(var m in sessionMutations) {var entry=m;test("native schema1 archive rejects "+m.Key,()=> {
    if(entry.Value==session) throw new Exception("Mutation failed to match native JSON");
    var files=new Dictionary<string,byte[]> {{"state.save",Envelope("VTCG-SAVE-1",entry.Value)}};
    Rejected(()=>SessionArchiveValidation.Validate(files,new UnityStateSerializer(),catalog));
   });}
  }
 }
}
