using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VintageCollector.Presentation;

public static class TiltPresentationChecks
{
    [Serializable] sealed class Report { public bool passed; public int checkCount; public string[] checks; public string failure; }
    public static void Run()
    {
        var checks=new System.Collections.Generic.List<string>(); string failure=null;
        var go=new GameObject("Owned tilt opt-in contract test");
        try {
            var ui=go.AddComponent<TabletopPresentation>();
            var field=typeof(TabletopPresentation).GetField("pointerTiltEnabled",BindingFlags.Public|BindingFlags.Instance);
            if(field==null || field.FieldType!=typeof(bool)) throw new Exception("Missing public serialized bool pointerTiltEnabled");
            checks.Add("Public bool opt-in exists");
            if((bool)field.GetValue(ui)) throw new Exception("Historical scenes must default off");
            checks.Add("Historical scenes default off");
            var serialized=new SerializedObject(ui);
            if(serialized.FindProperty("pointerTiltEnabled")==null) throw new Exception("Opt-in is not Unity serialized");
            checks.Add("Opt-in is Unity serialized");
            if(go.GetComponents<AudioSource>().Length!=0 || ui.CommittedCueCount!=0) throw new Exception("Passive opt-in contract creates no audio");
            checks.Add("Passive contract creates no audio");
            var observations=typeof(TabletopPresentation).GetProperty("TiltObservations",BindingFlags.Public|BindingFlags.Instance);
            if(observations==null || !observations.PropertyType.IsArray || observations.CanWrite) throw new Exception("Missing readonly current TiltObservations array API");
            var observationType=observations.PropertyType.GetElementType();
            if(observationType.Name!="TiltObservation" || observationType.DeclaringType!=typeof(TabletopPresentation) || !Attribute.IsDefined(observationType,typeof(SerializableAttribute))) throw new Exception("Missing nested serializable TiltObservation");
            foreach(var pair in new[]{new {name="key",type=typeof(string)},new {name="bounds",type=typeof(Rect)},new {name="pitchDegrees",type=typeof(float)},new {name="yawDegrees",type=typeof(float)},new {name="hovered",type=typeof(bool)},new {name="finish",type=typeof(int)},new {name="renderFrame",type=typeof(int)}}) {
                var f=observationType.GetField(pair.name);if(f==null || f.FieldType!=pair.type) throw new Exception("Observation field mismatch: "+pair.name);
            }
            checks.Add("Frozen nested observation API has exact public serializable fields");
            if(((Array)observations.GetValue(ui,null)).Length!=0) throw new Exception("Unrendered view leaks surfaces");
            checks.Add("Unrendered view exposes no surfaces");
            if(typeof(TabletopPresentation).GetMethod("DrawFace",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(Rect),typeof(OpeningCardView),typeof(int)},null)==null || typeof(TabletopPresentation).GetMethod("DrawBack",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(Rect)},null)==null) throw new Exception("Accepted native reflective drawing seams changed");
            checks.Add("Accepted native drawing fixture seams remain exact");
        } catch(Exception e) { failure=e.ToString(); }
        finally { UnityEngine.Object.DestroyImmediate(go); }
        File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("TILT_PRESENTATION_OUT"),"results.json"),JsonUtility.ToJson(new Report {passed=failure==null,checkCount=checks.Count,checks=checks.ToArray(),failure=failure},true));
        EditorApplication.Exit(failure==null?0:1);
    }
}
