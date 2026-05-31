using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneEndDialogueTrigger : GridZoneTriggerBase
{
    private const string Stage1SceneName = "Stage01_ParkingToHospital";
    private const string HospitalSceneName = "Hospital";

    private static readonly DialogueLine[] Stage1ExitPacifistLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "아직 따라와."),
        new DialogueLine("???",    SpeakerType.Delusion, "왜 안 끝냈어?"),
        new DialogueLine("채하민", SpeakerType.Normal,   "..."),
        new DialogueLine("채하민", SpeakerType.Normal,   "사람처럼 보였어."),
        new DialogueLine("???",    SpeakerType.Delusion, "그렇게 보면 죽어."),
    };

    private static readonly DialogueLine[] Stage1ExitMassacreLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "조용해졌어."),
        new DialogueLine("???",    SpeakerType.Delusion, "잘했어."),
        new DialogueLine("채하민", SpeakerType.Normal,   "뭐가."),
        new DialogueLine("???",    SpeakerType.Delusion, "망설이지 않았잖아."),
        new DialogueLine("채하민", SpeakerType.Normal,   "..."),
        new DialogueLine("???",    SpeakerType.Delusion, "안으로 가.\n아직 남았어."),
    };

    [SerializeField] private DialogueLine[] lines;
    [SerializeField] private string nextSceneName;
    [SerializeField] private float fadeOutDuration = 0.8f;

    private bool _lockHeld;

    protected override bool OnTriggered()
    {
        StartCoroutine(Run());
        return true;
    }

    private IEnumerator Run()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        RunRouteTracker.FinalizeStageForScene(sceneName);

        SetPlayerControlEnabled(false);
        CutsceneFreezeManager.Lock();
        _lockHeld = true;

        var overlay = ClickAdvanceCutsceneOverlay.EnsureInScene();
        DialogueLine[] selectedLines = GetLinesForRoute(sceneName);

        if (selectedLines != null && selectedLines.Length > 0)
        {
            yield return overlay.ShowLines(selectedLines);
            overlay.HideDialogue();
        }

        yield return overlay.FadeToBlack(fadeOutDuration);

        _lockHeld = false;
        CutsceneFreezeManager.ForceUnlock();

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    private DialogueLine[] GetLinesForRoute(string sceneName)
    {
        if (sceneName != Stage1SceneName || nextSceneName != HospitalSceneName)
            return lines;

        switch (RunRouteTracker.Stage1Route)
        {
            case StageRouteState.Pacifist:
                return Stage1ExitPacifistLines;
            case StageRouteState.Massacre:
                return Stage1ExitMassacreLines;
            default:
                return lines;
        }
    }

    private void OnDestroy()
    {
        if (_lockHeld)
        {
            _lockHeld = false;
            CutsceneFreezeManager.ForceUnlock();
        }
    }

    private static void SetPlayerControlEnabled(bool enabled)
    {
        PlayerController controller = FindFirstObjectByType<PlayerController>();
        if (controller != null) controller.enabled = enabled;

        PlayerCombat combat = FindFirstObjectByType<PlayerCombat>();
        if (combat != null) combat.enabled = enabled;


    }
}
