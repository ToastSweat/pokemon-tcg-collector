using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class TiltSurfaceChecks {
    public static void CompileOnly() {
        File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("TILT_SURFACE_OUT"),"results.json"),"{\"passed\":true,\"checkCount\":1,\"screenshotCount\":0,\"checks\":[\"Production helper and Editor check compiled without test fixture\"],\"screenshots\":[]}");
        EditorApplication.Exit(0);
    }
    public static void Build() {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Isolated surface capture").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color32(20,25,30,255);
        Type fixture=null;
        foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()) {fixture=assembly.GetType("SurfaceGpuFixture",false);if(fixture!=null)break;}
        if(fixture==null)throw new InvalidOperationException("SurfaceGpuFixture is isolated test-only code. Run tools/run_tilt_surface_native.py, not this check in the normal project.");
        new GameObject("Only surface fixture, no game/save/audio").AddComponent(fixture);
        string scenePath="Assets/SurfaceFixture.unity";EditorSceneManager.SaveScene(scene,scenePath);
        PlayerSettings.companyName="Private surface tests";PlayerSettings.productName="Tilt Surface GPU Fixture";
        PlayerSettings.defaultScreenWidth=720;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{UnityEngine.Rendering.GraphicsDeviceType.Direct3D11});
        string output=Path.Combine(Environment.GetEnvironmentVariable("TILT_SURFACE_ROOT"),"player/SurfaceFixture.exe");Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{scenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        Debug.Log("SURFACE BUILD "+report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings);
        EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
    }
}
