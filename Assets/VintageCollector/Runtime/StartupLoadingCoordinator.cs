using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using VintageCollector.Presentation;
namespace VintageCollector.Runtime {
 // Dedicated asset-free root in the future lightweight boot scene. No store authority.
 [DefaultExecutionOrder(-10000)]
 [DisallowMultipleComponent]
 public sealed class StartupLoadingCoordinator:MonoBehaviour {
  public string heavySceneName="BootstrapPerformance";
  public float startupDeadlineSeconds=180f;
  public static bool IsReady {get;private set;}
  public static bool IsRunning {get{return instance!=null;}}
  public string Failure {get{return policy==null?configurationError:policy.Error;}}
  public string Warning {get;private set;}
  private static StartupLoadingCoordinator instance;
  private StartupLoadingPolicy policy;
  private string configurationError;
  private AsyncOperation operation;
  private OpeningGame game;
  private bool painted,started,finished,subscribed,completionPainted,quitRequested,applicationQuitting;
  private int paintFrame,completionFrame;
  private bool completing,interrupted;
  private int sceneGeneration;
  private readonly System.Collections.Generic.Dictionary<Behaviour,bool> ui=new System.Collections.Generic.Dictionary<Behaviour,bool>();
  private readonly System.Collections.Generic.HashSet<GameObject> gatedRoots=new System.Collections.Generic.HashSet<GameObject>();
  private void Awake(){
   if(instance!=null&&instance!=this){enabled=false;Destroy(this);return;}
   instance=this;IsReady=false;DontDestroyOnLoad(gameObject);
   try {
    policy=new StartupLoadingPolicy(Time.realtimeSinceStartupAsDouble,startupDeadlineSeconds);
    if(string.IsNullOrEmpty(heavySceneName)||heavySceneName==gameObject.scene.name)throw new InvalidOperationException();
    SceneManager.sceneLoaded+=OnSceneLoaded;subscribed=true;
   } catch(Exception){configurationError="Startup configuration invalid. Quit and correct the boot scene.";Fail(configurationError);}
  }
  private void OnSceneLoaded(Scene scene,LoadSceneMode mode){
   if(!ReferenceEquals(instance,this)||finished||scene.name!=heavySceneName)return;
   sceneGeneration++;
   bool fault=false;
   foreach(var root in scene.GetRootGameObjects()){
    // Suppress independently of owner validation, including every late failure activation.
    try{Suppress(root);}catch(Exception){fault=true;}
    foreach(var owner in root.GetComponentsInChildren<OpeningGame>(true)){
     if(game!=null&&game!=owner)fault=true;else game=owner;
    }
   }
   if(fault){Fail("Startup input gate could not be established. Quit and inspect scene configuration.");RequestQuit();}
  }
  private void Suppress(GameObject root){
   gatedRoots.Add(root);
   var inputs=new System.Collections.Generic.List<Behaviour>();
   inputs.AddRange(root.GetComponentsInChildren<TabletopPresentation>(true));
   inputs.AddRange(root.GetComponentsInChildren<BinderPresentation>(true));
   inputs.AddRange(root.GetComponentsInChildren<SetSelectorPresentation>(true));
   inputs.AddRange(root.GetComponentsInChildren<ShoppingPresentation>(true));
   var errors=new System.Collections.Generic.List<Exception>();
   foreach(var b in inputs)try{if(b!=null){if(!ui.ContainsKey(b))ui.Add(b,b.enabled);b.enabled=false;}}catch(Exception e){errors.Add(e);}
   if(errors.Count!=0)throw new AggregateException(errors);
  }
  private void Update(){
   if(instance!=this||finished||applicationQuitting||completing)return;
   try {
    if(game!=null)Suppress(game.gameObject); // Retain the fence for host-added inputs after failure too.
    if(Failure!=null)return;
    policy.Tick(Time.realtimeSinceStartupAsDouble);
    if(policy.Stage==StartupStage.Failed){Fail(policy.Error);return;}
    // Actual Repaint, then a later frame, before beginning heavy dependency loading.
    if(!started){
     if(!painted||Time.frameCount<=paintFrame)return;
     started=true;
     if(!Application.CanStreamedLevelBeLoaded(heavySceneName)){Fail("Required gameplay scene is not in this player.");return;}
     operation=SceneManager.LoadSceneAsync(heavySceneName,LoadSceneMode.Single);
     if(operation==null){Fail("Unity did not create the scene loading operation.");return;}
    }
    policy.ObserveLoad(operation.progress,operation.isDone,Time.realtimeSinceStartupAsDouble);
    if(game!=null)Suppress(game.gameObject); // Also capture host-added selector/binder.
    if(policy.Stage==StartupStage.Preparing||policy.Stage==StartupStage.Complete){
     ObserveReadiness();
    }
    if(policy.Stage!=StartupStage.Complete)completionPainted=false;
    if(policy.Stage==StartupStage.Failed){Fail(policy.Error);return;}
    // Smoke/capture probes may proceed only after completion repaint AND restored input.
    if(policy.Stage==StartupStage.Complete&&completionPainted&&Time.frameCount>completionFrame){
     if(!string.IsNullOrEmpty(Warning))Debug.LogWarning(Warning);
     CompleteHandoff();
    }
   } catch(Exception){Fail("Startup failed while loading or checking readiness. Quit and inspect the player log.");RequestQuit();}
  }
  private void ObserveReadiness(){
     var collection=game==null?null:game.GetComponent<Phase5CollectionHost>();
     var media=game==null?null:game.GetComponent<Phase5MediaHost>();
     var table=game==null?null:game.GetComponent<TabletopPresentation>();
     bool valid=game!=null&&game.CurrentSession!=null&&game.Paths!=null&&game.ApprovedSets!=null&&game.CurrentView!=null&&game.CurrentView.stage!="error";
     string error=game!=null&&game.CurrentView!=null&&game.CurrentView.stage=="error"?"Game startup failed. Open the game error screen for its existing Retry, or Quit.":null;
     bool degraded=media!=null&&media.Adapter==null&&media.StatusMessage=="Phase5 media initialization failed; storage remains unchanged.";
     bool adapter=media!=null&&media.Adapter!=null;
     bool bound=table!=null&&adapter&&ReferenceEquals(table.Media,media.Adapter);
     // Library is optional for native providers. Inspect actual bound common-back availability only.
     Warning=valid&&collection!=null&&collection.IsReady&&(degraded||(adapter&&bound&&media.Adapter.CardBack==null))
      ?"Optional artwork unavailable; continuing with existing generic visuals. "+media.StatusMessage:null;
     policy.ObserveReady(valid,collection!=null&&collection.Game==game&&collection.IsReady,adapter||degraded,bound||(degraded&&table!=null),error,Time.realtimeSinceStartupAsDouble);
  }
  private void RestoreInputs(System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<Behaviour,bool>> saved){
   var errors=new System.Collections.Generic.List<Exception>();
   foreach(var pair in saved)try{if(pair.Key!=null)pair.Key.enabled=pair.Value;}catch(Exception e){errors.Add(e);}
   if(errors.Count!=0)throw new AggregateException("Startup UI restoration failed",errors);
  }
  private bool CanComplete(){
   return ReferenceEquals(instance,this)&&this!=null&&enabled&&!interrupted&&!finished&&!applicationQuitting&&!quitRequested&&Failure==null&&policy!=null&&policy.Stage==StartupStage.Complete;
  }
  private void CompleteHandoff(){
   if(!CanComplete())return;
   completing=true;
   var saved=new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<Behaviour,bool>>(ui);
   int generation=sceneGeneration;ui.Clear();
   bool restored=false;
   try {
    // Keep sceneLoaded subscribed while setters can synchronously reenter lifecycle/scene callbacks.
    RestoreInputs(saved);restored=true;
    if(CanComplete())ObserveReadiness(); // Re-read live game, collection, media and binding after all setters.
    if(restored&&CanComplete()&&generation==sceneGeneration&&ui.Count==0){
     if(subscribed){SceneManager.sceneLoaded-=OnSceneLoaded;subscribed=false;}
     finished=true;IsReady=true;Destroy(gameObject);return;
    }
   } catch(Exception){Fail("Startup UI restoration failed. Input remains disabled; Quit and inspect the player log.");}
   finally {completing=false;}
   if(applicationQuitting)return; // Normal process shutdown alone owns cleanup, never readiness.
   IsReady=false;completionPainted=false;
   // Restore cleared its ledger before callbacks. Retain original states for later explicit cleanup,
   // then re-fence every restored component and any inputs added by those callbacks.
   foreach(var pair in saved)ui[pair.Key]=pair.Value;
   if(!subscribed){SceneManager.sceneLoaded+=OnSceneLoaded;subscribed=true;}
   bool fault=false;
   foreach(var root in new System.Collections.Generic.List<GameObject>(gatedRoots))try{if(root!=null)Suppress(root);}catch(Exception){fault=true;}
   foreach(var pair in new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<Behaviour,bool>>(ui))try{if(pair.Key!=null)pair.Key.enabled=false;}catch(Exception){fault=true;}
   if(Failure==null)Fail("Startup readiness was interrupted during input restoration. Quit and inspect the player log.");
   if(fault)Debug.LogError("Startup compensating input fence encountered a component setter failure.");
   RequestQuit();
  }
  private void Fail(string message){
   if(policy!=null)policy.Fail(message);else configurationError=message;
   IsReady=false; // Failure is not permission: retain the lease and sceneLoaded fence while visible.
   // Unity has no async scene cancel. Never trap its activation queue at 0.9.
   if(operation!=null)operation.allowSceneActivation=true;
   Debug.LogError(message);
  }
  private void ReleaseGate(){
   if(subscribed){SceneManager.sceneLoaded-=OnSceneLoaded;subscribed=false;}
   var saved=new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<Behaviour,bool>>(ui);ui.Clear();
   try{RestoreInputs(saved);}catch(Exception){Debug.LogError("Startup attempted every UI restoration; a component rejected restoration.");}
  }
  public void RequestQuit(){
   if(!ReferenceEquals(instance,this)||quitRequested||applicationQuitting)return;
   quitRequested=true; // Latch before calling the sole owner: Quit may synchronously reenter lifecycle callbacks.
   try {
    var owners=UnityEngine.Object.FindObjectsByType<OpeningGame>(FindObjectsInactive.Include,FindObjectsSortMode.None);
    if(owners.Length!=0)owners[0].RequestQuit();else Application.Quit();
   } catch(Exception){Fail("Game Quit failed. Inspect the player log; cleanup ownership was not bypassed.");}
  }
  public bool DismissOwnerError(){
   if(instance!=this||finished||applicationQuitting||Failure==null||game==null||game.CurrentView==null||game.CurrentView.stage!="error")return false;
   finished=true;ReleaseGate();Destroy(gameObject);return true;
  }
  private void OnDisable(){
   if(instance!=this||finished||applicationQuitting)return;
   interrupted=true;
   Fail("Startup overlay was disabled before readiness. Quitting through the game owner.");RequestQuit();
  }
  private void OnApplicationQuit(){
   if(instance!=this||applicationQuitting)return;
   applicationQuitting=true;ReleaseGate();if(!finished)IsReady=false;
  }
  private void OnDestroy(){
   if(!ReferenceEquals(instance,this))return;
   if(finished||applicationQuitting){ReleaseGate();instance=null;return;}
   // No input rollback into a valid game. The managed callback/lease remain held until process exit.
   interrupted=true;
   Fail("Startup overlay was destroyed before readiness. Quitting through the game owner.");RequestQuit();
  }
  private void OnGUI(){
   if(instance!=this||finished)return;
   int depth=GUI.depth;Color color=GUI.color;
   try {
    GUI.depth=-10000;GUI.color=new Color(.035f,.055f,.07f,1);
    GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
    GUI.color=new Color(.88f,.93f,.92f,1);
    float width=Math.Min(520,Math.Max(200,Screen.width-40));float x=(Screen.width-width)/2;float y=Screen.height*.42f;
    bool complete=policy!=null&&policy.Stage==StartupStage.Complete;
    string status=Failure!=null?"Startup blocked":complete?"Ready":policy!=null&&policy.ShowPercentage?"Loading gameplay scene":"Preparing collection and artwork";
    GUI.Label(new Rect(x,y,width,30),status);
    if(Failure!=null){
     GUI.Label(new Rect(x,y+35,width,80),Failure);
     if(game!=null&&game.CurrentView!=null&&game.CurrentView.stage=="error"&&GUI.Button(new Rect(x,y+120,width,30),"Show game error / existing Retry"))DismissOwnerError();
    } else if((policy!=null&&policy.ShowPercentage)||complete){
     float progress=complete?1:policy.SceneProgress;
     GUI.color=new Color(.13f,.19f,.22f,1);GUI.DrawTexture(new Rect(x,y+40,width,18),Texture2D.whiteTexture);
     GUI.color=new Color(.32f,.78f,.64f,1);GUI.DrawTexture(new Rect(x,y+40,width*progress,18),Texture2D.whiteTexture);
     GUI.color=new Color(.88f,.93f,.92f,1);
     GUI.Label(new Rect(x,y+66,width,26),complete?"Startup complete":((int)(progress*100))+"% of scene loading (not overall startup)");
    } else GUI.Label(new Rect(x,y+40,width,60),"Waiting for configuration, collection and artwork readiness. No overall percentage is available.");
    if(!string.IsNullOrEmpty(Warning))GUI.Label(new Rect(x,y+165,width,60),Warning);
    if(GUI.Button(new Rect(x,y+235,width,30),"Quit"))RequestQuit();
    if(Event.current.type==EventType.Repaint){
     if(!painted){painted=true;paintFrame=Time.frameCount;}
     if(complete&&!completionPainted){completionPainted=true;completionFrame=Time.frameCount;}
    }
   } catch(Exception){Fail("Startup overlay could not render. Input remains disabled; quitting through the game owner.");RequestQuit();}
   finally {GUI.depth=depth;GUI.color=color;}
  }
 }
}
