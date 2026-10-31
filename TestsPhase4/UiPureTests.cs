using System;
using System.Collections.Generic;
using VintageCollector.Presentation;

public static class UiPureTests
{
    private static readonly List<string> checks = new List<string>();
    private static bool passed = true;
    private static void Check(bool value, string name) {
        checks.Add("{\"name\":\""+name+"\",\"passed\":"+(value?"true":"false")+"}"); passed &= value;
    }
    public static BinderView Fixture() {
        return new BinderView {open=true,mode="physical",filter="all",selectedCardId="synthetic:base:001:unstamped:holo", selectedCondition="unknown",quantityText="2", targetCount=101,uniqueOwned=1,totalQuantity=7,completionPercent=100.0/101,
            rows=new[] { new BinderCardView { id="synthetic:base:001:unstamped:holo", name="Original sample one", collectorNumber="1", finish="holo",quantity=7,owned=true } },backups=new[] {"synthetic-backup.zip"},selectedBackupIndex=0};
    }
    public static int Main() {
        Console.OutputEncoding = new System.Text.UTF8Encoding(false);
        var v=Fixture();
        foreach(var c in new[]{"binder-close","quit","mode:virtual","mode:physical","filter:all","filter:owned","filter:missing","filter:duplicates","filter:holo","search:","search:A:b é","page:0","select:"+v.selectedCardId,"condition:unknown","condition:near-mint","condition:lightly-played","condition:moderately-played","condition:heavily-played","condition:damaged","quantity:not an integer","quantity:","save-physical","backup","backup-select:0","import-preview"}) Check(BinderLayoutRules.CanEmit(v,c),"accept "+c);
        foreach(var c in new[]{"binder-open","mode:other","filter:rare","page:-1","page:999999999999","page:1","select:wrong","condition:graded","backup-select:1","backup-select:-1","import-confirm","import-cancel","generate","volume:0","quantityx:3"}) Check(!BinderLayoutRules.CanEmit(v,c),"reject "+c);
        v.mode="virtual";
        foreach(var c in new[]{"save-physical","condition:unknown","quantity:3"}) Check(!BinderLayoutRules.CanEmit(v,c),"virtual read only "+c);
        v.mode="physical";v.selectedCardId="hidden";Check(!BinderLayoutRules.CanEmit(v,"save-physical"),"hidden selection cannot save");
        v=Fixture();v.importSummary="Reviewed synthetic backup: virtual 2 physical 3";
        Check(BinderLayoutRules.CanEmit(v,"import-confirm"),"review enables explicit confirmation");
        Check(BinderLayoutRules.CanEmit(v,"import-cancel"),"review cancel reachable");
        foreach(var c in new[]{"save-physical","backup","mode:virtual","import-preview","backup-select:0"}) Check(!BinderLayoutRules.CanEmit(v,c),"review locks context "+c);
        v.error="Preview failed; draft retained";
        Check(!BinderLayoutRules.CanEmit(v,"import-confirm"),"error prevents import confirmation");
        Check(BinderLayoutRules.CanEmit(v,"import-cancel"),"failed review still cancellable");
        v=Fixture();v.error="unacknowledged edit";
        Check(!BinderLayoutRules.CanEmit(v,"backup"),"error prevents export of unacknowledged collection");
        Check(!BinderLayoutRules.CanEmit(v,"save-physical"),"error prevents physical save");
        Check(BinderLayoutRules.CanEmit(v,"quantity:draft retained"),"error allows draft repair");
        foreach(var state in new[]{"busy","restart"}) {
            v=Fixture();v.busy=state=="busy";v.restartRequired=state=="restart";
            foreach(var c in new[]{"save-physical","backup","import-preview","import-confirm","condition:unknown","quantity:4","mode:virtual","search:test","page:0","select:"+v.selectedCardId}) Check(!BinderLayoutRules.CanEmit(v,c),state+" locks "+c);
            Check(BinderLayoutRules.CanEmit(v,"binder-close"),state+" back reachable");Check(BinderLayoutRules.CanEmit(v,"quit"),state+" quit reachable");
        }
        v=Fixture();v.open=false;
        Check(BinderLayoutRules.CanEmit(v,"binder-open"),"closed binder opens");Check(!BinderLayoutRules.CanEmit(v,"backup"),"closed binder cannot export");
        Check(!BinderLayoutRules.CanEmit(null,"quit"),"null view safe");
        v=Fixture();v.rows=new[] {new BinderCardView {id="n10",collectorNumber="10"},null,new BinderCardView {id="n2",collectorNumber="2"},new BinderCardView {id="n9",collectorNumber="9"},new BinderCardView {id="n1",collectorNumber="1"},new BinderCardView {id=null}};
        var sorted=typeof(BinderLayoutRules).GetMethod("SortedRows");
        Check(sorted!=null,"safe numeric row ordering API exists");
        if(sorted!=null) {
            var rows=(BinderCardView[])sorted.Invoke(null,new object[]{v});
            Check(rows.Length==4 && rows[0].id=="n1" && rows[1].id=="n2" && rows[2].id=="n9" && rows[3].id=="n10","numeric set order excludes null records");
            Check(v.rows[0].id=="n10" && v.rows[1]==null,"sorting never mutates source array");
        }
        Check(BinderLayoutRules.Selected(v)==null,"stale selection does not leak metadata");
        Check(BinderLayoutRules.Condition(v)=="unknown","unknown default condition");
        v.selectedCondition="mint-fiction";Check(BinderLayoutRules.Condition(v)=="unknown","unknown invalid condition fallback");
        v.rows=new BinderCardView[101];for(int i=0;i<101;i++)v.rows[i]=new BinderCardView {id="eligible-"+i};
        Check(BinderLayoutRules.PageCount(v)==9,"101 supplied slots span nine pages");
        Check(BinderLayoutRules.CanEmit(v,"page:8") && !BinderLayoutRules.CanEmit(v,"page:9"),"last page boundaries");
        v.rows=null;Check(BinderLayoutRules.PageCount(v)==1 && BinderLayoutRules.RowCount(v)==0,"empty data safe page");
        var geometry=typeof(BinderLayoutRules).GetMethod("Geometry");Check(geometry!=null,"responsive layout API exists");
        if(geometry!=null) foreach(var dims in new[]{new[]{1280,720},new[]{1600,900},new[]{2560,1080},new[]{1280,480}}) {
            var g=geometry.Invoke(null,new object[]{(float)dims[0],(float)dims[1]});var t=g.GetType();
            float scale=(float)t.GetField("scale").GetValue(g),width=(float)t.GetField("width").GetValue(g),height=(float)t.GetField("height").GetValue(g),body=(float)t.GetField("bodyHeight").GetValue(g);
            Check(scale>=0.75f && Math.Abs(width*scale-dims[0])<0.01f && Math.Abs(height*scale-dims[1])<0.01f,"full viewport "+dims[0]+"x"+dims[1]);
            Check(body>=100 && body<=600 && height-body>=290,"fixed header footer reachable "+dims[0]+"x"+dims[1]);
        }
        Console.WriteLine("{\"passed\":"+(passed?"true":"false")+",\"testCount\":"+checks.Count+",\"checks\":["+string.Join(",",checks.ToArray())+"]}");
        return passed?0:1;
    }
}
