using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum StageRouteState
{
    Unknown,
    Pacifist,
    Mixed,
    Massacre
}

public enum BossRouteState
{
    Unknown,
    Killed,
    PacifistSurvived
}

public enum EndingRoute
{
    Normal,
    Pacifist,
    Massacre
}

public enum DeathRouteContext
{
    Normal,
    PacifistAttempt,
    MassacreAttempt,
    BossPacifistFinalHit
}

public static class RunRouteTracker
{
    private const string EntrySceneName = "Entry";
    private const string Stage1SceneName = "Stage01_ParkingToHospital";
    private const string Stage2SceneName = "Hospital";
    private const string BossSceneName = "Boss01_Elevator";

    private static readonly HashSet<int> stage1Registered = new HashSet<int>();
    private static readonly HashSet<int> stage1Killed = new HashSet<int>();
    private static readonly HashSet<int> stage2Registered = new HashSet<int>();
    private static readonly HashSet<int> stage2Killed = new HashSet<int>();

    private static string lastSceneName;
    private static bool bossMassacreRouteTriggerForTesting;

    public static StageRouteState Stage1Route { get; private set; } = StageRouteState.Unknown;
    public static StageRouteState Stage2Route { get; private set; } = StageRouteState.Unknown;
    public static BossRouteState BossRoute { get; private set; } = BossRouteState.Unknown;
    public static EndingRoute EndingRoute { get; private set; } = EndingRoute.Normal;
    public static DeathRouteContext DeathContext { get; private set; } = DeathRouteContext.Normal;
    public static bool BossMassacreRouteTriggerForTesting => bossMassacreRouteTriggerForTesting;

    public static int Stage1Kills   { get; private set; }
    public static int Stage1Total   { get; private set; }
    public static int Stage2Kills   { get; private set; }
    public static int Stage2Total   { get; private set; }

    // 스테이지별 사망(리트라이) 횟수 — 타이틀 복귀 시에만 초기화
    public static int Stage1Retries { get; private set; }
    public static int Stage2Retries { get; private set; }
    public static int BossRetries   { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        ResetRun();
        ResetRetries();
        lastSceneName = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapActiveScene()
    {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!string.IsNullOrEmpty(lastSceneName))
        {
            if (lastSceneName != scene.name)
                FinalizeStageForScene(lastSceneName);
            else
                IncrementRetryForScene(scene.name);  // 같은 씬 재로드 = 사망 후 리트라이
        }

        if (scene.name == EntrySceneName)
        {
            ResetRun();
            ResetRetries();   // 타이틀로 돌아가면 완전 초기화
        }
        else if (scene.name == Stage1SceneName)
        {
            ResetRun();       // ResetRun은 리트라이 카운트를 건드리지 않음
        }
        else
        {
            ResetStageForScene(scene.name);
        }

        lastSceneName = scene.name;
    }

    public static void ResetRun()
    {
        Stage1Route = StageRouteState.Unknown;
        Stage2Route = StageRouteState.Unknown;
        BossRoute = BossRouteState.Unknown;
        EndingRoute = EndingRoute.Normal;
        DeathContext = DeathRouteContext.Normal;
        bossMassacreRouteTriggerForTesting = false;

        ResetStage1();
        ResetStage2();
    }

    public static void ResetStageForScene(string sceneName)
    {
        if (sceneName == Stage1SceneName)
        {
            DeathContext = DeathRouteContext.Normal;
            ResetStage1();
            return;
        }

        if (sceneName == Stage2SceneName)
        {
            DeathContext = DeathRouteContext.Normal;
            ResetStage2();
            return;
        }

        if (sceneName == BossSceneName)
        {
            BossRoute = BossRouteState.Unknown;
            EndingRoute = EndingRoute.Normal;
            DeathContext = DeathRouteContext.Normal;
            bossMassacreRouteTriggerForTesting = false;
        }
    }

    public static void RegisterEnemyForCurrentScene(GameObject enemy)
    {
        if (enemy == null)
            return;

        string sceneName = SceneManager.GetActiveScene().name;
        int id = enemy.GetInstanceID();

        if (sceneName == Stage1SceneName)
        {
            if (stage1Registered.Add(id))
                Stage1Total = stage1Registered.Count;
        }
        else if (sceneName == Stage2SceneName)
        {
            if (stage2Registered.Add(id))
                Stage2Total = stage2Registered.Count;
        }
    }

