using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BodyCountUI : MonoBehaviour
{
    private const string UiName = "BodyCountUI";
    private const string Stage1SceneName = "Stage01_ParkingToHospital";
    private const string Stage2SceneName = "Hospital";
    private static readonly Color DefaultBodyCountColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);

    private Text bodyCountText;
    private int lastKills = -1;
    private int lastTotal = -1;
    private StageRouteState lastPreviewRoute = StageRouteState.Unknown;

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
        if (ShouldShowInScene(scene.name))
            EnsureInScene();
    }

    private static bool ShouldShowInScene(string sceneName)
    {
        return sceneName == Stage1SceneName || sceneName == Stage2SceneName;
    }

    private static BodyCountUI EnsureInScene()
    {
        BodyCountUI existing = FindFirstObjectByType<BodyCountUI>();
        if (existing != null)
            return existing;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        // 모든 캔버스에 ScaleWithScreenSize 적용 (Stage 1 등 기존 캔버스 포함)
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        GameObject go = new GameObject(UiName, typeof(RectTransform), typeof(Text), typeof(Shadow), typeof(BodyCountUI));
        go.transform.SetParent(canvas.transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(32f, -118f);
        rect.sizeDelta = new Vector2(360f, 66f);

        Text text = go.GetComponent<Text>();
        text.font = CreateBodyCountFont();
        text.fontSize = 22;
        text.alignment = TextAnchor.UpperLeft;
        text.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        text.raycastTarget = false;

        Shadow shadow = go.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        shadow.effectDistance = new Vector2(1f, -1f);

        BodyCountUI ui = go.GetComponent<BodyCountUI>();
        ui.bodyCountText = text;
        return ui;
    }

    private static Font CreateBodyCountFont()
    {
        Font font = Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "맑은 고딕", "Arial Unicode MS", "Noto Sans CJK KR" },
            24
        );

        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private void Awake()
    {
        bodyCountText = GetComponent<Text>();
    }

    private void Update()
    {
        bool visible = RunRouteTracker.TryGetCurrentStageKillInfo(out int kills, out int total) && total > 0;
        if (!visible)
        {
            if (bodyCountText != null && bodyCountText.enabled)
                bodyCountText.enabled = false;
            return;
        }

        if (bodyCountText != null && !bodyCountText.enabled)
            bodyCountText.enabled = true;

        StageRouteState previewRoute = PreviewRoute(kills, total);
        if (kills == lastKills && total == lastTotal && previewRoute == lastPreviewRoute)
            return;

        lastKills = kills;
        lastTotal = total;
        lastPreviewRoute = previewRoute;
        if (bodyCountText != null)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            bodyCountText.text = BuildDisplayText(sceneName, previewRoute, kills, total);
            bodyCountText.color = BuildDisplayColor(sceneName, previewRoute);
        }
    }

    private static string BuildDisplayText(string sceneName, StageRouteState previewRoute, int kills, int total)
    {
        if (previewRoute == StageRouteState.Massacre)
            return $"BC : {kills}\nNo more here...";
        return $"BC : {kills}";
    }

    private static Color BuildDisplayColor(string sceneName, StageRouteState previewRoute)
    {
        // 몰살(전멸) 루트일 때만 빨간색, 그 외는 기본 회백색
        return previewRoute == StageRouteState.Massacre
            ? new Color(1f, 0.48f, 0.45f, 0.94f)
            : DefaultBodyCountColor;
    }

    private static StageRouteState PreviewRoute(int kills, int total)
    {
        if (kills <= 0)
            return StageRouteState.Pacifist;

        if (total > 0 && kills >= total)
            return StageRouteState.Massacre;

        return StageRouteState.Mixed;
    }

    private static string RouteLabel(StageRouteState route)
    {
        switch (route)
        {
            case StageRouteState.Pacifist:
                return "ROUTE : NO KILL";
            case StageRouteState.Massacre:
                return "ROUTE : ALL KILL";
            case StageRouteState.Mixed:
                return "ROUTE : NORMAL";
            default:
                return "ROUTE : UNKNOWN";
        }
    }

    private static Color RouteColor(StageRouteState route)
    {
        switch (route)
        {
            case StageRouteState.Pacifist:
                return new Color(0.82f, 0.93f, 1f, 0.92f);
            case StageRouteState.Massacre:
                return new Color(1f, 0.48f, 0.45f, 0.94f);
            case StageRouteState.Mixed:
                return new Color(0.92f, 0.92f, 0.86f, 0.9f);
            default:
                return DefaultBodyCountColor;
        }
    }
}
