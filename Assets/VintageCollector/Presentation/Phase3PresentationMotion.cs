using System;

namespace VintageCollector.Presentation
{
    // Pure, deterministic decoration only. No DTO writes, RNG, authority or delayed callbacks.
    internal sealed class Phase3PresentationMotion
    {
        public float Clock { get; private set; }
        private float revealStart=float.NegativeInfinity, inspectionStart=float.NegativeInfinity;
        private int inspected=-1;
        public float RevealLift { get { return Decay(revealStart,0.28f); } }
        public float InspectionLift { get { return Decay(inspectionStart,0.22f); } }
        private float Decay(float start,float duration) {
            float p=Math.Max(0,1-(Clock-start)/duration); return p*p;
        }
        public void Advance(float delta,bool running) {
            if(running && delta>0 && !float.IsNaN(delta) && !float.IsInfinity(delta)) Clock+=delta;
        }
        public void PulseReveal() { revealStart=Clock; }
        public void Inspect(int index) {
            if(inspected==index) return;
            inspected=index; inspectionStart=index>=0?Clock:float.NegativeInfinity;
        }
    }
}
