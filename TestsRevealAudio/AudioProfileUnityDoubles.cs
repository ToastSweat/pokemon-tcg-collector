using System;
namespace UnityEngine
{
    public class Object { public string name; }
    public class Texture2D : Object { }
    public class AudioClip : Object { }
    public class ScriptableObject : Object { }
    public class MonoBehaviour : Object
    {
        public static object Component;
        protected T GetComponent<T>() where T : class { return Component as T; }
    }
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CreateAssetMenuAttribute : Attribute
    { public string fileName; public string menuName; }
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string value) { } }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RangeAttribute : Attribute { public RangeAttribute(int min, int max) { } }
}
namespace VintageCollector.Presentation
{
    public sealed class TabletopPresentation : UnityEngine.MonoBehaviour
    { public VintageCollector.Media.IRevealSoundMedia RevealSounds { get; set; } }
}
