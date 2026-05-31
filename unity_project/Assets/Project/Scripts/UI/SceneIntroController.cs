using System.Collections;
using UnityEngine;

public class SceneIntroController : MonoBehaviour
{
    [SerializeField] private DialogueLine[] introLines;
    [SerializeField] private bool fadeFromBlack = true;
    [SerializeField] private float fadeInDuration = 0.8f;

    private bool _lockHeld;

    private IEnumerator Start()
    {
        if (introLines == null || introLines.Length == 0)
            yield break;

        var overlay = ClickAdvanceCutsceneOverlay.EnsureInScene();

        SetPlayerControlEnabled(false);
        CutsceneFreezeManager.Lock();
        _lockHeld = true;

        if (fadeFromBlack)
        {
            yield return overlay.FadeToBlack(0f);          // 즉시 검정
            yield return overlay.FadeFromBlack(fadeInDuration);
        }

        yield return overlay.ShowLines(introLines);
        overlay.HideDialogue();

        _lockHeld = false;
        CutsceneFreezeManager.Unlock();
        SetPlayerControlEnabled(true);
    }

    private void OnDestroy()
    {
        if (_lockHeld)
        {
            _lockHeld = false;
            CutsceneFreezeManager.Unlock();
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
