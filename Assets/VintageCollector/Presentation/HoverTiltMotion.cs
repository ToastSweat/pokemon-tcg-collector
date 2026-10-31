namespace VintageCollector.Presentation {
    public sealed class HoverTiltMotion {
        public const float MaxPitchDegrees=12f, MaxYawDegrees=12f;
        public float PitchDegrees {get;private set;}
        public float YawDegrees {get;private set;}
        public float TargetPitchDegrees {get;private set;}
        public float TargetYawDegrees {get;private set;}
        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        static float Unit(float value) { return System.Math.Max(0f,System.Math.Min(1f,value)); }
        public void SetPointer(float u,float v,bool inside) {
            if(!inside || !Finite(u) || !Finite(v)) { TargetPitchDegrees=TargetYawDegrees=0f;return; }
            TargetYawDegrees=(Unit(u)*2f-1f)*MaxYawDegrees;
            TargetPitchDegrees=(1f-Unit(v)*2f)*MaxPitchDegrees;
        }
        public void Advance(float deltaSeconds,bool running) {
            if(!running || !Finite(deltaSeconds) || deltaSeconds<=0f)return;
            // Exact first-order response; double intermediates avoid cancellation/drift.
            double weight=1-System.Math.Exp(-12.0*deltaSeconds);
            PitchDegrees=(float)(PitchDegrees+(TargetPitchDegrees-(double)PitchDegrees)*weight);
            YawDegrees=(float)(YawDegrees+(TargetYawDegrees-(double)YawDegrees)*weight);
        }
        public void Reset() { PitchDegrees=YawDegrees=TargetPitchDegrees=TargetYawDegrees=0f; }
    }
}
