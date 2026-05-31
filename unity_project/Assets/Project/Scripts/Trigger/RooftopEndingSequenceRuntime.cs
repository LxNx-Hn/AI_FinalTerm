using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class RooftopEndingSequenceRuntime : MonoBehaviour
{
    // ── 체크포인트 구조 ───────────────────────────────────────────────────
    private struct RooftopCheckpoint
    {
        public float triggerX;
        public DialogueLine[] lines;
        public int memoryIndex;  // -1=없음, 0~4=memoryImages 인덱스
        public bool isFinal;
        public bool isMerge;     // true = RunFinalMergeSequence 사용
    }

    // ── 대사 데이터 ────────────────────────────────────────────────────────

    private static readonly DialogueLine[] RooftopStartLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "...여긴?"),
        new DialogueLine("???",    SpeakerType.Delusion, "끝."),
        new DialogueLine("채하민", SpeakerType.Normal,   "옥상?"),
        new DialogueLine("???",    SpeakerType.Delusion, "네가 정한 끝."),
        new DialogueLine("???",    SpeakerType.Delusion, "앞으로 가."),
    };

    private static readonly DialogueLine[] Memory01Lines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "아니야.\n저긴 저렇게 멀쩡하지 않았어."),
        new DialogueLine("???",    SpeakerType.Delusion, "그때도 멀쩡했어."),
        new DialogueLine("채하민", SpeakerType.Normal,   "닥쳐."),
    };

    private static readonly DialogueLine[] Memory02Lines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "병동은 무너져 있었어."),
        new DialogueLine("???",    SpeakerType.Delusion, "네가 무너진 쪽만 봤어."),
        new DialogueLine("채하민", SpeakerType.Normal,   "다들 변해 있었다고."),
        new DialogueLine("???",    SpeakerType.Delusion, "너를 부르고 있었어."),
    };

    private static readonly DialogueLine[] Memory03Lines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "달려들었어."),
        new DialogueLine("???",    SpeakerType.Delusion, "잡으려던 게 아니야."),
        new DialogueLine("채하민", SpeakerType.Normal,   "그럼 뭔데."),
        new DialogueLine("???",    SpeakerType.Delusion, "막으려던거지."),
    };

    private static readonly DialogueLine[] Memory04Lines =
    {
        new DialogueLine("??민",   SpeakerType.NameCrack1, "기억 안 나는 게 아니야."),
        new DialogueLine("채하민", SpeakerType.Normal,     "...방금 뭐라고 했어?"),
        new DialogueLine("??민",   SpeakerType.NameCrack1, "기억하지 않기로 한 거야."),
    };

    private static readonly DialogueLine[] Memory05Lines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,     "저 안에 있었어."),
        new DialogueLine("?하민",  SpeakerType.NameCrack2, "있었던 건 맞아."),
        new DialogueLine("채하민", SpeakerType.Normal,     "그게 날 막았어."),
        new DialogueLine("?하민",  SpeakerType.NameCrack2, "아니."),
        new DialogueLine("?하민",  SpeakerType.NameCrack2, "널 말리고 있었어."),
    };

    private static readonly DialogueLine[] FinalMergeNormalLines =
    {
        new DialogueLine("채하민", SpeakerType.MergedNormal,   "내가 본 것만...\n진짜였던 거야?"),
        new DialogueLine("채하민", SpeakerType.MergedDelusion, "아니.\n네가 버티려고 남긴 것만."),
        new DialogueLine("채하민", SpeakerType.MergedNormal,   "그럼 넌 뭐야."),
        new DialogueLine("채하민", SpeakerType.MergedDelusion, "네가 밀어낸 너."),
        new DialogueLine("채하민", SpeakerType.MergedNormal,   "...내가 나를?"),
        new DialogueLine("채하민", SpeakerType.MergedDelusion, "괴물로 만든 것도,\n목소리로 밀어낸 것도."),
        new DialogueLine("채하민", SpeakerType.MergedDelusion, "전부 너야."),
        new DialogueLine("채하민", SpeakerType.MergedNormal,   "..."),
        new DialogueLine("채하민", SpeakerType.MergedDelusion, "이제 봐."),
        new DialogueLine("채하민", SpeakerType.MergedNormal,   "내가 피했던 나를."),
    };

    private static readonly DialogueLine[] FinalMergePacifistLines =
    {
        new DialogueLine("채하민", SpeakerType.MergedNormal,   "내가 본 것만...\n진짜였던 거야?"),
        new DialogueLine("채하민", SpeakerType.MergedDelusion, "아니.\n네가 무서워서 남긴 것만."),
        new DialogueLine("채하민", SpeakerType.MergedNormal,   "그럼 난 왜 멈췄어."),
        new DialogueLine("채하민", SpeakerType.MergedDelusion, "아직 전부 믿지는 못했으니까."),
        new DialogueLine("채하민", SpeakerType.MergedNormal,   "그래도 죽이지 않았어."),
        new DialogueLine("채하민", SpeakerType.MergedDelusion, "그게 네가 처음으로 본 현실이야."),
        new DialogueLine("채하민", SpeakerType.MergedNormal,   "..."),
        new DialogueLine("채하민", SpeakerType.MergedDelusion, "이제 봐."),
        new DialogueLine("채하민", SpeakerType.MergedNormal,   "도망치지 않고 남은 나를."),
    };

    private static readonly DialogueLine[] FinalMergeMassacreLines =
    {
        new DialogueLine("채하민", SpeakerType.MergedNormal,   "다 끝냈어."),
        new DialogueLine("채하민", SpeakerType.MergedNormal,   "이제 아무도 안 와."),
        new DialogueLine("채하민", SpeakerType.MergedNormal,   "아무도 소리 지르지 않아."),
        new DialogueLine("채하민", SpeakerType.MergedNormal,   "아무도 나를 붙잡지 않아."),
        new DialogueLine("채하민", SpeakerType.MergedNormal,   "그럼 된 거잖아."),
        new DialogueLine("채하민", SpeakerType.MergedDelusion, "아니."),
        new DialogueLine("채하민", SpeakerType.MergedDelusion, "네가 멈춘 게 아니야."),
        new DialogueLine("채하민", SpeakerType.MergedDelusion, "남은 게 없어진 거야."),
        new DialogueLine("채하민", SpeakerType.MergedNormal,   "..."),
        new DialogueLine("채하민", SpeakerType.MergedNormal,   "그럼 아직 찾아야겠네."),
        new DialogueLine("채하민", SpeakerType.MergedNormal,   "분명,\n아직 남아있는 사람이 있을 거야."),
    };

    private static readonly DialogueLine[] LastNormalLines =
    {
        new DialogueLine("채하민", SpeakerType.MergedNormal, "누가 내 이름을 불렀다."),
        new DialogueLine("채하민", SpeakerType.MergedNormal, "이번에는,\n괴물의 목소리가 아니었다."),
        new DialogueLine("채하민", SpeakerType.MergedNormal, "나는 대답하지 못했다.\n\n하지만 이번에는,\n도망치지 않았다."),
    };

    private static readonly DialogueLine[] LastPacifistLines =
    {
        new DialogueLine("채하민", SpeakerType.MergedNormal, "누가 내 이름을 불렀다."),
        new DialogueLine("채하민", SpeakerType.MergedNormal, "이번에는,\n괴물의 목소리가 아니었다."),
        new DialogueLine("채하민", SpeakerType.MergedNormal, "나는 대답하지 못했다."),
        new DialogueLine("채하민", SpeakerType.MergedNormal, "대신,\n돌아섰다."),
        new DialogueLine("채하민", SpeakerType.MergedNormal, "아직 안에 있다."),
        new DialogueLine("채하민", SpeakerType.MergedNormal, "이번에는\n도망치지 않으려고."),
    };

    private static readonly DialogueLine[] LastMassacreLines =
    {
        new DialogueLine("채하민", SpeakerType.MergedNormal, "누가 내 이름을 불렀다."),
        new DialogueLine("채하민", SpeakerType.MergedNormal, "이번에도,\n사람 목소리처럼 들리지는 않았다."),
        new DialogueLine("채하민", SpeakerType.MergedNormal, "나는 대답하지 않았다."),
        new DialogueLine("채하민", SpeakerType.MergedNormal, "대신,\n다시 안으로 걸어갔다."),
        new DialogueLine("채하민", SpeakerType.MergedNormal, "아직 남아있는 사람이 있을 거야."),
        new DialogueLine("채하민", SpeakerType.MergedNormal, "아니."),
        new DialogueLine("채하민", SpeakerType.MergedNormal, "아직 남아있는 괴물이 있을 거야."),
    };

    // ── 체크포인트 배열 ────────────────────────────────────────────────────
    // 플레이어 시작 X ≈ -6.32. FloorMap 타일을 x=18까지 확장했으므로 더 넓게 분배.
    private static readonly RooftopCheckpoint[] Checkpoints =
    {
        new RooftopCheckpoint { triggerX = float.MinValue, lines = RooftopStartLines,  memoryIndex = -1, isFinal = false, isMerge = false },
        new RooftopCheckpoint { triggerX = -2.5f,          lines = Memory01Lines,       memoryIndex = 0,  isFinal = false, isMerge = false },
        new RooftopCheckpoint { triggerX =  1.5f,          lines = Memory02Lines,       memoryIndex = 1,  isFinal = false, isMerge = false },
        new RooftopCheckpoint { triggerX =  5.0f,          lines = Memory03Lines,       memoryIndex = 2,  isFinal = false, isMerge = false },
        new RooftopCheckpoint { triggerX =  8.5f,          lines = Memory04Lines,       memoryIndex = 3,  isFinal = false, isMerge = false },
        new RooftopCheckpoint { triggerX = 11.5f,          lines = Memory05Lines,       memoryIndex = 4,  isFinal = false, isMerge = false },
        new RooftopCheckpoint { triggerX = 14.0f,          lines = FinalMergeNormalLines, memoryIndex = -1, isFinal = false, isMerge = true  },
        new RooftopCheckpoint { triggerX = 17.5f,          lines = null,                memoryIndex = -1, isFinal = true,  isMerge = false },
    };

    // ── 씬 이름 ───────────────────────────────────────────────────────────
    private const string RooftopSceneName = "RooftopEndingWalk";
    private const string CreditsSceneName = "EndingCredits";

    // ── Inspector 필드 ────────────────────────────────────────────────────
    [SerializeField] private Sprite[] memoryImages;   // 5개: 1_Norm, 2_Norm, 2_1Norm, 2-2Norm, Boss_Norm
    [SerializeField] private Image    memoryImageUI;
    [SerializeField] private Image    darkScreenPanel;

    [Header("Ending Route Visuals")]
    [SerializeField] private Color massacreRooftopOverlayColor = new Color(0.65f, 0f, 0f, 0.18f);

    [Header("Rooftop Movement")]
    [SerializeField] private float rooftopLaneY = -2f;
    [SerializeField] private float rooftopMoveSpeed = 2.6f;
    [SerializeField] private float rooftopMinX = -6.32f;
    [SerializeField] private float rooftopMaxX = 18.0f;

    // ── 런타임 상태 ───────────────────────────────────────────────────────
    private ClickAdvanceCutsceneOverlay overlay;
    private GameObject   playerObject;
    private PlayerController playerController;
    private PlayerCombat     playerCombat;

    private PlayerVisualAnimator2D playerVisualAnimator;
    private SpriteRenderer[] playerRenderers;
    private Image routeTintOverlay;

    private int  nextCheckpointIndex;
    private bool sequenceBusy;
    private bool endingTriggered;
    private bool rooftopControlEnabled;
    private EndingRoute endingRoute = EndingRoute.Normal;
    private Vector2Int lastFacingDirection = Vector2Int.right;

    // ── 부트스트랩 ─────────────────────────────────────────────────────────
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
        if (scene.name != RooftopSceneName)
            return;

        EnsureInScene();
        BossBattleMusicRuntime.EnsureInScene()?.BeginRooftopMemory();
    }

    private static RooftopEndingSequenceRuntime EnsureInScene()
    {
        RooftopEndingSequenceRuntime existing = FindFirstObjectByType<RooftopEndingSequenceRuntime>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject("RooftopEndingSequenceRuntime");
        return go.AddComponent<RooftopEndingSequenceRuntime>();
    }

    // ── 초기화 ────────────────────────────────────────────────────────────
    private IEnumerator Start()
    {
        overlay = ClickAdvanceCutsceneOverlay.EnsureInScene();
        BossBattleMusicRuntime.EnsureInScene()?.BeginRooftopMemory();
        Time.timeScale = 0.3f;
        endingRoute = RunRouteTracker.GetEndingRoute();

        CachePlayer();
        PositionPlayerOnRooftopLane();
        DisableLegacyTriggers();
        DisableTilemapRenderers();

        if (playerCombat != null) playerCombat.enabled = false;


        // 옥상은 우측 방향 이동 — 초기 facing을 right으로 설정 (idleSide 애니메이션)
        playerController?.SetFacing(Vector2Int.right);

        HideHpUi();
        EnsureMemoryUi();
        ApplyRooftopRouteTint();

        // 씬 시작 대사 (즉시)
        yield return RunCheckpoint(Checkpoints[0]);
        nextCheckpointIndex = 1;
    }

    // ── 위치 기반 트리거 ──────────────────────────────────────────────────
    private void Update()
    {
        if (endingTriggered)
            return;

        if (playerObject == null)
        {
            CachePlayer();
            PositionPlayerOnRooftopLane();
        }

        if (playerObject == null)
            return;

        if (!sequenceBusy)
            HandleRooftopHorizontalMovement();

        if (sequenceBusy || nextCheckpointIndex >= Checkpoints.Length)
            return;

        RooftopCheckpoint checkpoint = Checkpoints[nextCheckpointIndex];
        if (playerObject.transform.position.x < checkpoint.triggerX)
            return;

        StartCoroutine(RunCheckpoint(checkpoint));
        nextCheckpointIndex++;
    }

    // ── 체크포인트 실행 ────────────────────────────────────────────────────
    private IEnumerator RunCheckpoint(RooftopCheckpoint checkpoint)
    {
        sequenceBusy = true;
        SetPlayerControlEnabled(false);

        if (checkpoint.isFinal)
        {
            yield return RunFinalEnding();
            yield break;
        }

        if (overlay == null)
            overlay = ClickAdvanceCutsceneOverlay.EnsureInScene();

        CutsceneFreezeManager.Lock();   // 대사/회상 중 완전 동결

        if (checkpoint.isMerge)
            yield return RunFinalMergeSequence(checkpoint.lines);
        else if (checkpoint.memoryIndex >= 0)
            yield return RunMemorySequence(checkpoint.memoryIndex, checkpoint.lines);
        else
        {
            yield return overlay.ShowLines(checkpoint.lines);
            overlay.HideDialogue();
        }

        CutsceneFreezeManager.Unlock(); // 0.3f 분위기 모드로 복원

        SetPlayerControlEnabled(true);
        sequenceBusy = false;
    }

    // ── 회상 연출 ─────────────────────────────────────────────────────────
    private IEnumerator RunMemorySequence(int imageIndex, DialogueLine[] lines)
    {
        // 하늘 영역을 살짝 어둡게 (0.25 — 검은박스 없이 은은하게)
        yield return FadeImage(darkScreenPanel, darkScreenPanel.color.a, 0.25f, 0.5f);

        // 회상 이미지 표시
        bool hasSprite = memoryImages != null
                         && imageIndex < memoryImages.Length
                         && memoryImages[imageIndex] != null;

        if (hasSprite && memoryImageUI != null)
        {
            memoryImageUI.sprite = memoryImages[imageIndex];
            memoryImageUI.gameObject.SetActive(true);

            // 흐릿 → 선명 (0.85f 유지 — 완전 불투명하면 팝업 카드처럼 보임)
            yield return FadeImage(memoryImageUI, 0f, 0.4f, 0.8f);
            yield return FadeImage(memoryImageUI, 0.4f, 0.85f, 0.7f);
        }

        // 대사
        if (overlay == null)
            overlay = ClickAdvanceCutsceneOverlay.EnsureInScene();

        if (lines != null)
            yield return overlay.ShowLines(lines);

        overlay.HideDialogue();

        // 이미지 페이드 아웃
        if (hasSprite && memoryImageUI != null)
        {
            yield return FadeImage(memoryImageUI, memoryImageUI.color.a, 0f, 1f);
            memoryImageUI.gameObject.SetActive(false);
        }

        // 화면 완전 복원 — 회상 후 하늘에 오버레이 없이 깨끗하게
        yield return FadeImage(darkScreenPanel, darkScreenPanel.color.a, 0f, 0.5f);
    }

    // ── 최종 통합 대사 ───────────────────────────────────────────────────
    private IEnumerator RunFinalMergeSequence(DialogueLine[] lines)
    {
        // 하늘 영역 살짝 어둡게 (최종 통합 몽타주)
        yield return FadeImage(darkScreenPanel, darkScreenPanel.color.a, 0.30f, 0.5f);

        bool hasImages = memoryImages != null && memoryImageUI != null;

        if (hasImages)
        {
            memoryImageUI.gameObject.SetActive(true);

            int[] montageOrder = { 0, 1, 2, 3, 4 };
            for (int i = 0; i < montageOrder.Length; i++)
            {
                int idx = montageOrder[i];
                if (idx < memoryImages.Length && memoryImages[idx] != null)
                {
                    memoryImageUI.sprite = memoryImages[idx];
                    bool isLast = (i == montageOrder.Length - 1);
                    bool isEmphasized = (idx == 2 || idx == 4);

                    float fadeIn = isLast ? 0.6f : 0.25f;
                    float hold = isEmphasized ? 0.45f : 0.2f;

                    yield return FadeImage(memoryImageUI, 0f, 1f, fadeIn);
                    yield return new WaitForSecondsRealtime(hold);

                    if (!isLast)
                        yield return FadeImage(memoryImageUI, 1f, 0f, 0.15f);
                }
            }

            Color baseColor = new Color(1f, 1f, 1f, 1f);
            Color glitchColor = new Color(1f, 0.25f, 0.25f, 1f);
            for (int g = 0; g < 4; g++)
            {
                float blend = 1f - g * 0.22f;
                memoryImageUI.color = Color.Lerp(glitchColor, baseColor, 1f - blend);
                yield return new WaitForSecondsRealtime(0.07f);
                memoryImageUI.color = baseColor;
                yield return new WaitForSecondsRealtime(0.05f);
            }
        }

        if (overlay == null)
            overlay = ClickAdvanceCutsceneOverlay.EnsureInScene();

        DialogueLine[] routeLines = GetFinalMergeLinesForRoute();
        if (routeLines != null)
            yield return overlay.ShowLines(routeLines);
        overlay.HideDialogue();

        if (hasImages && memoryImageUI != null)
        {
            yield return FadeImage(memoryImageUI, memoryImageUI.color.a, 0f, 0.8f);
            memoryImageUI.gameObject.SetActive(false);
        }

        // 화면 완전 복원
        yield return FadeImage(darkScreenPanel, darkScreenPanel.color.a, 0f, 0.5f);
    }

    // ── 엔딩 ──────────────────────────────────────────────────────────────
    private IEnumerator RunFinalEnding()
    {
        endingTriggered = true;
        SetPlayerControlEnabled(false);

        // 마지막 대사를 위해 Lock (0.3f → 0)
        CutsceneFreezeManager.Lock();

        // 회상 이미지 정리
        if (memoryImageUI != null && memoryImageUI.gameObject.activeSelf)
        {
            yield return FadeImage(memoryImageUI, memoryImageUI.color.a, 0f, 0.3f);
            memoryImageUI.gameObject.SetActive(false);
        }

        // 마지막 대사
        if (overlay == null)
            overlay = ClickAdvanceCutsceneOverlay.EnsureInScene();

        yield return overlay.ShowLines(GetLastLinesForRoute());
        overlay.HideDialogue();

        CutsceneFreezeManager.Unlock(); // 0.3f 복원

        if (endingRoute == EndingRoute.Pacifist || endingRoute == EndingRoute.Massacre)
        {
            yield return RunReturnToHospitalEnding();
            yield break;
        }

        // 글리치 효과 (renderer.enabled 토글 3회)
        if (playerRenderers != null)
        {
            for (int i = 0; i < 3; i++)
            {
                SetRenderersEnabled(false);
                yield return new WaitForSecondsRealtime(0.08f);
                SetRenderersEnabled(true);
                yield return new WaitForSecondsRealtime(0.12f);
            }
        }

        // 이동 중 동시에 페이드 아웃 (2.0s)
        playerController?.SetFacing(Vector2Int.right);
        playerVisualAnimator?.SetForcedWalk(Vector2Int.right, true);
        StartCoroutine(WalkPlayerHorizontally(Vector2Int.right, 1.8f, 2.0f));
        yield return FadePlayerRenderers(1f, 0f, 2.0f);

        yield return new WaitForSecondsRealtime(0.4f);
        yield return overlay.FadeToBlack(1f);

        CutsceneFreezeManager.ForceUnlock(); // 씬 로드 전 완전 초기화
        SceneManager.LoadScene(CreditsSceneName);
    }

    private IEnumerator RunReturnToHospitalEnding()
    {
        playerController?.SetFacing(Vector2Int.left);
        playerVisualAnimator?.SetForcedWalk(Vector2Int.left, true);
        yield return WalkPlayerHorizontally(Vector2Int.left, 1.8f, 2.0f);
        playerVisualAnimator?.SetForcedWalk(Vector2Int.left, false);

        yield return new WaitForSecondsRealtime(0.25f);
        yield return overlay.FadeToBlack(1f);

        CutsceneFreezeManager.ForceUnlock();
        SceneManager.LoadScene(CreditsSceneName);
    }

    private DialogueLine[] GetFinalMergeLinesForRoute()
    {
        switch (endingRoute)
        {
            case EndingRoute.Pacifist:
                return FinalMergePacifistLines;
            case EndingRoute.Massacre:
                return FinalMergeMassacreLines;
            default:
                return FinalMergeNormalLines;
        }
    }

    private DialogueLine[] GetLastLinesForRoute()
    {
        switch (endingRoute)
        {
            case EndingRoute.Pacifist:
                return LastPacifistLines;
            case EndingRoute.Massacre:
                return LastMassacreLines;
            default:
                return LastNormalLines;
        }
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────
    private IEnumerator FadeImage(Image img, float from, float to, float duration)
    {
        if (img == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            Color c = img.color;
            c.a = Mathf.Lerp(from, to, t);
            img.color = c;
            yield return null;
        }
        Color fc = img.color;
        fc.a = to;
        img.color = fc;
    }

    private IEnumerator FadePlayerRenderers(float from, float to, float duration)
    {
        if (playerRenderers == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(from, to, t);
            foreach (SpriteRenderer sr in playerRenderers)
            {
                // 루트 SpriteRenderer는 PlayerVisualAnimator2D가 비활성화한 상태.
                // 건드리면 정면 스프라이트가 노출되므로 건너뜀.
                if (sr == null || sr.gameObject == playerObject) continue;
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
            yield return null;
        }
    }

    private void SetRenderersEnabled(bool enabled)
    {
        if (playerRenderers == null) return;
        foreach (SpriteRenderer sr in playerRenderers)
        {
            // 루트 SpriteRenderer는 PlayerVisualAnimator2D가 비활성화한 상태.
            // 글리치 토글 시 재활성화하면 정면 스프라이트가 겹쳐 보이므로 건너뜀.
            if (sr == null || sr.gameObject == playerObject) continue;
            sr.enabled = enabled;
        }
    }

    private void EnsureMemoryUi()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cgo = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;
        }

        if (darkScreenPanel == null)
        {
            GameObject darkGo = new GameObject("RooftopDarkOverlay");
            darkGo.transform.SetParent(canvas.transform, false);
            darkGo.AddComponent<RectTransform>();
            darkScreenPanel = darkGo.AddComponent<Image>();
            darkScreenPanel.raycastTarget = false;
        }
        // 항상 앵커 업데이트 — 이미지 영역 배경만 살짝 어둡게 (난간 위만)
        // anchorMin.y ≈ 0.46 → 난간 상단에 검은박스 경계를 맞춤
        darkScreenPanel.rectTransform.anchorMin = new Vector2(0f, 0.42f);
        darkScreenPanel.rectTransform.anchorMax = new Vector2(1f, 1f);
        darkScreenPanel.rectTransform.offsetMin = Vector2.zero;
        darkScreenPanel.rectTransform.offsetMax = Vector2.zero;
        // 시작 시 투명 — 회상 연출 중에만 어둡게, 평소엔 오버레이 없음
        darkScreenPanel.color = new Color(0f, 0f, 0f, 0f);

        if (memoryImageUI == null)
        {
            GameObject memGo = new GameObject("RooftopMemoryImage");
            memGo.transform.SetParent(canvas.transform, false);
            memGo.AddComponent<RectTransform>();
            memoryImageUI = memGo.AddComponent<Image>();
            memoryImageUI.preserveAspect = true;
            memoryImageUI.raycastTarget  = false;
            Color mc = memoryImageUI.color;
            mc.a = 0f;
            memoryImageUI.color = mc;
            memGo.SetActive(false);
        }

        // 소프트 엣지 Material 적용 — Resources/MemoryImageFade.mat
        if (memoryImageUI.material == null || memoryImageUI.material.name == "Default UI Material")
        {
            Material fadeMat = Resources.Load<Material>("MemoryImageFade");
            if (fadeMat != null)
                memoryImageUI.material = fadeMat;
        }

        // 항상 앵커 업데이트 — 이미지 하단이 난간 상단에 딱 걸치도록 (anchorMin.y ≈ 0.46)
        memoryImageUI.rectTransform.anchorMin = new Vector2(0.05f, 0.42f);
        memoryImageUI.rectTransform.anchorMax = new Vector2(0.95f, 0.96f);
        memoryImageUI.rectTransform.offsetMin = Vector2.zero;
        memoryImageUI.rectTransform.offsetMax = Vector2.zero;

        EnsureRouteTintOverlay(canvas);
        RestoreDialogueUiOrder(canvas.transform);
    }

    private void EnsureRouteTintOverlay(Canvas canvas)
    {
        if (routeTintOverlay == null)
        {
            GameObject tintGo = new GameObject("RooftopRouteTintOverlay");
            tintGo.transform.SetParent(canvas.transform, false);
            tintGo.AddComponent<RectTransform>();
            routeTintOverlay = tintGo.AddComponent<Image>();
            routeTintOverlay.raycastTarget = false;
        }

        RectTransform rect = routeTintOverlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void ApplyRooftopRouteTint()
    {
        if (routeTintOverlay == null)
            return;

        bool massacre = endingRoute == EndingRoute.Massacre;
        routeTintOverlay.gameObject.SetActive(massacre);
        routeTintOverlay.color = massacre ? massacreRooftopOverlayColor : Color.clear;
    }

    private static void RestoreDialogueUiOrder(Transform canvasTransform)
    {
        Transform panel = canvasTransform.Find("CutscenePanel");
        Transform fade = canvasTransform.Find("FadePanel");

        if (panel != null)
            panel.SetAsLastSibling();

        if (fade != null)
            fade.SetAsLastSibling();
    }

    private static void HideHpUi()
    {
        HpTextUI hpUi = FindFirstObjectByType<HpTextUI>();
        if (hpUi != null) hpUi.gameObject.SetActive(false);
    }

    private void DisableLegacyTriggers()
    {
        GridZoneTriggerBase[] triggers = FindObjectsByType<GridZoneTriggerBase>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GridZoneTriggerBase trigger in triggers)
        {
            if (trigger != null)
                trigger.enabled = false;
        }
    }

    private static void DisableTilemapRenderers()
    {
        // 타일맵 시각 요소 비활성화 (Collider는 유지)
        TilemapRenderer[] renderers = FindObjectsByType<TilemapRenderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TilemapRenderer tr in renderers)
        {
            if (tr != null)
                tr.enabled = false;
        }
    }

    private void CachePlayer()
    {
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            PlayerController fallback = FindFirstObjectByType<PlayerController>();
            if (fallback != null) playerObject = fallback.gameObject;
        }
        if (playerObject == null)
        {
            GridOccupant fallback = FindFirstObjectByType<GridOccupant>();
            if (fallback != null) playerObject = fallback.gameObject;
        }
        if (playerObject == null) return;

        playerController = playerObject.GetComponent<PlayerController>();
        playerCombat     = playerObject.GetComponent<PlayerCombat>();

        playerVisualAnimator = GetActivePlayerVisualAnimator(playerObject);
        playerRenderers  = playerObject.GetComponentsInChildren<SpriteRenderer>(true);
    }

    private static PlayerVisualAnimator2D GetActivePlayerVisualAnimator(GameObject player)
    {
        if (player == null)
            return null;

        PlayerVisualAnimator2D[] animators = player.GetComponents<PlayerVisualAnimator2D>();
        for (int i = animators.Length - 1; i >= 0; i--)
        {
            PlayerVisualAnimator2D animator = animators[i];
            if (animator != null && animator.isActiveAndEnabled)
                return animator;
        }

        for (int i = animators.Length - 1; i >= 0; i--)
        {
            PlayerVisualAnimator2D animator = animators[i];
            if (animator != null && animator.enabled)
                return animator;
        }

        return animators.Length > 0 ? animators[animators.Length - 1] : null;
    }

    private void SetPlayerControlEnabled(bool enabled)
    {
        rooftopControlEnabled = enabled;
        // 옥상은 좌우 레인 전용 이동만 허용한다. 일반 격자 입력은 Y축/패널 영역으로 빠질 수 있다.
        if (playerController != null) playerController.enabled = false;
        if (!enabled)
        {
            // 대사 중: 걷기 애니메이션 중단 (캐릭터 정지)
            playerVisualAnimator?.SetForcedWalk(Vector2Int.right, false);
        }
        else
        {
            // 조작 재개: 즉시 걷기 애니메이션 시작 (키 입력 전에도 이동 모션 표시)
            playerController?.SetFacing(Vector2Int.right);
            playerVisualAnimator?.SetForcedWalk(Vector2Int.right, true);
        }
    }

    private void PositionPlayerOnRooftopLane()
    {
        if (playerObject == null)
            return;

        Vector3 pos = playerObject.transform.position;
        pos.x = Mathf.Clamp(pos.x, rooftopMinX, rooftopMaxX);
        pos.y = rooftopLaneY;
        playerObject.transform.position = pos;
    }

    private void HandleRooftopHorizontalMovement()
    {
        if (!rooftopControlEnabled || playerObject == null)
            return;

        int direction = 0;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            direction--;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            direction++;

        if (direction < 0)
        {
            lastFacingDirection = Vector2Int.left;
            playerController?.SetFacing(Vector2Int.left);
            playerVisualAnimator?.SetForcedWalk(Vector2Int.left, true);
        }
        else if (direction > 0)
        {
            lastFacingDirection = Vector2Int.right;
            playerController?.SetFacing(Vector2Int.right);
            playerVisualAnimator?.SetForcedWalk(Vector2Int.right, true);
        }
        else
        {
            playerVisualAnimator?.SetForcedWalk(lastFacingDirection, false);
        }

        Vector3 pos = playerObject.transform.position;
        pos.y = rooftopLaneY;
        if (direction != 0)
            pos.x = Mathf.Clamp(pos.x + direction * rooftopMoveSpeed * Time.unscaledDeltaTime, rooftopMinX, rooftopMaxX);

        playerObject.transform.position = pos;
    }

    private IEnumerator WalkPlayerHorizontally(Vector2Int direction, float distance, float duration)
    {
        if (playerObject == null)
            yield break;

        if (direction.x < 0)
            playerController?.SetFacing(Vector2Int.left);
        else if (direction.x > 0)
            playerController?.SetFacing(Vector2Int.right);
        playerVisualAnimator?.SetForcedWalk(direction.x < 0 ? Vector2Int.left : Vector2Int.right, true);

        Vector3 start = playerObject.transform.position;
        start.y = rooftopLaneY;
        Vector3 end = start + new Vector3(direction.x * distance, 0f, 0f);
        end.x = Mathf.Clamp(end.x, rooftopMinX, rooftopMaxX + 1.8f);
        end.y = rooftopLaneY;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (direction.x > 0)
            {
                playerController?.SetFacing(Vector2Int.right);
                playerVisualAnimator?.SetForcedWalk(Vector2Int.right, true);
            }
            else if (direction.x < 0)
            {
                playerController?.SetFacing(Vector2Int.left);
                playerVisualAnimator?.SetForcedWalk(Vector2Int.left, true);
            }

            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            playerObject.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        playerObject.transform.position = end;
    }
}
