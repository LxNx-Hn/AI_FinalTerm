using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Windows EXE build for ML-Agents RLTrain smoke.
/// Menu: Tools/Build RLTrain EXE (Windows x64)
/// Batch mode: Unity -batchmode -executeMethod RLTrainBatchBuild.Build
/// Output: <ProjectRoot>/../builds/windows/BossPPO_RLTrain/BossPPO_RLTrain.exe
/// </summary>
public static class RLTrainBatchBuild
{
    private const string ScenePath = "Assets/Project/Scenes/Boss01_Elevator_RLTrain.unity";

    private static string GetOutputExePath()
    {
        // Application.dataPath = .../unity_project/Assets
        // ProjectRoot             = .../unity_project
        // WorkRoot                = .../AI_FinalTerm_MLAgents_PPO_NEW
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string workRoot    = Path.GetDirectoryName(projectRoot);
        return Path.Combine(workRoot, "builds", "windows", "BossPPO_RLTrain", "BossPPO_RLTrain.exe");
    }

    [MenuItem("Tools/Build RLTrain EXE (Windows x64)")]
    public static void BuildFromMenu()
    {
        string outputExe = GetOutputExePath();
        Debug.Log($"[RLTrainBatchBuild] Output: {outputExe}");

        bool succeeded = RunBuild(outputExe);

        if (succeeded)
            EditorUtility.DisplayDialog("Build Succeeded", $"EXE created:\n{outputExe}", "OK");
        else
            EditorUtility.DisplayDialog("Build FAILED", "Check Unity Console for errors.", "OK");
    }

    // Called from batch mode: -executeMethod RLTrainBatchBuild.Build
    public static void Build()
    {
        string outputExe = GetOutputExePath();
        Debug.Log($"[RLTrainBatchBuild] Batch build output: {outputExe}");

        bool succeeded = RunBuild(outputExe);
        EditorApplication.Exit(succeeded ? 0 : 1);
    }

    private static bool RunBuild(string outputExe)
    {
        Debug.Log("[RLTrainBatchBuild] Starting Windows x64 EXE build...");
        Debug.Log($"[RLTrainBatchBuild] Scene: {ScenePath}");

        string outputDir = Path.GetDirectoryName(outputExe);
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        var options = new BuildPlayerOptions
        {
            scenes          = new[] { ScenePath },
            locationPathName = outputExe,
            target          = BuildTarget.StandaloneWindows64,
            options         = BuildOptions.None,
        };

        BuildReport report  = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[RLTrainBatchBuild] Build SUCCEEDED. Size: {summary.totalSize} bytes at {outputExe}");
            return true;
        }
        else
        {
            Debug.LogError($"[RLTrainBatchBuild] Build FAILED: {summary.result}, Errors: {summary.totalErrors}");
            return false;
        }
    }
}
