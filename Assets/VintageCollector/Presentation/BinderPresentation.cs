using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace VintageCollector.Presentation
{
    /// <summary>Original generic IMGUI overlay. All state and all operations belong to its host.</summary>
    public sealed class BinderPresentation : MonoBehaviour
    {
        private BinderView view;
        private bool pending;
        private string pendingCommand;
        private Font font;
        private GUIStyle label, button, input;
        private Vector2 bodyScroll, backupScroll, reviewScroll, noticeScroll;
        private readonly List<string> focusNames=new List<string>();
        private readonly List<string> previousFocusNames=new List<string>();
        private readonly Dictionary<string,Rect> clientRects=new Dictionary<string,Rect>();
        private readonly HashSet<string> bodyControls=new HashSet<string>();
        private bool drawingBody;
        private bool resetFocus;
        private Vector2 guiOrigin;
        private float guiScale=1;
        private Rect RootRect(Rect r) { return new Rect(r.position+guiOrigin,r.size); }
        public BinderView View {
            get { return view; }
            set {
                if(view==null || value==null || view.mode!=value.mode || view.printingId!=value.printingId || view.page!=value.page || view.search!=value.search || view.filter!=value.filter) bodyScroll=Vector2.zero;
                if(view==null || value==null || view.printingId!=value.printingId || view.open!=value.open) resetFocus=true;
                previousFocusNames.Clear();clientRects.Clear();bodyControls.Clear();
                view=value;pending=false;pendingCommand=null;
            }
        }
        public event Action<string> Command;
        public event Action<string> ShoppingRequested;
        public event Action FundRequested;
        public bool FundAvailable {get;set;}
        public void RequestFund() {
            if(!FundAvailable || ExternalOverlayVisible || pending || view==null || !view.open || view.busy || view.pendingPhysicalEdit || view.pendingImportConfirm || view.restartRequired || BinderLayoutRules.HasReview(view) || !string.IsNullOrEmpty(view.error))return;
            var h=FundRequested;if(h!=null)h();
        }
        private void FundButton(Rect r) {
            bool old=GUI.enabled;GUI.enabled=FundAvailable && !ExternalOverlayVisible && !pending;
            Register("collection-fund",r,GUI.enabled);
            if(GUI.Button(r,"Collection fund",button))RequestFund();GUI.enabled=old;
        }
        public bool ShoppingAvailable { get; set; }
        private bool externalOverlayVisible;
        public bool ExternalOverlayVisible {
            get { return externalOverlayVisible; }
            set { if(externalOverlayVisible==value) return; externalOverlayVisible=value;
                focusNames.Clear();previousFocusNames.Clear();clientRects.Clear();bodyControls.Clear();resetFocus=true; }
        }
        private bool CanRequestShopping(string id) {
            var row=BinderLayoutRules.Selected(view);
            return ShoppingAvailable && !ExternalOverlayVisible && !pending && view!=null && view.open &&
                view.mode=="physical" && !view.busy && !view.pendingPhysicalEdit && !view.pendingImportConfirm &&
                !view.restartRequired && !BinderLayoutRules.HasReview(view) && string.IsNullOrEmpty(view.error) &&
                row!=null && row.id==id && view.selectedCardId==id && !row.owned && row.quantity==0;
        }
        private void RequestShopping(string id) {
            if(!CanRequestShopping(id)) return;
            var handler=ShoppingRequested;if(handler!=null) handler(id);
        }
        private void ShoppingButton(Rect r,string id) {
            bool old=GUI.enabled;GUI.enabled=CanRequestShopping(id);
            Register("find-physical-card",r,GUI.enabled);
            if(GUI.Button(r,"Find physical card",button)) RequestShopping(id);
            GUI.enabled=old;
        }
        public bool IsOpen { get { return view!=null && view.open; } }
        public string QuantityText { get { return view==null?"":Safe(view.quantityText); } }
        // Test observers receive text/geometry actually rendered, not undisplayed DTO fields.
        internal Action<string> TextObserver { get; set; }
        internal Action<string,Rect,bool> ControlObserver { get; set; }
        internal Action<string,Rect,float> TextLayoutObserver { get; set; }
        private static readonly Color Felt=new Color32(20,43,35,255), Panel=new Color32(26,52,43,255), Raised=new Color32(35,66,54,255);
        private static readonly Color Brass=new Color32(222,188,116,255), Paper=new Color32(242,234,211,255), Muted=new Color32(171,194,179,255);
        private static readonly Color Accent=new Color32(124,208,172,255), Error=new Color32(255,185,158,255);
        private static string Safe(string s,string fallback="") { return string.IsNullOrEmpty(s)?fallback:s; }
        private static string N(int n) { return n.ToString(CultureInfo.InvariantCulture); }
        private bool Allowed(string command) { return !ExternalOverlayVisible && (!pending || command=="binder-close" || command=="quit") && BinderLayoutRules.CanEmit(view,command); }
        private void RequestCommand(string command) {
            if(!Allowed(command)) return;
            if(command=="save-physical" || command=="backup" || command=="import-preview" || command=="import-confirm") { pending=true;pendingCommand=command; }
            var handler=Command;if(handler!=null) handler(command);
        }
        private void EnsureStyles() {
            if(font==null) font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(label==null) label=new GUIStyle {font=font,richText=false,wordWrap=true,clipping=TextClipping.Clip};
            if(button==null) {
                button=new GUIStyle(GUI.skin.button) {font=font,fontSize=20,richText=false,wordWrap=true,padding=new RectOffset(10,10,5,5)};
                button.normal.textColor=Paper;button.hover.textColor=Brass;button.focused.textColor=Brass;button.active.textColor=Brass;
            }
            if(input==null) input=new GUIStyle(GUI.skin.textField) {font=font,fontSize=21,richText=false,padding=new RectOffset(10,10,7,5)};
        }
        private static void Fill(Rect r,Color color) { var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old; }
        private static void Border(Rect r,Color color) {
            Fill(new Rect(r.x,r.y,r.width,1),color);Fill(new Rect(r.x,r.yMax-1,r.width,1),color);
            Fill(new Rect(r.x,r.y,1,r.height),color);Fill(new Rect(r.xMax-1,r.y,1,r.height),color);
        }
        private void Text(Rect r,string text,int size,Color color,bool bold=false) {
            text=Safe(text);label.fontSize=size;label.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;label.normal.textColor=color;
            GUI.Label(r,text,label);if(TextObserver!=null) TextObserver(text);
            if(TextLayoutObserver!=null) TextLayoutObserver(text,RootRect(r),label.CalcHeight(new GUIContent(text),r.width));
        }
        private void Register(string name,Rect r,bool enabled) {
            GUI.SetNextControlName(name);if(enabled) {
                focusNames.Add(name);
                var root=RootRect(r);clientRects[name]=new Rect(root.position*guiScale,root.size*guiScale);if(drawingBody)bodyControls.Add(name);
            }
            if(ControlObserver!=null) ControlObserver(name,RootRect(r),enabled);
        }
        private void Button(Rect r,string text,string command,bool selected=false) {
            bool enabled=Allowed(command),old=GUI.enabled;GUI.enabled=enabled;
            Fill(r,selected?Raised:Panel);if(selected) Border(r,Brass);
            Register(command,r,enabled);
            if(GUI.Button(r,text,button)) RequestCommand(command);
            if(selected) {Border(r,Brass);Fill(new Rect(r.x+1,r.y+1,r.width-2,3),Brass);}
            if(GUI.GetNameOfFocusedControl()==command) Border(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),Accent);
            GUI.enabled=old;if(TextObserver!=null) TextObserver(text);
        }
        private void Field(Rect r,string current,string prefix,string focus) {
            bool enabled=Allowed(prefix+Safe(current)),old=GUI.enabled;GUI.enabled=enabled;
            Register(focus,r,enabled);
            string next=GUI.TextField(r,Safe(current),input);
            if(next!=Safe(current)) RequestCommand(prefix+next);
            GUI.enabled=old;
        }
        private void RevealFocus(string name) {
            Rect r;if(!bodyControls.Contains(name) || !clientRects.TryGetValue(name,out r)) return;
            var g=BinderLayoutRules.Geometry(Screen.width,Screen.height);
            if(name.StartsWith("backup-select:",StringComparison.Ordinal)) {
                int index;if(int.TryParse(name.Substring(14),out index)) {
                    float old=backupScroll.y;backupScroll.y=Math.Max(index*39-55,Math.Min(index*39,backupScroll.y));
                    r.y+=(old-backupScroll.y)*g.scale;
                }
            }
            float top=210*g.scale,bottom=(210+g.bodyHeight)*g.scale;
            if(r.yMin<top) bodyScroll.y=Math.Max(0,bodyScroll.y-(top-r.yMin)/g.scale);
            else if(r.yMax>bottom) bodyScroll.y+=(r.yMax-bottom)/g.scale;
        }
        private void Keyboard(Event e) {
            if(e.type!=EventType.KeyDown) return;
            if(e.keyCode==KeyCode.Escape) { RequestCommand(BinderLayoutRules.HasReview(view) && Allowed("import-cancel")?"import-cancel":"binder-close");e.Use();return; }
            if(e.keyCode!=KeyCode.Tab) return;
            // Tab traverses all reachable controls; no global input polling or precision gestures.
            if(previousFocusNames.Count>0) {
                int index=previousFocusNames.IndexOf(GUI.GetNameOfFocusedControl());
                index=index<0?(e.shift?previousFocusNames.Count-1:0):(index+(e.shift?-1:1)+previousFocusNames.Count)%previousFocusNames.Count;
                RevealFocus(previousFocusNames[index]);GUI.FocusControl(previousFocusNames[index]);
            }
            e.Use();
        }
        private void OnGUI() {
            if(ExternalOverlayVisible) {
                if(resetFocus) {GUI.FocusControl(null);GUIUtility.hotControl=0;GUIUtility.keyboardControl=0;resetFocus=false;}
                return;
            }
            if(!IsOpen) {previousFocusNames.Clear();clientRects.Clear();return;}
            EnsureStyles();GUI.depth=-1000;
            var oldMatrix=GUI.matrix;bool oldEnabled=GUI.enabled;var oldColor=GUI.color;
            GUI.enabled=true;GUI.color=Color.white;
            var metrics=BinderLayoutRules.Geometry(Screen.width,Screen.height);guiOrigin=Vector2.zero;guiScale=metrics.scale;
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(metrics.scale,metrics.scale,1));
            float left=(metrics.width-1600)/2;var e=Event.current;
            if(resetFocus) {GUI.FocusControl(null);resetFocus=false;}
            Keyboard(e);focusNames.Clear();bodyControls.Clear();clientRects.Clear();
            Fill(new Rect(0,0,metrics.width,metrics.height),Felt);
            Fill(new Rect(left+24,16,1552,72),Panel);
            bool hasSets=view.supportedSets!=null && view.supportedSets.Length>0;
            Text(new Rect(left+44,25,740,22),"THE COLLECTION  /  PRIVATE BINDER"+(string.IsNullOrEmpty(view.setLabel)?"":"  /  "+view.setLabel),16,Brass,true);
            Text(new Rect(left+44,48,740,38),view.mode=="physical"?"Physical ownership. Your confirmation, never inferred.":"Virtual revealed pulls. Only what you have seen.",28,Paper,true);
            FundButton(new Rect(left+788,32,230,42));
            if(hasSets) {
                int index=0;var used=new HashSet<string>(StringComparer.Ordinal);
                foreach(var choice in view.supportedSets) {
                    if(choice==null || index>=3 || !PresentationRules.IsSupportedSet(view.supportedSets,choice.printingId) || !used.Add(choice.printingId))continue;
                    Button(new Rect(left+1042+index*172,31,160,44),Safe(choice.label,choice.printingId),"binder-set:"+choice.printingId,view.printingId==choice.printingId);index++;
                }
            }
            bool review=BinderLayoutRules.HasReview(view);
            // The reviewed archive is a modal context; collection controls cannot retain keyboard focus.
            bool baseEnabled=GUI.enabled;if(review) GUI.enabled=false;
            Button(new Rect(left+40,102,748,54),"VIRTUAL REVEALED PULLS","mode:virtual",view.mode=="virtual");
            Button(new Rect(left+804,102,756,54),"PHYSICAL OWNERSHIP","mode:physical",view.mode=="physical");
            DrawStats(left);
            var viewport=new Rect(left+40,210,1520,metrics.bodyHeight);
            bodyScroll=GUI.BeginScrollView(viewport,bodyScroll,new Rect(0,0,1500,650),false,metrics.bodyHeight<650);
            guiOrigin=new Vector2(left+40-bodyScroll.x,210-bodyScroll.y);
            drawingBody=true;DrawCollection();DrawSelected();DrawBackups();drawingBody=false;
            GUI.EndScrollView();guiOrigin=Vector2.zero;GUI.enabled=baseEnabled;
            DrawFooter(left,metrics.height);
            if(review) DrawReview(left,metrics.height);
            if(e.type==EventType.Repaint) { previousFocusNames.Clear();previousFocusNames.AddRange(focusNames); }
            // Full-viewport overlay consumes unused pointer/key events. Host still gates its own table Update/hitboxes.
            if(e.type==EventType.MouseDown || e.type==EventType.MouseUp || e.type==EventType.MouseDrag || e.type==EventType.ScrollWheel || e.type==EventType.KeyDown || e.type==EventType.KeyUp) e.Use();
            GUI.matrix=oldMatrix;GUI.enabled=oldEnabled;GUI.color=oldColor;
        }
        private void DrawStats(float left) {
            string percent=double.IsNaN(view.completionPercent)||double.IsInfinity(view.completionPercent)?"—":view.completionPercent.ToString("0.0",CultureInfo.InvariantCulture)+"%";
            Text(new Rect(left+40,169,680,30),N(view.uniqueOwned)+" / "+N(view.targetCount)+" eligible printings  •  "+percent+" complete",23,Brass,true);
            Text(new Rect(left+746,171,814,28),N(view.totalQuantity)+(view.mode=="physical"?" confirmed cards":" revealed pulls")+"  /  One completion slot per printing, not quantity",18,Muted);
        }
        private void DrawCollection() {
            Text(new Rect(0,0,180,24),"FIND A PRINTING",15,Muted,true);
            Field(new Rect(190,0,850,38),view.search,"search:","binder-search");
            int i=0;foreach(var filter in BinderLayoutRules.Filters) {
                Button(new Rect(i*210,48,198,38),char.ToUpperInvariant(filter[0])+filter.Substring(1),"filter:"+filter,view.filter==filter);i++;
            }
            var rows=BinderLayoutRules.SortedRows(view);int page=Math.Max(0,Math.Min(view.page,BinderLayoutRules.PageCount(view)-1));
            int start=page*BinderLayoutRules.PageSize,end=Math.Min(start+BinderLayoutRules.PageSize,rows.Length);
            if(rows.Length==0) {
                Fill(new Rect(0,102,1040,374),Panel);
                Text(new Rect(36,180,968,56),"No projected slots match this view.",28,Paper,true);
                Text(new Rect(36,245,968,100),"Try All or clear search. This does not mean you own no physical cards. Only the parent’s privacy-filtered rows are shown.",23,Muted);
            }
            for(int index=start;index<end;index++) {
                var row=rows[index];int local=index-start,col=local%3,line=local/3;
                var r=new Rect(col*352,102+line*99,336,89);bool selected=row.id==view.selectedCardId;
                Button(r,"","select:"+row.id,selected);
                Fill(new Rect(r.x+1,r.y+1,5,r.height-2),row.owned?Accent:(Color)new Color32(80,101,88,255));
                string name="#"+Safe(row.collectorNumber,"?")+"  "+Safe(row.name,"Unnamed printing");
                label.fontSize=19;while(label.fontSize>14 && label.CalcHeight(new GUIContent(name),245)>46)label.fontSize--;
                Text(new Rect(r.x+16,r.y+5,245,46),name,label.fontSize,Paper,true);
                Text(new Rect(r.x+16,r.y+58,245,25),Safe(row.rarity,"Unspecified")+" / "+Safe(row.finish,"Unspecified"),14,Muted);
                Text(new Rect(r.x+265,r.y+12,65,65),row.owned?(row.quantity>1?"×"+N(row.quantity)+"\nDUP":"×"+N(row.quantity)+"\nOWNED"):"EMPTY",14,row.owned?Brass:Muted,true);
            }
            Button(new Rect(0,510,190,42),"Previous","page:"+N(page-1));
            Text(new Rect(214,518,500,34),"Page "+N(page+1)+" / "+N(BinderLayoutRules.PageCount(view))+"  ·  "+N(rows.Length)+" shown slots",21,Paper);
            Button(new Rect(850,510,190,42),"Next","page:"+N(page+1));
            Text(new Rect(0,569,1040,30),view.mode=="physical"?"Manual ownership is independent of virtual pulls. No quantity changes until Confirm ownership.":"Read-only revealed counts. Unrevealed active-pack identities are not supplied to this view.",17,Muted);
        }
        private static string BreakId(string value) {
            value=Safe(value);var parts=new List<string>();for(int n=0;n<value.Length;n+=45)parts.Add(value.Substring(n,Math.Min(45,value.Length-n)));
            return string.Join("\n",parts.ToArray());
        }
        private void DrawSelected() {
            float x=1064;Fill(new Rect(x,0,436,414),Panel);Border(new Rect(x,0,436,414),new Color32(73,98,77,255));
            var selected=BinderLayoutRules.Selected(view);
            Text(new Rect(x+20,14,396,25),view.mode=="physical"?"MANUAL OWNERSHIP / CONDITION BUCKET":"REVEALED PULLS / READ ONLY",16,Brass,true);
            if(selected==null) {
                Text(new Rect(x+20,62,396,140),"Select a printing to see its full stable identity.\n\nNo selection is inferred from hidden or missing rows.",22,Muted);return;
            }
            Text(new Rect(x+20,46,396,30),Safe(selected.name,"Unnamed printing"),22,Paper,true);
            Text(new Rect(x+20,82,396,62),BreakId(selected.id),15,Muted);
            if(view.mode!="physical") {
                Text(new Rect(x+20,156,396,150),"This printing: "+N(selected.quantity)+" revealed pulls.\n\nVirtual counts are read only. They never establish physical ownership.",23,Paper);return;
            }
            int i=0;foreach(var condition in BinderLayoutRules.Conditions) {
                Button(new Rect(x+20+(i%2)*202,150+(i/2)*39,194,34),condition,"condition:"+condition,BinderLayoutRules.Condition(view)==condition);i++;
            }
            Text(new Rect(x+20,273,175,28),"Bucket quantity",18,Muted);
            Field(new Rect(x+206,268,210,38),view.quantityText,"quantity:","binder-quantity");
            Button(new Rect(x+20,316,396,38),view.pendingPhysicalEdit?"Retry SAME ownership save":"Confirm ownership","save-physical");
            if(ShoppingAvailable) ShoppingButton(new Rect(x+20,366,396,36),selected.id);
        }
        private void DrawBackups() {
            float x=1064;Fill(new Rect(x,428,436,222),Panel);
            Text(new Rect(x+20,440,396,24),"BACKUPS / RESTORE AS A NEW PROFILE",15,Brass,true);
            Button(new Rect(x+20,471,192,36),"Create backup","backup");
            Button(new Rect(x+224,471,192,36),"Preview restore","import-preview");
            var backups=view.backups??new string[0];
            var previousOrigin=guiOrigin;
            backupScroll=GUI.BeginScrollView(new Rect(x+20,517,396,90),backupScroll,new Rect(0,0,374,Math.Max(90,backups.Length*39)));
            guiOrigin=previousOrigin+new Vector2(x+20-backupScroll.x,517-backupScroll.y);
            if(backups.Length==0)Text(new Rect(8,8,350,60),"No backups supplied. Create one explicitly.",18,Muted);
            for(int i=0;i<backups.Length;i++)Button(new Rect(0,i*39,374,35),Safe(backups[i],"Unnamed backup"),"backup-select:"+N(i),i==view.selectedBackupIndex);
            GUI.EndScrollView();guiOrigin=previousOrigin;
            Text(new Rect(x+20,616,396,32),"Preview is read only. Nothing is merged or overwritten.",16,Muted);
        }
        private void DrawFooter(float left,float height) {
            string message=view.restartRequired?"RESTART REQUIRED — restored NEW profile activates after Quit / reopen. Current profile remains untouched.":view.busy?"BUSY — waiting for the parent operation. Collection changes are locked.":pending?"REQUEST PENDING — "+pendingCommand+". Waiting for the parent’s acknowledged view.":Safe(view.error,Safe(view.status,"Collection ready. All changes require an explicit command."));
            if(!string.IsNullOrEmpty(view.error) && (view.busy || pending || view.restartRequired)) message+="  ERROR: "+view.error;
            if(!string.IsNullOrEmpty(view.recoveryNotice)) message+="  Recovery: "+view.recoveryNotice;
            label.fontSize=18;float textHeight=Math.Max(34,label.CalcHeight(new GUIContent(message),1214));
            noticeScroll=GUI.BeginScrollView(new Rect(left+40,height-84,1240,36),noticeScroll,new Rect(0,0,1214,textHeight));
            guiOrigin=new Vector2(left+40-noticeScroll.x,height-84-noticeScroll.y);
            Text(new Rect(0,0,1214,textHeight),message,18,string.IsNullOrEmpty(view.error)?Muted:Error);GUI.EndScrollView();guiOrigin=Vector2.zero;
            Text(new Rect(left+40,height-39,950,30),"Tab: move focus   ·   Enter / Space: activate   ·   Esc: Back (or cancel review)",17,Muted);
            bool musicEnabled=!BinderLayoutRules.HasReview(view) && !view.busy && !pending && !view.pendingPhysicalEdit && !view.pendingImportConfirm && !view.restartRequired;
            bool musicGuiEnabled=GUI.enabled;GUI.enabled=musicEnabled;
            var musicRect=new Rect(left+1296,height-86,264,36);
            Register("music-night-theme",musicRect,musicEnabled);
            bool relaxed=VintageCollector.Music.BackgroundMusicHost.RelaxedBinderTheme;
            if(GUI.Button(musicRect,relaxed?"Music: Night (08)":"Music: Day (05)",button))
                VintageCollector.Music.BackgroundMusicHost.RelaxedBinderTheme=!relaxed;
            GUI.enabled=musicGuiEnabled;
            Button(new Rect(left+1080,height-43,270,35),"Back to table","binder-close");
            Button(new Rect(left+1366,height-43,194,35),"Quit","quit");
        }
        private void DrawReview(float left,float height) {
            // Opaque modal deliberately covers collection controls while preserving Back/Quit below.
            Fill(new Rect(left+32,94,1536,height-188),Felt);Border(new Rect(left+32,94,1536,height-188),Brass);
            Text(new Rect(left+64,112,1450,38),"REVIEW RESTORE  /  NEW PROFILE ONLY",29,Brass,true);
            Text(new Rect(left+64,160,1450,66),"Confirm creates a separate profile and selects it for the next launch. Your current collection stays untouched. This is not a merge or overwrite.",24,Paper,true);
            float area=Math.Max(40,height-430);
            string content=Safe(view.importSummary)+(!string.IsNullOrEmpty(view.error)?"\n\nERROR: "+view.error:"");
            label.fontSize=22;float textHeight=Math.Max(area,label.CalcHeight(new GUIContent(content),1400));
            reviewScroll=GUI.BeginScrollView(new Rect(left+64,242,1460,area),reviewScroll,new Rect(0,0,1425,textHeight));
            guiOrigin=new Vector2(left+64-reviewScroll.x,242-reviewScroll.y);
            Text(new Rect(0,0,1400,textHeight),content,22,Paper);GUI.EndScrollView();guiOrigin=Vector2.zero;
            Button(new Rect(left+64,height-173,750,54),view.pendingImportConfirm?"Retry SAME reviewed restore":"Confirm restore to NEW profile · restart required","import-confirm");
            Button(new Rect(left+838,height-173,686,54),"Cancel review · keep current profile","import-cancel");
        }
    }
}
