using System;
using System.Collections.Generic;
using UnityEngine;
namespace VintageCollector.Presentation
{
    // A passive opt-in header component. Host rechecks every command and owns the projection.
    public sealed class SetSelectorPresentation : MonoBehaviour
    {
        private OpeningView view;
        private int version, observedVersion=-1, observedWidth, observedHeight;
        private string observedContext;
        private GUIStyle button, caption;
        private ControlObservation[] observations=new ControlObservation[0];
        public OpeningView View { get { return view; } set { view=value;version++;observations=new ControlObservation[0]; } }
        public event Action<string> Command;
        [Serializable] public sealed class ControlObservation { public string command;public Rect bounds;public int renderFrame; }
        private string Context()
        {
            if(view==null)return "";
            var key=new System.Text.StringBuilder();key.Append(view.stage).Append('|').Append(view.selectedPrintingId).Append('|');
            if(view.supportedSets!=null)foreach(var s in view.supportedSets)if(s!=null){
                string c="set:"+s.printingId;key.Append(c.Length).Append(':').Append(c).Append(':').Append(s.label).Append(':').Append(PresentationRules.CanEmit(view,c));
            }
            return key.ToString();
        }
        public ControlObservation[] ControlObservations {
            get {
                if(!isActiveAndEnabled || observedVersion!=version || observedWidth!=Screen.width || observedHeight!=Screen.height || observedContext!=Context())return new ControlObservation[0];
                var copy=new ControlObservation[observations.Length];for(int i=0;i<copy.Length;i++)copy[i]=new ControlObservation{command=observations[i].command,bounds=observations[i].bounds,renderFrame=observations[i].renderFrame};return copy;
            }
        }
        private void RequestCommand(string command)
        {
            if(string.IsNullOrEmpty(command) || !command.StartsWith("set:",StringComparison.Ordinal) || !PresentationRules.CanEmit(view,command))return;
            var handler=Command;if(handler!=null)handler(command);
        }
        private void OnGUI()
        {
            var controls=new List<ControlObservation>();
            var oldMatrix=GUI.matrix;bool oldEnabled=GUI.enabled;
            // Unity stores depth per IMGUI component, not per draw command. Keep this
            // component above the table; restoring depth would hide live controls under it.
            GUI.depth=-10;
            try {
                if(view==null || view.supportedSets==null)return;
                var l=PresentationRules.Layout(Screen.width,Screen.height);
                GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(l.scale,l.scale,1));GUI.depth=-10;
                if(button==null){var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");button=new GUIStyle(GUI.skin.button){font=font,fontSize=18,richText=false,wordWrap=false};caption=new GUIStyle{font=font,fontSize=16,richText=false};caption.normal.textColor=new Color32(218,181,104,255);}
                var used=new HashSet<string>(StringComparer.Ordinal);int i=0;
                foreach(var choice in view.supportedSets) {
                    if(choice==null || i>=3)continue;string command="set:"+choice.printingId;
                    if(!PresentationRules.CanEmit(view,command) || !used.Add(choice.printingId))continue;
                    if(i==0)GUI.Label(new Rect(748,22,324,23),view.stage=="results"?"Next pack":"Choose set",caption);
                    var r=new Rect(748+i*110,46,104,32);GUI.SetNextControlName(command);
                    string text=string.IsNullOrEmpty(choice.label)?choice.printingId:choice.label;
                    button.fontSize=18;while(button.fontSize>12 && button.CalcSize(new GUIContent(text)).x>r.width-8)button.fontSize--;
                    if(GUI.Button(r,text,button))RequestCommand(command);
                    if(choice.printingId==view.selectedPrintingId){var old=GUI.color;GUI.color=new Color32(218,181,104,255);GUI.DrawTexture(new Rect(r.x,r.yMax-2,r.width,2),Texture2D.whiteTexture);GUI.color=old;}
                    controls.Add(new ControlObservation{command=command,bounds=new Rect(r.position*l.scale,r.size*l.scale),renderFrame=Time.frameCount});i++;
                }
            }finally{
                if(Event.current.type==EventType.Repaint){observations=controls.ToArray();observedVersion=version;observedWidth=Screen.width;observedHeight=Screen.height;observedContext=Context();}
                GUI.matrix=oldMatrix;GUI.enabled=oldEnabled;
            }
        }
    }
}
