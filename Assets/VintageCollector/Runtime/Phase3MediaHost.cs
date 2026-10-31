using System;
using System.IO;
using UnityEngine;
using VintageCollector.Domain;
using VintageCollector.Media;
using VintageCollector.Presentation;

namespace VintageCollector.Runtime
{
    // Optional presentation resource owner only: no generation, commands, saves or playback.
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class Phase3MediaHost : MonoBehaviour
    {
        // Trusted scene configuration only. Legacy scenes retain the empty manifest and 16 MiPixel cap.
        [SerializeField] public string manifestRelativePath = "Assets/Supplied/AssetManifest.json";
        [SerializeField] public bool fullBaseSetBudget = false;
        public PresentationMediaLibrary Library { get; private set; }
        public bool UsesTestManifest { get; private set; }
        public string StatusMessage { get; private set; } = "Optional media awaiting usable game initialization.";
        private TabletopPresentation presentation;
        private bool terminal, attempted;

        private void Start() { Initialize(GetComponent<OpeningGame>()); }
        private void Update()
        {
            // Initial save errors can recover on the existing controller's Retry; never initiate that Retry here.
            // Once an actual load was attempted, do not poll media files or overwrite an external provider.
            if(!terminal && !attempted) Initialize(GetComponent<OpeningGame>());
        }

        public void Initialize(OpeningGame game)
        {
            if(terminal || attempted || Library!=null) return;
            if(game==null || game.gameObject!=gameObject || game.Paths==null || game.CurrentSession==null ||
               game.CurrentView==null || game.CurrentView.stage=="error" || game.catalogJson==null) return;
            attempted=true;
            try {
                // The same read-only TextAsset is parsed using the game's actual Unity serializer/catalog path.
                Catalog catalog=Catalog.FromData(JsonUtility.FromJson<CatalogData>(game.catalogJson.text));
                string root=ValidatedRoot(game);
                string manifest=manifestRelativePath;
                bool test=false;
                if(game.Paths.IsSmokeTest) {
                    string optional=OptionalManifest(root,game.Paths.ArtifactDirectory,File.Exists);
                    if(optional!=null) { manifest=optional; test=true; }
                }
                presentation=GetComponent<TabletopPresentation>();
                if(presentation==null) return;
                Library=PresentationMediaLibrary.Load(root,catalog,manifest,test,fullBaseSetBudget);
                UsesTestManifest=test;
                presentation.Media=Library;
                StatusMessage=Library.StatusText+(Library.Warnings.Count==0?"":"; optional media warnings; safe fallback active.");
            } catch(Exception) {
                // Never log exception messages/provenance: optional media cannot block pack/save access.
                StatusMessage="Optional media unavailable; generic presentation remains usable.";
            }
        }

        private static string OptionalManifest(string root,string artifactDirectory,Func<string,bool> exists)
        {
            try {
                string candidate=Path.GetFullPath(Path.Combine(artifactDirectory,"media-manifest.json"));
                string prefix=Path.Combine(root,".artifacts")+Path.DirectorySeparatorChar;
                if(!candidate.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)) return null;
                string relative=candidate.Substring(root.Length+1).Replace('\\','/');
                // Full lexical/root-to-leaf reparse validation must precede the existence probe.
                // Library Load still revalidates the actual opened handle before reading any bytes.
                string validated=MediaFiles.Resolve(root,relative,true);
                return exists(validated)?relative:null;
            } catch(Exception) {
                // An unsafe optional override cannot suppress the safe production manifest fallback.
                return null;
            }
        }

        private static string ValidatedRoot(OpeningGame game)
        {
            // Reject remote/device roots before any metadata/existence probe. Library rechecks files/handles.
            string configured=game.allowedProjectRoot;
            if(string.IsNullOrEmpty(configured) || configured.Length<3 || !char.IsLetter(configured[0]) ||
               configured[1]!=':' || (configured[2]!='/' && configured[2]!='\\')) throw new ArgumentException();
            string root=Path.GetFullPath(configured).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(new DriveInfo(Path.GetPathRoot(root)).DriveType==DriveType.Network) throw new ArgumentException();
            string[] args=game.Paths.IsSmokeTest?new[]{"--vcc-smoke="+game.Paths.SmokeScenario,
                "--vcc-save-root="+game.Paths.BaseSaveDirectory,"--vcc-artifacts="+game.Paths.ArtifactDirectory}:new string[0];
            // Pure revalidation only: never create/open a save or consume an untrusted process override.
            var validated=GamePaths.Resolve(root,Application.dataPath,Application.isEditor,args).WithProfile(game.Paths.CollectionProfileId);
            if(!string.Equals(validated.BaseSaveDirectory,game.Paths.BaseSaveDirectory,StringComparison.OrdinalIgnoreCase) ||
               !string.Equals(validated.SaveDirectory,game.Paths.SaveDirectory,StringComparison.OrdinalIgnoreCase) ||
               !string.Equals(validated.ArtifactDirectory,game.Paths.ArtifactDirectory,StringComparison.OrdinalIgnoreCase)) throw new ArgumentException();
            return root;
        }

        public void Shutdown()
        {
            terminal=true;
            if(presentation!=null && ReferenceEquals(presentation.Media,Library)) presentation.Media=null;
            if(Library!=null) { Library.Dispose(); Library=null; }
            UsesTestManifest=false;
            StatusMessage="Optional media host shut down.";
        }
        private void OnDestroy() { Shutdown(); }
        private void OnApplicationQuit() { Shutdown(); }
    }
}
