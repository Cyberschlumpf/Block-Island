using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds a macOS player for the automated 1.0 inventory test.
// batchmode: -executeMethod BlockIsland1TestBuild.Build -bi1buildpath <path/Block Island Test.app>
public static class BlockIsland1TestBuild {
    public static void Build() {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, "-bi1buildpath");
        string path = i >= 0 ? args[i + 1] : "Builds/Block Island Test.app";
        BuildPlayerOptions o = new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = path,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };
        BuildReport r = BuildPipeline.BuildPlayer(o);
        Debug.Log("BI1 TEST BUILD: " + r.summary.result + " errors=" + r.summary.totalErrors + " size=" + r.summary.totalSize + " -> " + path);
        if(Application.isBatchMode) EditorApplication.Exit(r.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
