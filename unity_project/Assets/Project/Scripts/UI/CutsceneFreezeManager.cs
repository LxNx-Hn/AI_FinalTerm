using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 컷씬/대사 중 Time.timeScale = 0 freeze 관리 (lock-count 방식).
/// Lock(): freeze 시작 (저장된 timeScale 보관)
/// Unlock(): freeze 해제 (저장된 timeScale 복원)
/// ForceUnlock(): 씬 로드 직전 완전 초기화
/// </summary>
public static class CutsceneFreezeManager
{
    private static int   _lockCount;
    private static float _savedTimeScale = 1f;

    public static bool IsFrozen => _lockCount > 0 || Mathf.Approximately(Time.timeScale, 0f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        _lockCount      = 0;
        _savedTimeScale = 1f;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene _, LoadSceneMode __)
    {
        _lockCount      = 0;
        _savedTimeScale = 1f;
        Time.timeScale  = 1f;
    }

    /// <summary>컷씬 시작 — timeScale 0으로 동결. 중첩 호출 안전.</summary>
    public static void Lock()
    {
        if (_lockCount == 0)
            _savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        _lockCount++;
        Time.timeScale = 0f;
    }

    /// <summary>컷씬 종료 — 저장된 timeScale 복원. 중첩 호출 안전.</summary>
    public static void Unlock()
    {
        _lockCount = Mathf.Max(0, _lockCount - 1);
        if (_lockCount == 0)
            Time.timeScale = _savedTimeScale;
    }

    /// <summary>씬 로드 직전 또는 긴급 복원 — lock count 무시하고 timeScale 1로 초기화.</summary>
    public static void ForceUnlock()
    {
        _lockCount      = 0;
        _savedTimeScale = 1f;
        Time.timeScale  = 1f;
    }
}
