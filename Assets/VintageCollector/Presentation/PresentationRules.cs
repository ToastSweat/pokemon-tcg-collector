using System;

namespace VintageCollector.Presentation
{
    // Pure projection policy; deliberately no Unity, generator or save dependency.
    public struct PresentationBox
    {
        public float x, y, width, height;
        public PresentationBox(float x, float y, float width, float height)
        { this.x=x; this.y=y; this.width=width; this.height=height; }
    }

    public sealed class PresentationLayout
    {
        public float scale, width, height;
        public PresentationBox table, sidebar, heroCard, primary, secondary, pause, quit, modal, resume, back, mute, quieter, louder;
        public PresentationBox[] resultsCards, revealedButtons;
    }

    public static class PresentationRules
    {
        public static PresentationLayout Layout(int pixelWidth, int pixelHeight)
        {
            var l = new PresentationLayout();
            l.scale = Math.Min(Math.Max(1,pixelWidth)/1600f, Math.Max(1,pixelHeight)/900f);
            l.width = Math.Max(1,pixelWidth)/l.scale; l.height = Math.Max(1,pixelHeight)/l.scale;
            float w=l.width, h=l.height;
            l.table = new PresentationBox(44,178,w-532,h-302);
            l.sidebar = new PresentationBox(w-460,178,416,h-302);
            float cardHeight=Math.Min(480,l.table.height-100), cardWidth=cardHeight*63/88;
            l.heroCard = new PresentationBox(l.table.x+(l.table.width-cardWidth)/2,l.table.y+(l.table.height-cardHeight)/2,cardWidth,cardHeight);
            l.primary = new PresentationBox(w-430,h-278,356,58);
            l.secondary = new PresentationBox(w-430,h-206,356,46);
            l.pause = new PresentationBox(w-300,h-99,156,44);
            l.quit = new PresentationBox(w-130,h-99,86,44);
            l.modal = new PresentationBox((w-1000)/2,(h-680)/2,1000,680);
            l.back = new PresentationBox(l.modal.x+560,l.modal.y+588,396,54);
            float px=(w-660)/2, py=(h-470)/2;
            l.resume = new PresentationBox(px+40,py+314,580,58);
            l.quieter = new PresentationBox(px+40,py+222,88,52);
            l.louder = new PresentationBox(px+146,py+222,88,52);
            l.mute = new PresentationBox(px+254,py+222,366,52);
            l.revealedButtons = new PresentationBox[11];
            float historyX=l.heroCard.x+l.heroCard.width+48;
            for(int i=0;i<11;i++) l.revealedButtons[i]=new PresentationBox(historyX+(i%3)*76,l.heroCard.y+120+(i/3)*52,64,42);
            l.resultsCards = new PresentationBox[11];
            float rh=Math.Min(250,(h-386)/2), rw=rh*63/88, gap=22;
            float startX=(w-(rw*6+gap*5))/2;
            for(int i=0;i<11;i++) l.resultsCards[i]=new PresentationBox(startX+(i%6)*(rw+gap),218+(i/6)*(rh+28),rw,rh);
            return l;
        }

        public static bool IsFoil(string finish)
        {
            return string.Equals(finish,"holo",StringComparison.OrdinalIgnoreCase);
        }

        public static int VisibleCount(OpeningView v)
        {
            if (v == null || v.cards == null || (v.stage != "revealing" && v.stage != "results")) return 0;
            return Math.Max(0, Math.Min(v.cards.Length, v.revealedCount));
        }

        public static bool CanInspect(OpeningView v, int index)
        {
            return index >= 0 && index < VisibleCount(v) && v.cards[index] != null;
        }

        public static bool CanEmit(OpeningView v, string command)
        {
            if (v == null || string.IsNullOrEmpty(command)) return false;
            if (command == "quit") return true;
            if (command.StartsWith("volume:", StringComparison.Ordinal))
            {
                float value;
                return float.TryParse(command.Substring(7), System.Globalization.NumberStyles.AllowDecimalPoint,
                    System.Globalization.CultureInfo.InvariantCulture, out value) && value >= 0 && value <= 1;
            }
            if (command == "close-inspect") return v.inspectIndex >= 0;
            if (v.paused) return command == "resume";
            if (command == "pause") return true;
            if (v.inspectIndex >= 0) return false;
            if (command.StartsWith("inspect:", StringComparison.Ordinal))
            {
                int index;
                return int.TryParse(command.Substring(8), System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out index) && CanInspect(v, index);
            }
            switch (command)
            {
                case "generate": return v.stage == "ready" || (v.stage == "results" && v.cards != null && v.cards.Length == 11 && v.revealedCount == 11);
                case "open": return v.stage == "sealed";
                case "next":
                case "skip": return v.stage == "revealing" && v.cards != null && v.revealedCount >= 0 && v.revealedCount < v.cards.Length;
                case "retry": return v.stage == "error";
                default: return false;
            }
        }

        public static string BackCommand(OpeningView v)
        {
            if (v == null) return "";
            if (v.inspectIndex >= 0) return "close-inspect";
            return v.paused ? "" : "pause";
        }
    }
}
