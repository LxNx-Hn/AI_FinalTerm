using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 불살 루트 전용 타이머 UI.
///
/// [진입타이머 구간]
///   · 보스 체력바(BossHPPanel) 바로 아래에, 같은 가로 길이·1/4 세로로 파란 선 표시.
///   · forcePhase3AfterSeconds 동안 오른쪽부터 줄어들며 남은 시간 표시.
///
/// [3페 유지 구간]
///   · 파란 선이 페이드아웃으로 사라짐.
///   · 보스 체력바 위에 검은 오버레이가 왼쪽부터 차오름(점점 쌓이는 구조).
///   · pacifistResolveAfterForcedPhase3Seconds 동안 꽉 채워진 후 최종 패턴 발동.
/// </summary>
public class PacifistTimerUI : MonoBehaviour
{
    private const string BossSceneName          = "Boss01_Elevator";
    private const float  BlueBarFadeOutDuration = 0.45f;

    private ElevatorBossController boss;
    private RectTransform          cachedBossHpRect; // 비활성 상태에서도 캐시
    private Image blueBar;                           // 타이머 파란 선 (BossHPPanel 자식)
    private Image blackOverlay;                      // 검은 오버레이 (BossHPPanel 자식)

    private bool initialized;

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
        if (scene.name != BossSceneName) return;

        // 불살 루트(Stage1+Stage2 모두 Pacifist) 또는 직접 진입(Unknown)일 때만 생성
        bool isPacifist =
            (RunRouteTracker.Stage1Route == StageRouteState.Pacifist
             && RunRouteTracker.Stage2Route == StageRouteState.Pacifist)
            || (RunRouteTracker.Stage1Route == StageRouteState.Unknown
                && RunRouteTracker.Stage2Route == StageRouteState.Unknown);

        if (!isPacifist) return;

