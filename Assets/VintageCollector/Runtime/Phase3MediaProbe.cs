using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VintageCollector.Runtime;
using VintageCollector.Presentation;
using VintageCollector.Media;

namespace VintageCollector.Verification
{
    // Explicit smoke observer only. Never plays a sound, dispatches a game command or awards anything.
    public sealed class Phase3MediaProbe : MonoBehaviour
    {
        [Serializable] public sealed class ClipInfo
        {
            public string action;
            public int samples, channels, frequency;
            public float peak, rms;
            public bool readbackSucceeded;
        }
        [Serializable] public sealed class Ack
        {
            public string command, stage;
            public int frame, revealed, cues;
            public float volume;
            public bool paused, appFocused, rendererFocused;
        }
        [Serializable] public sealed class Observation
        {
            public bool bound, testManifest, appFocused, paused, sourcePlaying, everPlaying, playOnAwake, loop;
            public string stage, status, lastCue;
            public int suppliedAssets, warnings, cues, generatedResults, revealedCount, virtualCards;
            public float volume, sourceVolume, outputPeak, maxOutputPeak, spatialBlend;
            public ClipInfo[] clips;
            public bool rendererFocused;
            public Ack[] acknowledgements;
        }
        private OpeningGame game;
        private Phase3MediaHost host;
        private TabletopPresentation presentation;
        private bool active, everPlaying;
        private string output;
        private ClipInfo[] clips;
        private float maxOutputPeak;
        private readonly float[] outputSamples = new float[512];
        private string previous = "";
        private readonly List<Ack> acknowledgements = new List<Ack>();
        private bool RendererFocused { get { return (bool)typeof(TabletopPresentation).GetField("focused", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(presentation); } }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            game = GetComponent<OpeningGame>();
            if (game == null || game.Paths == null || !game.Paths.IsSmokeTest) yield break;
            host = GetComponent<Phase3MediaHost>();
            presentation = GetComponent<TabletopPresentation>();
            if (host == null || presentation == null) yield break;
            output = Path.Combine(game.Paths.ArtifactDirectory, "media-probe.json");
            Directory.CreateDirectory(game.Paths.ArtifactDirectory);
            var info = new List<ClipInfo>();
            if (host.Library != null)
            {
                foreach (string command in new[] { "generate", "open", "next", "skip" })
                {
                    AudioClip clip = host.Library.Sound(command);
                    if (clip == null) continue;
                    var samples = new float[clip.samples * clip.channels];
                    bool readback = clip.GetData(samples, 0);
                    float peak = 0;
                    double sum = 0;
                    foreach (float value in samples) { peak = Mathf.Max(peak, Mathf.Abs(value)); sum += value * value; }
                    info.Add(new ClipInfo { action = command, samples = clip.samples, channels = clip.channels,
                        frequency = clip.frequency, peak = peak, rms = samples.Length == 0 ? 0 : (float)Math.Sqrt(sum / samples.Length),
                        readbackSucceeded = readback });
                }
            }
            clips = info.ToArray();
            presentation.Command += ObserveCommand;
            active = true;
        }

        private void ObserveCommand(string command)
        {
            if (!active || acknowledgements.Count >= 200) return;
            var v = game.CurrentView;
            acknowledgements.Add(new Ack { command = command, frame = Time.frameCount, stage = v.stage, revealed = v.revealedCount,
                cues = presentation.CommittedCueCount, volume = v.volume, paused = v.paused, appFocused = Application.isFocused, rendererFocused = RendererFocused });
        }

        private void OnDestroy()
        {
            if (presentation != null) presentation.Command -= ObserveCommand;
        }

        private void Update()
        {
            if (!active || game.CurrentView == null) return;
            var source = presentation.FoleySource;
            bool playing = source != null && source.isPlaying;
            everPlaying |= playing;
            float peak = 0;
            if (source != null)
            {
                source.GetOutputData(outputSamples, 0);
                foreach (float value in outputSamples) peak = Mathf.Max(peak, Mathf.Abs(value));
            }
            maxOutputPeak = Mathf.Max(maxOutputPeak, peak);
            var view = game.CurrentView;
            var library = host.Library;
            var observation = new Observation {
                bound = library != null && ReferenceEquals(presentation.Media, library),
                testManifest = host.UsesTestManifest, suppliedAssets = library == null ? 0 : library.SuppliedAssetCount,
                warnings = library == null ? 0 : library.Warnings.Count,
                status = host.StatusMessage, stage = view.stage, paused = view.paused, appFocused = Application.isFocused,
                cues = presentation.CommittedCueCount, lastCue = presentation.LastCommittedCue,
                generatedResults = game.GeneratedResultCount, revealedCount = view.revealedCount, virtualCards = view.totalVirtualCards,
                volume = view.volume, sourceVolume = source == null ? 0 : source.volume,
                sourcePlaying = playing, everPlaying = everPlaying, outputPeak = peak, maxOutputPeak = maxOutputPeak,
                spatialBlend = source == null ? 0 : source.spatialBlend,
                playOnAwake = source != null && source.playOnAwake, loop = source != null && source.loop, clips = clips,
                rendererFocused = RendererFocused, acknowledgements = acknowledgements.ToArray()
            };
            string json = JsonUtility.ToJson(observation, true);
            if (json == previous) return;
            try
            {
                string temporary = output + ".writing";
                File.WriteAllText(temporary, json);
                if (File.Exists(output)) File.Replace(temporary, output, null);
                else File.Move(temporary, output);
                previous = json;
            }
            catch (IOException) { /* Test observer can briefly hold the report; retry identical data next frame. */ }
        }
    }
}
