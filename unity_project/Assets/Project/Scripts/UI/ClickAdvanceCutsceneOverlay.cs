using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClickAdvanceCutsceneOverlay : MonoBehaviour
{
    // ── 오브젝트 이름 상수 ──────────────────────────────────────────────────
    private const string CanvasName           = "Canvas";
    private const string PanelName            = "CutscenePanel";
    private const string PortraitName         = "PortraitImage";
    private const string RightContainerName   = "RightContainer";
    private const string NameTextName         = "SpeakerNameText";
    private const string TextName             = "CutsceneText";
    private const string LegacyTextChildName  = "CutsceneTextLegacy";
    private const string FadeName             = "FadePanel";

    // ── UI 참조 ─────────────────────────────────────────────────────────────
    private GameObject panelObject;
    private Text       messageText;
    private Image      fadeImage;
    private Image      portraitImage;
    private Text       nameText;

    private static Font koreanSafeFont;

    // ── 싱글톤 생성 ─────────────────────────────────────────────────────────
    public static ClickAdvanceCutsceneOverlay EnsureInScene()
    {
        ClickAdvanceCutsceneOverlay existing = FindFirstObjectByType<ClickAdvanceCutsceneOverlay>();
        if (existing != null)
        {
            existing.EnsureUi();
            return existing;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
        }

        NormalizeCanvas(canvas);

        ClickAdvanceCutsceneOverlay overlay = canvas.gameObject.GetComponent<ClickAdvanceCutsceneOverlay>();
        if (overlay == null)
            overlay = canvas.gameObject.AddComponent<ClickAdvanceCutsceneOverlay>();

        overlay.EnsureUi();
        return overlay;
    }

    private void Awake() { EnsureUi(); }

    // ── 기존 string 오버로드 (하위 호환) ────────────────────────────────────
    public IEnumerator ShowLine(string line, float minAdvanceDelay = 0.08f)
    {
        EnsureUi();
        ApplyDialogueStyle(new DialogueLine { speakerType = SpeakerType.Narration });
        panelObject.SetActive(true);
        messageText.text = line;
        yield return WaitForAdvance(minAdvanceDelay);
    }

    public IEnumerator ShowLines(string[] lines, float minAdvanceDelay = 0.08f)
    {
        if (lines == null) yield break;
        foreach (string line in lines)
            yield return ShowLine(line, minAdvanceDelay);
    }

    // ── DialogueLine 오버로드 ────────────────────────────────────────────────
    public IEnumerator ShowLine(DialogueLine line, float minAdvanceDelay = 0.08f)
    {
        EnsureUi();
        ApplyDialogueStyle(line);
        panelObject.SetActive(true);
        messageText.text = HasVisibleSpeakerName(line) ? "\n" + line.body : line.body;
        yield return WaitForAdvance(minAdvanceDelay);
    }

    public IEnumerator ShowLines(DialogueLine[] lines, float minAdvanceDelay = 0.08f)
    {
        if (lines == null) yield break;
        foreach (DialogueLine line in lines)
            yield return ShowLine(line, minAdvanceDelay);
    }

    // ── 숨김 ────────────────────────────────────────────────────────────────
    public void HideDialogue()
    {
        EnsureUi();
        panelObject.SetActive(false);
    }

    // ── 페이드 패널 알파 초기화 (GameOver 등 남은 검정 오버레이 제거용) ──────
    public void ResetFade()
    {
        EnsureUi();
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = 0f;
        fadeImage.color = c;
    }

    // ── 페이드 ──────────────────────────────────────────────────────────────
    public IEnumerator FadeToBlack(float duration)   => Fade(1f, duration);
    public IEnumerator FadeFromBlack(float duration) => Fade(0f, duration);

    public IEnumerator Fade(float targetAlpha, float duration)
    {
        EnsureUi();
        float startAlpha = fadeImage.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            Color c = fadeImage.color;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            fadeImage.color = c;
            yield return null;
        }

        Color fc = fadeImage.color;
        fc.a = targetAlpha;
        fadeImage.color = fc;
    }

    // ── 입력 대기 ────────────────────────────────────────────────────────────
    private IEnumerator WaitForAdvance(float minAdvanceDelay)
    {
        float unlockAt = Time.unscaledTime + Mathf.Max(0f, minAdvanceDelay);
        while (Time.unscaledTime < unlockAt)
            yield return null;

        while (true)
        {
            if (Input.GetMouseButtonDown(0)           ||
                Input.GetKeyDown(KeyCode.Space)        ||
                Input.GetKeyDown(KeyCode.Return)       ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
                break;
            yield return null;
        }
    }

    // ── 화자 스타일 적용 ────────────────────────────────────────────────────
    private void ApplyDialogueStyle(DialogueLine line)
    {
        bool showName = HasVisibleSpeakerName(line);

        if (nameText != null)
        {
            nameText.gameObject.SetActive(showName);
            if (showName)
            {
                nameText.text  = $"[ {line.speakerName} ]";
                nameText.color = GetNameColor(line.speakerType);
            }
        }

        if (messageText != null)
            messageText.color = GetBodyColor(line.speakerType);

        if (portraitImage != null)
        {
            PortraitDatabase db = PortraitDatabase.Instance;
            Sprite portrait = db != null ? db.GetPortrait(line.speakerType) : null;

            if (portrait != null)
            {
                portraitImage.gameObject.SetActive(true);
                portraitImage.sprite = portrait;
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
                if (line.speakerType != SpeakerType.Narration)
                    Debug.LogWarning($"[ClickAdvanceCutsceneOverlay] SpeakerType.{line.speakerType} 초상화 없음 — PortraitDatabase 확인 필요");
            }
        }
    }

    // ── 색상 규칙 ────────────────────────────────────────────────────────────
    private static Color GetNameColor(SpeakerType t)
    {
        switch (t)
        {
            case SpeakerType.Delusion:
            case SpeakerType.NameCrack1:     return HexColor("#C93434");
            case SpeakerType.NameCrack2:     return HexColor("#D45A5A");
            case SpeakerType.MergedDelusion: return HexColor("#E6B0B0");
            default:                         return HexColor("#E8E8E8");
        }
    }

    private static Color GetBodyColor(SpeakerType t)
    {
        switch (t)
        {
            case SpeakerType.Delusion:
            case SpeakerType.NameCrack1:     return HexColor("#D86A6A");
            case SpeakerType.NameCrack2:     return HexColor("#E09090");
            case SpeakerType.MergedDelusion: return HexColor("#E6C0C0");
            default:                         return HexColor("#DCDCDC");
        }
    }

    private static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    private static bool HasVisibleSpeakerName(DialogueLine line)
    {
        return !string.IsNullOrEmpty(line.speakerName)
               && line.speakerType != SpeakerType.Narration;
    }

    // ── UI 생성 / 확인 ───────────────────────────────────────────────────────
    private void EnsureUi()
    {
        if (panelObject != null && messageText != null && fadeImage != null)
            return;

        Canvas canvas = GetComponent<Canvas>() ?? FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        NormalizeCanvas(canvas);

        Transform canvasT = canvas.transform;

        // ── 패널 (하단 28%) ──────────────────────────────────────────────
        panelObject = FindOrCreateChild(canvasT, PanelName);
        RectTransform panelRect = EnsureRectTransform(panelObject);
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0.28f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panelObject.GetComponent<Image>() ?? panelObject.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.82f);
        panelObject.SetActive(false);

        // Portrait: square profile art should nearly fill the dialogue box height.
        GameObject portraitObj = FindOrCreateChild(panelObject.transform, PortraitName);
        RectTransform portraitRect = EnsureRectTransform(portraitObj);
        portraitRect.anchorMin = new Vector2(0f, 0f);
        portraitRect.anchorMax = new Vector2(0f, 1f);
        portraitRect.offsetMin = new Vector2(0f, 0f);
        portraitRect.offsetMax = new Vector2(300f, 0f);

        portraitImage = portraitObj.GetComponent<Image>() ?? portraitObj.AddComponent<Image>();
        portraitImage.preserveAspect = true;
        portraitImage.color = Color.white;
        portraitObj.SetActive(false);

        // ── 오른쪽 컨테이너 (초상화 이후) ───────────────────────────────
        GameObject rightContainer = FindOrCreateChild(panelObject.transform, RightContainerName);
        RectTransform rightRect = EnsureRectTransform(rightContainer);
        rightRect.anchorMin = new Vector2(0f, 0f);
        rightRect.anchorMax = new Vector2(1f, 1f);
        rightRect.offsetMin = new Vector2(316f, 0f);
        rightRect.offsetMax = new Vector2(-8f,  0f);

        // ── 이름 태그 (오른쪽 상단 28%) ─────────────────────────────────
        GameObject nameObj = FindOrCreateChild(rightContainer.transform, NameTextName);
        RectTransform nameRect = EnsureRectTransform(nameObj);
        nameRect.anchorMin = new Vector2(0f, 0.74f);
        nameRect.anchorMax = new Vector2(1f, 0.96f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;

        nameText = nameObj.GetComponent<Text>() ?? nameObj.AddComponent<Text>();
        nameText.font                  = GetKoreanSafeFont();
        nameText.fontSize              = 22;
        nameText.color                 = Color.white;
        nameText.alignment             = TextAnchor.LowerLeft;
        nameText.horizontalOverflow    = HorizontalWrapMode.Wrap;
        nameText.verticalOverflow      = VerticalWrapMode.Truncate;
        nameText.raycastTarget         = false;
        nameObj.SetActive(false);

        // ── 본문 텍스트 컨테이너 (오른쪽 하단 72%) ──────────────────────
        GameObject textContainer = FindOrCreateChild(rightContainer.transform, TextName);
        RectTransform containerRect = EnsureRectTransform(textContainer);
        containerRect.anchorMin = new Vector2(0f, 0.08f);
        containerRect.anchorMax = new Vector2(1f, 0.74f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = new Vector2(0f, -4f);

        // 기존 씬에 TMP 컴포넌트가 있으면 비활성화
        TextMeshProUGUI tmpText = textContainer.GetComponent<TextMeshProUGUI>();
        if (tmpText != null) tmpText.enabled = false;

        // 실제 렌더링용 Legacy Text 자식
        GameObject legacyObj = FindOrCreateChild(textContainer.transform, LegacyTextChildName);
        RectTransform legacyRect = EnsureRectTransform(legacyObj);
        legacyRect.anchorMin = Vector2.zero;
        legacyRect.anchorMax = Vector2.one;
        legacyRect.offsetMin = Vector2.zero;
        legacyRect.offsetMax = Vector2.zero;

        messageText = legacyObj.GetComponent<Text>() ?? legacyObj.AddComponent<Text>();
        messageText.text              = string.Empty;
        messageText.font              = GetKoreanSafeFont();
        messageText.fontSize          = 26;
        messageText.color             = HexColor("#DCDCDC");
        messageText.alignment         = TextAnchor.UpperLeft;
        messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        messageText.verticalOverflow  = VerticalWrapMode.Truncate;
        messageText.raycastTarget     = false;

        // ── 페이드 오버레이 (전체 화면) ─────────────────────────────────
        GameObject fadeObject = FindOrCreateChild(canvasT, FadeName);
        RectTransform fadeRect = EnsureRectTransform(fadeObject);
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        fadeImage = fadeObject.GetComponent<Image>() ?? fadeObject.AddComponent<Image>();
        fadeImage.color = Color.clear;
        fadeImage.raycastTarget = false;

        // 렌더 순서: 페이드가 최상단
        fadeObject.transform.SetAsLastSibling();
        panelObject.transform.SetAsLastSibling();
        fadeObject.transform.SetAsLastSibling();
    }

    // ── 헬퍼 ────────────────────────────────────────────────────────────────
    private static GameObject FindOrCreateChild(Transform parent, string objectName)
    {
        Transform child = parent.Find(objectName);
        if (child != null) return child.gameObject;
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform EnsureRectTransform(GameObject go)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect == null) rect = go.AddComponent<RectTransform>();
        return rect;
    }

    private static void NormalizeCanvas(Canvas canvas)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.transform.localScale = Vector3.one;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode    = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static Font GetKoreanSafeFont()
    {
        if (koreanSafeFont != null) return koreanSafeFont;
        string[] fontNames = { "Malgun Gothic", "맑은 고딕", "Arial Unicode MS", "Noto Sans CJK KR" };
        koreanSafeFont = Font.CreateDynamicFontFromOSFont(fontNames, 28);
        if (koreanSafeFont == null)
            koreanSafeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return koreanSafeFont;
    }
}