    public static void RegisterEnemyDeathForCurrentScene(GameObject enemy)
    {
        if (enemy == null)
            return;

        string sceneName = SceneManager.GetActiveScene().name;
        int id = enemy.GetInstanceID();

        if (sceneName == Stage1SceneName)
        {
            stage1Registered.Add(id);
            Stage1Total = stage1Registered.Count;

            if (stage1Killed.Add(id))
                Stage1Kills = stage1Killed.Count;
        }
        else if (sceneName == Stage2SceneName)
        {
            stage2Registered.Add(id);
            Stage2Total = stage2Registered.Count;

            if (stage2Killed.Add(id))
                Stage2Kills = stage2Killed.Count;
        }
    }

    public static void FinalizeStageForScene(string sceneName)
    {
        if (sceneName == Stage1SceneName)
            Stage1Route = DecideStageRoute(Stage1Kills, Stage1Total);
        else if (sceneName == Stage2SceneName)
            Stage2Route = DecideStageRoute(Stage2Kills, Stage2Total);
    }

    public static void SetBossKilled()
    {
        BossRoute = BossRouteState.Killed;
    }

    public static void SetBossPacifistSurvived()
    {
        BossRoute = BossRouteState.PacifistSurvived;
    }

    public static void SetBossMassacreRouteTriggerForTesting(bool active)
    {
        bossMassacreRouteTriggerForTesting = active;
    }

    public static EndingRoute DecideEnding()
    {
        bool directBossRouteTest =
            Stage1Route == StageRouteState.Unknown
            && Stage2Route == StageRouteState.Unknown;

        if (directBossRouteTest)
        {
            if (BossRoute == BossRouteState.PacifistSurvived && !bossMassacreRouteTriggerForTesting)
            {
                EndingRoute = EndingRoute.Pacifist;
                return EndingRoute;
            }

            if (BossRoute == BossRouteState.Killed && bossMassacreRouteTriggerForTesting)
            {
                EndingRoute = EndingRoute.Massacre;
                return EndingRoute;
            }

            EndingRoute = EndingRoute.Normal;
            return EndingRoute;
        }

        if (Stage1Route == StageRouteState.Pacifist
            && Stage2Route == StageRouteState.Pacifist
            && BossRoute == BossRouteState.PacifistSurvived)
        {
            EndingRoute = EndingRoute.Pacifist;
            return EndingRoute;
        }

        if (Stage1Route == StageRouteState.Massacre
            && Stage2Route == StageRouteState.Massacre
            && BossRoute == BossRouteState.Killed)
        {
            EndingRoute = EndingRoute.Massacre;
            return EndingRoute;
        }

        EndingRoute = EndingRoute.Normal;
        return EndingRoute;
    }

    public static EndingRoute GetEndingRoute()
    {
        return DecideEnding();
    }

    public static bool TryGetCurrentStageKillInfo(out int kills, out int total)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == Stage1SceneName)
        {
            kills = Stage1Kills;
            total = Stage1Total;
            return true;
        }

        if (sceneName == Stage2SceneName)
        {
            kills = Stage2Kills;
            total = Stage2Total;
            return true;
        }

        kills = 0;
        total = 0;
        return false;
    }

    public static void SetDeathContext(DeathRouteContext context)
    {
        DeathContext = context;
    }

    public static void ClearDeathContext()
    {
        DeathContext = DeathRouteContext.Normal;
    }

    // ── 개발자 디버그 전용 ──────────────────────────────────────────────────
    /// <summary>개발자 디버그 씬 전용 — 스테이지 1 루트를 강제 지정합니다.</summary>
    public static void ForceStage1Route(StageRouteState route) { Stage1Route = route; }

    /// <summary>개발자 디버그 씬 전용 — 스테이지 2 루트를 강제 지정합니다.</summary>
    public static void ForceStage2Route(StageRouteState route) { Stage2Route = route; }

    private static StageRouteState DecideStageRoute(int kills, int total)
    {
        if (kills <= 0)
            return StageRouteState.Pacifist;

        if (total > 0 && kills >= total)
            return StageRouteState.Massacre;

        return StageRouteState.Mixed;
    }

    private static void ResetStage1()
    {
        Stage1Kills = 0;
        Stage1Total = 0;
        Stage1Route = StageRouteState.Unknown;
        stage1Registered.Clear();
        stage1Killed.Clear();
    }

    private static void ResetStage2()
    {
        Stage2Kills = 0;
        Stage2Total = 0;
        Stage2Route = StageRouteState.Unknown;
        stage2Registered.Clear();
        stage2Killed.Clear();
    }

    private static void ResetRetries()
    {
        Stage1Retries = 0;
        Stage2Retries = 0;
        BossRetries   = 0;
    }

    private static void IncrementRetryForScene(string sceneName)
    {
        if (sceneName == Stage1SceneName)
            Stage1Retries++;
        else if (sceneName == Stage2SceneName)
            Stage2Retries++;
        else if (sceneName == BossSceneName)
            BossRetries++;
    }
}
