using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class IntegrationFixtureBuild
{
    public static void Run()
    {
        string root=Path.GetDirectoryName(Application.dataPath);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("Owned passive tabletop native input fixture").AddComponent<IntegrationFixture>();
        EditorSceneManager.SaveScene(scene,"Assets/Integration.unity");
        PlayerSettings.companyName="PrivateFixture";PlayerSettings.productName="TiltPresentationOwnedFixture";
        PlayerSettings.defaultIsNativeResolution=false;PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.resizableWindow=true;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{"Assets/Integration.unity"},locationPathName=Path.Combine(root,".artifacts/player/Integration.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
        EditorApplication.Exit(result.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded?0:1);
    }
}
