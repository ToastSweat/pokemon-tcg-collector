using System;
using System.Collections.Generic;
using VintageCollector.Presentation;
public static class SurfaceMotionTests {
    static readonly List<string> checks=new List<string>();
    static void Check(bool value,string name) { if(!value)throw new Exception(name);checks.Add(name); }
    static bool Near(float a,float b) {return Math.Abs(a-b)<0.0001f;}
    public static int Main(string[] args) {
        try {
            var m=new HoverTiltMotion();m.SetPointer(1,.5f,true);m.Advance(.1f,true);
            Check(m.YawDegrees>0 && m.YawDegrees<12 && m.PitchDegrees==0,"right smooth yaw without overshoot");
            if(args.Length==0 || args[0]!="slice1") {
                m.SetPointer(0,.5f,true);m.Advance(10,true);Check(Near(m.YawDegrees,-12),"left yaw");
                m.SetPointer(.5f,0,true);m.Advance(10,true);Check(Near(m.PitchDegrees,12),"up pitch");
                m.SetPointer(.5f,1,true);m.Advance(10,true);Check(Near(m.PitchDegrees,-12),"down pitch");
                m.SetPointer(.5f,.5f,true);m.Advance(10,true);Check(Near(m.PitchDegrees,0)&&Near(m.YawDegrees,0),"center neutral");
                m.SetPointer(100,-100,true);Check(m.TargetYawDegrees==12&&m.TargetPitchDegrees==12,"pointer clamp");
                m.Advance(.1f,true);float p=m.PitchDegrees,y=m.YawDegrees;
                m.SetPointer(0,1,true);m.Advance(1,false);Check(p==m.PitchDegrees&&y==m.YawDegrees,"running false exactly freezes");
                foreach(float dt in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1f,0f})m.Advance(dt,true);
                Check(p==m.PitchDegrees&&y==m.YawDegrees,"invalid delta does not move");
                m.SetPointer(float.NaN,1,true);Check(m.TargetYawDegrees==0&&m.TargetPitchDegrees==0,"NaN pointer neutral");
                m.SetPointer(1,float.PositiveInfinity,true);Check(m.TargetYawDegrees==0&&m.TargetPitchDegrees==0,"infinite pointer neutral");
                m.SetPointer(1,0,true);m.Advance(float.MaxValue,true);Check(m.YawDegrees==12&&m.PitchDegrees==12,"finite extreme delta safe");
                m.SetPointer(1,0,false);m.Advance(.01f,true);Check(m.PitchDegrees>0&&m.PitchDegrees<12,"outside smooth return");m.Advance(20,true);Check(Near(m.PitchDegrees,0)&&Near(m.YawDegrees,0),"outside reaches neutral");
                var a=new HoverTiltMotion();var b=new HoverTiltMotion();a.SetPointer(.9f,.2f,true);b.SetPointer(.9f,.2f,true);
                for(int i=0;i<30;i++)a.Advance(1f/30,true);for(int i=0;i<144;i++)b.Advance(1f/144,true);
                Check(Near(a.PitchDegrees,b.PitchDegrees)&&Near(a.YawDegrees,b.YawDegrees),"frame rate independent exponential");
                for(int i=0;i<200;i++){m.SetPointer(i%2,1-i%2,true);m.Advance(.003f,true);Check(Math.Abs(m.PitchDegrees)<=12&&Math.Abs(m.YawDegrees)<=12,"bounded reversal "+i);}
                m.Reset();Check(m.PitchDegrees==0&&m.YawDegrees==0&&m.TargetPitchDegrees==0&&m.TargetYawDegrees==0,"reset all state");
            }
            Console.WriteLine("{\"passed\":true,\"checkCount\":"+checks.Count+",\"checks\":[\""+string.Join("\",\"",checks.ToArray())+"\"]}");return 0;
        }catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
