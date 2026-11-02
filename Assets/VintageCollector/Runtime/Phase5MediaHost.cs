using System;
using System.IO;
using UnityEngine;
using VintageCollector.Domain;
using VintageCollector.Media;
using VintageCollector.Presentation;
namespace VintageCollector.Runtime {
 // Optional resource/selector owner. Never generates, acknowledges, reloads a store or plays a clip.
 [DefaultExecutionOrder(-100)]
 [DisallowMultipleComponent]
 public sealed class Phase5MediaHost:MonoBehaviour {
  public string manifestRelativePath="Assets/Supplied/BaseSetMaterials/Manifest.json";
  public bool fullBaseSetBudget=true;
  public NativeBaseMediaProfile nativeBaseMediaProfile;
  public NativeBaseMediaProvider NativeBaseProvider {get;private set;}
  public bool IsReady {get {return !terminal && Adapter!=null;} }
  public SuppliedCardArtProfile[] suppliedCardArtProfiles=new SuppliedCardArtProfile[0];
  public PresentationMediaLibrary Library {get;private set;}
  public Phase5PresentationMedia Adapter {get;private set;}
  public string StatusMessage {get;private set;}="Optional Phase5 media awaiting admitted configuration.";
  private OpeningGame game;
  private TabletopPresentation table;
  private SetSelectorPresentation selector;
  private bool terminal,attempted;
  private void Start(){Initialize(GetComponent<OpeningGame>());}
  private void Update(){if(!terminal&&!attempted)Initialize(GetComponent<OpeningGame>());}
  public void Initialize(OpeningGame owner) {
   if(terminal||attempted||owner==null||owner.gameObject!=gameObject||owner.ApprovedSets==null||owner.Paths==null||owner.CurrentSession==null||owner.CurrentView==null||owner.CurrentView.stage=="error")return;
   table=GetComponent<TabletopPresentation>();if(table==null)return;
   var configs=owner.GetPhase5PresentationConfiguration();
   attempted=true;game=owner;
   try {
    if(nativeBaseMediaProfile!=null) {
     try {NativeBaseProvider=new NativeBaseMediaProvider(nativeBaseMediaProfile,id=>owner.ApprovedSets.FindCard(id).PrintingId,fullBaseSetBudget?NativeBaseMediaProvider.PixelLimit:PresentationMediaLibrary.TotalTexturePixelLimit);} catch(Exception) {NativeBaseProvider=null;}
    }
    if(NativeBaseProvider==null) {
    string root=ValidatedRoot(owner);
    Library=PresentationMediaLibrary.Load(root,owner.ApprovedSets.FindSet(Phase5InputPolicy.BasePrintingId).Catalog,manifestRelativePath,false,fullBaseSetBudget);
    }
   } catch(Exception) {
    // Image absence/failure must not suppress independently configured borrowed native audio.
    StatusMessage="Optional Base images unavailable; native reveal profiles and generic visuals remain available.";
   }
   try {
    var registry=owner.ApprovedSets;
    Func<string,string> resolver=delegate(string cardId) {
     if(string.IsNullOrEmpty(cardId))return null;
     try {return registry.FindCard(cardId).PrintingId;}
     catch(System.Collections.Generic.KeyNotFoundException){return null;}
     catch(ArgumentException){return null;}
    };
    SuppliedCardArtMap art=null;
    bool artRejected=false;
    try {art=SuppliedCardArtMap.Create(suppliedCardArtProfiles,resolver,delegate(string cardId) {
     if(string.IsNullOrEmpty(cardId))return false;
     try {return registry.FindCard(cardId).Finish=="holo";}
     catch(System.Collections.Generic.KeyNotFoundException){return false;}
     catch(ArgumentException){return false;}
    });}
    catch(Exception) {artRejected=true;}
    // Optional art refusal must not disable accepted Base images, native audio or controls.
    if(NativeBaseProvider!=null) {
     configs=(Phase5SetConfig[])configs.Clone();
     for(int i=0;i<configs.Length;i++) {
      var c=configs[i];if(c==null||c.printingId!=Phase5InputPolicy.BasePrintingId)continue;
      configs[i]=new Phase5SetConfig {printingId=c.printingId,displayLabel=c.displayLabel,catalogJson=c.catalogJson,modelJson=c.modelJson,revealAudioProfile=c.revealAudioProfile,cardFaceTemplate=c.cardFaceTemplate,cardBack=c.cardBack,wrapperBack=c.wrapperBack,
       wrapperFronts=new[]{NativeBaseProvider.WrapperFrontForVariant(0),NativeBaseProvider.WrapperFrontForVariant(1),NativeBaseProvider.WrapperFrontForVariant(2)}};
     }
    }
    Adapter=new Phase5PresentationMedia((IPresentationMedia)NativeBaseProvider??Library,configs,resolver,art);
    selector=GetComponent<SetSelectorPresentation>();if(selector==null)selector=gameObject.AddComponent<SetSelectorPresentation>();
    selector.Command+=OnSelectorCommand;
    game.ViewPublished+=OnViewPublished;
    game.ShuttingDown+=Shutdown;
    // Context is configured before binding even the initial passive projection. No acknowledgement.
    OnViewPublished(game.CurrentView);
    table.Media=Adapter;
    table.RevealSounds=Adapter;
    StatusMessage=(NativeBaseProvider!=null?NativeBaseProvider.StatusText+" ":Library==null?"Optional Base images unavailable. ":Library.StatusText+" ")+
     (artRejected?"Optional supplied art rejected; templates and native reveal audio retained.":
      art!=null && art.FaceCount>0?"Supplied stamped visual substitutes admitted; missing faces use templates.":"No supplied card faces; templates and native reveal audio retained.");
   } catch(Exception) {
    Shutdown();
    StatusMessage="Phase5 media initialization failed; storage remains unchanged.";
   }
  }
  private void OnSelectorCommand(string command){if(!terminal&&game!=null)game.DispatchCommand(command);}
  private void OnViewPublished(OpeningView view) {
   if(terminal||Adapter==null||view==null)return;
   // A preceding synchronous subscriber may already have published a stricter gate.
   // Use the current owner projection rather than a superseded outer event argument.
   if(game!=null && game.CurrentView!=null)view=game.CurrentView;
   string printing=string.IsNullOrEmpty(view.activePackPrintingId)?view.selectedPrintingId:view.activePackPrintingId;
   Adapter.SetContext(printing,string.IsNullOrEmpty(view.activePackPrintingId)?null:view.transactionId);
   if(selector!=null)selector.View=view;
  }
  private static string ValidatedRoot(OpeningGame owner) {
   string configured=owner.allowedProjectRoot;
   if(string.IsNullOrEmpty(configured)||configured.Length<3||!char.IsLetter(configured[0])||configured[1]!=':'||(configured[2]!='/'&&configured[2]!='\\'))throw new ArgumentException();
   string root=Path.GetFullPath(configured).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
   if(new DriveInfo(Path.GetPathRoot(root)).DriveType==DriveType.Network)throw new ArgumentException();
   string[] args=owner.Paths.IsSmokeTest?new[]{"--vcc-smoke="+owner.Paths.SmokeScenario,"--vcc-save-root="+owner.Paths.BaseSaveDirectory,"--vcc-artifacts="+owner.Paths.ArtifactDirectory}:new string[0];
   var validated=GamePaths.ResolvePhase5(root,Application.dataPath,Application.isEditor,args).WithProfile(owner.Paths.CollectionProfileId);
   if(!string.Equals(validated.BaseSaveDirectory,owner.Paths.BaseSaveDirectory,StringComparison.OrdinalIgnoreCase)||!string.Equals(validated.SaveDirectory,owner.Paths.SaveDirectory,StringComparison.OrdinalIgnoreCase)||!string.Equals(validated.ArtifactDirectory,owner.Paths.ArtifactDirectory,StringComparison.OrdinalIgnoreCase))throw new ArgumentException();
   return root;
  }
  public void Shutdown() {
   if(terminal)return;terminal=true;
   if(game!=null){game.ViewPublished-=OnViewPublished;game.ShuttingDown-=Shutdown;}
   if(selector!=null)selector.Command-=OnSelectorCommand;
   // Native Unity object guard plus exact C# interface ownership. Setters stop the old consumer.
   try {
    if(table!=null) {
     if(ReferenceEquals(table.Media,Adapter))table.Media=null;
     if(ReferenceEquals(table.RevealSounds,Adapter))table.RevealSounds=null;
    }
   } finally {
    Adapter=null;
    try {if(NativeBaseProvider!=null){var native=NativeBaseProvider;NativeBaseProvider=null;native.Dispose();}}
    finally {if(Library!=null){var owned=Library;Library=null;owned.Dispose();}}
   }
   StatusMessage="Phase5 media host shut down.";
  }
  private void OnDestroy(){Shutdown();}
  private void OnApplicationQuit(){Shutdown();}
 }
}
