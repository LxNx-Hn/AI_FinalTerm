using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Entry 씬 우하단에 완전 투명한 개발자 디버그 진입 버튼을 생성합니다.
/// 클릭하면 DevDebug 씬으로 이동합니다.
/// </summary>
public class DevDebugEntryButton : MonoBehaviour
{
    private const string EntrySceneName = "Entry";
    private const string DebugSceneName = "DevDebug";
    private const string RuntimeObjectName = "__DevDebugEntryBtn";
    private const string ButtonObjectName = "__DevDebugBtn";

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
        if (scene.name != EntrySceneName) return;
        if (FindFirstObjectByType<DevDebugEntryButton>() != null) return;
        new GameObject(RuntimeObjectName).AddComponent<DevDebugEntryButton>();
    }

    // ── 초기화 ──────────────────────────────────────────────────────────────
    private void Start()
    {
        DevDebugEntryButton[] buttons = FindObjectsByType<DevDebugEntryButton>(FindObjectsSortMode.None);
        if (buttons.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        // 기존 캔버스에 붙임 — 없으면 새로 만들기
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        EnsureEventSystem();
        if (GameObject.Find(ButtonObjectName) != null)
            return;

        CreateButton(canvas.transform);
    }

    // ── 버튼 생성 ────────────────────────────────────────────────────────────
    private static void CreateButton(Transform canvasT)
    {
        var go = new GameObject(ButtonObjectName,
            typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(canvasT, false);
        go.transform.SetAsLastSibling();

        // 우하단, 60×40 px
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(1f, 0f);
        rt.sizeDelta        = new Vector2(60f, 40f);
        rt.anchoredPosition = new Vector2(-8f, 8f);

        // 완전 투명 이미지 (레이캐스트는 유지)
        var img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = true;

        // 버튼 색상도 전부 투명
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.normalColor      = new Color(0f, 0f, 0f, 0f);
        cb.highlightedColor = new Color(0f, 0f, 0f, 0f);
        cb.pressedColor     = new Color(0f, 0f, 0f, 0f);
        cb.selectedColor    = new Color(0f, 0f, 0f, 0f);
        cb.fadeDuration     = 0f;
        btn.colors = cb;

        btn.onClick.AddListener(() => SceneManager.LoadScene(DebugSceneName));
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
