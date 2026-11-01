using System;using System.Collections.Generic;using UnityEngine;using VintageCollector.Runtime;
namespace VintageCollector.Presentation {
 public sealed class FundDraft {public string Opening="100.00",Suggestion="10.00",Amount="",Note="";public bool Enabled=true;public FundDraft Copy(){return (FundDraft)MemberwiseClone();}}
 public sealed class FundRow {public string Id,Caption;}
 public sealed class FundView {public long Version,DraftVersion;public string ProfileId,Message,Available,Planned;public bool Busy,CanMutate,CanRetry,CanExport;public int PlanPage,HistoryPage;public FundDraft Draft;public FundRow[] Plans=new FundRow[0],History=new FundRow[0];}
 // Passive intent renderer. No store, bank, network, ownership or filesystem authority.
 public sealed class FundPresentation:MonoBehaviour {
  FundView current;FundDraft editing;long draftVersion=-1;GUIStyle label,button,input,toggle;Vector2 scroll;readonly List<string> focus=new List<string>();int control;bool changed;string queuedAction,queuedValue;
  public Func<bool> Authority {get;set;}public event Action<string,string> ActionRequested;public event Action<FundDraft> DraftEdited;
  public void Bind(FundView value){bool opening=current==null;current=value;if(value==null){editing=null;return;}if(opening||draftVersion!=value.DraftVersion){editing=value.Draft.Copy();draftVersion=value.DraftVersion;}if(opening)scroll=Vector2.zero;}
  bool Allowed(){return StartupLoadingCoordinator.IsReady&&current!=null&&Authority!=null&&Authority();}
  public void Emit(string action,string value=null){if(!Allowed())return;var h=ActionRequested;if(h!=null)h(action,value);}
  public void SubmitDraft(FundDraft value){if(!Allowed()||!current.CanMutate||value==null)return;var h=DraftEdited;if(h!=null)h(value.Copy());}
  void Name(){string n="fund-"+(control++);GUI.SetNextControlName(n);if(GUI.enabled)focus.Add(n);}
  void Button(string caption,string action,string value=null){Name();if(GUILayout.Button(caption,button)) {if(changed){SubmitDraft(editing);changed=false;}if(queuedAction==null){queuedAction=action;queuedValue=value;}}}
  string Field(string caption,string value,int limit){GUILayout.Label(caption,label);Name();string next=GUILayout.TextField(value??"",limit,input);if(next!=(value??""))changed=true;return next;}
  void Styles(){if(label!=null)return;label=new GUIStyle(GUI.skin.label){richText=false,wordWrap=true,fontSize=20};button=new GUIStyle(GUI.skin.button){richText=false,wordWrap=true,fontSize=20};input=new GUIStyle(GUI.skin.textField){richText=false,fontSize=20};toggle=new GUIStyle(GUI.skin.toggle){richText=false,wordWrap=true,fontSize=20};}
  void OnGUI(){queuedAction=null;queuedValue=null;if(!Allowed())return;Styles();var v=current;bool oldEnabled=GUI.enabled;var oldColor=GUI.color;var oldMatrix=GUI.matrix;int oldDepth=GUI.depth;GUI.depth=-2000;GUI.enabled=true;GUI.color=Color.white;
   GUI.matrix=Matrix4x4.identity;GUI.color=new Color(0.06f,0.13f,0.11f,1);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;
   float scale=Math.Min(Screen.width/1600f,Screen.height/900f);GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1));
   focus.Clear();control=0;changed=false;
   try {
    if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape){Event.current.Use();Emit("close");return;}
    GUILayout.BeginArea(new Rect(60,30,1480,840));GUILayout.BeginVertical(GUI.skin.box);
    GUILayout.BeginHorizontal();GUILayout.Label("COLLECTION FUND  /  Profile "+v.ProfileId,label);Button("Close","close");Button("Quit","quit");GUILayout.EndHorizontal();
    GUILayout.Label("MANUAL RECORD ONLY — Bank not connected. No money moved. Purchases do not change physical ownership.",label);
    GUILayout.Label("Recorded available: USD "+v.Available+"    Planned (not cash): USD "+v.Planned,label);GUILayout.Label((v.Busy?"BUSY — last acknowledged balance. Close is available. ":"")+(v.Message??""),label);
    scroll=GUILayout.BeginScrollView(scroll,GUILayout.Height(650));
    GUI.enabled=v.CanMutate;GUILayout.Label("Configuration • applies to future opened-pack suggestions",label);
    GUILayout.BeginHorizontal();GUILayout.BeginVertical();editing.Opening=Field("Opening balance (USD)",editing.Opening,24);GUILayout.EndVertical();GUILayout.BeginVertical();editing.Suggestion=Field("Suggestion per newly opened pack (USD)",editing.Suggestion,24);GUILayout.EndVertical();GUILayout.EndHorizontal();
    Name();bool enabled=GUILayout.Toggle(editing.Enabled,"Enable future pack suggestions (planned only)",toggle);if(enabled!=editing.Enabled){editing.Enabled=enabled;changed=true;}Button("Save configuration (opening changes append audit corrections)","configure");
    GUILayout.Label("Pending plans • page "+v.PlanPage+" • confirm only when you choose to record a manual contribution",label);
    foreach(var row in v.Plans){GUILayout.BeginHorizontal();GUILayout.Label(row.Caption,label);Button("Confirm","confirm",row.Id);Button("Cancel plan","cancel",row.Id);GUILayout.EndHorizontal();}
    GUI.enabled=true;GUILayout.BeginHorizontal();Button("Previous plans","plans-prev");Button("Next plans","plans-next");GUILayout.EndHorizontal();
    GUI.enabled=v.CanMutate;editing.Amount=Field("Manual amount USD (correction allows leading minus)",editing.Amount,24);editing.Note=Field("Audit note • required for manual entries",editing.Note,500);
    GUILayout.BeginHorizontal();Button("Record deposit","deposit");Button("Record purchase","purchase");Button("Append correction","adjust");GUILayout.EndHorizontal();
    GUI.enabled=true;GUILayout.Label("Append-only history • newest first • page "+v.HistoryPage,label);foreach(var row in v.History)GUILayout.Label(row.Caption,label);
    GUILayout.BeginHorizontal();Button("Previous history","history-prev");Button("Next history","history-next");GUILayout.EndHorizontal();GUILayout.EndScrollView();
    GUILayout.BeginHorizontal();GUI.enabled=v.CanRetry;Button("Retry exact intent / reload","retry");GUI.enabled=v.CanExport;Button("Export acknowledged CSV locally","csv");GUI.enabled=true;GUILayout.EndHorizontal();
    GUILayout.EndVertical();GUILayout.EndArea();if(changed)SubmitDraft(editing);
    if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Tab&&focus.Count>0){int i=focus.IndexOf(GUI.GetNameOfFocusedControl());i=(i+(Event.current.shift?-1:1)+focus.Count)%focus.Count;GUI.FocusControl(focus[i]);Event.current.Use();}
   }finally{GUI.enabled=oldEnabled;GUI.color=oldColor;GUI.matrix=oldMatrix;GUI.depth=oldDepth;}
   string action=queuedAction,value=queuedValue;queuedAction=null;queuedValue=null;if(action!=null)Emit(action,value);
  }
 }
}
