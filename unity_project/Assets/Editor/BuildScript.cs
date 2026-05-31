using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    private static string[] GetEnabledScenes()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
            .Select(scene => scene.path)
            .ToArray();
    }

    [MenuItem("Build/Build Windows x64")]
    public static void BuildWindowsX64()
    {
        string outputDir  = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Build", "Windows");
        string outputPath = Path.Combine(outputDir, "CODE_BLUE.exe");
        string[] scenes = GetEnabledScenes();
        Directory.CreateDirectory(outputDir);

        if (scenes.Length == 0)
        {
            Debug.LogError("[BuildScript] Build FAILED: no enabled scenes in EditorBuildSettings.");
            return;
        }

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes           = scenes,
            locationPathName = outputPath,
            target           = BuildTarget.StandaloneWindows64,
            options          = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log("[BuildScript] Build succeeded: " + outputPath);
        else
            Debug.LogError("[BuildScript] Build FAILED: " + report.summary.result);
    }

    [MenuItem("Build/Build RL Train (Windows x64)")]
    public static void BuildRLTrain()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputDir   = Path.Combine(projectRoot, "builds", "windows", "BossPPO_RLTrain");
        string outputPath  = Path.Combine(outputDir, "BossPPO_RLTrain.exe");
        Directory.CreateDirectory(outputDir);

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes           = new[] { "Assets/Project/Scenes/Boss01_Elevator_RLTrain.unity" },
            locationPathName = outputPath,
            target           = BuildTarget.StandaloneWindows64,
            options          = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log("[BuildScript] RL Train build succeeded: " + outputPath);
        else
        {
            Debug.LogError("[BuildScript] RL Train build FAILED: " + report.summary.result);
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Build/Build macOS")]
    public static void BuildMacOS()
    {
        string outputDir  = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Build", "macOS");
        string outputPath = Path.Combine(outputDir, "CODE_BLUE.app");
        string[] scenes = GetEnabledScenes();
        Directory.CreateDirectory(outputDir);

        if (scenes.Length == 0)
        {
            Debug.LogError("[BuildScript] Build FAILED: no enabled scenes in EditorBuildSettings.");
            return;
        }

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes           = scenes,
            locationPathName = outputPath,
            target           = BuildTarget.StandaloneOSX,
            options          = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log("[BuildScript] Build succeeded: " + outputPath);
        else
            Debug.LogError("[BuildScript] Build FAILED: " + report.summary.result);
    }
}
