using System;
using System.IO;
using System.Web.Script.Serialization;
using VintageCollector.Domain;

// Real proposed catalog; compiles only with unchanged production Data.cs/Catalog.cs.
public static class JungleCatalogChecks
{
    public static int Main(string[] args)
    {
        try
        {
            var serializer = new JavaScriptSerializer();
            var input = File.ReadAllText(args[0]);
            var data = serializer.Deserialize<CatalogData>(input);
            var catalog = Catalog.FromData(data);
            int checks = 0;
            if (catalog.Cards.Count != 64) throw new Exception("Expected 64 real members");
            Console.WriteLine("PASS real Catalog.FromData loads 64 Jungle members"); checks++;
            foreach (var c in catalog.Cards)
            {
                if (c.PrintingId != "jungle-en-unlimited1999" || c.Edition != "unlimited" || c.Variant != "standard") throw new Exception("Printing identity drift");
            }
            Console.WriteLine("PASS proposed standard Unlimited identities preserved"); checks++;
            if (catalog.CompletionTargetStatus != "proposed-not-approved") throw new Exception("Candidate approval drift");
            Console.WriteLine("PASS proposed-not-approved status survives production deserialization"); checks++;
            if (catalog.Find("pkmn-jungle-en-unlimited1999-064-nonholo-standard").Kind != "trainer") throw new Exception("Trainer identity drift");
            Console.WriteLine("PASS number 64 resolves as trainer, not energy"); checks++;
            data.cards[1].id = data.cards[0].id;
            bool rejected = false;
            try { Catalog.FromData(data); } catch (ArgumentException) { rejected = true; }
            if (!rejected) throw new Exception("Duplicate ID accepted");
            Console.WriteLine("PASS real Catalog.FromData rejects duplicate ID mutation"); checks++;
            Console.WriteLine("CHECK_COUNT=" + checks + " PASSED=true");
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.ToString());
            return 1;
        }
    }
}
