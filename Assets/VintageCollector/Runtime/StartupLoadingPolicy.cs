using System;
namespace VintageCollector.Runtime {
 public enum StartupStage { LoadingScene, Preparing, Complete, Failed }
 public sealed class StartupLoadingPolicy {
  private readonly double deadline;
  public StartupLoadingPolicy(double now,double timeout){
   if(double.IsNaN(now)||double.IsInfinity(now)||double.IsNaN(timeout)||double.IsInfinity(timeout)||timeout<=0)throw new ArgumentOutOfRangeException("timeout");
   deadline=now+timeout;
  }
  public StartupStage Stage {get;private set;}
  public float SceneProgress {get;private set;}
  public bool ShowPercentage {get{return Stage==StartupStage.LoadingScene;}}
  public string Error {get;private set;}
  public void ObserveLoad(float progress,bool done,double now){
   Tick(now);if(Stage!=StartupStage.LoadingScene)return;
   if(!float.IsNaN(progress)&&!float.IsInfinity(progress))SceneProgress=Math.Max(SceneProgress,Math.Max(0,Math.Min(1,progress/.9f)));
   if(done){SceneProgress=1;Stage=StartupStage.Preparing;}
  }
  public void ObserveReady(bool valid,bool collection,bool adapter,bool bound,string error,double now){
   Tick(now);if(Stage!=StartupStage.Preparing&&Stage!=StartupStage.Complete)return;
   if(!string.IsNullOrEmpty(error)){Fail(error);return;}
   Stage=valid&&collection&&adapter&&bound?StartupStage.Complete:StartupStage.Preparing;
  }
  public void Tick(double now){if(Stage!=StartupStage.Complete&&Stage!=StartupStage.Failed&&now>=deadline)Fail("Startup deadline exceeded. Quit and inspect the player log; no storage retry was attempted.");}
  public void Fail(string error){if(Stage==StartupStage.Failed)return;Error=error;Stage=StartupStage.Failed;}
 }
 // Capture each exact component once, including initially-disabled components.
 public sealed class StartupUiLease<T> where T:class {
  private readonly Func<T,bool> get;private readonly Action<T,bool> set;
  private readonly System.Collections.Generic.Dictionary<T,bool> states=new System.Collections.Generic.Dictionary<T,bool>();
  public StartupUiLease(Func<T,bool> get,Action<T,bool> set){this.get=get;this.set=set;}
  public void Suppress(T item){if(item==null)return;if(!states.ContainsKey(item))states.Add(item,get(item));set(item,false);}
  public void Restore(){
   var saved=new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<T,bool>>(states);states.Clear();
   var errors=new System.Collections.Generic.List<Exception>();
   foreach(var pair in saved)try{set(pair.Key,pair.Value);}catch(Exception e){errors.Add(e);}
   if(errors.Count!=0)throw new AggregateException("Startup UI rollback failed for one or more components",errors);
  }
 }
}
