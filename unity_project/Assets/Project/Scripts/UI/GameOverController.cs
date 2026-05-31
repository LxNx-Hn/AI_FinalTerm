using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverController : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject deathScarePanel;
    [SerializeField] private CanvasGroup deathScareCanvasGroup;
    [SerializeField] private Image deathScareImage;
    [SerializeField] private Sprite deathScareSprite;
    [SerializeField] private KeyCode retryKey = KeyCode.R;
    [SerializeField] private float deathDelay = 1.2f;
    [SerializeField] private float deathScareHoldSeconds = 2.0f;
    [SerializeField] private float deathScareFadeSeconds = 1.15f;

    [Header("Death Scare Audio")]
    [SerializeField] private float scareAudioVolume = 0.85f;

    [Header("Death Dialogue")]
    [SerializeField] private DialogueLine[] deathDialogueLines;

    [Header("Title Scene")]
    [SerializeField] private string titleSceneName = "Entry";

    private static readonly DialogueLine[] Stage01DeathLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "아, 안 돼—!"),
        new DialogueLine("???",    SpeakerType.Delusion, "늦었어."),
    };

    private static readonly DialogueLine[] Stage02DeathLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "오지 마...!"),
        new DialogueLine("???",    SpeakerType.Delusion, "아직도 사람으로 보여?"),
    };

    private static readonly DialogueLine[] Boss01DeathLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "아, 안 돼...!"),
        new DialogueLine("???",    SpeakerType.Delusion, "아직 못 나가."),
    };

    private static readonly DialogueLine[] BossPacifistFinalHitDeathLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "버텼는데..."),
        new DialogueLine("???",    SpeakerType.Delusion, "끝까지는 아니었어."),
    };

    private static readonly DialogueLine[] PacifistAttemptDeathLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "손을 들지 않았는데..."),
        new DialogueLine("???",    SpeakerType.Delusion, "그래서 죽은 거야."),
    };

    private static readonly DialogueLine[] MassacreAttemptDeathLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "아직 다 못 끝냈어."),
        new DialogueLine("???",    SpeakerType.Delusion, "그럼 다시 해."),
    };

    private PlayerHealth player;
    private bool showing;
    private bool retryEnabled;
    private bool suppressForRlMode;

    public bool IsShowing => showing;

    private void Start()
    {
        Time.timeScale = 1f;
        suppressForRlMode = IsRlModeActive();
        player = FindFirstObjectByType<PlayerHealth>();
        if (player != null)
        {
            player.onDead.AddListener(HandlePlayerDead);
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (deathScarePanel != null)
            deathScarePanel.SetActive(false);

        ApplyDeathScareSprite();
    }

    private void Update()
    {
        if (suppressForRlMode)
        {
            return;
        }

        if (retryEnabled && Input.GetKeyDown(retryKey))
        {
            CutsceneFreezeManager.ForceUnlock();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.onDead.RemoveListener(HandlePlayerDead);
        }

        if (showing)
            CutsceneFreezeManager.ForceUnlock();
    }

    private void HandlePlayerDead()
    {
        if (showing || suppressForRlMode)
            return;

        StartCoroutine(ShowGameOverAfterDelay());
    }

    private static bool IsRlModeActive()
    {
        return FindFirstObjectByType<BossPlayerAgent>() != null ||
               FindFirstObjectByType<BossRLEpisodeResetter>() != null;
    }

    private IEnumerator ShowGameOverAfterDelay()
    {
        showing = true;
        // ── 즉시 freeze ─ 적이 계속 움직이지 않도록 ──────────────────────
        CutsceneFreezeManager.Lock();
        DisablePlayerControls();

        if (deathDelay > 0f)
            yield return new WaitForSecondsRealtime(deathDelay);

        // ── 사망 공포 이미지 ──────────────────────────────────────────────
        EnsureDeathScareUi();
        ApplyDeathScareSprite();
        if (deathScarePanel != null)
        {
            deathScarePanel.SetActive(true);
            PlayScareAudio();

            if (deathScareCanvasGroup != null)
                deathScareCanvasGroup.alpha = 1f;

            if (deathScareImage != null)
                deathScareImage.enabled = deathScareImage.sprite != null;

            if (deathScareHoldSeconds > 0f)
                yield return new WaitForSecondsRealtime(deathScareHoldSeconds);

            if (deathScareCanvasGroup != null && deathScareFadeSeconds > 0f)
            {
                float elapsed = 0f;
                while (elapsed < deathScareFadeSeconds)
                {
                    elapsed += Time.unscaledDeltaTime;
                    deathScareCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / deathScareFadeSeconds);
                    yield return null;
                }
            }

            deathScarePanel.SetActive(false);
        }

        // ── 사망 대사 ────────────────────────────────────────────────────
        {
            var overlay = ClickAdvanceCutsceneOverlay.EnsureInScene();
            DialogueLine[] lines = GetDeathDialogueLines();
            if (lines != null && lines.Length > 0)
            {
                yield return overlay.ShowLines(lines);
                overlay.HideDialogue();
            }
            overlay.ResetFade();  // 항상 호출 — 남은 페이드 패널 알파 초기화
        }

        // ── 게임오버 패널 표시 ────────────────────────────────────────────
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();   // 최상단으로
            EnsureTitleButton(gameOverPanel);
        }

        retryEnabled = true;  // 패널 표시 후에만 R키 재시작 허용

        // CutsceneFreezeManager.Lock()이 timeScale=0 유지 (게임오버 일시정지)
        // RetryKey / TitleButton 클릭 시 ForceUnlock() 호출
    }

    private void EnsureTitleButton(GameObject panel)
    {
        EnsureEventSystem();

        CanvasGroup panelGroup = panel.GetComponent<CanvasGroup>();
        if (panelGroup != null)
        {
            panelGroup.interactable = true;
            panelGroup.blocksRaycasts = true;
        }

        Transform existing = panel.transform.Find("TitleButton");
        if (existing != null)
        {
            Button existingButton = existing.GetComponent<Button>();
            if (existingButton != null)
                ConfigureTitleButton(existingButton);
            return;
        }

        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Font font = Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "맑은 고딕", "Arial Unicode MS" }, 28);
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject btnGo = new GameObject("TitleButton",
            typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(panel.transform, false);

        RectTransform rect = btnGo.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0.5f, 0f);
        rect.anchorMax        = new Vector2(0.5f, 0f);
        rect.pivot            = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 20f);
        rect.sizeDelta        = new Vector2(240f, 56f);

        btnGo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(btnGo.transform, false);
        RectTransform tRect = textGo.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;

        Text label = textGo.GetComponent<Text>();
        label.text      = "타이틀로";
        label.font      = font;
        label.fontSize  = 26;
        label.color     = Color.white;
        label.alignment = TextAnchor.MiddleCenter;

        ConfigureTitleButton(btnGo.GetComponent<Button>());
    }

    private void EnsureDeathScareUi()
    {
        if (deathScarePanel != null || deathScareSprite == null)
            return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        GameObject panelGo = new GameObject("DeathScarePanel", typeof(RectTransform), typeof(CanvasGroup));
        panelGo.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        deathScarePanel = panelGo;
        deathScareCanvasGroup = panelGo.GetComponent<CanvasGroup>();

        GameObject backdropGo = new GameObject("DeathScareBackdrop", typeof(RectTransform), typeof(Image));
        backdropGo.transform.SetParent(panelGo.transform, false);
        RectTransform backdropRect = backdropGo.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        Image backdropImage = backdropGo.GetComponent<Image>();
        backdropImage.color = Color.black;
        backdropImage.raycastTarget = false;

        GameObject imageGo = new GameObject("DeathScareImage", typeof(RectTransform), typeof(Image));
        imageGo.transform.SetParent(panelGo.transform, false);
        RectTransform imageRect = imageGo.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        deathScareImage = imageGo.GetComponent<Image>();
        deathScareImage.preserveAspect = true;
        deathScareImage.raycastTarget = false;
        panelGo.SetActive(false);
    }

    private void ApplyDeathScareSprite()
    {
        if (deathScareImage != null && deathScareSprite != null)
            deathScareImage.sprite = deathScareSprite;
    }

    private void ConfigureTitleButton(Button button)
    {
        button.interactable = true;
        button.transform.SetAsLastSibling();
        if (button.targetGraphic != null)
            button.targetGraphic.raycastTarget = true;

        button.onClick.RemoveListener(ReturnToTitle);
        button.onClick.AddListener(ReturnToTitle);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private DialogueLine[] GetDeathDialogueLines()
    {
        if (deathDialogueLines != null && deathDialogueLines.Length > 0)
            return deathDialogueLines;

        DeathRouteContext context = ResolveDeathRouteContext();
        if (context == DeathRouteContext.BossPacifistFinalHit)
            return BossPacifistFinalHitDeathLines;

        if (context == DeathRouteContext.MassacreAttempt)
            return MassacreAttemptDeathLines;

        if (context == DeathRouteContext.PacifistAttempt)
            return PacifistAttemptDeathLines;

        switch (SceneManager.GetActiveScene().name)
        {
            case "Hospital":
                return Stage02DeathLines;
            case "Boss01_Elevator":
                return Boss01DeathLines;
            case "Stage01_ParkingToHospital":
                return Stage01DeathLines;
            default:
                return null;
        }
    }

    private static DeathRouteContext ResolveDeathRouteContext()
    {
        if (RunRouteTracker.DeathContext != DeathRouteContext.Normal)
            return RunRouteTracker.DeathContext;

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Boss01_Elevator")
        {
            if (RunRouteTracker.Stage1Route == StageRouteState.Massacre
                && RunRouteTracker.Stage2Route == StageRouteState.Massacre)
                return DeathRouteContext.MassacreAttempt;

            if (RunRouteTracker.Stage1Route == StageRouteState.Pacifist
                && RunRouteTracker.Stage2Route == StageRouteState.Pacifist)
                return DeathRouteContext.PacifistAttempt;

            return DeathRouteContext.Normal;
        }

        if (!RunRouteTracker.TryGetCurrentStageKillInfo(out int kills, out int total))
            return DeathRouteContext.Normal;

        if (kills <= 0)
            return DeathRouteContext.PacifistAttempt;

        if (total > 0 && kills >= total)
            return DeathRouteContext.MassacreAttempt;

        return DeathRouteContext.Normal;
    }

    private void PlayScareAudio()
    {
        string resourcePath;
        switch (SceneManager.GetActiveScene().name)
        {
            case "Stage01_ParkingToHospital": resourcePath = "DeathAudio/ST1"; break;
            case "Hospital":                  resourcePath = "DeathAudio/ST2"; break;
            case "Boss01_Elevator":           resourcePath = "DeathAudio/ST3"; break;
            default: return;
        }

        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip == null) return;

        var go = new GameObject("_death_scare_sfx");
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = scareAudioVolume;
        src.spatialBlend = 0f;
        src.Play();
        Destroy(go, clip.length + 0.5f);
    }

    private void ReturnToTitle()
    {
        CutsceneFreezeManager.ForceUnlock();
        SceneManager.LoadScene(titleSceneName);
    }

    private void DisablePlayerControls()
    {
        if (player == null)
            return;

        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        PlayerCombat combat = player.GetComponent<PlayerCombat>();
        if (combat != null) combat.enabled = false;


        OneStepOnDirectionKey footsteps = player.GetComponent<OneStepOnDirectionKey>();
        if (footsteps != null) footsteps.enabled = false;
    }
}
