using System;
using System.Reflection;

public static class AppearanceChecks
{
    static void Require(bool value,string message) {if(!value)throw new Exception(message);}
    static int Variant(string transaction) {
        var type=typeof(AppearanceChecks).Assembly.GetType("VintageCollector.Presentation.WrapperVariant");
        Require(type!=null,"Missing presentation-only wrapper variant selector");
        return (int)type.GetMethod("ForTransaction").Invoke(null,new object[]{transaction});
    }
    static void Stable() {int actual=Variant("fixture-pack");Require(actual>=0&&actual<3,"A pack must choose one of the three wrapper bindings");Require(actual==Variant("fixture-pack"),"The same transaction must retain its wrapper");}
    static void IdlePreview() {Require(Variant(null)==0 && Variant("")==0,"Idle/no-transaction preview must use A without inventing a pack");}
    static void FixedUtf8Vectors() {
        Require(Variant("fixture-pack")==2,"Pinned appearance identity must not drift");
        Require(Variant("transaction-A")==0 && Variant("caseSensitive")==1,"Independent Python FNV/UTF8 vectors must match");
        Require(Variant("Pokémon-交易")==1,"UTF8 transaction encoding must be portable");
    }
    static void ThreeVariantsReachable() {
        var seen=new System.Collections.Generic.HashSet<int>();
        for(int i=0;i<300;i++) {string id="fixture-"+i.ToString(System.Globalization.CultureInfo.InvariantCulture);int variant=Variant(id);Require(variant>=0&&variant<3 && variant==Variant(id),"All fixture transactions must be bounded and stable");seen.Add(variant);}
        Require(seen.SetEquals(new[]{0,1,2}),"All three wrapper designs must be reachable without generation RNG");
    }
    static void CultureIndependent() {
        var before=System.Globalization.CultureInfo.CurrentCulture;
        try {int expected=Variant("transaction-I-İ");System.Globalization.CultureInfo.CurrentCulture=new System.Globalization.CultureInfo("tr-TR");Require(Variant("transaction-I-İ")==expected,"Appearance must not depend on display culture");}
        finally {System.Globalization.CultureInfo.CurrentCulture=before;}
    }
    public static int Main() {
        int failures=0;var tests=new System.Collections.Generic.Dictionary<string,Action> {
            {"stable-bounded-transaction-variant",Stable},{"idle-preview-A",IdlePreview},
            {"fixed-UTF8-vectors",FixedUtf8Vectors},{"three-variants-reachable",ThreeVariantsReachable},{"culture-independent",CultureIndependent}
        };
        foreach(var test in tests)try{test.Value();Console.WriteLine("PASS "+test.Key);}catch(Exception error){failures++;Console.WriteLine("FAIL "+test.Key+": "+error.Message);}
        Console.WriteLine("CHECKS "+tests.Count+" FAILED "+failures);return failures==0?0:1;
    }
}
