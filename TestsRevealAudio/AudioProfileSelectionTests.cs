using System;
using System.Reflection;
using UnityEngine;
using VintageCollector.Media;

public static class AudioProfileSelectionTests
{
    static int checks;
    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        checks++; Console.WriteLine("PASS: " + name);
    }
    static Type ProfileType;
    static object Profile;
    static FieldInfo Field(string name) { return ProfileType.GetField(name); }
    static AudioClip Clip(string name) { return new AudioClip { name = name }; }
    static AudioClip Select(int position, string kind = "pokemon", string finish = "nonholo")
    { return (AudioClip)ProfileType.GetMethod("RevealSound").Invoke(Profile, new object[] { position, kind, finish }); }

    static void Lifecycle(Type type, object value, string name)
    { type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(value, null); }

    public static int Main(string[] args)
    {
        try
        {
            ProfileType = typeof(IPresentationMedia).Assembly.GetType("VintageCollector.Audio.RevealAudioProfile");
            Check(ProfileType != null, "profile type exists");
            Profile = Activator.CreateInstance(ProfileType);
            Check(Profile is ScriptableObject, "profile is a native ScriptableObject");
            Check((int)Field("rareRevealPosition").GetValue(Profile) == 8, "rare position defaults to eight");
            AudioClip[] pulls = (AudioClip[])Field("pullSounds").GetValue(Profile);
            Check(pulls.Length == 11, "eleven Inspector pull slots");
            for (int i = 0; i < pulls.Length; i++) pulls[i] = Clip("pull" + (i + 1));
            Check(Select(1) == pulls[0], "first pull resolves one-based slot");
            Check(Select(11) == pulls[10], "last pull resolves one-based slot");
            Type optional = typeof(IPresentationMedia).Assembly.GetType("VintageCollector.Media.IRevealSoundMedia");
            Check(optional != null && optional.IsInstanceOfType(Profile), "profile supplies optional additive interface");
            AudioClip nonPokemon = Clip("nonPokemonRare");
            Field("nonPokemonRare").SetValue(Profile, nonPokemon);
            Check(Select(8, "trainer") == nonPokemon, "non-Pokemon rare overrides position eight");
            Check(Select(7, "trainer", "holo") == pulls[6], "nonrare positions do not reveal outcome");
            Field("nonPokemonRare").SetValue(Profile, null);
            Check(Select(8, "trainer") == pulls[7], "missing rare falls back to its position");
            AudioClip pokemon = Clip("pokemonRare");
            Field("pokemonRare").SetValue(Profile, pokemon);
            Field("nonPokemonRare").SetValue(Profile, nonPokemon);
            Check(Select(8) == pokemon, "Pokemon nonholo uses its own rare stinger");
            Field("pokemonRare").SetValue(Profile, null);
            Check(Select(8) == pulls[7], "missing Pokemon stinger never substitutes non-Pokemon stinger");
            Field("pokemonRare").SetValue(Profile, pokemon);
            AudioClip holo = Clip("holoRare");
            Field("holoRare").SetValue(Profile, holo);
            Check(Select(8, "pokemon", "holo") == holo, "holo has priority over Pokemon kind");
            Check(Select(8, "trainer", "holo") == holo, "holo finish is authoritative independently of kind");
            Field("holoRare").SetValue(Profile, null);
            Check(Select(8, "pokemon", "holo") == pulls[7], "missing holo goes to position not another rare category");
            Field("holoRare").SetValue(Profile, holo);
            Field("pullSounds").SetValue(Profile, null);
            Check(Select(8, "pokemon", "holo") == holo, "assigned rare survives absent position array");
            Check(Select(1) == null, "absent array yields generic fallback on ordinary pull");
            Field("pullSounds").SetValue(Profile, new AudioClip[1]);
            Check(Select(8) == pokemon, "assigned rare survives short position array");
            Check(Select(11) == null, "short array yields generic fallback outside its slots");
            Field("pullSounds").SetValue(Profile, pulls);
            foreach (int invalid in new[] { int.MinValue, -1, 0, 12, int.MaxValue })
                Check(Select(invalid, "pokemon", "holo") == null, "invalid one-based position " + invalid + " returns null");
            for (int i = 1; i <= 11; i++)
                if (i != 8)
                    foreach (string kind in new[] { "pokemon", "trainer", "energy", null })
                        foreach (string finish in new[] { "holo", "nonholo", null })
                            Check(Select(i, kind, finish) == pulls[i - 1], "nonspoiling pull " + i + " kind=" + kind + " finish=" + finish);
            Check(Select(8, "energy", null) == nonPokemon, "other catalog kind uses non-Pokemon rare");
            Field("rareRevealPosition").SetValue(Profile, 10);
            Check(Select(8, "pokemon", "holo") == pulls[7], "former rare position becomes ordinary");
            Check(Select(10, "pokemon", "holo") == holo, "configured rare slot controls override");
            Field("rareRevealPosition").SetValue(Profile, 0);
            Check(Select(8, "pokemon", "holo") == pulls[7], "invalid configured rare slot has no override");
            pulls[7] = null;
            Check(Select(8) == null, "missing ordinary clip yields renderer generic fallback");
            Type hostType = typeof(IPresentationMedia).Assembly.GetType("VintageCollector.Audio.RevealAudioHost");
            Check(hostType != null, "optional host type exists");
            object host = Activator.CreateInstance(hostType);
            var target = new VintageCollector.Presentation.TabletopPresentation();
            MonoBehaviour.Component = target;
            hostType.GetField("profile").SetValue(host, Profile);
            Lifecycle(hostType, host, "OnEnable");
            Check(ReferenceEquals(target.RevealSounds, Profile), "host binds Inspector profile to colocated presentation");
            Lifecycle(hostType, host, "OnDestroy");
            Check(target.RevealSounds == null, "destroy detaches owned binding");
            Lifecycle(hostType, host, "OnEnable");
            object replacement = Activator.CreateInstance(ProfileType);
            target.RevealSounds = (IRevealSoundMedia)replacement;
            Lifecycle(hostType, host, "OnDestroy");
            Check(ReferenceEquals(target.RevealSounds, replacement), "destroy preserves replacement provider");
            hostType.GetField("profile").SetValue(host, null);
            Lifecycle(hostType, host, "OnEnable");
            Check(ReferenceEquals(target.RevealSounds, replacement), "missing profile preserves existing provider");
            MonoBehaviour.Component = null;
            Lifecycle(hostType, host, "OnEnable");
            Lifecycle(hostType, host, "OnDestroy");
            Check(true, "missing colocated presentation is harmless");
            Console.WriteLine("RESULT checks=" + checks + " passed=true");
            return 0;
        }
        catch (Exception e) { Console.WriteLine(e); Console.WriteLine("RESULT checks=" + checks + " passed=false"); return 1; }
    }
}
