using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageClearTrigger : GridZoneTriggerBase
{
    public string nextSceneName;
    public bool requireKeycard;

    private const string HospitalSceneName = "Hospital";
    private const string Boss01SceneName = "Boss01_Elevator";

    private static readonly DialogueLine[] HospitalClearLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "열렸다..."),
        new DialogueLine("???",    SpeakerType.Delusion, "들어가."),
        new DialogueLine("채하민", SpeakerType.Normal,   "뒤에서 따라와."),
        new DialogueLine("???",    SpeakerType.Delusion, "뒤는 보지 마."),
        new DialogueLine("채하민", SpeakerType.Normal,   "왜?"),
        new DialogueLine("???",    SpeakerType.Delusion, "잡히면 끝나."),
        new DialogueLine("채하민", SpeakerType.Normal,   "...알았어."),
    };

    private static readonly DialogueLine[] HospitalClearPacifistLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "열렸다..."),
        new DialogueLine("???",    SpeakerType.Delusion, "아직도 안 끝냈어."),
        new DialogueLine("채하민", SpeakerType.Normal,   "끝내야 하는 건지 모르겠어."),
        new DialogueLine("???",    SpeakerType.Delusion, "모르면 잡혀."),
        new DialogueLine("채하민", SpeakerType.Normal,   "그런데..."),
        new DialogueLine("채하민", SpeakerType.Normal,   "잡으려는 것 같지 않았어."),
    };

    private static readonly DialogueLine[] HospitalClearMassacreLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "열렸다..."),
        new DialogueLine("???",    SpeakerType.Delusion, "좋아."),
        new DialogueLine("채하민", SpeakerType.Normal,   "이제 끝이야?"),
        new DialogueLine("???",    SpeakerType.Delusion, "하나 남았어."),
        new DialogueLine("채하민", SpeakerType.Normal,   "..."),
        new DialogueLine("???",    SpeakerType.Delusion, "마지막이야."),
    };

    private static bool hospitalClearInProgress;
    private bool transitioning;
    private bool lockHeld;

    protected override bool OnTriggered()
    {
        if (transitioning)
            return true;

        if (requireKeycard)
        {
            if (GameState.Instance == null || !GameState.Instance.hasKeycard)
            {
                return false;
            }
        }

        string sceneName = SceneManager.GetActiveScene().name;
        RunRouteTracker.FinalizeStageForScene(sceneName);

        if (ShouldPlayHospitalClearDialogue())
        {
            if (hospitalClearInProgress)
                return true;

            hospitalClearInProgress = true;
            StartCoroutine(RunHospitalClearDialogue());
            return true;
        }

        SceneManager.LoadScene(nextSceneName);
        return true;
    }

    public static void ResetHospitalClearState()
    {
        hospitalClearInProgress = false;
    }

    private bool ShouldPlayHospitalClearDialogue()
    {
        return SceneManager.GetActiveScene().name == HospitalSceneName
               && nextSceneName == Boss01SceneName;
    }

    private IEnumerator RunHospitalClearDialogue()
    {
        transitioning = true;
        SetPlayerControlEnabled(false);
        CutsceneFreezeManager.Lock();
        lockHeld = true;

        var overlay = ClickAdvanceCutsceneOverlay.EnsureInScene();
        yield return overlay.ShowLines(GetHospitalClearLines());
        overlay.HideDialogue();

        lockHeld = false;
        CutsceneFreezeManager.ForceUnlock();
        SceneManager.LoadScene(nextSceneName);
    }

    private static DialogueLine[] GetHospitalClearLines()
    {
        switch (RunRouteTracker.Stage2Route)
        {
            case StageRouteState.Pacifist:
                return HospitalClearPacifistLines;
            case StageRouteState.Massacre:
                return HospitalClearMassacreLines;
            default:
                return HospitalClearLines;
        }
    }

    private void OnDestroy()
    {
        if (lockHeld)
        {
            lockHeld = false;
            CutsceneFreezeManager.ForceUnlock();
        }
    }

    private static void SetPlayerControlEnabled(bool enabled)
    {
        PlayerController controller = FindFirstObjectByType<PlayerController>();
        if (controller != null) controller.enabled = enabled;

        PlayerCombat combat = FindFirstObjectByType<PlayerCombat>();
        if (combat != null) combat.enabled = enabled;


        OneStepOnDirectionKey footsteps = FindFirstObjectByType<OneStepOnDirectionKey>();
        if (footsteps != null) footsteps.enabled = enabled;
    }
}

