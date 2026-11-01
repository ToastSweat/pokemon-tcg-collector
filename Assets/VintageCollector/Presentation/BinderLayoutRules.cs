using System;
using System.Globalization;

namespace VintageCollector.Presentation
{
    /// <summary>Presentation policy only; never decides eligibility, ownership or archive validity.</summary>
    public static class BinderLayoutRules
    {
        public struct Metrics { public float scale, width, height, bodyHeight; }
        public static Metrics Geometry(float pixelWidth,float pixelHeight) {
            float scale=Math.Max(0.75f,Math.Min(pixelWidth/1600f,pixelHeight/900f));
            float height=pixelHeight/scale;
            return new Metrics {scale=scale,width=pixelWidth/scale,height=height,bodyHeight=Math.Max(100,Math.Min(600,height-300))};
        }
        public static BinderCardView[] SortedRows(BinderView view) {
            var rows=new System.Collections.Generic.List<BinderCardView>();
            if(view!=null && view.rows!=null) foreach(var row in view.rows) if(row!=null && !string.IsNullOrEmpty(row.id)) rows.Add(row);
            rows.Sort((a,b)=> {
                int an,bn;
                if(!int.TryParse(a.collectorNumber,NumberStyles.None,CultureInfo.InvariantCulture,out an)) an=int.MaxValue;
                if(!int.TryParse(b.collectorNumber,NumberStyles.None,CultureInfo.InvariantCulture,out bn)) bn=int.MaxValue;
                int n=an.CompareTo(bn); return n!=0?n:string.CompareOrdinal(a.id,b.id);
            });
            return rows.ToArray();
        }
        public const int PageSize = 12;
        public static readonly string[] Filters = { "all", "owned", "missing", "duplicates", "holo" };
        public static readonly string[] Conditions = { "unknown", "near-mint", "lightly-played", "moderately-played", "heavily-played", "damaged" };
        private static bool Known(string[] values, string value) { return Array.IndexOf(values,value)>=0; }
        public static bool HasReview(BinderView view) { return view!=null && !string.IsNullOrWhiteSpace(view.importSummary); }
        public static string Condition(BinderView view) { return view!=null && Known(Conditions,view.selectedCondition)?view.selectedCondition:"unknown"; }
        public static BinderCardView Selected(BinderView view) {
            if(view==null || view.rows==null || string.IsNullOrEmpty(view.selectedCardId)) return null;
            foreach(var row in view.rows) if(row!=null && row.id==view.selectedCardId) return row;
            return null;
        }
        public static int RowCount(BinderView view) {
            int count=0; if(view!=null && view.rows!=null) foreach(var row in view.rows) if(row!=null && !string.IsNullOrEmpty(row.id)) count++;
            return count;
        }
        public static int PageCount(BinderView view) { return Math.Max(1,(RowCount(view)+PageSize-1)/PageSize); }
        private static bool Index(string text,int count) { int n; return int.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture,out n) && n>=0 && n<count; }
        public static bool CanEmit(BinderView view, string command) {
            if(view==null || string.IsNullOrEmpty(command)) return false;
            if(command=="binder-open") return !view.open;
            if(!view.open) return false;
            if(command=="binder-close" || command=="quit") return true;
            if(view.busy || view.restartRequired) return false;
            if(view.pendingPhysicalEdit) return command=="save-physical";
            if(view.pendingImportConfirm) return command=="import-confirm" && HasReview(view);
            if(command=="import-cancel") return HasReview(view);
            if(command=="import-confirm") return HasReview(view) && string.IsNullOrEmpty(view.error);
            if(HasReview(view)) return false;
            if(command.StartsWith("binder-set:",StringComparison.Ordinal))
                return view.canSelectSet && string.IsNullOrEmpty(view.error) && PresentationRules.IsSupportedSet(view.supportedSets,command.Substring(11));
            if(command=="mode:virtual" || command=="mode:physical") return true;
            if(command.StartsWith("filter:",StringComparison.Ordinal)) return Known(Filters,command.Substring(7));
            if(command.StartsWith("search:",StringComparison.Ordinal)) return true;
            if(command.StartsWith("page:",StringComparison.Ordinal)) return Index(command.Substring(5),PageCount(view));
            if(command.StartsWith("select:",StringComparison.Ordinal)) {
                if(view.rows==null) return false;
                foreach(var row in view.rows) if(row!=null && !string.IsNullOrEmpty(row.id) && row.id==command.Substring(7)) return true;
                return false;
            }
            if(command=="backup") return string.IsNullOrEmpty(view.error);
            if(command.StartsWith("backup-select:",StringComparison.Ordinal)) return Index(command.Substring(14),view.backups==null?0:view.backups.Length);
            if(command=="import-preview") return view.backups!=null && view.selectedBackupIndex>=0 && view.selectedBackupIndex<view.backups.Length && !string.IsNullOrEmpty(view.backups[view.selectedBackupIndex]);
            if(view.mode!="physical" || Selected(view)==null) return false;
            if(command=="save-physical") return string.IsNullOrEmpty(view.error);
            if(command.StartsWith("quantity:",StringComparison.Ordinal)) return true;
            if(command.StartsWith("condition:",StringComparison.Ordinal)) return Known(Conditions,command.Substring(10));
            return false;
        }
    }
}
