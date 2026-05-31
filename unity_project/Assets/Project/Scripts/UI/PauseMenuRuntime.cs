using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuRuntime : MonoBehaviour
{
    private const string PauseMenuName = "PauseMenuRuntime";
    private const string TitleSceneName = "Entry";

    private static readonly HashSet<string> SupportedScenes = new HashSet<string>
    {
        "Stage01_ParkingToHospital",
        "Hospital",
        "Boss01_Elevator",
        "RooftopEndingWalk"
    };

    private readonly List<BehaviourState> disabledBehaviours = new List<BehaviourState>(8);

    private Canvas canvas;
    private GameObject panel;
    private bool isShowing;

    private readonly struct BehaviourState
    {
        public readonly Behaviour Behaviour;
        public readonly bool WasEnabled;

        public BehaviourState(Behaviour behaviour)
        {
            Behaviour = behaviour;
            WasEnabled = behaviour != null && behaviour.enabled;
        }
    }

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
        if (!SupportedScenes.Contains(scene.name))
            return;

        EnsureInScene();
    }

    private static PauseMenuRuntime EnsureInScene()
    {
        PauseMenuRuntime existing = FindFirstObjectByType<PauseMenuRuntime>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject(PauseMenuName);
        return go.AddComponent<PauseMenuRuntime>();
    }

    private void Awake()
    {
        BuildUi();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (isShowing)
        {
            HideMenu();
            return;
        }

        if (CutsceneFreezeManager.IsFrozen || IsGameOverShowing())
            return;

        ShowMenu();
    }

    private void OnDestroy()
    {
        if (isShowing)
            HideMenu();
    }

    private void ShowMenu()
    {
        if (panel == null)
            BuildUi();

        CutsceneFreezeManager.Lock();
        DisablePlayerControls();
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        isShowing = true;
    }

    private void HideMenu()
    {
        if (!isShowing)
            return;

        RestorePlayerControls();
        CutsceneFreezeManager.Unlock();

        if (panel != null)
            panel.SetActive(false);

        isShowing = false;
    }

    private void RetryScene()
    {
        CutsceneFreezeManager.ForceUnlock();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ReturnToTitle()
    {
        CutsceneFreezeManager.ForceUnlock();
        SceneManager.LoadScene(TitleSceneName);
    }

    private void BuildUi()
    {
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("PauseMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        EnsureEventSystem();

        Font font = Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "맑은 고딕", "Arial Unicode MS", "Noto Sans CJK KR" },
            28
        );
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        panel = new GameObject("PauseMenuPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.32f, 0.28f);
        panelRect.anchorMax = new Vector2(0.68f, 0.72f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);

        AddText(panel.transform, "일시정지", font, 32, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.92f));
        AddButton(panel.transform, "리트라이", font, 28, new Vector2(0.12f, 0.42f), new Vector2(0.88f, 0.60f), RetryScene);
        AddButton(panel.transform, "타이틀", font, 28, new Vector2(0.12f, 0.18f), new Vector2(0.88f, 0.36f), ReturnToTitle);

        panel.SetActive(false);
    }

    private static void AddText(Transform parent, string text, Font font, int fontSize, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(8f, 0f);
        rect.offsetMax = new Vector2(-8f, 0f);

        Text label = go.GetComponent<Text>();
        label.text = text;
        label.font = font;
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private static void AddButton(Transform parent, string text, Font font, int fontSize, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction action)
    {
        GameObject go = new GameObject(text + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(4f, 4f);
        rect.offsetMax = new Vector2(-4f, -4f);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.16f, 0.16f, 0.16f, 0.94f);

        AddText(go.transform, text, font, fontSize, Vector2.zero, Vector2.one);

        Button button = go.GetComponent<Button>();
        button.onClick.AddListener(action);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static bool IsGameOverShowing()
    {
        GameOverController gameOver = FindFirstObjectByType<GameOverController>();
        return gameOver != null && gameOver.IsShowing;
    }

    private void DisablePlayerControls()
    {
        RestorePlayerControls();

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null)
            return;

        AddDisabledBehaviour(playerHealth.GetComponent<PlayerController>());
        AddDisabledBehaviour(playerHealth.GetComponent<PlayerCombat>());

        AddDisabledBehaviour(playerHealth.GetComponent<OneStepOnDirectionKey>());
        AddDisabledBehaviour(playerHealth.GetComponent<HospitalPriorityMoveController>());
    }

    private void RestorePlayerControls()
    {
        for (int i = 0; i < disabledBehaviours.Count; i++)
        {
            BehaviourState state = disabledBehaviours[i];
            if (state.Behaviour != null)
                state.Behaviour.enabled = state.WasEnabled;
        }

        disabledBehaviours.Clear();
    }

    private void AddDisabledBehaviour(Behaviour behaviour)
    {
        if (behaviour == null)
            return;

        disabledBehaviours.Add(new BehaviourState(behaviour));
        behaviour.enabled = false;
    }
}
