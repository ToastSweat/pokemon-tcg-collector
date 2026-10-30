using System;
using System.Globalization;
using UnityEngine;

namespace VintageCollector.Presentation
{
    /// <summary>Passive, original placeholder tabletop. No generator, awards, file IO or save authority.</summary>
    public sealed class TabletopPresentation : MonoBehaviour
    {
        public OpeningView View { get; set; }
        public event Action<string> Command;
        // Internal test observer receives labels actually passed to native OnGUI, never hidden DTO fields.
        internal Action<string> TextObserver { get; set; }

        private Font font;
        private Texture2D disc, diamond;
        private GUIStyle textStyle, hitStyle;
        private float tearRemaining;
        private float lastAudibleVolume = 0.5f;
        private const float TearDuration = 0.55f;
        private static readonly Color Ink = new Color32(36, 44, 40, 255);
        private static readonly Color Paper = new Color32(241, 231, 204, 255);
        private static readonly Color Muted = new Color32(174, 186, 167, 255);
        private static readonly Color Gold = new Color32(218, 181, 104, 255);
        private static readonly Color Felt = new Color32(33, 62, 54, 255);
        private static readonly Color Panel = new Color32(28, 39, 35, 255);
        private static readonly Color Teal = new Color32(113, 177, 156, 255);

        public void NotifyCommitted(string command)
        {
            // Optional audio intentionally omitted: volume/mute remain real parent commands.
            // No view mutation or positive action cue can occur on an uncommitted click.
            if (command == "open") tearRemaining = TearDuration;
        }

        private void RequestCommand(string command)
        {
            if (!PresentationRules.CanEmit(View, command)) return;
            var handler = Command;
            if (handler != null) handler(command);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && View != null && !View.paused) RequestCommand("pause");
        }

        private void Update()
        {
            if (View != null && !View.paused) tearRemaining = Mathf.Max(0, tearRemaining-Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            if (disc != null) Destroy(disc);
            if (diamond != null) Destroy(diamond);
        }

        private void EnsureResources()
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (textStyle == null) textStyle = new GUIStyle { font=font, richText=false, wordWrap=true, clipping=TextClipping.Clip, padding=new RectOffset(0,0,0,0) };
            if (hitStyle == null) hitStyle = new GUIStyle();
            if (disc != null) return;
            disc = new Texture2D(64,64,TextureFormat.RGBA32,false) { name="Original abstract circle", hideFlags=HideFlags.HideAndDontSave };
            var pixels = new Color[64*64];
            for(int y=0;y<64;y++) for(int x=0;x<64;x++) pixels[y*64+x]=new Color(1,1,1,Mathf.Clamp01(32-Vector2.Distance(new Vector2(x+0.5f,y+0.5f),new Vector2(32,32))));
            disc.SetPixels(pixels); disc.Apply();
            diamond = new Texture2D(64,64,TextureFormat.RGBA32,false) { name="Original bounded diamond", hideFlags=HideFlags.HideAndDontSave };
            for(int y=0;y<64;y++) for(int x=0;x<64;x++) pixels[y*64+x]=new Color(1,1,1,Mathf.Clamp01(32-Mathf.Abs(x+0.5f-32)-Mathf.Abs(y+0.5f-32)));
            diamond.SetPixels(pixels); diamond.Apply();
        }

