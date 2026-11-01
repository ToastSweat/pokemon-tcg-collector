using UnityEngine;
using VintageCollector.Runtime;
using VintageCollector.Presentation;

namespace VintageCollector.Music
{
    [DisallowMultipleComponent]
    public sealed class BackgroundMusicHost : MonoBehaviour
    {
        public static BackgroundMusicHost Instance { get; private set; }
        public static bool RelaxedBinderTheme { get; set; } // Session only; never persisted.
        public int CurrentTrack { get; private set; }
        public int ActiveAudioSources {
            get { int n = 0; foreach (var s in sources) if (s != null && s.isPlaying) n++; return n; }
        }
        // Snapshot of the two native source references for the parent's opt-in probe.
        public AudioSource[] AudioSources { get { return (AudioSource[])sources.Clone(); } }
        private BackgroundMusicProfile profile;
        private readonly AudioSource[] sources = new AudioSource[2];
        private readonly float[] weights = new float[2];
        private readonly bool[] started = new bool[2];
        private OpeningGame game;
        private BinderPresentation binder;
        private ShoppingPresentation shopping;
        private int currentSource;
        private bool closed, suspended, focused, applicationPaused, hadOwner;
        private float nextFind;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; RelaxedBinderTheme = false; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            new GameObject("Background Music").AddComponent<BackgroundMusicHost>();
        }
        private void Awake()
        {
            if (Instance != null && Instance != this) { enabled = false; Destroy(this); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            focused = Application.isFocused;
            profile = Resources.Load<BackgroundMusicProfile>("BackgroundMusic/BackgroundMusicProfile");
        }
        private void Update() { Refresh(Time.unscaledDeltaTime); }
        private void Refresh(float delta)
        {
            if (closed || Instance != this) return;
            if (game == null) {
                if (!ReferenceEquals(game, null)) { Detach(); StopAll(); }
                if (Time.unscaledTime >= nextFind) {
                    nextFind = Time.unscaledTime + 0.25f;
                    var found = Object.FindFirstObjectByType<OpeningGame>();
                    if (found != null) {
                        game = found; hadOwner = true;
                        game.ShuttingDown += OnOwnerShutdown;
                        game.ViewPublished += OnViewPublished;
                    }
                }
            }
            if (game != null && !game.IsAcceptingInput) { OnOwnerShutdown(); return; }
            if (game != null) {
                if (binder == null) binder = game.GetComponent<BinderPresentation>();
                if (shopping == null) shopping = game.GetComponent<ShoppingPresentation>();
            }
            var view = game == null ? null : game.CurrentView;
            bool loading = StartupLoadingCoordinator.IsRunning && !StartupLoadingCoordinator.IsReady;
            string stage = view == null ? null : view.stage;
            string printing = view == null ? null : stage == "ready" ? view.selectedPrintingId : view.activePackPrintingId;
            int track = game == null && hadOwner ? 0 : MusicSelection.Select(
                loading ? null : stage, printing, !loading && binder != null && binder.IsOpen,
                !loading && shopping != null && shopping.Current != null, game != null, RelaxedBinderTheme);
            float volume = view == null ? 0.5f : SafeGain(view.volume);
            bool pause = !focused || applicationPaused || (view != null && view.paused) || volume <= 0;
            AudioClip clip = profile != null && profile.tracks != null && track > 0 && track <= profile.tracks.Length
                ? profile.tracks[track - 1] : null;
            if (clip == null) { StopAll(); return; } // An absent optional profile is deliberately silent.
            EnsureSources();
            if (CurrentTrack != track || sources[currentSource].clip != clip) {
                if (sources[currentSource].clip == clip) CurrentTrack = track;
                else SwitchTrack(track, clip, !pause);
            }
            if (pause) Suspend();
            else {
                Resume();
                float step = Mathf.Max(0f, delta) / 0.5f;
                for (int i = 0; i < 2; i++) {
                    weights[i] = Mathf.MoveTowards(weights[i], i == currentSource ? 1f : 0f, step);
                    if (i != currentSource && weights[i] <= 0f) ClearSource(i);
                }
            }
            float gain = SafeGain(profile.gain) * volume;
            for (int i = 0; i < 2; i++) sources[i].volume = weights[i] * gain;
        }
        private static float SafeGain(float value) { return float.IsNaN(value) ? 0f : Mathf.Clamp01(value); }
        private void EnsureSources()
        {
            for (int i = 0; i < 2; i++) if (sources[i] == null) {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false; source.loop = true; source.spatialBlend = 0f;
                source.volume = 0f; sources[i] = source;
            }
        }
        private void SwitchTrack(int track, AudioClip clip, bool audible)
        {
            bool crossfade = audible && !suspended && CurrentTrack != 0 && sources[currentSource].isPlaying;
            int next = crossfade ? 1 - currentSource : currentSource;
            if (!crossfade) { ClearSource(0); ClearSource(1); }
            ClearSource(next); currentSource = next;
            sources[next].clip = clip; weights[next] = crossfade ? 0f : 1f;
            CurrentTrack = track;
            // Resume starts only the newly selected context, not an obsolete muted/faded clip.
            if (audible) { sources[next].Play(); started[next] = true; }
        }
        private void Suspend()
        {
            if (suspended) return;
            suspended = true;
            foreach (var source in sources) if (source != null) source.Pause();
        }
        private void Resume()
        {
            for (int i = 0; i < 2; i++) if (sources[i].clip != null) {
                if (!started[i]) { sources[i].Play(); started[i] = true; }
                else if (suspended) sources[i].UnPause();
            }
            suspended = false;
        }
        private void ClearSource(int i)
        {
            if (sources[i] != null) { sources[i].Stop(); sources[i].clip = null; sources[i].volume = 0f; }
            weights[i] = 0f; started[i] = false;
        }
        private void StopAll() { ClearSource(0); ClearSource(1); CurrentTrack = 0; suspended = false; }
        private void OnViewPublished(OpeningView ignored) { Refresh(0f); }
        private void OnApplicationFocus(bool focus) { focused = focus; if (!focus) Suspend(); }
        private void OnApplicationPause(bool pause) { applicationPaused = pause; if (pause) Suspend(); }
        private void Detach()
        {
            if (!ReferenceEquals(game, null)) {
                game.ShuttingDown -= OnOwnerShutdown;
                game.ViewPublished -= OnViewPublished;
            }
            game = null; binder = null; shopping = null;
        }
        private void OnOwnerShutdown() { Close(); Destroy(gameObject); }
        private void OnApplicationQuit() { Close(); }
        private void OnDisable() { Close(); }
        private void OnDestroy() { Close(); }
        private void Close()
        {
            if (closed) return;
            closed = true; StopAll(); Detach();
            if (ReferenceEquals(Instance, this)) Instance = null;
        }
    }
}
