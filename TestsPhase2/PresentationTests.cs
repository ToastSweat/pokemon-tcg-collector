using System;
using System.Collections.Generic;
using System.Reflection;

public static class PresentationTests
{
    private static readonly List<string> passed = new List<string>();
    private static string filter;
    private static void Check(string name, Action test)
    {
        if (!name.Contains(filter)) return;
        test(); passed.Add(name);
    }
    private static void Assert(bool condition, string message)
    { if (!condition) throw new Exception(message); }
    private static double F(object box,string field) { return Convert.ToDouble(box.GetType().GetField(field).GetValue(box)); }
    private static void Bounds(object box,double scale,int w,int h) {
        Assert(F(box,"x")>=0 && F(box,"y")>=0 && F(box,"width")>0 && F(box,"height")>0,"Negative geometry");
        Assert((F(box,"x")+F(box,"width"))*scale<=w+0.01 && (F(box,"y")+F(box,"height"))*scale<=h+0.01,"Clipped control");
    }
    private static void Ratio(object box) { Assert(Math.Abs(F(box,"width")/F(box,"height")-63.0/88.0)<0.0001,"Incorrect 63:88 card ratio"); }
    private static bool Overlap(object a,object b) { return F(a,"x")<F(b,"x")+F(b,"width") && F(b,"x")<F(a,"x")+F(a,"width") && F(a,"y")<F(b,"y")+F(b,"height") && F(b,"y")<F(a,"y")+F(a,"height"); }
    public static int Main(string[] args)
    {
        filter = args.Length > 0 ? args[0] : "";
        try
        {
            Check("frozen DTO fields and defaults", () => {
                var t = Assembly.GetExecutingAssembly().GetType("VintageCollector.Presentation.OpeningView");
                Assert(t != null, "OpeningView missing");
                var v = Activator.CreateInstance(t);
                foreach (string field in new[] {"stage", "transactionId", "errorMessage", "recoveryNotice", "modelLabel", "statusLine", "saveLocation", "revealedCount", "totalVirtualCards", "cards", "paused", "volume", "inspectIndex"})
                    Assert(t.GetField(field) != null, "Frozen field missing: "+field);
                Assert((int)t.GetField("inspectIndex").GetValue(v) == -1, "Default inspection must be closed");
                Assert(Math.Abs((float)t.GetField("volume").GetValue(v) - 0.5f) < 0.001f, "Default volume must be 0.5");
                var c = Assembly.GetExecutingAssembly().GetType("VintageCollector.Presentation.OpeningCardView");
                Assert(c != null, "OpeningCardView missing");
                foreach (string field in new[] {"id", "name", "collectorNumber", "rarity", "kind", "finish", "isNew"})
                    Assert(c.GetField(field) != null, "Frozen card field missing: "+field);
            });
            Check("visibility fails closed for hidden cards and invalid results", () => {
                var t = Assembly.GetExecutingAssembly().GetType("VintageCollector.Presentation.PresentationRules");
                Assert(t != null, "Visibility policy missing");
                var visible = t.GetMethod("VisibleCount");
                var inspect = t.GetMethod("CanInspect");
                var cards = new VintageCollector.Presentation.OpeningCardView[11];
                for (int i = 0; i < 11; i++) cards[i] = new VintageCollector.Presentation.OpeningCardView { name = "SECRET-"+i };
                var v = new VintageCollector.Presentation.OpeningView { stage = "revealing", cards = cards };
                for (int n = 0; n <= 11; n++) {
                    v.revealedCount = n;
                    Assert((int)visible.Invoke(null, new object[] {v}) == n, "Wrong visible count");
                    for (int i = -1; i <= 11; i++)
                        Assert((bool)inspect.Invoke(null, new object[] {v, i}) == (i >= 0 && i < n), "Hidden inspection allowed");
                }
                foreach (string stage in new[] {"ready", "sealed", "error", "unknown", null}) {
                    v.stage = stage; v.revealedCount = 11;
                    Assert((int)visible.Invoke(null, new object[] {v}) == 0, "Faces leaked in "+stage);
                }
                v.stage = "results"; v.revealedCount = 4;
                Assert((int)visible.Invoke(null, new object[] {v}) == 4, "Incomplete results leaked");
                v.revealedCount = -1;
                Assert((int)visible.Invoke(null, new object[] {v}) == 0, "Negative reveal count");
                v.revealedCount = 50;
                Assert((int)visible.Invoke(null, new object[] {v}) == 11, "Reveal count exceeds cards");
                v.cards[0] = null;
                Assert(!(bool)inspect.Invoke(null, new object[] {v, 0}), "Null card inspectable");
                v.cards = null;
                Assert((int)visible.Invoke(null, new object[] {v}) == 0, "Null cards visible");
            });
            Check("stage command gating and invariant volume", () => {
                var t = Assembly.GetExecutingAssembly().GetType("VintageCollector.Presentation.PresentationRules");
                Assert(t.GetMethod("CanEmit") != null, "Command policy missing");
                Func<VintageCollector.Presentation.OpeningView,string,bool> can = (v,c) => (bool)t.GetMethod("CanEmit").Invoke(null, new object[] {v,c});
                var v1 = new VintageCollector.Presentation.OpeningView { stage = "ready" };
                Assert(can(v1,"generate") && !can(v1,"open") && !can(v1,"next") && !can(v1,"retry"), "Ready commands");
                v1.stage = "sealed";
                Assert(can(v1,"open") && !can(v1,"generate") && !can(v1,"skip"), "Sealed commands");
                v1.stage = "revealing"; v1.cards = new VintageCollector.Presentation.OpeningCardView[11];
                for (int i=0;i<11;i++) v1.cards[i] = new VintageCollector.Presentation.OpeningCardView();
                v1.revealedCount = 1;
                Assert(can(v1,"next") && can(v1,"skip") && can(v1,"inspect:0") && !can(v1,"inspect:1"), "Reveal controls");
                Assert(!can(v1,"inspect:-1") && !can(v1,"inspect:11") && !can(v1,"inspect:NaN"), "Index validation");
                v1.stage = "results";
                Assert(!can(v1,"generate"), "Incomplete results cannot generate");
                v1.revealedCount = 11;
                Assert(can(v1,"generate") && !can(v1,"skip") && can(v1,"inspect:10"), "Complete results controls");
                v1.paused = true;
                Assert(can(v1,"resume") && can(v1,"quit") && !can(v1,"generate") && !can(v1,"inspect:0"), "Pause blocks advance");
                v1.paused = false; v1.stage = "error";
                Assert(can(v1,"retry") && can(v1,"quit") && !can(v1,"next") && !can(v1,"generate"), "Error controls");
                foreach(string c in new[] {"volume:0", "volume:0.5", "volume:1"}) Assert(can(v1,c), "Rejected volume "+c);
                foreach(string c in new[] {"volume:NaN", "volume:Infinity", "volume:1.1", "volume:-0.1", "volume:0,5", "award", "save", "inspect:0", "", null})
                    Assert(!can(v1,c), "Invalid command "+c);
                Assert((string)t.GetMethod("BackCommand").Invoke(null,new object[]{v1}) == "pause", "Escape pause");
                v1.inspectIndex = 20;
                Assert((string)t.GetMethod("BackCommand").Invoke(null,new object[]{v1}) == "close-inspect", "Back inspection even invalid index");
                v1.inspectIndex = -1; v1.paused = true;
                Assert((string)t.GetMethod("BackCommand").Invoke(null,new object[]{v1}) == "", "Pause requires explicit Resume");
            });
            Check("responsive layout card ratio and reachable controls", () => {
                var t = Assembly.GetExecutingAssembly().GetType("VintageCollector.Presentation.PresentationRules");
                Assert(t.GetMethod("Layout") != null, "Responsive layout missing");
                foreach (var size in new[] {new[]{1280,720},new[]{1600,900},new[]{2560,1080},new[]{1280,360},new[]{800,480},new[]{640,360},new[]{900,1200}}) {
                    object l = t.GetMethod("Layout").Invoke(null,new object[]{size[0],size[1]});
                    var lt=l.GetType();
                    double scale = Convert.ToDouble(lt.GetField("scale").GetValue(l));
                    Assert(Math.Abs(scale-Math.Min(size[0]/1600.0,size[1]/900.0)) < 0.0001, "Reference scaling");
                    foreach (string field in new[]{"table","sidebar","heroCard","primary","secondary","pause","quit","modal","resume","back","mute","quieter","louder"})
                        Bounds(lt.GetField(field).GetValue(l),scale,size[0],size[1]);
                    var card = lt.GetField("heroCard").GetValue(l);
                    Ratio(card);
                    Assert(lt.GetField("revealedButtons")!=null,"Revealed history controls missing");
                    Array history=(Array)lt.GetField("revealedButtons").GetValue(l);
                    Assert(history.Length==11,"All revealed positions must remain reachable");
                    for(int i=0;i<11;i++) {
                        Bounds(history.GetValue(i),scale,size[0],size[1]);
                        Assert(!Overlap(history.GetValue(i),card),"History overlaps hero");
                        for(int j=0;j<i;j++) Assert(!Overlap(history.GetValue(i),history.GetValue(j)),"History buttons overlap");
                    }
                    Array results=(Array)lt.GetField("resultsCards").GetValue(l);
                    Assert(results.Length==11,"Need all eleven result slots");
                    for(int i=0;i<11;i++) {
                        Bounds(results.GetValue(i),scale,size[0],size[1]); Ratio(results.GetValue(i));
                        var table=lt.GetField("table").GetValue(l);
                        Assert(F(results.GetValue(i),"y")+F(results.GetValue(i),"height")<=F(table,"y")+F(table,"height")-24,"Result overlaps table footer");
                        for(int j=0;j<i;j++) Assert(!Overlap(results.GetValue(i), results.GetValue(j)),"Result overlap");
                    }
                }
            });
            Check("foil cue only on explicit holo finish", () => {
                var t=Assembly.GetExecutingAssembly().GetType("VintageCollector.Presentation.PresentationRules");
                Assert(t.GetMethod("IsFoil")!=null,"Finish policy missing");
                foreach(string finish in new[]{"holo","Holo"}) Assert((bool)t.GetMethod("IsFoil").Invoke(null,new object[]{finish}),"Holo cue absent");
                foreach(string finish in new[]{"nonHolo","non-holo","Non-holo","plain","",null}) Assert(!(bool)t.GetMethod("IsFoil").Invoke(null,new object[]{finish}),"Non-holo cue leaked");
            });
            if (passed.Count == 0) throw new Exception("No tests selected");
            Console.WriteLine("{\"passed\":true,\"checkCount\":"+passed.Count+",\"checks\":[\""+string.Join("\",\"", passed.ToArray())+"\"]}");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine("FAIL "+e); return 1; }
    }
}