        private static Rect R(PresentationBox b) { return new Rect(b.x,b.y,b.width,b.height); }
        private static Rect Inset(Rect r,float inset) { return new Rect(r.x+inset,r.y+inset,r.width-2*inset,r.height-2*inset); }
        private static Rect At(Rect r,float x,float y,float w,float h) { return new Rect(r.x+x*r.width,r.y+y*r.height,w*r.width,h*r.height); }
        private static string Safe(string value,string fallback="") { return string.IsNullOrEmpty(value)?fallback:value; }
        private void Fill(Rect rect,Color color)
        {
            var old=GUI.color; GUI.color=color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=old;
        }
        private void Outline(Rect r,Color color,float thickness=1)
        {
            Fill(new Rect(r.x,r.y,r.width,thickness),color); Fill(new Rect(r.x,r.yMax-thickness,r.width,thickness),color);
            Fill(new Rect(r.x,r.y,thickness,r.height),color); Fill(new Rect(r.xMax-thickness,r.y,thickness,r.height),color);
        }
        private int FitTextSize(Rect rect,string value,int maximum)
        {
            int size=maximum, minimum=Mathf.Min(12,maximum);
            textStyle.fontSize=size;
            while(size>minimum && textStyle.CalcHeight(new GUIContent(Safe(value)),rect.width)>rect.height) textStyle.fontSize=--size;
            return size;
        }
        private void Label(Rect rect,string value,int size,Color color,bool bold=false,TextAnchor align=TextAnchor.UpperLeft)
        {
            textStyle.fontStyle=bold?FontStyle.Bold:FontStyle.Normal; textStyle.alignment=align;
            textStyle.fontSize=FitTextSize(rect,value,size);
            textStyle.normal.textColor=color;
            GUI.Label(rect,Safe(value),textStyle);
            if (TextObserver != null) TextObserver(Safe(value));
        }
        private void Dot(Rect r,Color color)
        { var old=GUI.color; GUI.color=color; GUI.DrawTexture(r,disc); GUI.color=old; }
        private void Diamond(Rect r,Color color)
        {
            // Rotating GUI.matrix inside BeginGroup can escape native clip space.
            // The procedural mask remains axis-aligned and obeys the illustration clip.
            var old=GUI.color; GUI.color=color; GUI.DrawTexture(r,diamond); GUI.color=old;
        }
        private void Button(Rect rect,string label,string command,bool primary=false)
        {
            bool prior=GUI.enabled;
            GUI.enabled=prior && PresentationRules.CanEmit(View,command);
            bool hover=GUI.enabled && rect.Contains(Event.current.mousePosition);
            Fill(rect,primary?(hover?Paper:Gold):(hover?new Color32(65,88,75,255):new Color32(42,60,51,255)));
            if(!primary) Outline(rect,new Color32(93,113,92,255));
            bool clicked=GUI.Button(rect,GUIContent.none,hitStyle);
            Label(new Rect(rect.x+10,rect.y+4,rect.width-20,rect.height-8),label,20,primary?Ink:Paper,true,TextAnchor.MiddleCenter);
            GUI.enabled=prior;
            if(clicked) RequestCommand(command);
        }

