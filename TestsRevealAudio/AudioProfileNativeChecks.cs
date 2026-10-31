using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VintageCollector.Audio;
using VintageCollector.Presentation;

[InitializeOnLoad]
public static class AudioProfileNativeChecks
{
    const string PendingKey = "AudioProfileNativeChecks.Pending";
    static AudioProfileNativeChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
            {
                SessionState.SetBool(PendingKey, false);
                Verify();
            }
        };
    }

    public static void BeginVerify()
    {
        SessionState.SetBool(PendingKey, true);
        EditorApplication.EnterPlaymode();
    }

    const string Root = "Assets/VintageCollector/Audio/";
    const string AssetPath = Root + "DefaultRevealAudioProfile.asset";
    [Serializable] class Report
    {
        public bool passed;
        public string failure;
        public int checkCount;
        public List<string> checks = new List<string>();
        public List<ClipResult> clips = new List<ClipResult>();
    }
    [Serializable] class ClipResult
    {
        public string name;
        public int samples;
        public int channels;
        public int frequency;
        public float peak;
        public float rms;
        public float maxPcmError;
    }
    static Report report;
    static void Check(bool value, string name)
    {
        if (!value) throw new Exception("FAIL: " + name);
        report.checks.Add(name);
        Debug.Log("PASS: " + name);
    }

    public static void AuthorAndVerify()
    {
        try
        {
            if (File.Exists(AssetPath)) throw new IOException("Refusing to overwrite an existing profile asset");
            var profile = ScriptableObject.CreateInstance<RevealAudioProfile>();
            for (int i = 0; i < 11; i++) profile.pullSounds[i] = Import("Pull_" + (i + 1).ToString("D2"));
            profile.nonPokemonRare = Import("Rare_NonPokemon");
            profile.pokemonRare = Import("Rare_Pokemon");
            profile.holoRare = Import("Rare_Holo");
            AssetDatabase.CreateAsset(profile, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            BeginVerify();
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    static AudioClip Import(string name)
    {
        string path = Root + "Clips/" + name + ".wav";
        var importer = (AudioImporter)AssetImporter.GetAtPath(path);
        if (importer == null) throw new IOException("Missing original WAV importer: " + path);
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = false;
        importer.loadInBackground = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    public static void Verify()
    {
        report = new Report();
        try
        {
            var profile = AssetDatabase.LoadAssetAtPath<RevealAudioProfile>(AssetPath);
            Check(profile != null, "native profile asset exists");
            Check(profile.pullSounds != null && profile.pullSounds.Length == 11, "native profile has eleven pull references");
            Check(profile.rareRevealPosition == 8, "native profile rare slot eight");
            var clips = new List<AudioClip>(profile.pullSounds);
            clips.Add(profile.nonPokemonRare); clips.Add(profile.pokemonRare); clips.Add(profile.holoRare);
            var unique = new HashSet<AudioClip>();
            foreach (AudioClip clip in clips)
            {
                Check(clip != null && unique.Add(clip), "distinct imported reference " + (clip == null ? "missing" : clip.name));
                string path = AssetDatabase.GetAssetPath(clip);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                Check(importer.defaultSampleSettings.compressionFormat == AudioCompressionFormat.PCM, "PCM import " + clip.name);
                Check(clip.LoadAudioData(), "native load data " + clip.name);
                float[] samples = new float[clip.samples * clip.channels];
                Check(clip.GetData(samples, 0), "native sample readback " + clip.name);
                var result = new ClipResult { name = clip.name, samples = clip.samples, channels = clip.channels, frequency = clip.frequency };
                Check(clip.channels == 1 && clip.frequency == 44100 && clip.samples > 0, "native format " + clip.name);
                byte[] wav = File.ReadAllBytes(path);
                Check(wav.Length == 44 + samples.Length * 2, "source sample count " + clip.name);
                double sum = 0;
                for (int i = 0; i < samples.Length; i++)
                {
                    float sample = samples[i];
                    CheckFinite(sample);
                    result.peak = Mathf.Max(result.peak, Mathf.Abs(sample));
                    sum += sample * sample;
                    float source = BitConverter.ToInt16(wav, 44 + i * 2) / 32768f;
                    result.maxPcmError = Mathf.Max(result.maxPcmError, Mathf.Abs(sample - source));
                }
                result.rms = (float)Math.Sqrt(sum / samples.Length);
                Check(result.maxPcmError < .00004f, "import preserves real source PCM " + clip.name);
                Check(result.peak < .8f && result.rms > .025f, "native signal headroom " + clip.name);
                Check(samples[0] == 0 && samples[samples.Length - 1] == 0, "native fade endpoints " + clip.name);
                report.clips.Add(result);
            }
            for (int i = 1; i <= 11; i++)
                if (i != 8) Check(profile.RevealSound(i, "pokemon", "holo") == profile.pullSounds[i-1], "native nonspoiling pull " + i);
            Check(profile.RevealSound(8, "trainer", "nonholo") == profile.nonPokemonRare, "native non-Pokemon rare");
            Check(profile.RevealSound(8, "pokemon", "nonholo") == profile.pokemonRare, "native Pokemon rare");
            Check(profile.RevealSound(8, "trainer", "holo") == profile.holoRare, "native authoritative holo priority");
            var serialized = new SerializedObject(profile);
            Check(serialized.FindProperty("pullSounds").arraySize == 11 && serialized.FindProperty("holoRare").objectReferenceValue == profile.holoRare, "Inspector exposes replaceable native AudioClip references");
            var go = new GameObject("AudioProfileNativeHostFixture");
            go.SetActive(false);
            try
            {
                var presentation = go.AddComponent<TabletopPresentation>();
                var host = go.AddComponent<RevealAudioHost>();
                host.profile = profile;
                go.SetActive(true);
                Check(ReferenceEquals(presentation.RevealSounds, profile), "native host binds actual presentation on activation");
                Check(go.GetComponentsInChildren<AudioSource>(true).Length == 0, "host startup never creates or plays audio");
                UnityEngine.Object.DestroyImmediate(host);
                Check(presentation.RevealSounds == null, "native destroy detaches owned binding");
                go.SetActive(false);
                host = go.AddComponent<RevealAudioHost>(); host.profile = profile;
                go.SetActive(true);
                var replacement = ScriptableObject.CreateInstance<RevealAudioProfile>();
                presentation.RevealSounds = replacement;
                UnityEngine.Object.DestroyImmediate(host);
                Check(ReferenceEquals(presentation.RevealSounds, replacement), "native destroy preserves replacement");
                UnityEngine.Object.DestroyImmediate(replacement);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            report.passed = true;
        }
        catch (Exception e) { report.failure = e.ToString(); Debug.LogError(e); }
        report.checkCount = report.checks.Count;
        string output = Environment.GetEnvironmentVariable("AUDIO_PROFILE_OUT");
        File.WriteAllText(Path.Combine(output, "results.json"), JsonUtility.ToJson(report, true));
        EditorApplication.Exit(report.passed ? 0 : 1);
    }

    static void CheckFinite(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) throw new Exception("Nonfinite imported sample");
    }
}
