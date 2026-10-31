using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class RenderFixtureBuild {
    public static void Run() { Build(false); }
    public static void RunLegacy() { Build(true); }
    static void Build(bool legacy) {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Isolated native render camera").AddComponent<Camera>();
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color32(20,25,30,255);
        camera.gameObject.AddComponent<AudioListener>();
        var type=typeof(RenderFixture).Assembly.GetType(legacy?"PresentationRenderFixture":"RenderFixture");
        new GameObject("Explicit isolated render fixture").AddComponent(type);
        string path="Assets/MaterialsRenderFixture.unity"; EditorSceneManager.SaveScene(scene,path);
        PlayerSettings.companyName="Private isolated tests"; PlayerSettings.productName="Materials Render Fixture";
        PlayerSettings.defaultScreenWidth=1000; PlayerSettings.defaultScreenHeight=800;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed; PlayerSettings.runInBackground=true;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{UnityEngine.Rendering.GraphicsDeviceType.Direct3D11});
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../.artifacts/player/RenderFixture.exe"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{path},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        Debug.Log("MATERIALS ISOLATED BUILD "+report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings);
        EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
    }
}
