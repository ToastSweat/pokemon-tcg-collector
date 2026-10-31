using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VintageCollector.Runtime;
using VintageCollector.Presentation;
using VintageCollector.Domain;
using VintageCollector.Media;

// Explicit isolated native entry point. No startup hook, generation or live-save path.
public static class MaterialsPresentationChecks
{
    [Serializable] sealed class Report { public bool passed; public int checkCount; public string[] checks; public string failure; }
    static readonly List<string> checks=new List<string>();
    static void Check(bool b,string n) { if(!b) throw new Exception(n); checks.Add(n); }
    static FieldInfo Field(string n) { return typeof(Phase3MediaHost).GetField(n,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic); }
    [Serializable] sealed class Entry { public string role="wrapper-front",cardId="",relativePath,source="Original synthetic native host fixture",rights="Original synthetic",restrictions="Private test only",sha256; }
    [Serializable] sealed class Manifest { public int schemaVersion=1; public string status="Synthetic host opt-in",intendedUse="Private tests",redistributionPermission="none",rightsDefault="Original synthetic",notes="Not actual supplied artwork"; public Entry[] assets; }
    static string Hash(byte[] b) { using(var h=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant(); }
    static void ConfiguredHost() {
        string root=Path.GetDirectoryName(Application.dataPath),dir=Path.Combine(root,".artifacts/host-materials-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        string relative="Assets/Supplied/materials-host-synthetic.png",manifest="Assets/Supplied/materials-host-synthetic.json";
        var t=new Texture2D(260,475,TextureFormat.RGBA32,false);byte[] bytes;
        try { var pixels=new Color32[260*475];for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(70,120,190,255);t.SetPixels32(pixels);t.Apply();bytes=t.EncodeToPNG(); }finally{UnityEngine.Object.DestroyImmediate(t);}
        File.WriteAllBytes(Path.Combine(root,relative),bytes);
        File.WriteAllText(Path.Combine(root,manifest),JsonUtility.ToJson(new Manifest {assets=new[]{new Entry {relativePath=relative,sha256=Hash(bytes)}}}));
        foreach(string profile in new[]{"","c913a31b0c664da7a83487d68e72f2ce"}) {
            var go=new GameObject("Isolated actual controller material manifest fixture");var game=go.AddComponent<OpeningGame>();Phase3MediaHost host=null;
            try {
                game.allowedProjectRoot=root;game.catalogJson=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json");game.modelJson=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json");
                game.InitializeForProfile(new[]{"--vcc-smoke=ready","--vcc-save-root="+Path.Combine(dir,"saves"),"--vcc-artifacts="+dir},profile);
                Check(game.CurrentView.stage=="ready" && game.CurrentSession!=null,"Actual controller initialized in isolated profile "+profile);
                var ui=go.GetComponent<TabletopPresentation>();var before=game.CurrentSession;var rng=UnityEngine.Random.state;
                host=go.AddComponent<Phase3MediaHost>();Field("manifestRelativePath").SetValue(host,manifest);host.Initialize(game);
                Check(host.Library!=null && host.Library.SuppliedAssetCount==1 && host.Library.WrapperFront!=null,"Trusted configured manifest rather than old empty manifest selected "+profile);
                Check(!host.UsesTestManifest && host.Library.Warnings.Count==0,"Trusted production-subtree manifest does not enable test paths "+profile);
                Check(ReferenceEquals(before,game.CurrentSession) && game.GeneratedResultCount==0 && ui.CommittedCueCount==0 && go.GetComponents<AudioSource>().Length==0 && JsonUtility.ToJson(rng)==JsonUtility.ToJson(UnityEngine.Random.state),"Host bind leaves session RNG and startup audio untouched "+profile);
                Check(!File.Exists(Path.Combine(game.Paths.SaveDirectory,"state.save")),"Host does not create a session save "+profile);
                var face=host.Library.WrapperFront;var clip=host.Library.Sound("generate");Check(clip!=null && clip.samples>0,"Configured materials retain original generic foley "+profile);
                host.Shutdown();Check(ui.Media==null && face==null && clip==null && host.Library==null,"Host detach destroys owned resources "+profile);
            } finally {if(host!=null)host.Shutdown();game.Shutdown();UnityEngine.Object.DestroyImmediate(go);}
        }
    }
    static void BudgetHost() {
        string root=Path.GetDirectoryName(Application.dataPath);
        var catalogText=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json");
        var cat=Catalog.FromData(JsonUtility.FromJson<CatalogData>(catalogText.text));
        var entries=new Entry[4];
        for(int n=0;n<4;n++) {
            var t=new Texture2D(2016,2816,TextureFormat.RGBA32,false);byte[] bytes;
            try {var p=new Color32[2016*2816];for(int i=0;i<p.Length;i++)p[i]=new Color32((byte)(40+n),100,160,255);t.SetPixels32(p);t.Apply();bytes=t.EncodeToPNG();}finally{UnityEngine.Object.DestroyImmediate(t);}
            string relative="Assets/Supplied/host-budget-"+n+".png";File.WriteAllBytes(Path.Combine(root,relative),bytes);
            entries[n]=new Entry {role="card-face",cardId=cat.Cards[n].Id,relativePath=relative,sha256=Hash(bytes)};
        }
        string manifest="Assets/Supplied/host-budget.json";File.WriteAllText(Path.Combine(root,manifest),JsonUtility.ToJson(new Manifest {assets=entries}));
        foreach(bool full in new[]{false,true}) {
            string dir=Path.Combine(root,".artifacts/host-budget-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            var go=new GameObject("Explicit host texture budget fixture");var game=go.AddComponent<OpeningGame>();Phase3MediaHost host=null;
            try {
                game.allowedProjectRoot=root;game.catalogJson=catalogText;game.modelJson=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json");
                game.Initialize(new[]{"--vcc-smoke=ready","--vcc-save-root="+Path.Combine(dir,"saves"),"--vcc-artifacts="+dir});
                host=go.AddComponent<Phase3MediaHost>();Field("manifestRelativePath").SetValue(host,manifest);Field("fullBaseSetBudget").SetValue(host,full);host.Initialize(game);
                Check(host.Library!=null,"Budget host optional library usable full="+full);
                Check(host.Library.SuppliedAssetCount==(full?4:2),"Only explicit trusted host full budget admits four unique large faces full="+full+" count="+host.Library.SuppliedAssetCount);
            }finally{if(host!=null)host.Shutdown();game.Shutdown();UnityEngine.Object.DestroyImmediate(go);}
        }
    }
    static void UnsafeConfiguration() {
        string root=Path.GetDirectoryName(Application.dataPath);
        foreach(string path in new[]{"../Assets/Supplied/AssetManifest.json","https://untrusted.invalid/media.json",".artifacts/untrusted.json","Assets/Supplied/missing.json"}) {
            string dir=Path.Combine(root,".artifacts/unsafe-config-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            var go=new GameObject("Optional unsafe configuration fixture");var game=go.AddComponent<OpeningGame>();Phase3MediaHost host=null;
            try {
                game.allowedProjectRoot=root;game.catalogJson=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json");game.modelJson=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json");
                game.Initialize(new[]{"--vcc-smoke=ready","--vcc-save-root="+Path.Combine(dir,"saves"),"--vcc-artifacts="+dir});
                var session=game.CurrentSession;host=go.AddComponent<Phase3MediaHost>();Field("manifestRelativePath").SetValue(host,path);Field("fullBaseSetBudget").SetValue(host,true);host.Initialize(game);
                Check(host.Library!=null && host.Library.SuppliedAssetCount==0 && host.Library.Warnings.Count>0 && !host.UsesTestManifest,"Unsafe/missing configured manifest remains optional and does not widen path policy "+path);
                Check(game.CurrentView.stage=="ready" && ReferenceEquals(session,game.CurrentSession) && game.GeneratedResultCount==0 && go.GetComponents<AudioSource>().Length==0 && host.Library.Sound("next")!=null,"Optional configuration failure retains usable game and generic foley without sound "+path);
            }finally{if(host!=null)host.Shutdown();game.Shutdown();UnityEngine.Object.DestroyImmediate(go);}
        }
    }
    static void ActualManifestHost() {
        string root=Path.GetDirectoryName(Application.dataPath),manifest="Assets/Supplied/BaseSetMaterials/Manifest.json";
        if(!File.Exists(Path.Combine(root,manifest))) {checks.Add("Actual supplied host inputs unavailable; synthetic host checks only");return;}
        string dir=Path.Combine(root,".artifacts/actual-host-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        var go=new GameObject("Actual private supplied materials host fixture");var game=go.AddComponent<OpeningGame>();Phase3MediaHost host=null;string transaction=null,saveHash=null;int expectedCount=0;
        try {
            game.allowedProjectRoot=root;game.catalogJson=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json");game.modelJson=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json");
            game.InitializeForProfile(new[]{"--vcc-smoke=ready","--vcc-save-root="+Path.Combine(dir,"saves"),"--vcc-artifacts="+dir},"a4d5cc3004c04deaa20883120379cf31");
            var session=game.CurrentSession;host=go.AddComponent<Phase3MediaHost>();Field("manifestRelativePath").SetValue(host,manifest);Field("fullBaseSetBudget").SetValue(host,true);host.Initialize(game);
            var expected=JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(root,manifest)));
            Check(expected.assets.Length>0 && host.Library!=null && host.Library.SuppliedAssetCount==expected.assets.Length && host.Library.Warnings.Count==0,"Actual full supplied manifest binds every declared entry through trusted host flag count="+(host.Library==null?-1:host.Library.SuppliedAssetCount));
            Check(!host.UsesTestManifest && game.GeneratedResultCount==0 && ReferenceEquals(session,game.CurrentSession) && go.GetComponents<AudioSource>().Length==0 && go.GetComponent<TabletopPresentation>().CommittedCueCount==0,"Actual supplied host binding is silent and passive in collection profile");
            var metallic=host.Library as IWrapperMetallicMedia;Check(metallic!=null && metallic.WrapperFrontMask!=null && metallic.WrapperBackMask!=null,"Actual front/back metallic masks exposed without changing base provider interface");
            expectedCount=expected.assets.Length;
            // Deliberate fixture action only, never host startup: establish a real isolated restore target.
            game.GenerateForSmoke(12345);transaction=game.CurrentSession.CurrentPack.TransactionId;
            Check(game.GeneratedResultCount==1 && game.CurrentSession.TotalVirtualCards==11 && go.GetComponent<TabletopPresentation>().CommittedCueCount==1,"Explicit isolated fixture Generate commits once with existing generic foley");
            saveHash=Hash(File.ReadAllBytes(Path.Combine(game.Paths.SaveDirectory,"state.save")));
            var source=go.GetComponent<TabletopPresentation>().FoleySource;
            var front=host.Library.WrapperFront;var back=host.Library.CardBack;host.Shutdown();Check(front==null && back==null && source.clip==null && !source.isPlaying,"Actual supplied host resources disposed after source detach/stop");
        }finally{if(host!=null)host.Shutdown();game.Shutdown();UnityEngine.Object.DestroyImmediate(go);}
        go=new GameObject("Actual supplied restored controller fixture");game=go.AddComponent<OpeningGame>();host=null;
        try {
            game.allowedProjectRoot=root;game.catalogJson=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json");game.modelJson=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json");
            game.InitializeForProfile(new[]{"--vcc-smoke=ready","--vcc-save-root="+Path.Combine(dir,"saves"),"--vcc-artifacts="+dir},"a4d5cc3004c04deaa20883120379cf31");
            host=go.AddComponent<Phase3MediaHost>();Field("manifestRelativePath").SetValue(host,manifest);Field("fullBaseSetBudget").SetValue(host,true);host.Initialize(game);
            var ui=go.GetComponent<TabletopPresentation>();
            Check(game.CurrentView.stage=="sealed" && game.CurrentSession.CurrentPack.TransactionId==transaction && game.CurrentSession.TotalVirtualCards==11 && game.GeneratedResultCount==0,"Actual supplied profile restores exact saved pack without generation/reaward");
            Check(host.Library.SuppliedAssetCount==expectedCount && ui.CommittedCueCount==0 && go.GetComponents<AudioSource>().Length==0 && saveHash==Hash(File.ReadAllBytes(Path.Combine(game.Paths.SaveDirectory,"state.save"))),"Restored full materials bind without audio or save-byte mutation");
        }finally{if(host!=null)host.Shutdown();game.Shutdown();UnityEngine.Object.DestroyImmediate(go);}
    }
    public static void Run() {
        checks.Clear(); string failure=null; GameObject go=null;
        try {
            go=new GameObject("Isolated trusted host configuration fixture");
            var host=go.AddComponent<Phase3MediaHost>();
            var manifest=Field("manifestRelativePath");
            Check(manifest!=null && manifest.FieldType==typeof(string),"Trusted serialized manifest field exists");
            Check(manifest.IsPublic || Attribute.IsDefined(manifest,typeof(SerializeField)),"Manifest field is Unity serialized");
            Check((string)manifest.GetValue(host)=="Assets/Supplied/AssetManifest.json","Old scenes default to unchanged empty manifest");
            var budget=Field("fullBaseSetBudget");
            Check(budget!=null && budget.FieldType==typeof(bool),"Explicit serialized full set budget exists");
            Check(budget.IsPublic || Attribute.IsDefined(budget,typeof(SerializeField)),"Budget field is Unity serialized");
            Check(!(bool)budget.GetValue(host),"Old scenes do not elevate texture budget");
            Check(host.Library==null && go.GetComponents<AudioSource>().Length==0,"Configuration alone creates no resources or automatic sound");
            UnityEngine.Object.DestroyImmediate(go); go=null;
            ConfiguredHost();
            BudgetHost();
            UnsafeConfiguration();
            ActualManifestHost();
        } catch(Exception e) {failure=e.ToString(); Debug.LogError(e);}
        finally { if(go!=null) UnityEngine.Object.DestroyImmediate(go); }
        File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("MATERIALS_RENDER_OUT"),"results.json"),JsonUtility.ToJson(new Report {passed=failure==null,checkCount=checks.Count,checks=checks.ToArray(),failure=failure},true));
        EditorApplication.Exit(failure==null?0:1);
    }
}
