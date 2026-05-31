using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 기본 전체화면으로 시작하며, F11로 창모드/전체화면을 토글합니다.
/// 창모드에서는 1280×720 고정 비율 창으로 전환됩니다.
/// </summary>
public class FullScreenEnforcer : MonoBehaviour
{
    private const int WindowedWidth  = 1280;
    private const int WindowedHeight = 720;

    private static FullScreenEnforcer instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        EnsureInstance();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        ApplyFullScreen();
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject("FullScreenEnforcer");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<FullScreenEnforcer>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
            ToggleFullScreen();
    }

    // ── 공개 API (외부에서도 호출 가능) ─────────────────────────────────────
    public static void ToggleFullScreen()
    {
        if (Screen.fullScreenMode == FullScreenMode.Windowed)
            ApplyFullScreen();
        else
            ApplyWindowed();
    }

    private static void ApplyFullScreen()
    {
        Resolution r = Screen.currentResolution;
        Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
    }

    private static void ApplyWindowed()
    {
        Screen.SetResolution(WindowedWidth, WindowedHeight, FullScreenMode.Windowed);
    }
}
