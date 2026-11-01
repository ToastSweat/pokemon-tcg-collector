using System;using System.Collections.Generic;using UnityEngine;using VintageCollector.Runtime;
namespace VintageCollector.Presentation {
 // Passive, detached rendering only. No stores, URIs, clipboard, or platform actions.
 public sealed class ShoppingPresentation:MonoBehaviour {
  ShoppingView current;ShoppingDraft editing;long version=-1;Vector2 listScroll,editorScroll;GUIStyle label,button,input,toggle;readonly List<string> focus=new List<string>();int control;bool changed;
  public Func<bool> Authority{get;set;}public event Action<string,string> ActionRequested;public event Action<ShoppingDraft> DraftEdited;
  public ShoppingView Current{get{return current;}}
  public void Bind(ShoppingView view){current=view;if(view==null){editing=null;version=-1;return;}if(view.Version!=version){editing=view.Draft;version=view.Version;}}
  bool Allowed(){return StartupLoadingCoordinator.IsReady&&current!=null&&current.CanAct&&Authority!=null&&Authority();}
  public void Emit(string action,string value=null){if(action!="close"&&action!="quit"&&!Allowed())return;var handler=ActionRequested;if(handler!=null)handler(action,value);}
  public void SubmitDraft(ShoppingDraft d){if(!Allowed()||!current.CanMutate)return;var h=DraftEdited;if(h!=null)h(d.Copy());}
  public static Rect PanelBounds(int width,int height){float scale=Math.Min(width/1600f,height/900f);return new Rect(80*scale,60*scale,1440*scale,780*scale);}
  void Name(){string n="shopping-"+(control++);focus.Add(n);GUI.SetNextControlName(n);}
  void Button(string text,string command,string value=null){Name();if(GUILayout.Button(text,button))Emit(command,value);}
  string Field(string caption,string value,int max){GUILayout.Label(caption,label);Name();string next=GUILayout.TextField(value??"",max,input);if(next!=(value??""))changed=true;return next;}
  bool Toggle(string caption,bool value){Name();bool next=GUILayout.Toggle(value,caption,toggle);if(next!=value)changed=true;return next;}
  string Review(string caption,string value,string expected,string other){Name();string display=string.IsNullOrEmpty(value)?"Unknown":value;if(GUILayout.Button(caption+": "+display+" (click to choose)",button)){changed=true;return string.IsNullOrEmpty(value)?expected:value==expected?other:null;}return value;}
  void Styles(){if(label!=null)return;label=new GUIStyle(GUI.skin.label){richText=false,wordWrap=true,clipping=TextClipping.Clip};button=new GUIStyle(GUI.skin.button){richText=false,wordWrap=true,clipping=TextClipping.Clip};input=new GUIStyle(GUI.skin.textField){richText=false,clipping=TextClipping.Clip};toggle=new GUIStyle(GUI.skin.toggle){richText=false,wordWrap=true,clipping=TextClipping.Clip};}
  void OnGUI(){if(!Allowed())return;Styles();var view=current;var r=PanelBounds(Screen.width,Screen.height);focus.Clear();control=0;changed=false;
   if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape){Event.current.Use();Emit("close");return;}
   GUILayout.BeginArea(r);GUILayout.BeginVertical(GUI.skin.box);GUILayout.BeginHorizontal();GUILayout.Label("FIND PHYSICAL CARD • Manual comparison",label);Button("Close","close");Button("Quit","quit");GUILayout.EndHorizontal();
   var w=view.Wanted;GUILayout.Label(w.DisplayName+"  #"+w.CollectorNumber+"/"+w.CollectorTotal+"  •  "+w.SetName+"  • English • Unlimited • "+w.Finish+" • Raw Near Mint • USD",label);
   GUILayout.Label("UNVERIFIED CANDIDATES — user-entered asking-price snapshots, not live stock, independent verification, or cheapest-price advice.",label);
   GUILayout.BeginHorizontal();Button("Search eBay US","search","ebay");Button("Search TCGplayer","search","tcgplayer");Button("Copy exact description","copy");Button("Reload notes","reload");GUILayout.EndHorizontal();
   GUILayout.Label(view.NotesLabel+" • "+view.Message,label);
   GUILayout.BeginHorizontal();GUILayout.BeginVertical(GUILayout.Width(r.width*.34f));
   listScroll=GUILayout.BeginScrollView(listScroll,GUILayout.Height(Math.Max(100,r.height-260)));GUILayout.Label("Saved manual comparisons",label);foreach(var row in view.Records){Button(row.Caption,"select",row.Id);GUILayout.Label(row.Status+" — "+row.Summary,label);}GUILayout.EndScrollView();
   bool was=GUI.enabled;GUI.enabled=was&&view.CanMutate;Button("New note","new");GUI.enabled=was;GUILayout.EndVertical();GUILayout.BeginVertical();
   editorScroll=GUILayout.BeginScrollView(editorScroll,GUILayout.Height(Math.Max(100,r.height-260)));
   if(editing==null)GUILayout.Label("Select a saved record or New note. Merely opening this panel contacts no provider.",label);
   else {var d=editing.Data;var m=d.rawNearMintUSD;var review=d.review;GUI.enabled=was&&view.CanMutate;
    Name();if(GUILayout.Button("Provider: "+d.provider+" (change)",button)){d.provider=d.provider=="ebay"?"tcgplayer":"ebay";changed=true;}
    d.listingUrl=Field("Exact listing URL (no repair or tracking removal)",d.listingUrl,2048);d.title=Field("Listing title (not evidence)",d.title,300);d.seller=Field("Seller",d.seller,200);d.note=Field("Private manual note",d.note,2000);d.observedUtc=Field("Observed UTC: yyyy-MM-ddTHH:mm:ssZ",d.observedUtc,20);
    m.priceKnown=Toggle("Asking price known (USD)",m.priceKnown);editing.Price=Field("Asking price",editing.Price,30);m.shippingKnown=Toggle("Shipping known",m.shippingKnown);editing.Shipping=Field("Shipping",editing.Shipping,30);m.taxKnown=Toggle("Tax known",m.taxKnown);editing.Tax=Field("Tax",editing.Tax,30);m.feesKnown=Toggle("Fees known",m.feesKnown);editing.Fees=Field("Fees",editing.Fees,30);
    GUILayout.Label("Review observations: click each field through Unknown / wanted / mismatch. Never inferred from title or image.",label);
    review.language=Review("Language",review.language,"en","ja");review.setId=Review("Set",review.setId,w.SetId,w.SetId=="base"?"jungle":"base");review.collectorNumber=Review("Collector number",review.collectorNumber,w.CollectorNumber,"different");
    string total=Review("Printed total",review.collectorTotal==0?null:review.collectorTotal.ToString(),w.CollectorTotal.ToString(),"999");review.collectorTotal=string.IsNullOrEmpty(total)?0:int.Parse(total);
    review.edition=Review("Edition",review.edition,"unlimited","first-edition");review.finish=Review("Finish",review.finish,w.Finish,w.Finish=="holo"?"nonholo":"holo");review.grading=Review("Grading",review.grading,"raw-ungraded","graded");review.condition=Review("Condition",review.condition,"Near Mint","Played");review.manualReviewed=Toggle("I personally checked listing details",review.manualReviewed);GUI.enabled=was;
   }
   GUILayout.EndScrollView();GUILayout.BeginHorizontal();GUI.enabled=was&&view.CanMutate&&editing!=null;Button("Save note","save");Button("Delete…","delete");GUI.enabled=was&&view.CanRetry;Button("Retry exact write","retry");GUI.enabled=was&&editing!=null;Button("Open listing","listing");GUI.enabled=was;GUILayout.EndHorizontal();
   if(view.ConfirmDelete){GUILayout.BeginHorizontal();Button("Confirm delete saved note","delete-confirm");Button("Cancel","delete-cancel");GUILayout.EndHorizontal();}
   GUILayout.EndVertical();GUILayout.EndHorizontal();GUILayout.Label("IMPORTANT: collection ZIP backups do NOT include shopping notes. Unknown amounts are not zero; only all-known components yield a full total. Closing retains drafts and any uncertain exact write in this session.",label);GUILayout.EndVertical();GUILayout.EndArea();
   if(changed&&editing!=null)SubmitDraft(editing);
   if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Tab&&focus.Count>0){int i=focus.IndexOf(GUI.GetNameOfFocusedControl());i=(i+(Event.current.shift?-1:1)+focus.Count)%focus.Count;GUI.FocusControl(focus[i]);Event.current.Use();}
  }
 }
}