        new GameObject("PacifistTimerUI").AddComponent<PacifistTimerUI>();
    }

    // ── 라이프사이클 ─────────────────────────────────────────────────────────
    private void Update()
    {
        if (!initialized)
        {
            TryInitialize();
            return;
        }

        if (boss == null || !boss.IsPacifistTimerEnabled)
        {
            HideAll();
            return;
        }

        if (boss.IsPhase3ForcedByPacifistTimer)
        {
            TickBlueBarFadeOut();
            TickBlackOverlayFill();
        }
        else if (boss.IsBattleTimerRunning)
        {
            TickBlueBarDrain();
            ResetBlackOverlay();
        }
        else
        {
            // 전투 타이머 꺼짐 (인트로 중, 보스 사망 후 등)
            HideAll();
        }
    }

    // ── 초기화 ──────────────────────────────────────────────────────────────
    private void TryInitialize()
    {
        if (boss == null)
            boss = FindFirstObjectByType<ElevatorBossController>();
        if (boss == null || !boss.IsPacifistTimerEnabled) return;

        // ① 패널을 비활성화 상태에서도 찾아 캐시한다.
        //    ElevatorBossController.Start() → IntroRoutine이 첫 Update() 이전에
        //    SetBossHudVisible(false)를 호출하므로 GameObject.Find()로는 찾을 수 없다.
        if (cachedBossHpRect == null)
            cachedBossHpRect = FindBossHpPanel();
        if (cachedBossHpRect == null) return;

        // ② 바 생성은 패널이 실제로 화면에 표시될 때(= 배틀 시작)까지 미룬다.
        //    BossHPPanel은 stretch anchor라 비활성 상태에서 rect.height = 0이므로,
        //    활성화 후에야 올바른 높이를 구할 수 있다.
        if (!cachedBossHpRect.gameObject.activeInHierarchy) return;

        CreateBlueBar(cachedBossHpRect);
        CreateBlackOverlay(cachedBossHpRect);
        initialized = true;
    }

    /// <summary>
    /// BossHPPanel의 RectTransform을 찾는다. 비활성화 오브젝트도 포함.
    /// 이름 직접 검색 → BossHpBar 컴포넌트로 부모 탐색 순으로 시도.
    /// </summary>
    private static RectTransform FindBossHpPanel()
    {
        // 1차: 이름으로 직접 검색 — FindObjectsByType으로 비활성 포함
        Transform[] all = FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform t in all)
        {
            if (t.name == "BossHPPanel")
            {
                RectTransform r = t.GetComponent<RectTransform>();
                if (r != null) return r;
            }
        }

        // 2차: BossHpBar 컴포넌트 기준으로 캔버스 직전 부모를 탐색
        BossHpBar[] hpBars = FindObjectsByType<BossHpBar>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (hpBars != null && hpBars.Length > 0 && hpBars[0] != null)
        {
            Transform tr = hpBars[0].transform;
            while (tr.parent != null && tr.parent.GetComponent<Canvas>() == null)
                tr = tr.parent;
            RectTransform r = tr.GetComponent<RectTransform>();
            if (r != null) return r;
        }

        return null;
    }

    // ── UI 생성 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 보스 체력바 바로 아래에 파란 타이머 바를 생성한다.
    /// · 가로: BossHPPanel과 동일 (anchorMin/Max X = 0..1)
    /// · 세로: BossHPPanel 높이의 1/4
    /// · 자식 anchor: 부모 하단 기준, 아래로 늘어남
    /// </summary>
    private void CreateBlueBar(RectTransform bossHpRect)
    {
        float parentH = bossHpRect.rect.height;
        float barH    = Mathf.Max(8f, parentH * 0.25f);

        GameObject go = new GameObject("PacifistTimerBar",
            typeof(RectTransform), typeof(Image));
        go.transform.SetParent(bossHpRect, false);   // BossHPPanel 자식

        RectTransform rect = go.GetComponent<RectTransform>();
        // 부모 하단 전체 너비에 anchor, 아래 방향으로 barH 만큼 늘어남
        rect.anchorMin        = new Vector2(0f, 0f);
        rect.anchorMax        = new Vector2(1f, 0f);
        rect.pivot            = new Vector2(0.5f, 1f);   // 피벗: 상단 중앙
        rect.anchoredPosition = Vector2.zero;            // 피벗(상단)을 부모 하단에 맞춤
        rect.sizeDelta        = new Vector2(0f, barH);   // 너비=부모 100%, 높이=barH

        Image img         = go.GetComponent<Image>();
        img.color         = new Color(0.25f, 0.65f, 1f, 0.92f);
        img.raycastTarget = false;
        img.type          = Image.Type.Filled;
        img.fillMethod    = Image.FillMethod.Horizontal;
        img.fillOrigin    = (int)Image.OriginHorizontal.Left; // fillAmount 1→0 시 오른쪽이 먼저 사라짐
        img.fillAmount    = 1f;

        blueBar = img;
    }

    /// <summary>
    /// 보스 체력바 위에 씌우는 검은 오버레이를 생성한다.
    /// BossHPPanel 전체를 덮는 자식으로 추가, 왼쪽부터 채워지며 점점 쌓임.
    /// </summary>
    private void CreateBlackOverlay(RectTransform bossHpRect)
    {
        GameObject go = new GameObject("PacifistBlackOverlay",
            typeof(RectTransform), typeof(Image));
        go.transform.SetParent(bossHpRect, false);   // BossHPPanel 자식

        RectTransform rect = go.GetComponent<RectTransform>();
        // 부모 전체를 꽉 채움
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img         = go.GetComponent<Image>();
        img.color         = new Color(0f, 0f, 0f, 0f);  // 시작 완전 투명
        img.raycastTarget = false;
        img.type          = Image.Type.Filled;
        img.fillMethod    = Image.FillMethod.Horizontal;
        img.fillOrigin    = (int)Image.OriginHorizontal.Left; // 왼쪽부터 차오름
        img.fillAmount    = 0f;

        // 체력바 위 최상단에 렌더링
        go.transform.SetAsLastSibling();

        blackOverlay = img;
    }

    // ── 프레임 업데이트 ──────────────────────────────────────────────────────
    private void TickBlueBarDrain()
    {
        if (blueBar == null) return;

        // 알파 복원
        Color c = blueBar.color;
        c.a = 0.92f;
        blueBar.color = c;

        float total   = boss.ForcePhase3AfterSeconds;
        float elapsed = boss.BossBattleElapsed;
        // fillOrigin=Left: fillAmount 1→0 으로 감소 시 오른쪽부터 줄어듦
        blueBar.fillAmount = total > 0f ? 1f - Mathf.Clamp01(elapsed / total) : 0f;
    }

    private void TickBlueBarFadeOut()
    {
        if (blueBar == null) return;

        Color c = blueBar.color;
        if (c.a > 0f)
        {
            c.a = Mathf.MoveTowards(c.a, 0f, Time.deltaTime / BlueBarFadeOutDuration);
            blueBar.color = c;
        }
        blueBar.fillAmount = 0f;
    }

    private void TickBlackOverlayFill()
    {
        if (blackOverlay == null) return;
        if (boss.IsPacifistResolveStarted)
        {
            // 최종 패턴 발동됐으면 꽉 채운 채 유지
            blackOverlay.fillAmount = 1f;
            Color fc = blackOverlay.color;
            fc.a = 0.92f;
            blackOverlay.color = fc;
            return;
        }

        float total   = boss.PacifistResolveAfterForcedPhase3Seconds;
        float elapsed = boss.ForcedPhase3Elapsed;
        float t       = total > 0f ? Mathf.Clamp01(elapsed / total) : 1f;

        blackOverlay.fillAmount = t;
        Color c = blackOverlay.color;
        c.a = Mathf.Lerp(0.70f, 0.92f, t);   // 채워질수록 진해짐
        blackOverlay.color = c;
    }

    private void ResetBlackOverlay()
    {
        if (blackOverlay == null) return;
        blackOverlay.fillAmount = 0f;
        Color c = blackOverlay.color;
        c.a = 0f;
        blackOverlay.color = c;
    }

    private void HideAll()
    {
        if (blueBar != null)
        {
            Color c = blueBar.color; c.a = 0f; blueBar.color = c;
        }
        if (blackOverlay != null)
        {
            Color c = blackOverlay.color; c.a = 0f; blackOverlay.color = c;
        }
    }
}
