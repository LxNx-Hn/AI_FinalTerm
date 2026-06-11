using System;
using System.IO;
using Unity.InferenceEngine;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds an evaluation-only RLTrain executable with a selected ONNX attached.
/// It leaves the training scene untouched by copying it to an eval scene first.
/// </summary>
public static class RLEval210BatchBuild
{
    private const string SourceScenePath = "Assets/Project/Scenes/Boss01_Elevator_RLTrain.unity";
    private const string EvalScenePath = "Assets/Project/Scenes/Boss01_Elevator_RLTrain_Eval210.unity";
    private const string ModelAssetPath = "Assets/Project/RLModels/BossPlayer_Eval210_Current.onnx";
    private const float EvalEpisodeSeconds = 210f;

    public static void Build()
    {
        bool succeeded = false;

        try
        {
            string outputExe = GetOutputExePath();
            ConfigureEvalScene();
            succeeded = RunBuild(outputExe);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RLEval210BatchBuild] FAILED: {ex}");
            succeeded = false;
        }

        EditorApplication.Exit(succeeded ? 0 : 1);
    }

    private static string GetWorkRoot()
    {
        string unityProjectRoot = Path.GetDirectoryName(Application.dataPath);
        return Path.GetDirectoryName(unityProjectRoot);
    }

    private static string GetOutputExePath()
    {
#if UNITY_EDITOR_OSX
        return Path.Combine(GetWorkRoot(), "builds", "macos", "BossPPO_RLTrain_Eval210.app");
#else
        return Path.Combine(GetWorkRoot(), "builds", "windows", "BossPPO_RLTrain_Eval210", "BossPPO_RLTrain_Eval210.exe");
#endif
    }

    private static BuildTarget GetBuildTarget()
    {
#if UNITY_EDITOR_OSX
        return BuildTarget.StandaloneOSX;
#else
        return BuildTarget.StandaloneWindows64;
#endif
    }

    private static void ConfigureEvalScene()
    {
        Debug.Log("[RLEval210BatchBuild] Preparing eval scene.");

        if (!File.Exists(SourceScenePath))
            throw new FileNotFoundException($"Source scene missing: {SourceScenePath}");

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(EvalScenePath) != null)
            AssetDatabase.DeleteAsset(EvalScenePath);

        if (!AssetDatabase.CopyAsset(SourceScenePath, EvalScenePath))
            throw new IOException($"Could not copy scene to {EvalScenePath}");

        bool heuristicOnly = IsHeuristicOnlyBuild();
        ModelAsset model = heuristicOnly ? null : ImportModelAsset();
        Scene scene = EditorSceneManager.OpenScene(EvalScenePath, OpenSceneMode.Single);
        BossPlayerAgent agent = UnityEngine.Object.FindFirstObjectByType<BossPlayerAgent>();
        if (agent == null)
            throw new MissingComponentException("BossPlayerAgent not found in eval scene.");

        var behavior = agent.GetComponent<BehaviorParameters>();
        if (behavior == null)
            throw new MissingComponentException("BehaviorParameters not found on BossPlayerAgent.");

        var serializedAgent = new SerializedObject(agent);
        SerializedProperty maxEpisodeSeconds = serializedAgent.FindProperty("maxEpisodeSeconds");
        if (maxEpisodeSeconds == null)
            throw new MissingFieldException("BossPlayerAgent.maxEpisodeSeconds serialized field not found.");

        maxEpisodeSeconds.floatValue = EvalEpisodeSeconds;
        serializedAgent.ApplyModifiedPropertiesWithoutUndo();

        behavior.Model = model;
        behavior.BehaviorType = heuristicOnly ? BehaviorType.HeuristicOnly : BehaviorType.InferenceOnly;
        behavior.InferenceDevice = InferenceDevice.Default;
        EditorUtility.SetDirty(agent);
        EditorUtility.SetDirty(behavior);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[RLEval210BatchBuild] Eval scene saved: {EvalScenePath}");
        Debug.Log($"[RLEval210BatchBuild] Timeout seconds: {EvalEpisodeSeconds}");
        Debug.Log($"[RLEval210BatchBuild] BehaviorType: {behavior.BehaviorType}");
        Debug.Log($"[RLEval210BatchBuild] Model asset path: {ModelAssetPath}");
    }

    private static bool IsHeuristicOnlyBuild()
    {
        string[] args = Environment.GetCommandLineArgs();
        foreach (string arg in args)
            if (string.Equals(arg, "-heuristicOnly", StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static ModelAsset ImportModelAsset()
    {
        string sourceOnnx = GetModelSourcePath();
        if (!File.Exists(sourceOnnx))
            throw new FileNotFoundException($"Eval ONNX missing: {sourceOnnx}");

        string assetDir = Path.GetDirectoryName(ModelAssetPath);
        string assetDirAbsolute = Path.Combine(Path.GetDirectoryName(Application.dataPath), assetDir);
        if (!Directory.Exists(assetDirAbsolute))
            Directory.CreateDirectory(assetDirAbsolute);

        foreach (string staleWeightFile in Directory.GetFiles(assetDirAbsolute, "*.onnx.data", SearchOption.TopDirectoryOnly))
            File.Delete(staleWeightFile);

        string targetOnnx = Path.Combine(Path.GetDirectoryName(Application.dataPath), ModelAssetPath);
        File.Copy(sourceOnnx, targetOnnx, overwrite: true);

        string sourceModelDir = GetModelSearchRoot(sourceOnnx);
        string[] externalWeightFiles = Directory.GetFiles(sourceModelDir, "*.onnx.data", SearchOption.AllDirectories);
        foreach (string externalWeightFile in externalWeightFiles)
        {
            string targetWeightFile = Path.Combine(assetDirAbsolute, Path.GetFileName(externalWeightFile));
            File.Copy(externalWeightFile, targetWeightFile, overwrite: true);
        }
        Debug.Log($"[RLEval210BatchBuild] Model source: {sourceOnnx}");
        Debug.Log($"[RLEval210BatchBuild] Model search root: {sourceModelDir}");
        Debug.Log($"[RLEval210BatchBuild] Copied {externalWeightFiles.Length} ONNX external weight file(s).");

        AssetDatabase.ImportAsset(ModelAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        var model = AssetDatabase.LoadAssetAtPath<ModelAsset>(ModelAssetPath);
        if (model == null)
            throw new FileLoadException($"Could not import ONNX as ModelAsset: {ModelAssetPath}");

        return model;
    }

    private static string GetModelSourcePath()
    {
        string defaultPath = Path.Combine(GetWorkRoot(), "results", "BossPPO_SafeActionMask_100k_v1", "BossPlayer.onnx");
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-evalModelSource")
            {
                string candidate = args[i + 1];
                return Path.GetFullPath(Path.IsPathRooted(candidate)
                    ? candidate
                    : Path.Combine(GetWorkRoot(), candidate));
            }
        }

        return defaultPath;
    }

    private static string GetModelSearchRoot(string sourceOnnx)
    {
        DirectoryInfo dir = new DirectoryInfo(Path.GetDirectoryName(sourceOnnx));
        while (dir != null)
        {
            if (dir.Parent != null &&
                string.Equals(dir.Parent.Name, "results", StringComparison.OrdinalIgnoreCase))
                return dir.FullName;
            dir = dir.Parent;
        }

        return Path.GetDirectoryName(sourceOnnx);
    }

    private static bool RunBuild(string outputExe)
    {
        string outputDir = Path.GetDirectoryName(outputExe);
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        bool oldRunInBackground = PlayerSettings.runInBackground;
        Debug.Log($"[RLEval210BatchBuild] old runInBackground: {oldRunInBackground}");

        try
        {
            PlayerSettings.runInBackground = true;
            Debug.Log($"[RLEval210BatchBuild] build runInBackground: {PlayerSettings.runInBackground}");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { EvalScenePath },
                locationPathName = outputExe,
                target = GetBuildTarget(),
                options = BuildOptions.None,
            };

            Debug.Log($"[RLEval210BatchBuild] Building eval EXE: {outputExe}");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[RLEval210BatchBuild] Build SUCCEEDED. Size: {summary.totalSize} bytes");
                return true;
            }

            Debug.LogError($"[RLEval210BatchBuild] Build FAILED: {summary.result}, Errors: {summary.totalErrors}");
            return false;
        }
        finally
        {
            PlayerSettings.runInBackground = oldRunInBackground;
            Debug.Log($"[RLEval210BatchBuild] restored runInBackground: {PlayerSettings.runInBackground}");
        }
    }
}
