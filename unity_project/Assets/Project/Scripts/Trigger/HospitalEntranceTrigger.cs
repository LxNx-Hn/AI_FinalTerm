using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HospitalEntranceTrigger : MonoBehaviour
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

    public Vector2Int size = new Vector2Int(3, 2);
    public string nextSceneName = "Hospital";
    public TextMeshProUGUI promptText;
    public string promptMessage = "Press E to Enter Hospital";
    [SerializeField] private float transitionDelay = 0.15f;
    [SerializeField] private float fadeDuration = 0.55f;

    private GridOccupant player;
    private bool transitioning;
    private bool freezeHeld;

    private void Start()
    {
        CachePlayer();
        SetPromptVisible(false);
    }

    private void Update()
    {
        if (player == null)
        {
            CachePlayer();
        }

        if (player == null || GridManager.Instance == null)
        {
            SetPromptVisible(false);
            return;
        }

        bool inside = Contains(player.CurrentCell);
        SetPromptVisible(inside);

        if (!inside)
        {
            return;
        }

        if (!transitioning && Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(EnterHospitalRoutine());
        }
    }

    private IEnumerator EnterHospitalRoutine()
    {
        transitioning = true;
        SetPromptVisible(false);
        string sceneName = SceneManager.GetActiveScene().name;
        RunRouteTracker.FinalizeStageForScene(sceneName);

        CutsceneFreezeManager.Lock();
        freezeHeld = true;

        DialogueLine[] routeLines = GetRouteExitLines(sceneName);
        if (routeLines != null && routeLines.Length > 0)
        {
            var overlay = ClickAdvanceCutsceneOverlay.EnsureInScene();
            yield return overlay.ShowLines(routeLines);
            overlay.HideDialogue();
        }

        CanvasGroup fader = CreateFadeOverlay();
        float startTime = Time.unscaledTime;
        while (Time.unscaledTime - startTime < fadeDuration)
        {
            float t = Mathf.Clamp01((Time.unscaledTime - startTime) / fadeDuration);
            fader.alpha = t;
            yield return null;
        }

        fader.alpha = 1f;

        if (transitionDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(transitionDelay);
        }

        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            CutsceneFreezeManager.ForceUnlock();
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private DialogueLine[] GetRouteExitLines(string sceneName)
    {
        if (sceneName != Stage1SceneName || nextSceneName != HospitalSceneName)
            return null;

        switch (RunRouteTracker.Stage1Route)
        {
            case StageRouteState.Pacifist:
                return Stage1ExitPacifistLines;
            case StageRouteState.Massacre:
                return Stage1ExitMassacreLines;
            default:
                return null;
        }
    }

    private void OnDisable()
    {
        ReleaseFreeze();
    }

    private void OnDestroy()
    {
        ReleaseFreeze();
    }

    private static CanvasGroup CreateFadeOverlay()
    {
        GameObject canvasGo = new GameObject("StageTransitionFadeCanvas");
        DontDestroyOnLoad(canvasGo);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasGo.AddComponent<GraphicRaycaster>();
        CanvasGroup group = canvasGo.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = true;

        GameObject imageGo = new GameObject("Fade");
        imageGo.transform.SetParent(canvasGo.transform, false);
        Image image = imageGo.AddComponent<Image>();
        image.color = Color.black;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Object.Destroy(canvasGo, 3f);
        return group;
    }

    private bool Contains(Vector2Int cell)
    {
        Vector2Int center = GridManager.Instance.WorldToCell(transform.position);
        int minX = center.x - size.x / 2;
        int maxX = minX + size.x - 1;
        int minY = center.y - size.y / 2;
        int maxY = minY + size.y - 1;

        return cell.x >= minX && cell.x <= maxX && cell.y >= minY && cell.y <= maxY;
    }

    private void CachePlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.GetComponent<GridOccupant>();
        }
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptText == null)
        {
            return;
        }

        promptText.gameObject.SetActive(visible);
        if (visible)
        {
            promptText.text = promptMessage;
        }
    }

    private void ReleaseFreeze()
    {
        if (!freezeHeld)
        {
            return;
        }

        freezeHeld = false;
        CutsceneFreezeManager.Unlock();
    }
}
