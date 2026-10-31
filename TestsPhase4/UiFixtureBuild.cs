// ISOLATED ONLY: copied to .artifacts/phase4-ui-project, never main scene/settings.
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class UiFixtureBuild
{
    public static void Run() {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Synthetic fixture camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        new GameObject("SYNTHETIC binder UI only - no collection services").AddComponent<UiRenderFixture>();
        EditorSceneManager.SaveScene(scene,"Assets/UiFixture.unity");
        PlayerSettings.companyName="Private original UI study";PlayerSettings.productName="SYNTHETIC Binder UI Fixture";
        PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{UnityEngine.Rendering.GraphicsDeviceType.Direct3D11});
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../.artifacts/player/BinderFixture.exe"));Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{"Assets/UiFixture.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        Debug.Log("ISOLATED BINDER FIXTURE "+report.summary.result+" errors="+report.summary.totalErrors);
        EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
    }
}
