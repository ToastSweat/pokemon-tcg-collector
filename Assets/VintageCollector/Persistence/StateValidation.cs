using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using VintageCollector.Domain;
namespace VintageCollector.Persistence
{
    // A supported schema is a promise to interpret every award and every reveal, not just parse JSON.
    internal static class StateValidation
    {
        internal static void Identity(PackRecordData p,Catalog catalog) { Identity(p,ApprovedSetRegistry.ForLegacyBase(catalog)); }
        internal static void Identity(PackRecordData p,ApprovedSetRegistry registry)
        {
            if(p==null) throw new InvalidDataException("Null pack record");
            if(string.IsNullOrWhiteSpace(p.transactionId) || p.transactionId.Length>256 || p.transactionId.Trim()!=p.transactionId) throw new InvalidDataException("Invalid transactionId");
            foreach(char c in p.transactionId) if(char.IsControl(c)) throw new InvalidDataException("Control in transactionId");
            DateTime date;
            if(p.createdUtc==null || !p.createdUtc.EndsWith("Z",StringComparison.Ordinal) || !DateTime.TryParseExact(p.createdUtc,new[]{"yyyy-MM-dd'T'HH:mm:ss'Z'","yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"},CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal|DateTimeStyles.AdjustToUniversal,out date)) throw new InvalidDataException("createdUtc must be ISO 8601 UTC (Z)");
            if(p.seed==0) throw new InvalidDataException("Zero seed");
            ApprovedSetDefinition definition;
            try { definition=registry.FindSet(p.printingId); }
            catch(Exception e) { if(!(e is KeyNotFoundException) && !(e is ArgumentNullException)) throw; throw new InvalidDataException("Unsupported printing",e); }
            if(p.catalogVersion!=definition.CatalogVersion || p.modelId!=definition.ModelId || p.modelVersion!=definition.ModelVersion || p.status!=definition.Status || p.rngAlgorithm!=definition.RngAlgorithm) throw new InvalidDataException("Unsupported catalog/model tuple");
            if(p.orderedCardIds==null || p.orderedCardIds.Length!=11 || p.newToVirtual==null || p.newToVirtual.Length!=11) throw new InvalidDataException("Expected 11 card identities and new flags");
            if(p.revealedCount<0 || p.revealedCount>11 || (p.revealedCount>0 && !p.wrapperOpened)) throw new InvalidDataException("Invalid wrapper/reveal state");
            try { definition.ValidateOrderedCardIds(p.orderedCardIds); }
            catch(ArgumentException e) { throw new InvalidDataException("Invalid approved pack identities",e); }
        }
        internal static void Session(SessionData d,Catalog catalog) { Session(d,ApprovedSetRegistry.ForLegacyBase(catalog)); }
        internal static void Session(SessionData d,ApprovedSetRegistry registry)
        {
            if(d==null) throw new InvalidDataException("Missing session");
            if(d.schemaVersion!=1) throw new InvalidDataException("Unsupported session schemaVersion: "+d.schemaVersion);
            if(d.packs==null) throw new InvalidDataException("Missing archive");
            var transactions=new HashSet<string>(StringComparer.Ordinal); var seen=new HashSet<string>(StringComparer.Ordinal);
            long min=d.packs.Length,max=d.packs.Length; int total=0;
            for(int i=0;i<d.packs.Length;i++) {
                var p=d.packs[i]; Identity(p,registry);
                if(!transactions.Add(p.transactionId)) throw new InvalidDataException("Duplicate transactionId");
                if(i<d.packs.Length-1 && p.revealedCount!=11) throw new InvalidDataException("Incomplete earlier archive entry");
                if(p.wrapperOpened) { min++; max++; }
                if(p.revealedCount>0) min++;
                max+=p.revealedCount;
                for(int j=0;j<11;j++) if(p.newToVirtual[j]!=seen.Add(p.orderedCardIds[j])) throw new InvalidDataException("Inconsistent first-time virtual flags");
                total=checked(total+11);
            }
            if(d.revision<min || d.revision>max) throw new InvalidDataException("Revision cannot describe this archive");
        }
    }
}