[DefaultExecutionOrder(-80)]
public class HospitalSceneDialogueRuntime : MonoBehaviour
{
    private const string HospitalSceneName = "Hospital";

    private static readonly DialogueLine[] HospitalStartLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "여긴..."),
        new DialogueLine("채하민", SpeakerType.Normal,   "사람이 있어야 할 곳이잖아."),
        new DialogueLine("???",    SpeakerType.Delusion, "다 변했어."),
        new DialogueLine("채하민", SpeakerType.Normal,   "다?"),
        new DialogueLine("???",    SpeakerType.Delusion, "네가 본 대로."),
        new DialogueLine("채하민", SpeakerType.Normal,   "그럼...\n다가오기 전에 끝내야 해."),
    };

    private static readonly DialogueLine[] HospitalStartPacifistLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "여긴..."),
        new DialogueLine("채하민", SpeakerType.Normal,   "사람이 있어야 할 곳이잖아."),
        new DialogueLine("???",    SpeakerType.Delusion, "그래서 더 위험해."),
        new DialogueLine("채하민", SpeakerType.Normal,   "아까도 그렇게 말했어."),
        new DialogueLine("???",    SpeakerType.Delusion, "아까도 네가 망설였지."),
        new DialogueLine("채하민", SpeakerType.Normal,   "..."),
        new DialogueLine("채하민", SpeakerType.Normal,   "이번에도 먼저 피할 거야."),
    };

    private static readonly DialogueLine[] HospitalStartMassacreLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "여긴..."),
        new DialogueLine("???",    SpeakerType.Delusion, "또 있어."),
        new DialogueLine("채하민", SpeakerType.Normal,   "보여."),
        new DialogueLine("???",    SpeakerType.Delusion, "이번엔 멈추지 마."),
        new DialogueLine("채하민", SpeakerType.Normal,   "..."),
        new DialogueLine("채하민", SpeakerType.Normal,   "다가오기 전에 끝내야 해."),
    };

    private bool lockHeld;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
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
        if (scene.name != HospitalSceneName)
            return;

        StageClearTrigger.ResetHospitalClearState();
        EnsureInScene();
    }

    private static HospitalSceneDialogueRuntime EnsureInScene()
    {
        HospitalSceneDialogueRuntime existing = FindFirstObjectByType<HospitalSceneDialogueRuntime>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject("HospitalSceneDialogueRuntime");
        return go.AddComponent<HospitalSceneDialogueRuntime>();
    }

    private IEnumerator Start()
    {
        yield return null;

        var overlay = ClickAdvanceCutsceneOverlay.EnsureInScene();
        SetPlayerControlEnabled(false);
        CutsceneFreezeManager.Lock();
        lockHeld = true;

        yield return overlay.FadeToBlack(0f);
        yield return overlay.FadeFromBlack(0.8f);
        yield return overlay.ShowLines(GetHospitalStartLines());
        overlay.HideDialogue();

        lockHeld = false;
        CutsceneFreezeManager.Unlock();
        SetPlayerControlEnabled(true);
    }

    private void OnDestroy()
    {
        if (lockHeld)
        {
            lockHeld = false;
            CutsceneFreezeManager.ForceUnlock();
        }
    }

    private static void SetPlayerControlEnabled(bool enabled)
    {
        PlayerController controller = FindFirstObjectByType<PlayerController>();
        if (controller != null) controller.enabled = enabled;

        PlayerCombat combat = FindFirstObjectByType<PlayerCombat>();
        if (combat != null) combat.enabled = enabled;


        OneStepOnDirectionKey footsteps = FindFirstObjectByType<OneStepOnDirectionKey>();
        if (footsteps != null) footsteps.enabled = enabled;
    }

    private static DialogueLine[] GetHospitalStartLines()
    {
        switch (RunRouteTracker.Stage1Route)
        {
            case StageRouteState.Pacifist:
                return HospitalStartPacifistLines;
            case StageRouteState.Massacre:
                return HospitalStartMassacreLines;
            default:
                return HospitalStartLines;
        }
    }
}
