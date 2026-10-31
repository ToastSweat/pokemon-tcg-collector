using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VintageCollector.Media;
using VintageCollector.Presentation;

namespace VintageCollector.EditorChecks
{
    // Explicit isolated native checks only. Never runs on startup or touches game/save code.
    public static class Phase3PresentationChecks
    {
        [Serializable] private sealed class Report { public bool passed; public int checkCount; public string[] checks; public string failure; }
        private static readonly List<string> checks = new List<string>();
        private static void Check(bool ok,string name) { if(!ok) throw new Exception(name); checks.Add(name); }
        private static object Get(object c,string name) { return c.GetType().GetProperty(name).GetValue(c,null); }
        private static void Call(object c,string method,params object[] args) { c.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,args); }
        // Explicit fake: original deterministic sine, not production media or a historical recording.
        private sealed class LabelledFakeMedia : IPresentationMedia
        {
            public readonly AudioClip clip;
            public int calls;
            public LabelledFakeMedia() {
                clip=AudioClip.Create("TEST ONLY original sine canary",4800,1,24000,false);
                var samples=new float[4800];
                for(int i=0;i<samples.Length;i++) samples[i]=0.08f*Mathf.Sin(i*0.13f)*(1-i/(float)samples.Length);
                clip.SetData(samples,0);
            }
            public Texture2D CardFace(string id) { return null; }
            public Texture2D HoloMask(string id) { return null; }
            public Texture2D CardBack { get { return null; } }
            public Texture2D WrapperFront { get { return null; } }
            public Texture2D WrapperBack { get { return null; } }
            public AudioClip Sound(string action) { calls++; return clip; }
            public string StatusText { get { return "TEST ONLY fake provider / original synthetic foley"; } }
            public int SuppliedAssetCount { get { return 0; } }
        }
        public static void Run()
        {
            checks.Clear(); GameObject go=null; string failure=null;
            try {
                var type=typeof(TabletopPresentation);
                Check(type.GetProperty("Media")!=null && type.GetProperty("Media").PropertyType==typeof(IPresentationMedia),"Frozen IPresentationMedia property exists");
                Check(type.GetProperty("FoleySource")!=null && type.GetProperty("FoleySource").PropertyType==typeof(AudioSource),"Frozen AudioSource diagnostic exists");
                Check(type.GetProperty("CommittedCueCount")!=null && type.GetProperty("CommittedCueCount").PropertyType==typeof(int),"Frozen cue counter exists");
                Check(type.GetProperty("LastCommittedCue")!=null && type.GetProperty("LastCommittedCue").PropertyType==typeof(string),"Frozen last cue exists");
                go=new GameObject("Labelled synthetic native presentation fixture");
                var c=go.AddComponent<TabletopPresentation>();
                var source=(AudioSource)Get(c,"FoleySource");
                Check(source!=null && !source.playOnAwake && !source.loop && source.spatialBlend==0,"Foley is nonlooping 2D with no play on awake");
                Check(!source.isPlaying && (int)Get(c,"CommittedCueCount")==0,"No startup playback or committed cue");
                Check(Mathf.Abs(source.volume-0.5f)<0.0001f,"Native source starts at frozen default volume 0.5");
                var fake=new LabelledFakeMedia();
                try {
                    type.GetProperty("Media").SetValue(c,fake);
                    Check(fake.calls==0 && !source.isPlaying,"Binding provider never plays");
                    var data=new float[fake.clip.samples]; fake.clip.GetData(data,0);
                    float peak=0; bool finite=true; foreach(float sample in data) { finite &= !float.IsNaN(sample) && !float.IsInfinity(sample); peak=Mathf.Max(peak,Mathf.Abs(sample)); }
                    Check(finite,"All native clip samples finite");
                    Check(fake.clip.samples==4800 && fake.clip.channels==1 && peak>0.01f && peak<0.1f,"Native clip contains original bounded nonzero samples");
                    var v=new OpeningView {stage="ready",transactionId="fixture-a"}; c.View=v;
                    Call(c,"RequestCommand","generate");
                    Check(fake.calls==0 && c.CommittedCueCount==0,"Unacknowledged request is silent");
                    c.NotifyCommitted("invalid");
                    Check(fake.calls==0,"Unknown commit is silent");
                    c.View=new OpeningView {stage="sealed",transactionId="fixture-a"};
                    c.NotifyCommitted("generate");
                    Check(c.CommittedCueCount==1 && c.LastCommittedCue=="generate" && source.clip==fake.clip && fake.calls==1,"Committed generate requests exactly one native cue");
                    c.NotifyCommitted("generate");
                    Check(c.CommittedCueCount==1 && fake.calls==1,"Duplicate acknowledgement does not replay");
                    c.NotifyCommitted("next"); c.NotifyCommitted("skip"); c.NotifyCommitted("open");
                    Check(c.CommittedCueCount==1,"Invalid postcommit stage cannot cue");
                    var cards=new OpeningCardView[11]; for(int i=0;i<11;i++) cards[i]=new OpeningCardView {id="canary-"+i};
                    c.View=new OpeningView {stage="sealed",transactionId="invalid-only",revealedCount=9}; c.NotifyCommitted("generate");
                    c.View=new OpeningView {stage="results",transactionId="invalid-only",cards=cards,revealedCount=1}; c.NotifyCommitted("next");
                    Check(c.CommittedCueCount==1 && fake.calls==1,"Inconsistent sealed/results projections cannot cue");
                    c.View=new OpeningView {stage="revealing",transactionId="fixture-a",cards=cards}; c.NotifyCommitted("open");
                    Check(c.CommittedCueCount==2 && c.LastCommittedCue=="open","Committed open cues without revealing");
                    c.View=new OpeningView {stage="revealing",transactionId="fixture-a",cards=cards,revealedCount=1}; c.NotifyCommitted("next");
                    Check(c.CommittedCueCount==3 && c.LastCommittedCue=="next" && c.View.revealedCount==1,"Committed next cues without DTO mutation");
                    c.View=new OpeningView {stage="results",transactionId="fixture-a",cards=cards,revealedCount=11}; c.NotifyCommitted("skip");
                    Check(c.CommittedCueCount==4 && c.LastCommittedCue=="skip" && fake.calls==4,"Skip requests one cue not eleven");
                    c.NotifyCommitted("skip"); Check(c.CommittedCueCount==4,"Skip duplicate cannot burst");
                    c.View=new OpeningView {stage="sealed",transactionId="fixture-b",volume=0}; c.NotifyCommitted("generate");
                    Check(source.volume==0 && source.clip==null && fake.calls==4,"Mute immediately stops and clears without cue");
                    c.View=new OpeningView {stage="sealed",transactionId="fixture-b",volume=0.7f}; c.NotifyCommitted("generate");
                    Check(source.volume==0.7f && c.CommittedCueCount==4,"Unmute cannot replay consumed commit");
                    c.View=new OpeningView {stage="sealed",transactionId="fixture-c",paused=true}; c.NotifyCommitted("generate");
                    c.View=new OpeningView {stage="sealed",transactionId="fixture-c"}; c.NotifyCommitted("generate");
                    Check(source.clip==null && c.CommittedCueCount==4,"Pause stops and Resume cannot replay suppressed commit");
                    c.View=new OpeningView {stage="sealed",transactionId="fixture-d",volume=2}; c.NotifyCommitted("generate");
                    Check(source.volume==1 && c.CommittedCueCount==5,"Volume clamps immediately at one");
                    c.View=new OpeningView {stage="error"}; c.NotifyCommitted("next");
                    Check(source.clip==null && !source.isPlaying && c.CommittedCueCount==5,"Error immediately stops and cannot cue");
                    c.View=new OpeningView {stage="sealed",transactionId="fixture-e"}; c.NotifyCommitted("generate");
                    Call(c,"OnApplicationFocus",false);
                    Check(source.clip==null && !source.isPlaying,"Focus loss immediately stops native source");
                    Call(c,"OnApplicationFocus",true);
                    Check(c.CommittedCueCount==6,"Focus gain never replays");
                    c.Media=null;
                    Check(source.clip==null && !source.isPlaying && fake.clip.samples==4800,"Provider detach stops without destroying external resources");
                    c.Media=fake; c.View=new OpeningView {stage="sealed",transactionId="fixture-f"}; c.NotifyCommitted("generate");
                    Call(c,"OnDestroy");
                    Check(source.clip==null && !source.isPlaying && fake.clip.samples==4800,"Destroy stops but never disposes external clip");
                } finally { c.Media=null; UnityEngine.Object.DestroyImmediate(fake.clip); }
            }
            catch(Exception e) { failure=e.ToString(); Debug.LogError(failure); }
            finally {
                if(go!=null) UnityEngine.Object.DestroyImmediate(go);
                string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../.artifacts/presentation-engine-report.json"));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path,JsonUtility.ToJson(new Report {passed=failure==null,checkCount=checks.Count,checks=checks.ToArray(),failure=failure},true));
                Debug.Log("PHASE3 PRESENTATION "+(failure==null?"PASS":"FAIL")+" count="+checks.Count);
            }
            if(Application.isBatchMode) EditorApplication.Exit(failure==null?0:1);
        }
    }
}
