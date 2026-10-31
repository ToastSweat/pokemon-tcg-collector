using System;
using System.Collections.Generic;
using System.Globalization;

// Extracted-production boundary harness, not Unity/native playback proof.
public sealed class AudioClip { public readonly string name; public AudioClip(string name) { this.name=name; } }
public sealed class AudioSource {
    public AudioClip clip; public float volume; public int plays,stops; public bool isPlaying;
    public void Play() { plays++;isPlaying=true; }
    public void Stop() { stops++;isPlaying=false; }
}
public static class Mathf { public static float Clamp01(float value) { return Math.Max(0,Math.Min(1,value)); } }
public interface IPresentationMedia { AudioClip Sound(string command); }
public interface IRevealSoundMedia { AudioClip RevealSound(int oneBasedPosition,string kind,string finish); }
public sealed class Phase3PresentationMotion { public int pulses; public void PulseReveal() { pulses++; } }
public sealed class OpeningCardView { public string id,name,collectorNumber,rarity,kind,finish;public bool isNew; }
public sealed class OpeningView {
    public string stage,transactionId; public int revealedCount,inspectIndex=-1;
    public OpeningCardView[] cards;public bool paused;public float volume=.5f;
}
public sealed class BoundaryPresentation {
    public OpeningView View;
    public IPresentationMedia Media;
    private IRevealSoundMedia revealSounds;
    private readonly AudioSource foleySource=new AudioSource();
    public AudioSource FoleySource {get {return foleySource;}}
    public bool focused=true,CollectionInputBlocked,CollectionOverlayVisible;
    private string cueTransaction;
    private readonly HashSet<string> consumedCues=new HashSet<string>(StringComparer.Ordinal);
    private readonly Phase3PresentationMotion motion=new Phase3PresentationMotion();
    private float tearRemaining;private const float TearDuration=.55f;
    public int CommittedCueCount {get;private set;}
    public string LastCommittedCue {get;private set;}
    // EXTRACTED_MEMBERS
}
public sealed class GenericMedia:IPresentationMedia {
    public readonly AudioClip next=new AudioClip("generic-next");public readonly AudioClip skip=new AudioClip("generic-skip");
    public int calls;
    public AudioClip Sound(string command) { calls++;return command=="skip"?skip:next; }
}
public sealed class AssignedMedia:IRevealSoundMedia {
    public readonly AudioClip selected=new AudioClip("assigned-current-reveal");
    public int calls,position;public string kind,finish;public bool missing;
    public AudioClip RevealSound(int position,string kind,string finish) {
        calls++;this.position=position;this.kind=kind;this.finish=finish;return missing?null:selected;
    }
}
public static class RendererBoundaryChecks {
    private static void Require(bool condition,string message) {if(!condition)throw new Exception(message);}
    private static BoundaryPresentation New(int count=8) {
        var cards=new OpeningCardView[11];
        for(int i=0;i<11;i++) cards[i]=new OpeningCardView {id="concealed-"+i,kind="energy",finish="non-holo"};
        cards[count-1]=new OpeningCardView {id="already-acknowledged",kind="pokemon",finish="holo"};
        return new BoundaryPresentation {View=new OpeningView {transactionId="transaction-A",stage="revealing",revealedCount=count,cards=cards},Media=new GenericMedia()};
    }
    private static void AssignedCurrentReveal() {
        var p=New();var assigned=new AssignedMedia();p.RevealSounds=assigned;p.NotifyCommitted("next");
        Require(ReferenceEquals(p.FoleySource.clip,assigned.selected),"Acknowledged reveal must use its assigned clip, not generic Foley");
        Require(assigned.calls==1 && assigned.position==8 && assigned.kind=="pokemon" && assigned.finish=="holo","Only acknowledged position8 metadata may reach provider");
        Require(p.CommittedCueCount==1 && p.LastCommittedCue=="next","One existing acknowledgement cue is retained");
    }
    private static void BinderBlockConsumesSilently() {
        var p=New();var assigned=new AssignedMedia();p.RevealSounds=assigned;p.CollectionInputBlocked=true;p.NotifyCommitted("next");
        Require(assigned.calls==0 && p.CommittedCueCount==0 && !p.FoleySource.isPlaying,"Binder block must suppress provider lookup and playback");
        p.CollectionInputBlocked=false;p.NotifyCommitted("next");
        Require(assigned.calls==0 && p.CommittedCueCount==0,"Closing binder must not replay a consumed reveal");
    }
    private static void OverlayStopsCurrentCue() {
        var p=New();p.RevealSounds=new AssignedMedia();p.NotifyCommitted("next");
        Require(p.FoleySource.isPlaying,"Fixture must start a cue");
        p.CollectionOverlayVisible=true;p.NotifyCommitted("inspect:0");
        Require(!p.FoleySource.isPlaying && p.FoleySource.clip==null,"Visible collection overlay must stop the current cue even before input-block propagation");
    }
    private static void DuplicateAndNextTarget() {
        var p=New();var a=new AssignedMedia();p.RevealSounds=a;p.NotifyCommitted("next");p.NotifyCommitted("next");
        Require(a.calls==1 && p.CommittedCueCount==1,"Repeated acknowledgement cannot duplicate sound");
        p.View.revealedCount=9;p.NotifyCommitted("next");
        Require(a.calls==2 && a.position==9 && p.CommittedCueCount==2,"A distinct next acknowledged position cues once");
    }
    private static void AllElevenPositions() {
        var p=New();var a=new AssignedMedia();p.RevealSounds=a;
        for(int i=1;i<=11;i++) {p.View.revealedCount=i;p.NotifyCommitted("next");Require(a.position==i && a.calls==i,"Each acknowledged position reaches the provider exactly once: "+i);}
        Require(p.CommittedCueCount==11,"All11pulls, including held final card, have one assignable cue");
        p.View.stage="results";p.NotifyCommitted("results");p.NotifyCommitted("next");
        Require(p.CommittedCueCount==11,"Explicit results and duplicate final acknowledgement remain silent");
    }
    private static void SuppressionConsumes(string gate) {
        var p=New();var a=new AssignedMedia();p.RevealSounds=a;
        if(gate=="mute")p.View.volume=0;
        if(gate=="pause")p.View.paused=true;
        if(gate=="focus")p.focused=false;
        if(gate=="overlay")p.CollectionOverlayVisible=true;
        p.NotifyCommitted("next");
        Require(a.calls==0 && p.CommittedCueCount==0,"Suppressed acknowledgement must not ask provider: "+gate);
        p.View.volume=.5f;p.View.paused=false;p.focused=true;p.CollectionOverlayVisible=false;p.NotifyCommitted("next");
        Require(a.calls==0 && p.CommittedCueCount==0,"Removing suppression must not replay: "+gate);
    }
    private static void MissingClipFallsBack() {
        var p=New();var a=new AssignedMedia {missing=true};p.RevealSounds=a;p.NotifyCommitted("next");
        Require(a.calls==1 && ReferenceEquals(p.FoleySource.clip,((GenericMedia)p.Media).next),"Missing optional assignment falls back to generic next");
    }
    private static void MissingProviderKeepsLegacy() {
        var p=New();p.NotifyCommitted("next");
        Require(ReferenceEquals(p.FoleySource.clip,((GenericMedia)p.Media).next) && p.CommittedCueCount==1,"Historical scenes retain generic cue behavior");
    }
    private static void OptionalAudioWithoutImageMedia() {
        var p=New();var a=new AssignedMedia();p.Media=null;p.RevealSounds=a;p.NotifyCommitted("next");
        Require(ReferenceEquals(p.FoleySource.clip,a.selected),"Optional reveal audio does not require image/media host availability");
    }
    private static void SkipNeverLooksUpPulls() {
        var p=New();var a=new AssignedMedia();p.RevealSounds=a;p.View.stage="results";p.View.revealedCount=11;p.NotifyCommitted("skip");p.NotifyCommitted("skip");
        Require(a.calls==0 && p.CommittedCueCount==1 && ReferenceEquals(p.FoleySource.clip,((GenericMedia)p.Media).skip),"Skip retains one generic cue, never fanfare catch-up");
    }
    private static void InspectAndBackRemainSilent() {
        var p=New();var a=new AssignedMedia();p.RevealSounds=a;p.NotifyCommitted("next");
        p.View.inspectIndex=7;p.NotifyCommitted("inspect:7");p.NotifyCommitted("next");p.View.inspectIndex=-1;p.NotifyCommitted("back");p.NotifyCommitted("next");
        Require(a.calls==1 && p.CommittedCueCount==1,"Inspect/back never create or replay pull music");
    }
    private static void FailedWriteThenRetryOnce() {
        var p=New();var a=new AssignedMedia();p.RevealSounds=a;p.View.stage="error";p.NotifyCommitted("next");
        Require(a.calls==0 && p.CommittedCueCount==0,"Unacknowledged write error cannot classify or play sound");
        p.View.stage="revealing";p.NotifyCommitted("next");p.NotifyCommitted("next");
        Require(a.calls==1 && p.CommittedCueCount==1,"Successful retry cues the same absolute target once");
    }
    private static void ReplacementStopsWithoutReplay() {
        var p=New();var a=new AssignedMedia();p.RevealSounds=a;p.NotifyCommitted("next");int stops=p.FoleySource.stops;
        p.RevealSounds=a;Require(p.FoleySource.isPlaying && p.FoleySource.stops==stops,"Idempotent provider binding must not interrupt");
        var replacement=new AssignedMedia();p.RevealSounds=replacement;
        Require(!p.FoleySource.isPlaying && p.FoleySource.clip==null,"Replacing assignments stops old clip");
        p.NotifyCommitted("next");Require(replacement.calls==0 && p.CommittedCueCount==1,"Replacing assignments never replays consumed acknowledgement");
    }
    private static void StartupBindingIsSilent() {
        var p=New();p.RevealSounds=new AssignedMedia();
        Require(p.FoleySource.plays==0 && p.CommittedCueCount==0,"Startup/resume projection and binding never play sound without an acknowledgement");
    }
    private static void NewTransactionReusesPositions() {
        var p=New();var a=new AssignedMedia();p.RevealSounds=a;p.NotifyCommitted("next");p.View.transactionId="transaction-B";p.NotifyCommitted("next");
        Require(a.calls==2 && p.CommittedCueCount==2,"Different transactions can cue identical reveal positions");
    }
    private static void InvalidProjectionsDoNotLookup() {
        string[] cases={"no-view","no-transaction","no-cards","wrong-card-count","zero-revealed","too-many-revealed","sealed","inspection"};
        foreach(string invalid in cases) {
            var p=New();var a=new AssignedMedia();p.RevealSounds=a;
            if(invalid=="no-view")p.View=null;
            else if(invalid=="no-transaction")p.View.transactionId="";
            else if(invalid=="no-cards")p.View.cards=null;
            else if(invalid=="wrong-card-count")p.View.cards=new OpeningCardView[10];
            else if(invalid=="zero-revealed")p.View.revealedCount=0;
            else if(invalid=="too-many-revealed")p.View.revealedCount=12;
            else if(invalid=="sealed")p.View.stage="sealed";
            else p.View.inspectIndex=7;
            p.NotifyCommitted("next");Require(a.calls==0 && p.CommittedCueCount==0,"No lookup on incompatible projection: "+invalid);
        }
    }
    private static void VolumeRemainsMasterControl() {
        var p=New();p.RevealSounds=new AssignedMedia();p.View.volume=.25f;p.NotifyCommitted("next");
        Require(p.FoleySource.volume==.25f,"Assigned sounds respect existing master volume");
        p.View.volume=0;p.NotifyCommitted("volume-down");
        Require(!p.FoleySource.isPlaying && p.FoleySource.clip==null,"Muting stops current clip, even outside another reveal");
    }
    public static int Main(string[] args) {
        var tests=new Dictionary<string,Action> {
            {"assigned-current-reveal",AssignedCurrentReveal},
            {"binder-block-consumes-silently",BinderBlockConsumesSilently},
            {"overlay-stops-current-cue",OverlayStopsCurrentCue},
            {"duplicate-and-next-target",DuplicateAndNextTarget},
            {"all-eleven-positions",AllElevenPositions},
            {"mute-consumes-silently",()=>SuppressionConsumes("mute")},
            {"pause-consumes-silently",()=>SuppressionConsumes("pause")},
            {"focus-consumes-silently",()=>SuppressionConsumes("focus")},
            {"overlay-consumes-silently",()=>SuppressionConsumes("overlay")},
            {"missing-clip-fallback",MissingClipFallsBack},
            {"legacy-provider-fallback",MissingProviderKeepsLegacy},
            {"audio-without-image-media",OptionalAudioWithoutImageMedia},
            {"skip-never-looks-up-pulls",SkipNeverLooksUpPulls},
            {"inspect-back-silent",InspectAndBackRemainSilent},
            {"failed-write-retry-once",FailedWriteThenRetryOnce},
            {"replacement-stops-no-replay",ReplacementStopsWithoutReplay},
            {"startup-binding-silent",StartupBindingIsSilent},
            {"new-transaction-same-position",NewTransactionReusesPositions},
            {"invalid-projections-no-lookup",InvalidProjectionsDoNotLookup},
            {"master-volume",VolumeRemainsMasterControl}
        };
        int failed=0,count=0;foreach(var test in tests) {
            if(args.Length>0 && args[0]!="" && args[0]!=test.Key)continue;
            count++;try {test.Value();Console.WriteLine("PASS "+test.Key);}catch(Exception e){failed++;Console.WriteLine("FAIL "+test.Key+": "+e.Message);}
        }
        Console.WriteLine("CHECKS "+count+" FAILED "+failed);return count==0||failed>0?1:0;
    }
}