        private void OnGUI()
        {
            EnsureResources();
            var l=PresentationRules.Layout(Screen.width,Screen.height);
            var oldMatrix=GUI.matrix; var oldColor=GUI.color; bool oldEnabled=GUI.enabled;
            try
            {
                GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(l.scale,l.scale,1));
                var v=View;
                if(Event.current.type==EventType.KeyDown && Event.current.keyCode==KeyCode.Escape)
                { RequestCommand(PresentationRules.BackCommand(v)); Event.current.Use(); }
                DrawBackground(l);
                DrawHeader(l,v);
                bool modal=v!=null && (v.paused || v.inspectIndex>=0);
                GUI.enabled=oldEnabled && !modal;
                if(v!=null && v.stage=="results") DrawResults(l,v);
                else DrawOpening(l,v);
                DrawFooter(l,v);
                GUI.enabled=oldEnabled;
                if(v!=null && v.paused) DrawPause(l,v);
                else if(v!=null && v.inspectIndex>=0) DrawInspection(l,v);
            }
            finally { GUI.matrix=oldMatrix; GUI.color=oldColor; GUI.enabled=oldEnabled; }
        }

        private void DrawBackground(PresentationLayout l)
        {
            Fill(new Rect(0,0,l.width,l.height),new Color32(46,39,32,255));
            // Controlled straight grain, original geometry rather than downloaded textures.
            for(float y=3;y<l.height;y+=19) Fill(new Rect(0,y,l.width,1),new Color(0.7f,0.53f,0.32f,0.035f));
            Fill(new Rect(18,18,l.width-36,l.height-36),Panel);
            Outline(new Rect(24,24,l.width-48,l.height-48),new Color32(84,83,63,255));
            Fill(new Rect(44,127,l.width-88,1),new Color32(87,99,79,255));
        }

        private void DrawHeader(PresentationLayout l,OpeningView v)
        {
            Label(new Rect(48,37,690,47),"Vintage Card Collector",36,Paper,true);
            Label(new Rect(50,89,760,26),"1999  /  English Base Set Unlimited",20,Muted);
            Label(new Rect(l.width-508,43,460,25),"PLACEHOLDER  /  TABLETOP STUDY",18,Gold,true,TextAnchor.UpperRight);
            Label(new Rect(l.width-608,82,560,29),"Virtual cards  "+(v==null?"—":v.totalVirtualCards.ToString(CultureInfo.InvariantCulture))+"   ·   Physical tracking not configured",17,Muted,false,TextAnchor.UpperRight);
            Label(new Rect(50,145,900,25),StageLabel(v),18,Gold,true);
        }
        private static string StageLabel(OpeningView v)
        {
            if(v==null) return "WAITING FOR RUNTIME  /  NO PACK GENERATED";
            switch(v.stage)
            {
                case "ready": return "01  /  CHOOSE YOUR PACK";
                case "sealed": return "02  /  SAVED & SEALED";
                case "revealing": return "03  /  REVEAL  "+Mathf.Clamp(v.revealedCount,0,11)+" OF 11";
                case "results": return "04  /  YOUR PACK, IN SUPPLIED ORDER";
                case "error": return "PRESENTATION BLOCKED  /  RETRY REQUIRED";
                default: return "WAITING FOR SUPPORTED VIEW";
            }
        }

        private void DrawOpening(PresentationLayout l,OpeningView v)
        {
            Rect table=R(l.table), side=R(l.sidebar);
            Fill(table,Felt); Outline(Inset(table,10),new Color32(74,99,77,255));
            // Corner rule marks leave the central opening area quiet.
            Label(new Rect(table.x+26,table.y+22,300,24),"THE OPENING TABLE",16,Muted,true);
            Label(new Rect(table.x+26,table.yMax-45,table.width-52,25),"Original abstract visuals  ·  63:88 cards  ·  No branded artwork",16,Muted,false,TextAnchor.MiddleCenter);
            Fill(side,Panel); Outline(side,new Color32(68,86,70,255));
            Rect hero=R(l.heroCard);
            if(v==null || v.stage=="ready" || v.stage=="sealed") DrawWrapper(hero,v!=null && v.stage=="sealed");
            else if(v.stage=="revealing")
            {
                int visible=PresentationRules.VisibleCount(v);
                Rect stack=new Rect(hero.x-hero.width*0.70f,hero.y+hero.height*0.17f,hero.width*0.53f,hero.height*0.53f);
                if(visible<11) {
                    for(int i=3;i>=0;i--) DrawBack(new Rect(stack.x+i*3,stack.y-i*3,stack.width,stack.height));
                    Label(new Rect(stack.x-18,stack.yMax+22,stack.width+36,44),(11-visible)+" unrevealed",18,Muted,false,TextAnchor.MiddleCenter);
                }
                if(visible>0 && PresentationRules.CanInspect(v,visible-1)) {
                    DrawFace(hero,v.cards[visible-1],visible-1);
                    FaceHit(hero,visible-1);
                } else DrawBack(hero);
                if(visible>0) {
                    Rect first=R(l.revealedButtons[0]);
                    Label(new Rect(first.x,first.y-66,216,25),"REVEALED POSITIONS",16,Gold,true);
                    Label(new Rect(first.x,first.y-35,216,25),"Click to inspect",17,Muted);
                    for(int j=0;j<Math.Min(11,visible);j++)
                        Button(R(l.revealedButtons[j]),(j+1).ToString(CultureInfo.InvariantCulture),"inspect:"+j.ToString(CultureInfo.InvariantCulture));
                }
                // The wrapper cue is restricted to an edge and never delays authoritative cards.
                if(tearRemaining>0) DrawTear(new Rect(table.x+40,table.yMax-88,table.width-80,28));
            }
            else {
                DrawBack(hero);
                Label(new Rect(table.x+80,table.y+70,table.width-160,68),"Opening held safely",28,Paper,true,TextAnchor.MiddleCenter);
            }
            DrawSide(l,v);
        }

        private void DrawSide(PresentationLayout l,OpeningView v)
        {
            Rect s=R(l.sidebar); float x=s.x+30, w=s.width-60;
            string stage=v==null?"waiting":v.stage;
            string heading,body;
            switch(stage)
            {
                case "ready": heading="A small ritual.\nEleven discoveries."; body="Only supported option\n1999 English Base Set Unlimited\n\nGenerate requests a new pack. The runtime must save it before the wrapper appears."; break;
                case "sealed": heading="A saved pack.\nReady when you are."; body="Open the placeholder wrapper with one click.\n\nIts contents and order are already fixed. Opening does not roll any cards."; break;
                case "revealing":
                    int i=PresentationRules.VisibleCount(v)-1;
                    heading=i>=0 && PresentationRules.CanInspect(v,i)?Safe(v.cards[i].name,"Unnamed card"):"The first card\nis waiting.";
                    body=i>=0 && PresentationRules.CanInspect(v,i)?"Card "+(i+1)+" of 11\n"+Safe(v.cards[i].rarity)+"  ·  "+Safe(v.cards[i].finish)+"\n\n"+(v.cards[i].isNew?"NEW TO VIRTUAL COLLECTION":"DUPLICATE VIRTUAL CARD")+"\n\nClick the face to inspect. Only revealed cards can be inspected.":"Reveal the next saved position, or skip directly to the ordered results.";
                    break;
                case "error": heading="Cannot advance."; body="No new contents are shown while the runtime reports an error. Retry the pending action or quit.\n\n"+Safe(v.errorMessage,"Save or runtime unavailable."); break;
                default: heading="Waiting for\nthe runtime."; body="The presentation cannot generate, save or award cards on its own. No pack is generated on startup."; break;
            }
            Label(new Rect(x,s.y+30,w,105),heading,30,Paper,true);
            Fill(new Rect(x,s.y+150,w,1),new Color32(79,95,77,255));
            Label(new Rect(x,s.y+174,w,235),body,20,stage=="error"?Gold:Muted);
            if(stage=="ready") Button(R(l.primary),"Generate pack","generate",true);
            if(stage=="sealed") Button(R(l.primary),"Open wrapper","open",true);
            if(stage=="revealing") {
                Button(R(l.primary),"Reveal next card","next",true);
                Button(R(l.secondary),"Skip to results","skip");
            }
            if(stage=="error") {
                Button(R(l.primary),"Retry pending action","retry",true);
                Button(R(l.secondary),"Quit","quit");
            }
            if(stage=="ready" || stage=="sealed") Label(R(l.secondary),"No dragging required.",18,Muted,false,TextAnchor.MiddleCenter);
        }

        private void DrawWrapper(Rect card,bool sealedPack)
        {
            Rect r=new Rect(card.x-8,card.y-8,card.width+16,card.height+16);
            Fill(new Rect(r.x+9,r.y+12,r.width,r.height),new Color(0,0,0,0.23f));
            Fill(r,new Color32(181,154,97,255));
            Fill(new Rect(r.x+9,r.y+25,r.width-18,r.height-50),new Color32(53,79,68,255));
            for(int i=0;i<17;i++) {
                float x=r.x+6+i*(r.width-12)/17;
                Fill(new Rect(x,r.y+3,2,18),new Color32(132,112,75,255));
                Fill(new Rect(x,r.yMax-21,2,18),new Color32(132,112,75,255));
            }
            Outline(Inset(r,19),Gold,2);
            Label(At(r,0.10f,0.10f,0.8f,0.08f),"PLACEHOLDER WRAPPER",16,Gold,true,TextAnchor.MiddleCenter);
            Diamond(At(r,0.25f,0.28f,0.5f,0.35f),new Color32(129,150,112,255));
            Dot(At(r,0.35f,0.35f,0.30f,0.21f),Gold);
            Label(At(r,0.12f,0.66f,0.76f,0.15f),"VINTAGE\nCARD COLLECTOR",24,Paper,true,TextAnchor.MiddleCenter);
            Label(At(r,0.1f,0.84f,0.8f,0.07f),sealedPack?"SAVED  ·  11 VIRTUAL CARDS":"PREVIEW  ·  NOT GENERATED",15,Gold,true,TextAnchor.MiddleCenter);
        }

        private void DrawTear(Rect r)
        {
            float t=1-tearRemaining/TearDuration;
            Rect strip=new Rect(r.x+t*30,r.y-t*22,r.width*(1-0.13f*t),r.height);
            Fill(strip,new Color32(181,154,97,(byte)(255*(1-t))));
            Label(strip,"WRAPPER OPENED",16,Paper,true,TextAnchor.MiddleCenter);
        }

        private void DrawBack(Rect r)
        {
            Fill(new Rect(r.x+7,r.y+9,r.width,r.height),new Color(0,0,0,0.22f));
            Fill(r,Paper); Fill(Inset(r,r.width*0.035f),new Color32(47,75,74,255));
            Outline(Inset(r,r.width*0.075f),Gold,2);
            Diamond(At(r,0.19f,0.27f,0.62f,0.44f),new Color32(124,150,126,255));
            Dot(At(r,0.35f,0.39f,0.30f,0.215f),new Color32(199,175,123,255));
            Label(At(r,0.1f,0.77f,0.8f,0.12f),"PLACEHOLDER\nCARD BACK",Mathf.RoundToInt(r.width*0.047f),Paper,true,TextAnchor.MiddleCenter);
        }

        private void DrawFace(Rect r,OpeningCardView c,int index)
        {
            if(c==null) { DrawBack(r); return; }
            bool small=r.width<230; float fs=r.width/350;
            Fill(new Rect(r.x+6,r.y+8,r.width,r.height),new Color(0,0,0,0.23f));
            Fill(r,new Color32(219,184,112,255));
            Rect inner=Inset(r,r.width*0.035f); Fill(inner,Paper);
            Outline(inner,new Color32(160,146,104,255));
            Label(At(r,0.08f,0.045f,0.84f,0.11f),Safe(c.name,"Unnamed card"),small?17:Mathf.RoundToInt(25*fs),Ink,true);
            Rect art=At(r,0.08f,0.17f,0.84f,0.37f);
            DrawAbstractArt(art,index,PresentationRules.IsFoil(c.finish));
            Label(At(r,0.09f,0.565f,0.82f,0.075f),"PLACEHOLDER ART",small?12:Mathf.RoundToInt(14*fs),Ink,true,TextAnchor.MiddleCenter);
            Label(At(r,0.09f,0.66f,0.82f,0.11f),Safe(c.kind)+"  ·  "+Safe(c.rarity),small?14:Mathf.RoundToInt(19*fs),Ink);
            Label(At(r,0.09f,0.775f,0.82f,0.06f),"#"+Safe(c.collectorNumber)+"   "+Safe(c.finish),small?13:Mathf.RoundToInt(16*fs),Ink);
            Rect badge=At(r,0.07f,0.875f,0.86f,0.08f);
            Fill(badge,c.isNew?new Color32(56,105,83,255):new Color32(111,99,76,255));
            Label(badge,c.isNew?"NEW · VIRTUAL":"DUPLICATE",small?13:Mathf.RoundToInt(17*fs),Paper,true,TextAnchor.MiddleCenter);
        }

        private void DrawAbstractArt(Rect art,int index,bool foil)
        {
            // Palette and geometry use only revealed position, never hidden identity.
            Color[] colors={new Color32(108,136,114,255),new Color32(153,137,111,255),new Color32(101,131,139,255),new Color32(149,120,110,255)};
            Fill(art,colors[Math.Abs(index)%colors.Length]);
            GUI.BeginGroup(art);
            float w=art.width,h=art.height;
            Dot(new Rect(w*0.57f,h*0.12f,h*0.46f,h*0.46f),Gold);
            Diamond(new Rect(w*0.04f,h*0.31f,w*0.62f,w*0.62f),new Color32(51,83,73,255));
            Diamond(new Rect(w*0.50f,h*0.55f,w*0.58f,w*0.58f),new Color32(73,97,79,255));
            Fill(new Rect(0,h*0.87f,w,h*0.13f),new Color32(44,65,58,255));
            if(foil) for(int i=0;i<5;i++) Diamond(new Rect(w*(0.15f+i*0.16f),h*(0.19f+(i%2)*0.19f),7,7),Paper);
            GUI.EndGroup();
            Outline(art,Ink);
        }

        private void FaceHit(Rect r,int index)
        {
            if(!PresentationRules.CanInspect(View,index)) return;
            if(GUI.enabled && r.Contains(Event.current.mousePosition)) Outline(r,Teal,3);
            if(GUI.Button(r,GUIContent.none,hitStyle)) RequestCommand("inspect:"+index.ToString(CultureInfo.InvariantCulture));
        }

        private void DrawResults(PresentationLayout l,OpeningView v)
        {
            Fill(new Rect(44,178,l.width-88,l.height-302),Felt);
            int visible=Math.Min(11,PresentationRules.VisibleCount(v)), fresh=0;
            for(int i=0;i<visible;i++) {
                if(v.cards[i]!=null && v.cards[i].isNew) fresh++;
                DrawFace(R(l.resultsCards[i]),v.cards[i],i); FaceHit(R(l.resultsCards[i]),i);
            }
            for(int i=visible;i<11;i++) DrawBack(R(l.resultsCards[i]));
            Label(new Rect(66,184,1000,28),visible+" revealed   ·   "+fresh+" new to virtual   ·   "+(visible-fresh)+" duplicates   ·   Click a card to inspect",18,Paper);
            Button(new Rect(l.width-310,140,266,34),"Generate new pack","generate",true);
            Label(new Rect(64,l.height-151,l.width-128,24),"Original saved order, left to right. New / duplicate refers only to the virtual collection.",17,Muted,false,TextAnchor.MiddleCenter);
        }

        private void DrawFooter(PresentationLayout l,OpeningView v)
        {
            string model=Safe(v==null?null:v.modelLabel,"Model A 0.1.0");
            Label(new Rect(50,l.height-99,l.width-380,25),"PROVISIONAL RULES  ·  "+model,17,Gold,true);
            string notice=v==null?"No runtime attached.":Safe(v.recoveryNotice,Safe(v.statusLine,"Placeholder visuals; physical tracking is not configured."));
            Label(new Rect(50,l.height-69,l.width-380,34),notice,16,Muted);
            Button(R(l.pause),"Pause","pause"); Button(R(l.quit),"Quit","quit");
        }

        private void Dim(PresentationLayout l) { Fill(new Rect(0,0,l.width,l.height),new Color(0.025f,0.04f,0.035f,0.88f)); }
        private void DrawPause(PresentationLayout l,OpeningView v)
        {
            Dim(l);
            Rect p=new Rect((l.width-660)/2,(l.height-470)/2,660,470);
            Fill(p,Panel); Outline(p,Gold,2);
            Label(new Rect(p.x+40,p.y+29,580,48),"A moment at the table",32,Paper,true);
            Label(new Rect(p.x+40,p.y+95,580,58),"Paused. Resume explicitly when you are ready.\nQuitting does not request a last-minute save.",20,Muted);
            float volume=Mathf.Clamp01(float.IsNaN(v.volume)?0.5f:v.volume);
            if(volume>0) lastAudibleVolume=volume;
            Label(new Rect(p.x+40,p.y+172,580,29),"Volume  "+Mathf.RoundToInt(volume*100)+"%  ·  Silent placeholder presentation",19,Gold);
            Button(R(l.quieter),"−","volume:"+Mathf.Clamp01(volume-0.1f).ToString("0.0",CultureInfo.InvariantCulture));
            Button(R(l.louder),"+","volume:"+Mathf.Clamp01(volume+0.1f).ToString("0.0",CultureInfo.InvariantCulture));
            Button(R(l.mute),volume>0?"Mute":"Unmute",volume>0?"volume:0":"volume:"+lastAudibleVolume.ToString("0.0",CultureInfo.InvariantCulture));
            Button(R(l.resume),"Resume","resume",true);
            Button(new Rect(p.x+40,p.y+390,580,46),"Quit","quit");
        }

        private void DrawInspection(PresentationLayout l,OpeningView v)
        {
            Dim(l); Rect p=R(l.modal); Fill(p,Panel); Outline(p,Gold,2);
            int i=v.inspectIndex;
            if(PresentationRules.CanInspect(v,i)) {
                Rect card=new Rect(p.x+30,p.y+30,620*63f/88,620);
                DrawFace(card,v.cards[i],i);
                Rect info=new Rect(p.x+560,p.y+40,396,508);
                Label(new Rect(info.x,info.y,info.width,28),"REVEALED CARD  "+(i+1)+" / 11",18,Gold,true);
                Label(new Rect(info.x,info.y+57,info.width,105),Safe(v.cards[i].name),32,Paper,true);
                Label(new Rect(info.x,info.y+181,info.width,181),"Collector number  "+Safe(v.cards[i].collectorNumber)+"\n"+Safe(v.cards[i].kind)+"  ·  "+Safe(v.cards[i].rarity)+"\nFinish  "+Safe(v.cards[i].finish)+"\n\n"+(v.cards[i].isNew?"New to virtual collection":"Duplicate virtual card"),21,Muted);
                Label(new Rect(info.x,info.y+382,info.width,105),"PLACEHOLDER VISUALS\nOriginal geometric illustration, not card artwork. Physical tracking not configured.",19,Gold);
            } else Label(new Rect(p.x+65,p.y+80,p.width-130,180),"This card is not available for inspection.\nUnrevealed names and faces remain hidden.",28,Paper,true);
            Button(R(l.back),"Back to table","close-inspect",true);
        }
    }
}
