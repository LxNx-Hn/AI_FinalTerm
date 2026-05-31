using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 개발자 전용 디버그 씬(DevDebug)이 로드될 때 메뉴 UI를 런타임으로 생성합니다.
/// Inspector 설정 없이 코드만으로 동작합니다.
/// </summary>
public class DevDebugMenuRuntime : MonoBehaviour
{
    private const string DebugSceneName  = "DevDebug";
    private const string EntryScene      = "Entry";
    private const string Stage2Scene     = "Hospital";
    private const string BossScene       = "Boss01_Elevator";
    private const string RooftopScene    = "RooftopEndingWalk";
    private const string RuntimeObjectName = "DevDebugMenuRuntime";
    private const string CanvasObjectName = "DevDebugCanvas";

    // ── 부트스트랩 ──────────────────────────────────────────────────────────
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
        if (scene.name != DebugSceneName) return;
        if (FindFirstObjectByType<DevDebugMenuRuntime>() != null) return;
        new GameObject(RuntimeObjectName).AddComponent<DevDebugMenuRuntime>();
    }

    // ── 초기화 ──────────────────────────────────────────────────────────────
    private void Awake()
    {
        DevDebugMenuRuntime[] runtimes = FindObjectsByType<DevDebugMenuRuntime>(FindObjectsSortMode.None);
        if (runtimes.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        BuildUI();
    }

    // ── UI 생성 ─────────────────────────────────────────────────────────────
    private void BuildUI()
    {
        if (GameObject.Find(CanvasObjectName) != null)
            return;

        var canvasGo = new GameObject(CanvasObjectName,
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        EnsureEventSystem();

        Transform root = canvas.transform;
        var font = GetFont();

        // 배경
        AddImage(root, "BG", new Color(0.06f, 0.06f, 0.10f, 0.97f),
            Vector2.zero, Vector2.one, asFirstSibling: true);

        // 타이틀
        AddLabel(root, font, "[ 개발자 메뉴 ]", 38, Color.white,
            new Vector2(0f, 0.91f), new Vector2(1f, 1f));

        // ── 스테이지 2 ──────────────────────────────────────────────────────
        AddLabel(root, font, "■  스테이지 2 바로가기", 22, Tint(0.75f, 0.88f, 1f),
            new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.87f));

        AddButtonRow(root, font, 0.72f, 0.79f,
            Btn("노말",  DarkBlue,       () => GoStage2(StageRouteState.Unknown)),
            Btn("몰살",  DarkRed,        () => GoStage2(StageRouteState.Massacre)),
            Btn("불살",  DarkCyan,       () => GoStage2(StageRouteState.Pacifist)));

        // ── 보스 스테이지 ────────────────────────────────────────────────────
        AddLabel(root, font, "■  보스 스테이지 바로가기", 22, Tint(0.75f, 0.88f, 1f),
            new Vector2(0.04f, 0.58f), new Vector2(0.96f, 0.65f));

        AddButtonRow(root, font, 0.50f, 0.57f,
            Btn("노말",  DarkBlue,       () => GoBoss(StageRouteState.Unknown)),
            Btn("불살",  DarkCyan,       () => GoBoss(StageRouteState.Pacifist)),
            Btn("몰살",  DarkRed,        () => GoBoss(StageRouteState.Massacre)));

        // ── 옥상 마무리 ──────────────────────────────────────────────────────
        AddLabel(root, font, "■  옥상 마무리 바로가기", 22, Tint(0.75f, 0.88f, 1f),
            new Vector2(0.04f, 0.36f), new Vector2(0.96f, 0.43f));

        AddButtonRow(root, font, 0.28f, 0.35f,
            Btn("노말",  DarkBlue,       () => GoRooftop(EndingRoute.Normal)),
            Btn("불살",  DarkCyan,       () => GoRooftop(EndingRoute.Pacifist)),
            Btn("몰살",  DarkRed,        () => GoRooftop(EndingRoute.Massacre)));

        // ── 하단 ─────────────────────────────────────────────────────────────
        AddButtonRow(root, font, 0.07f, 0.17f,
            Btn("◀  타이틀로",  DarkGray, () => SceneManager.LoadScene(EntryScene)),
            Btn("✕  종료",     DarkRed2, ApplicationQuitHelper.Quit));
    }

    // ── 라우트 핸들러 ────────────────────────────────────────────────────────
    private static void GoStage2(StageRouteState stage1Override)
    {
        RunRouteTracker.ResetRun();
        if (stage1Override != StageRouteState.Unknown)
            RunRouteTracker.ForceStage1Route(stage1Override);
        SceneManager.LoadScene(Stage2Scene);
    }

    private static void GoBoss(StageRouteState routeOverride)
    {
        RunRouteTracker.ResetRun();
        if (routeOverride != StageRouteState.Unknown)
        {
            RunRouteTracker.ForceStage1Route(routeOverride);
            RunRouteTracker.ForceStage2Route(routeOverride);
        }
        SceneManager.LoadScene(BossScene);
    }

    private static void GoRooftop(EndingRoute ending)
    {
        RunRouteTracker.ResetRun();
        switch (ending)
        {
            case EndingRoute.Pacifist:
                RunRouteTracker.ForceStage1Route(StageRouteState.Pacifist);
                RunRouteTracker.ForceStage2Route(StageRouteState.Pacifist);
                RunRouteTracker.SetBossPacifistSurvived();
                break;
            case EndingRoute.Massacre:
                RunRouteTracker.ForceStage1Route(StageRouteState.Massacre);
                RunRouteTracker.ForceStage2Route(StageRouteState.Massacre);
                RunRouteTracker.SetBossKilled();
                break;
        }
        SceneManager.LoadScene(RooftopScene);
    }

    // ── 색상 상수 ────────────────────────────────────────────────────────────
    private static Color DarkBlue => new Color(0.12f, 0.18f, 0.32f, 1f);
    private static Color DarkRed  => new Color(0.32f, 0.08f, 0.08f, 1f);
    private static Color DarkCyan => new Color(0.06f, 0.22f, 0.30f, 1f);
    private static Color DarkGray => new Color(0.18f, 0.18f, 0.22f, 1f);
    private static Color DarkRed2 => new Color(0.28f, 0.10f, 0.10f, 1f);
    private static Color Tint(float r, float g, float b) => new Color(r, g, b, 1f);

    // ── UI 헬퍼 ──────────────────────────────────────────────────────────────
    private struct BtnDef
    {
        public string label;
        public Color  bg;
        public Action onClick;
    }

    private static BtnDef Btn(string label, Color bg, Action onClick)
        => new BtnDef { label = label, bg = bg, onClick = onClick };

    private static void AddButtonRow(Transform root, Font font,
        float yMin, float yMax, params BtnDef[] defs)
    {
        int n = defs.Length;
        float btnW   = (n == 2) ? 0.28f : 0.26f;
        float spacer = (n == 2) ? 0.06f : 0.03f;
        float total  = n * btnW + (n - 1) * spacer;
        float xStart = 0.5f - total * 0.5f;

        for (int i = 0; i < n; i++)
        {
            float xMin = xStart + i * (btnW + spacer);
            float xMax = xMin + btnW;
            AddButton(root, font, defs[i].label, defs[i].bg, defs[i].onClick,
                new Vector2(xMin, yMin), new Vector2(xMax, yMax));
        }
    }

    private static void AddButton(Transform root, Font font, string label,
        Color bg, Action onClick, Vector2 ancMin, Vector2 ancMax)
    {
        var go = new GameObject("Btn_" + label,
            typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(root, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = bg;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(4, 2); trt.offsetMax = new Vector2(-4, -2);
        var t = textGo.GetComponent<Text>();
        t.text = label; t.font = font; t.fontSize = 26;
        t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.normalColor      = bg;
        cb.highlightedColor = new Color(
            Mathf.Min(1f, bg.r + 0.18f),
            Mathf.Min(1f, bg.g + 0.18f),
            Mathf.Min(1f, bg.b + 0.18f), 1f);
        cb.pressedColor = new Color(
            Mathf.Max(0f, bg.r - 0.08f),
            Mathf.Max(0f, bg.g - 0.08f),
            Mathf.Max(0f, bg.b - 0.08f), 1f);
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick?.Invoke());
    }

    private static void AddLabel(Transform root, Font font, string text, int size,
        Color color, Vector2 ancMin, Vector2 ancMax)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(root, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<Text>();
        t.text = text; t.font = font; t.fontSize = size;
        t.color = color; t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
    }

    private static void AddImage(Transform root, string name, Color color,
        Vector2 ancMin, Vector2 ancMax, bool asFirstSibling = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(root, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        if (asFirstSibling) go.transform.SetAsFirstSibling();
    }

    private static Font GetFont()
    {
        var f = Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "맑은 고딕", "Arial Unicode MS" }, 26);
        return f != null ? f : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
