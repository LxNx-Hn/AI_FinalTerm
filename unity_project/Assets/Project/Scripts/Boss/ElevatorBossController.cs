using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Elevator Boss Controller
/// 기준: 사용자가 제공한 vFinal-Lock-4 명세.
///
/// 핵심 규칙:
/// - 딜타임 없음.
/// - 취약 시간 없음.
/// - 3대 제한 없음.
/// - BossHealth는 전투 중 항상 피격 가능 상태.
/// - PlayerCombat이 읽는 BossCell은 GridManager 기준 월드 셀.
/// - 돌진 계열 패턴은 실타격 발생과 동시에 보스가 빠르게 지나가는 시각 효과를 실행.
/// </summary>
public class ElevatorBossController : MonoBehaviour
{
    private const string BossHitSpriteResourcePath = "Boss/BOSSASSET/BOSS/Sprite/UNUSE/banshee - hit";
    private const string IntroNoiseResourcePath = "Boss/BGM/isee";
    private const string DashVoiceResourcesPath = "Boss/BOSSASSET/BOSS/SFX/DASH";
    private const string DashSmokePrefabAssetPath = "Assets/Mirza Beig/Cinematic Explosions FREE/Prefabs/Smoke/Smoke 1.prefab";
    private const string BossHitPrefabAssetPath = "Assets/VFXPACK_IMPACT_WALLCOEUR_FreeVersion/00_Prefab/0_Classic/VFX_Blood_02.prefab";

    private static readonly DialogueLine[] DefaultIntroDialogueLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "뭐지? 이 소리는?"),
        new DialogueLine("채하민", SpeakerType.Normal,   "앞쪽인가?"),
        new DialogueLine("???",    SpeakerType.Delusion, "보지 마."),
        new DialogueLine("채하민", SpeakerType.Normal,   "...뭐야, 저건."),
    };

    private static readonly DialogueLine[] PacifistIntroDialogueLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "뭐지? 이 소리는?"),
        new DialogueLine("채하민", SpeakerType.Normal,   "앞쪽인가?"),
        new DialogueLine("???",    SpeakerType.Delusion, "보지 마."),
        new DialogueLine("채하민", SpeakerType.Normal,   "..."),
        new DialogueLine("채하민", SpeakerType.Normal,   "정말 괴물인 거 맞아?"),
        new DialogueLine("???",    SpeakerType.Delusion, "보지 말라고 했잖아."),
        new DialogueLine("채하민", SpeakerType.Normal,   "그럼 봐야겠네."),
    };

    private static readonly DialogueLine[] MassacreIntroDialogueLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "뭐지? 이 소리는?"),
        new DialogueLine("???",    SpeakerType.Delusion, "마지막이야."),
        new DialogueLine("채하민", SpeakerType.Normal,   "앞쪽인가?"),
        new DialogueLine("???",    SpeakerType.Delusion, "이번엔 놓치지 마."),
        new DialogueLine("채하민", SpeakerType.Normal,   "..."),
        new DialogueLine("채하민", SpeakerType.Normal,   "끝내면 돼."),
        new DialogueLine("???",    SpeakerType.Delusion, "그래."),
    };

    private static readonly DialogueLine[] DefaultPostBossDialogueLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "끝났어."),
        new DialogueLine("???",    SpeakerType.Delusion, "끝난 게 아니야."),
        new DialogueLine("채하민", SpeakerType.Normal,   "...또 너야?"),
        new DialogueLine("???",    SpeakerType.Delusion, "벗겨진 거야."),
        new DialogueLine("채하민", SpeakerType.Normal,   "뭐가."),
        new DialogueLine("???",    SpeakerType.Delusion, "네가 붙잡고 있던 것."),
        new DialogueLine("채하민", SpeakerType.Normal,   "불쾌한 기운이 온몸을 훑고 지나갔다."),
        new DialogueLine("채하민", SpeakerType.Normal,   "이겼다는 느낌은 들지 않았다."),
        new DialogueLine("채하민", SpeakerType.Normal,   "그저,\n안쪽에서 무언가가 조용히 무너진 것 같았다."),
        new DialogueLine("???",    SpeakerType.Delusion, "이제 보게 될 거야."),
        new DialogueLine("채하민", SpeakerType.Normal,   "뭘."),
        new DialogueLine("???",    SpeakerType.Delusion, "네가 두고 간 것."),
        new DialogueLine("채하민", SpeakerType.Normal,   "시야가 끊겼다."),
        new DialogueLine("채하민", SpeakerType.Normal,   "소리도 사라졌다."),
    };

    private static readonly DialogueLine[] DefaultPacifistPostBossExitDialogueLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "...죽지 않았어."),
        new DialogueLine("???",    SpeakerType.Delusion, "그래서 끝난 것 같아?"),
        new DialogueLine("채하민", SpeakerType.Normal,   "아니."),
        new DialogueLine("채하민", SpeakerType.Normal,   "그래도 지나갈 거야."),
    };

    private static readonly DialogueLine[] DefaultNormalPostBossExitDialogueLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "...죽이지 못했어."),
        new DialogueLine("???",    SpeakerType.Delusion, "그런데도 지나가?"),
        new DialogueLine("채하민", SpeakerType.Normal,   "모르겠어."),
        new DialogueLine("채하민", SpeakerType.Normal,   "그래도 앞으로 가야 해."),
    };

    private static readonly DialogueLine[] DefaultMassacrePostBossDialogueLines =
    {
        new DialogueLine("채하민", SpeakerType.Normal,   "끝났어."),
        new DialogueLine("???",    SpeakerType.Delusion, "그래."),
        new DialogueLine("채하민", SpeakerType.Normal,   "이제 조용해졌어."),
        new DialogueLine("???",    SpeakerType.Delusion, "아직 위에 남아 있어."),
        new DialogueLine("채하민", SpeakerType.Normal,   "그럼 확인하러 가야겠네."),
        new DialogueLine("???",    SpeakerType.Delusion, "마지막까지."),
    };

    [Header("References")]
    public BossHealth bossHealth;
    public BossPatternCaster caster;

    [Header("Visual")]
    [Tooltip("보스 루트가 아니라 자식 BossSprite를 넣으세요. 위치는 루트가 움직이고, 이 Transform은 회전/표시만 담당합니다.")]
    [SerializeField] private Transform bossSpriteTransform;

    // 애니메이션 클립이 방향별로 나뉘어 있으므로 기본 false.
    // 삼각형 placeholder 사용 시에만 true로 켜세요.
    [SerializeField] private bool rotateBossVisualByFacing = false;

    [Tooltip("삼각형 스프라이트가 기본으로 아래를 보면 180을 넣으세요. 기본이 위면 0입니다.")]
    [SerializeField] private float visualRotationOffsetDegrees = 0f;

    [Tooltip("BossSprite localPos Y 오프셋. 스프라이트 발이 셀 중앙에 오도록 조정 (sprite.bounds.extents.y 값을 기본으로 런타임에 자동 설정됩니다).")]
    [SerializeField] private float bossSpriteYOffset = 0f;
    [SerializeField] private float bossSpriteFootCellOffsetY = -1f;
    [SerializeField] private float bossSpriteHorizontalFacingOffsetY = -0.2f;
    [SerializeField] private float bossSpriteHorizontalAttackExtraOffsetY = 0f;
    [SerializeField] private float bossSpriteBackAttackExtraOffsetY = -0.5f;
    [SerializeField] private float bossSpriteLRMoveOffsetY = 0f; // 좌우 이동 스프라이트 전용 Y 보정

    [Header("Visual Driver")]
    [SerializeField] private BossAnimatorDriver animDriver;

    [Header("VFX Prefabs")]
    [SerializeField] private GameObject scratchVFXPrefab;
    [SerializeField] private GameObject slamVFXPrefab;
    [SerializeField] private GameObject dashSmokeVFXPrefab;
    [SerializeField] private GameObject markAtkVFXPrefab;
    [SerializeField] private GameObject bossHitVfxPrefab;
    [SerializeField] private Vector3 slamVfxWorldOffset = new Vector3(0f, -1.5f, 0f);

    [Header("SFX")]
    [SerializeField] private AudioSource bossAudioSource;
    [SerializeField] private AudioClip bossHitSfx;
    [SerializeField] private AudioClip bossAttackSfx;
    [SerializeField] private AudioClip bossEnhancedAttackSfx;
    [SerializeField] private AudioClip bossDashWhooshSfx;
    [SerializeField] private AudioClip bossDashScreamSfx;
    [SerializeField] private AudioClip bossScreamSfx;
    [SerializeField] private AudioClip bossIntroScreamSfx;
    [SerializeField] private AudioClip bossAppearSfx;
    [SerializeField] private AudioClip bossSlamSfx;
    [SerializeField] private AudioClip bossMarkWarningSfx;
    [SerializeField] private AudioClip bossDeathSfx;
    [SerializeField] private string dashVoiceResourcesPath = DashVoiceResourcesPath;

    [Header("Timing - LandingSlam Drop")]
    [SerializeField] private float slamDropHeight = 1.4f;
    [SerializeField] private float slamDropDuration = 0.22f;
    [SerializeField] private float postSlamRoarDuration = 0.8f;

    [Header("Timing - Scream")]
    [SerializeField] private float screamDuration = 1.0f;

    [Header("Timing - Death Anim")]
    [SerializeField] private float bossDeathAnimDuration = 1.2f;

    [Header("Timing - MarkATK VFX Rotation")]
    [SerializeField] private float markAtkHorizontalRotOffset = 0f;
    [SerializeField] private float markAtkVerticalRotOffset = 90f;

    [Header("MarkATK Mask")]
    [SerializeField] private bool useMarkAtkArenaMask = true;
    [SerializeField] private int markAtkMaskSortingOrder = 10;

    [Header("Timing - Dash Smoke")]
    [SerializeField] private float dashSmokeSpawnInterval = 0.05f;
    [SerializeField] private float dashSmokeFadeDuration = 0.3f;
    [SerializeField] private float dashSmokeLifetime = 2.2f;

    [Header("SCRATCH VFX Tuning")]
    [SerializeField] private float scratchCardinalLengthPadding = 1.8f;
    [SerializeField] private float scratchCardinalWidthPadding = 0.9f;
    [SerializeField] private float scratchDiagonalLengthPadding = 2.1f;
    [SerializeField] private float scratchDiagonalWidth = 3.15f;
    [SerializeField] private float scratchRotationOffsetDegrees = 0f;
    [SerializeField] private Vector2 scratchScaleMultiplier = new Vector2(1.18f, 1.12f);
    [SerializeField] private float scratchAlpha = 0.42f;
    [SerializeField] private bool clipDashVfxInsideArena = true;
    [SerializeField] private float dashSmokeAlpha = 0.72f;
    [SerializeField] private float dashSmokeScale = 1.35f;

    [Header("Boss Hit VFX")]
    [SerializeField] private Vector3 bossHitVfxLocalOffset = new Vector3(0f, 0.6f, 0f);
    [SerializeField] private float bossHitVfxScale = 0.85f;
    [SerializeField] private float bossHitVfxLifetime = 1.4f;

    [Header("Audio Tuning")]
    [SerializeField] private float battleBgmVolume = 0.34f;
    [SerializeField] private float phase3BattleBgmVolume = 0.95f;
    [SerializeField] private float phase3AmbienceVolume = 0.22f;
    [SerializeField] private float bossSfxVolume = 0.18f;
    [SerializeField] private float introNoiseVolume = 0.12f;
    [SerializeField] private float dashScreamVolumeScale = 0.35f;
    [SerializeField] private float dashScreamVolumeBoost = 0.30f;
    [SerializeField] private float deathScreamVolumeScale = 0.85f;

    [Header("Cutscene")]
    [SerializeField] private float cutsceneAdvanceDelay = 0.08f;
    [SerializeField] private Vector2Int introAutoMoveDirection = new Vector2Int(0, 1);
    [SerializeField] private float introAutoMoveDuration = 0.23f;
    [SerializeField] private DialogueLine[] introDialogueLines;
    [SerializeField] private DialogueLine[] postBossDialogueLines;
    [SerializeField] private DialogueLine[] pacifistPostBossExitDialogueLines;
    [SerializeField] private DialogueLine[] normalPostBossExitDialogueLines;
    [SerializeField] private DialogueLine[] massacrePostBossDialogueLines;

    [Header("RL Training")]
    [SerializeField] private bool skipIntroForTraining = false;

    [Header("Post-Boss Exit")]
    [SerializeField] private Vector2Int postBossExitCenterCell = new Vector2Int(0, 3);
    [SerializeField] private Vector2Int postBossExitSize = new Vector2Int(3, 1);

    [Header("Phase 3 UI Warning")]
    [SerializeField] private bool usePhaseDangerOverlay = true;
    [SerializeField] private float phaseDangerBaseAlpha = 0.12f;
    [SerializeField] private float phaseDangerDashAlpha = 0.22f;
    [SerializeField] private float phaseDangerPulseDuration = 0.24f;
    [SerializeField] private float phaseDangerEdgeThickness = 28f;

    [Header("Phase 3 Entry Effect")]
    [SerializeField] private float phase3EntryFlashAlpha = 0.35f;
    [SerializeField] private float phase3EntryFlashFadeDuration = 1.2f;

    [Header("Phase Thresholds")]
    [SerializeField] private int phase2HpPercent = 70;
    [SerializeField] private int phase3HpPercent = 40;
    [SerializeField] private int finalPhaseHpPercent = 10;

    [Header("Scene")]
    public string nextSceneName = "RooftopEndingWalk";
    public float introWaitSeconds = 1.5f;
    [SerializeField] private float postIntroBattleGraceTime = 1.1f;

    [Header("Boss Pacifist Timeout")]
    [SerializeField] private bool enableBossPacifistTimeout = true;
    [SerializeField] private float forcePhase3AfterSeconds = 300f;
    [SerializeField] private float pacifistResolveAfterForcedPhase3Seconds = 60f;
    [SerializeField] private float pacifistFinalRoarTime = 0.8f;
    [SerializeField] private float pacifistFinalWarningTime = 1.0f;
    [SerializeField] private float pacifistFinalDamageTime = 0.15f;
    [SerializeField] private Vector2Int pacifistFinalBossCell = new Vector2Int(0, 3);

    [Header("Boss Route Testing")]
    [SerializeField] private bool forceBossMassacreRouteForTesting;

    [Header("Timing - Movement")]
    [SerializeField] private float moveOneCellTime = 0.30f;
    [SerializeField] private float oneCellDashTime = 0.14f;
    [SerializeField] private float afterBasicAttackDelay = 0.05f;
    [SerializeField] private float afterBasicAttackRecoveryDelay = 0.17f;
    [SerializeField] private float basicAttackRecoveryTime = 0.5f;

    [Tooltip("일반 5연속 할퀴기에서 보스가 이동 없이 제자리 공격을 반복할 때만 추가되는 후딜입니다. 강화 할퀴기 연계에는 적용하지 않습니다.")]
    [SerializeField] private float noMoveNormalScratchRecoveryTime = 0.35f;
    [SerializeField] private float noMoveNormalScratchRecoveryExtra = 0.07f;

    [Header("Timing - Basic Attacks")]
    [Tooltip("일반 할퀴기 경고 시간. HP 50% 전/후 모두 기존 속도를 유지합니다.")]
    [SerializeField] private float normalScratchWarningTime = 0.46f;
    [SerializeField] private float normalScratchWarningEaseTime = 0.04f;

    [Tooltip("일반 할퀴기 실제 타격 지속 시간.")]
    [SerializeField] private float normalScratchDamageTime = 0.15f;

    [Tooltip("강화 할퀴기 직전 경고 시간. 너무 즉발이 되지 않도록 최소 반응 시간을 둡니다.")]
    [SerializeField] private float enhancedScratchFlashWarningTime = 0.10f;

    [Tooltip("강화 할퀴기 실제 타격 지속 시간. 타격 이펙트가 보이도록 일반 할퀴기와 비슷하게 둡니다.")]
    [SerializeField] private float enhancedScratchDamageTime = 0.15f;

    [Header("Timing - Pattern")]
    [SerializeField] private float patternStepGap = 0.35f;
    [SerializeField] private float patternStepGapReduction = 0.06f;
    [SerializeField] private float dashPatternWarningReduction = 0.04f;
    [SerializeField] private float dashPatternWarningExtra = 0.02f;
    [SerializeField] private float sweepFirstDashExtraWarning = 0.20f;
    [SerializeField] private float patternEntryWarningTime = 0.70f;
    [SerializeField] private float patternEntryDamageTime = 0.15f;
    [SerializeField] private float afterPatternEntryDashDelay = 0.65f;
    [SerializeField] private float afterPatternEntryDashDelayReduction = 0.05f;

    [Header("Timing - Dash Visual")]
    [Tooltip("돌진 실타격과 동시에 보스가 지나가는 시간입니다. 작을수록 빠르게 보입니다.")]
    [SerializeField] private float dashVisualTime = 0.14f;

    [Tooltip("true면 돌진 패턴 타격 시 보스 삼각형이 실제로 빠르게 지나갑니다.")]
    [SerializeField] private bool playDashVisualOnPatternDamage = true;

    [Header("Timing - Mark Dash")]
    [SerializeField] private float markFlashTime = 0.68f;
    [SerializeField] private float markDamageTime = 0.15f;
    [SerializeField] private float afterMarkDashDelay = 0.60f;

    [Header("Fake Mark Dash")]
    [SerializeField] private bool enableFakeMarkDash = false;
    [SerializeField] private int fakeMarkStartPhase = 3;
    [SerializeField] private bool skipFirstMarkDashInFakePhase = true;

    [SerializeField, Range(0f, 1f)]
    private float fakeModeChance = 0.5f;

    [SerializeField] private bool testingFakeALot = false;

    [SerializeField] private GameObject normalMarkVfxPrefab;
    [SerializeField] private GameObject fakeMarkVfxPrefab;

    [Header("Timing - Landing / Final")]
    [SerializeField] private float landingWarningTime = 0.64f;
    [SerializeField] private float landingDamageTime = 0.15f;
    [SerializeField] private float afterLandingDelay = 0.30f;

    [SerializeField] private float finalSlamWarningTime = 0.66f;
    [SerializeField] private float finalDashWarningTime = 0.70f;
    [SerializeField] private float finalDashWarningReduction = 0.40f;
    [SerializeField] private float finalDamageTime = 0.15f;
    [SerializeField] private float afterFinalActionDelay = 0.30f;
    [SerializeField] private float finalSlamToDashDelay = 0.08f;
    [SerializeField] private float afterFinalDashDelay = 0.24f;

    [Header("Final Phase Dash Options")]
    [Tooltip("false(기본): 기존 찍고→경고장판→돌진 유지. true: 경고장판 없이 4방향 랜덤 즉시 돌진.")]
    [SerializeField] private bool finalSlamUseRandomNoWarningDash = false;

    private const int Min = -3;
    private const int Max = 3;

    // bossCell은 보스전 7x7 내부 좌표입니다. 월드 셀이 아닙니다.
    private Vector2Int bossCell = new Vector2Int(0, 2);
    private Vector2Int bossFacing = Vector2Int.down;

    // PlayerCombat이 읽는 값입니다.
    // PlayerCombat의 attackCells는 GridManager 기준 월드 셀이므로 BossCell도 같은 좌표계여야 합니다.
    public Vector2Int BossCell
    {
        get
        {
            if (GridManager.Instance != null)
                return GridManager.Instance.WorldToCell(transform.position);

            return bossCell;
        }
    }

    // 디버그/패턴 확인용 내부 좌표입니다.
    public Vector2Int BossArenaCell => bossCell;
    public Transform DiagnosticBossVisualRoot => bossSpriteTransform;
    public bool DiagnosticIsDashInProgress => diagnosticDashInProgress;
    public bool DiagnosticIsMarkDashInProgress => diagnosticMarkDashInProgress;
    public Vector2Int DiagnosticDashStartCell => diagnosticDashStartCell;
    public Vector2Int DiagnosticDashEndCell => diagnosticDashEndCell;
    public Vector2Int DiagnosticDashCurrentCell => diagnosticDashCurrentCell;
    public IReadOnlyList<Vector2Int> DiagnosticDashLaneCells => diagnosticDashLaneCells;

    private enum BossPhase
    {
        Phase1,
        Phase2,
        Phase3,
        Final
    }

    private enum MarkDashVariant
    {
        NormalVertical,
        NormalHorizontal,
        FakeVertical,
        FakeHorizontal
    }

    /// <summary>
    /// 돌진 비주얼 프로파일.
    /// Full        = SCRATCH + Smoke(거리기반) + Whoosh + DashScream  ← 외부 돌진 기본값
    /// InternalQuiet = SCRATCH + Whoosh만  ← 내부 이동 계열(PatternEntry, FinalDash)
    /// </summary>
    private enum DashVisualProfile
    {
        Full,
        InternalQuiet
    }

    private BossPhase currentPhase = BossPhase.Phase1;

    private bool phase2Triggered = false;
    private bool phase3Triggered = false;
    private bool finalTriggered = false;

    private bool phase1FirstDone = false;
    private bool phase2FirstDone = false;
    private bool phase3FirstDone = false;
    private bool hasSkippedFirstMarkDashInFakePhase = false;
    private bool diagnosticDashInProgress = false;
    private bool diagnosticMarkDashInProgress = false;
    private Vector2Int diagnosticDashStartCell = Vector2Int.zero;
    private Vector2Int diagnosticDashEndCell = Vector2Int.zero;
    private Vector2Int diagnosticDashCurrentCell = Vector2Int.zero;
    private readonly List<Vector2Int> diagnosticDashLaneCells = new List<Vector2Int>();


    private GridOccupant playerOccupant;
    private Transform playerTransform;
    private GridMover playerMover;
    private PlayerController playerController;
    private PlayerCombat playerCombat;

    private PlayerHealth playerHealth;
    private ClickAdvanceCutsceneOverlay cutsceneOverlay;
    private AudioClip introNoiseClip;

    private bool movedDuringLastApproach = false;
    private bool deathStarted = false;
    private bool waitingForPostBossExit = false;
    private bool exitSceneQueued = false;
    private bool phase3AmbienceActivated = false;
    private bool phase3EntryEffectFired;
    private BossHpBar bossHpBarRef;
    private SpriteMask markAtkArenaMask;
    private SpriteMask dashVfxArenaMask;
    private static Sprite runtimeMaskSprite;
    private static Sprite[] bossHitVfxFrames;
    private Image phaseDangerOverlayImage;
    private Sprite runtimePhaseDangerOverlaySprite;
    private Coroutine phaseDangerPulseRoutine;
    private AudioClip[] dashVoiceClips;
    private int lastDashVoiceIndex = -1;
    private float bossSpritePoseOffsetY = 0f;
    private GameObject bossHudRoot;
    private bool battleTimerRunning;
    private float bossBattleElapsed;
    private float forcedPhase3Elapsed;
    private bool phase3ForcedByPacifistTimer;
    private bool pacifistResolveStarted;

    // ── PacifistTimerUI 읽기 전용 API ────────────────────────────────────────
    public bool IsPacifistTimerEnabled                  => enableBossPacifistTimeout;
    public bool IsBattleTimerRunning                    => battleTimerRunning;
    public bool IsPhase3ForcedByPacifistTimer           => phase3ForcedByPacifistTimer;
    public bool IsPacifistResolveStarted                => pacifistResolveStarted;
    public float BossBattleElapsed                      => bossBattleElapsed;
    public float ForcePhase3AfterSeconds                => Mathf.Max(0f, forcePhase3AfterSeconds);
    public float ForcedPhase3Elapsed                    => forcedPhase3Elapsed;
    public float PacifistResolveAfterForcedPhase3Seconds => Mathf.Max(0f, pacifistResolveAfterForcedPhase3Seconds);

    // BossSprite의 기준 localPosition (발 기준 오프셋 포함)
    private Vector3 BossSpriteRestPos
    {
        get
        {
            float cellSize = caster != null ? caster.CellSize : 1f;
            float facingOffset = bossFacing.x != 0 ? bossSpriteHorizontalFacingOffsetY * cellSize : 0f;
            float poseOffset = bossSpritePoseOffsetY * cellSize;
            return new Vector3(0f, bossSpriteYOffset + (bossSpriteFootCellOffsetY * cellSize) + facingOffset + poseOffset, 0f);
        }
    }

    private bool spritePivotAdjusted = false;

    private void Awake()
    {
        if (bossHealth == null)
            bossHealth = GetComponent<BossHealth>();

        if (caster == null)
            caster = GetComponent<BossPatternCaster>();

#if UNITY_EDITOR
        AssignEditorVfxReferences();
#endif
        ApplyRuntimeBossTuning();
    }

    private void Start()
    {
        CachePlayer();
        CachePlayerSceneRefs();

        if (bossHealth == null || caster == null)
        {
            enabled = false;
            return;
        }

        bossCell = new Vector2Int(0, 2);
        SetBossFacing(Vector2Int.down);
        SyncBossRootToCell();
        SetBossVisible(false);
        EnsureMarkAtkArenaMask();
        EnsurePhaseDangerOverlay();
        UpdatePhaseDangerOverlayImmediate();
        cutsceneOverlay = ClickAdvanceCutsceneOverlay.EnsureInScene();
        introNoiseClip = Resources.Load<AudioClip>(IntroNoiseResourcePath);
        RunRouteTracker.SetBossMassacreRouteTriggerForTesting(forceBossMassacreRouteForTesting);
        if (!HasUsableDialogue(introDialogueLines))
            introDialogueLines = GetIntroDialogueLinesForRoute();
        if (!HasUsableDialogue(postBossDialogueLines))
            postBossDialogueLines = GetPostBossDialogueLinesForRoute();
        LoadDashVoiceClips();

        if (bossAudioSource != null)
            bossAudioSource.volume = bossSfxVolume;

        if (BossBattleMusicRuntime.Instance != null)
        {
            BossBattleMusicRuntime.Instance.SetMainVolume(battleBgmVolume);
            BossBattleMusicRuntime.Instance.SetPhase3MainVolume(phase3BattleBgmVolume);
            BossBattleMusicRuntime.Instance.SetPhase3AmbienceVolume(phase3AmbienceVolume);
        }

        // 딜타임/취약시간 없음. 전투 중 보스는 항상 피격 가능.
        bossHealth.SetDamageable(true);

        bossHealth.onDead.AddListener(OnBossDead);
        bossHealth.onDamaged.AddListener(OnBossHit);

        StartCoroutine(BossRoutine());
    }

    private void Update()
    {
        TickBossPacifistTimer();

        if (!waitingForPostBossExit || exitSceneQueued)
            return;

        if (IsPlayerInsidePostBossExit())
        {
            exitSceneQueued = true;
            RunRouteTracker.DecideEnding();

            if (RunRouteTracker.BossRoute == BossRouteState.PacifistSurvived)
            {
                StartCoroutine(LoadNextSceneAfterNonKilledBossExitDialogue());
                return;
            }

            SceneManager.LoadScene(nextSceneName);
        }
    }

    private IEnumerator BossRoutine()
    {
        yield return IntroRoutine();
        StartBossBattleTimer();

        while (bossHealth.currentHp > 0)
        {
            EvaluatePhase();

            if (currentPhase == BossPhase.Final)
            {
                yield return PatternEntryDash();

                if (bossHealth.currentHp <= 0)
                    yield break;

                yield return RunFinalPhaseCycleAfterEntryDash();

                if (bossHealth.currentHp <= 0)
                    yield break;

                continue;
            }

            yield return BasicAttackLoop();

            if (bossHealth.currentHp <= 0)
                yield break;

            EvaluatePhase();

            yield return PatternEntryDash();

            if (bossHealth.currentHp <= 0)
                yield break;

            if (currentPhase == BossPhase.Final)
            {
                yield return RunFinalPhaseCycleAfterEntryDash();

                if (bossHealth.currentHp <= 0)
                    yield break;

                continue;
            }

            yield return RunPhaseCycle();

            if (bossHealth.currentHp <= 0)
                yield break;

            yield return LandingSlam();
        }
    }

    private void EvaluatePhase()
    {
        if (phase3ForcedByPacifistTimer)
        {
            phase3Triggered = true;
            phase2Triggered = true;
            currentPhase = BossPhase.Phase3;
            ActivatePhase3PresentationIfNeeded();
            return;
        }

        int hp = bossHealth.currentHp;
        int max = bossHealth.maxHp;

        // Final: 10% 이하.
        // Final은 반복 페이즈가 아니라 "최후의 발악 1회 + 3페이즈 복귀" 구조다.
        // 상위 페이즈로 바로 건너뛰는 경우 하위 triggered도 같이 true 처리해 역행을 막는다.
        if (!finalTriggered && IsHpAtOrBelowPercent(hp, max, finalPhaseHpPercent))
        {
            finalTriggered = true;
            phase3Triggered = true;
            phase2Triggered = true;

            currentPhase = BossPhase.Final;
            ActivatePhase3PresentationIfNeeded();
            return;
        }

        // Phase3: 기본값 40% 이하.
        // Phase3로 바로 진입한 뒤 다음 EvaluatePhase에서 Phase2로 역행하지 않도록 phase2Triggered도 같이 켠다.
        if (!phase3Triggered && IsHpAtOrBelowPercent(hp, max, phase3HpPercent))
        {
            phase3Triggered = true;
            phase2Triggered = true;

            currentPhase = BossPhase.Phase3;
            ActivatePhase3PresentationIfNeeded();
            return;
        }

        // Phase2: 70% 이하.
        // 이미 Phase3/Final에 들어간 상태라면 Phase2로 덮어쓰지 않는다.
        if (!phase2Triggered && IsHpAtOrBelowPercent(hp, max, phase2HpPercent))
        {
            phase2Triggered = true;

            if (currentPhase == BossPhase.Phase1)
                currentPhase = BossPhase.Phase2;

            UpdatePhaseDangerOverlayImmediate();
            ActivatePhase3PresentationIfNeeded();
            return;
        }

        ActivatePhase3PresentationIfNeeded();
    }

    private bool IsHpAtOrBelowPercent(int hp, int max, int percent)
    {
        percent = Mathf.Clamp(percent, 0, 100);
        return hp * 100 <= max * percent;
    }

    private static DialogueLine[] GetIntroDialogueLinesForRoute()
    {
        if (IsDirectBossRouteTest())
        {
            return RunRouteTracker.BossMassacreRouteTriggerForTesting
                ? MassacreIntroDialogueLines
                : PacifistIntroDialogueLines;
        }

        if (RunRouteTracker.Stage1Route == StageRouteState.Pacifist
            && RunRouteTracker.Stage2Route == StageRouteState.Pacifist)
        {
            return PacifistIntroDialogueLines;
        }

        if (RunRouteTracker.Stage1Route == StageRouteState.Massacre
            && RunRouteTracker.Stage2Route == StageRouteState.Massacre)
        {
            return MassacreIntroDialogueLines;
        }

        return DefaultIntroDialogueLines;
    }

    private DialogueLine[] GetPostBossDialogueLinesForRoute()
    {
        bool massacreContext =
            (RunRouteTracker.Stage1Route == StageRouteState.Massacre
             && RunRouteTracker.Stage2Route == StageRouteState.Massacre)
            || (IsDirectBossRouteTest() && RunRouteTracker.BossMassacreRouteTriggerForTesting);

        if (massacreContext)
        {
            return HasUsableDialogue(massacrePostBossDialogueLines)
                ? massacrePostBossDialogueLines
                : DefaultMassacrePostBossDialogueLines;
        }

        return DefaultPostBossDialogueLines;
    }

    private static bool IsDirectBossRouteTest()
    {
        return RunRouteTracker.Stage1Route == StageRouteState.Unknown
            && RunRouteTracker.Stage2Route == StageRouteState.Unknown;
    }

    private void StartBossBattleTimer()
    {
        bossBattleElapsed = 0f;
        forcedPhase3Elapsed = 0f;
        phase3ForcedByPacifistTimer = false;
        pacifistResolveStarted = false;
        battleTimerRunning = enableBossPacifistTimeout;
    }

    private void TickBossPacifistTimer()
    {
        if (!battleTimerRunning || !enableBossPacifistTimeout || deathStarted || pacifistResolveStarted)
            return;

        if (bossHealth == null || bossHealth.currentHp <= 0)
            return;

        if (!phase3ForcedByPacifistTimer && phase3Triggered)
        {
            battleTimerRunning = false;
            return;
        }

        bossBattleElapsed += Time.deltaTime;

        if (!phase3ForcedByPacifistTimer && bossBattleElapsed >= Mathf.Max(0f, forcePhase3AfterSeconds))
        {
            ForcePhase3ForPacifistTimer();
        }

        if (!phase3ForcedByPacifistTimer)
            return;

        forcedPhase3Elapsed += Time.deltaTime;
        if (forcedPhase3Elapsed >= Mathf.Max(0f, pacifistResolveAfterForcedPhase3Seconds))
        {
            BeginPacifistResolve();
        }
    }

    private void ForcePhase3ForPacifistTimer()
    {
        phase3ForcedByPacifistTimer = true;
        forcedPhase3Elapsed = 0f;
        phase3Triggered = true;
        phase2Triggered = true;
        currentPhase = BossPhase.Phase3;
        ActivatePhase3PresentationIfNeeded();
        UpdatePhaseDangerOverlayImmediate();
    }

    private void BeginPacifistResolve()
    {
        if (pacifistResolveStarted || deathStarted)
            return;

        pacifistResolveStarted = true;
        battleTimerRunning = false;
        StopAllCoroutines();
        StartCoroutine(PacifistResolveRoutine());
    }

    private IEnumerator PacifistResolveRoutine()
    {
        CachePlayerSceneRefs();
        SetBossHudVisible(false);
        ClearActivePatternObjects();

        if (bossHealth != null)
            bossHealth.SetDamageable(false);

        ForcePhase3ForPacifistTimer();

        Vector2Int targetCell = ClampCell(pacifistFinalBossCell);
        Vector2Int moveDelta = targetCell - bossCell;
        if (moveDelta != Vector2Int.zero)
            SetBossFacing(moveDelta);

        yield return MoveBossRootToCell(targetCell, moveOneCellTime);
        bossCell = targetCell;
        SyncBossRootToCell();
        SetBossFacing(Vector2Int.down);
        animDriver?.PlayIdle(bossFacing);

        SetBossVisible(true);
        SetBossSpritePoseOffset(0f);
        animDriver?.PlayScream(bossFacing);
        PlaySfx(bossScreamSfx != null ? bossScreamSfx : bossIntroScreamSfx);
        yield return new WaitForSeconds(Mathf.Max(0f, pacifistFinalRoarTime));

        List<Vector2Int> allCells = GetAllArenaCells();
        yield return caster.CastWarningOnly(
            allCells,
            pacifistFinalWarningTime,
            fillOriginCell: targetCell,
            animateWarningFill: true,
            fillDirection: Vector2Int.down
        );

        Vector2Int dashEnd = new Vector2Int(targetCell.x, Min);
        if (playDashVisualOnPatternDamage)
        {
            StartCoroutine(DashBossAlongArenaLine(
                targetCell,
                dashEnd,
                Vector2Int.down,
                Mathf.Max(0.01f, pacifistFinalDamageTime),
                hideAfter: false
            ));
        }

        ApplyDashVisuals(allCells, targetCell, dashEnd, DashVisualProfile.Full);
        yield return new WaitForSeconds(Mathf.Max(0.01f, pacifistFinalDamageTime));

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning("[ElevatorBossController] Pacifist resolve could not find PlayerHealth.");
            yield break;
        }

        RunRouteTracker.SetDeathContext(DeathRouteContext.BossPacifistFinalHit);
        playerHealth.TakeDamage(1);
        yield return null;

        if (playerHealth.IsDead || playerHealth.currentHp <= 0)
            yield break;

        RunRouteTracker.ClearDeathContext();
        RunRouteTracker.SetBossPacifistSurvived();
        RunRouteTracker.DecideEnding();
        CutsceneFreezeManager.ForceUnlock();
        SetBossVisible(false);
        waitingForPostBossExit = true;
        SetPlayerControlEnabled(true);
    }

    private List<Vector2Int> GetAllArenaCells()
    {
        List<Vector2Int> cells = new List<Vector2Int>(49);
        for (int y = Min; y <= Max; y++)
        {
            for (int x = Min; x <= Max; x++)
            {
                cells.Add(new Vector2Int(x, y));
            }
        }

        return cells;
    }

    private static void ClearActivePatternObjects()
    {
        foreach (DamageTile tile in FindObjectsByType<DamageTile>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tile != null)
                Destroy(tile.gameObject);
        }

        foreach (PatternTile tile in FindObjectsByType<PatternTile>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tile != null)
                Destroy(tile.gameObject);
        }

        foreach (MergedPatternWarningVisual visual in FindObjectsByType<MergedPatternWarningVisual>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (visual != null)
                Destroy(visual.gameObject);
        }
    }


    private void ActivatePhase3PresentationIfNeeded()
    {
        UpdatePhaseDangerOverlayImmediate();

        if (!phase3AmbienceActivated && IsPhase3OrLater())
        {
            phase3AmbienceActivated = true;
            BossBattleMusicRuntime.Instance?.SetPhase3AmbienceEnabled(true);
        }

        if (!phase3EntryEffectFired && IsPhase3OrLater())
        {
            phase3EntryEffectFired = true;
            FirePhase3EntryEffect();
        }
    }

    private void ApplyRuntimeBossTuning()
    {
        if (Mathf.Approximately(markFlashTime, 0.82f) ||
            Mathf.Approximately(markFlashTime, 0.74f) ||
            Mathf.Approximately(markFlashTime, 0.70f) ||
            Mathf.Approximately(markFlashTime, 0.52f))
        {
            markFlashTime = 0.68f;
        }

        // Unify all animation states to the ATTACK_LEFT/RIGHT reference Y position (-1.2f total foot offset).
        // This makes IDLE/MOVE/ATTACK all sit at the same height regardless of facing direction.
        bossSpriteFootCellOffsetY        = -1.2f;
        bossSpriteHorizontalFacingOffsetY      = 0f;
        bossSpriteHorizontalAttackExtraOffsetY = 0f;
        bossSpriteBackAttackExtraOffsetY       = 0f;
        // 좌우 이동 스프라이트는 0.5칸+2px 위에 그려져 있으므로 이동 중에만 보정. (32PPU 기준 2px = 0.0625)
        bossSpriteLRMoveOffsetY = -0.4f;

        normalScratchWarningTime = 0.46f;
        normalScratchWarningEaseTime = 0.04f;
        enhancedScratchFlashWarningTime = 0.10f;
        normalScratchDamageTime = 0.15f;
        enhancedScratchDamageTime = 0.15f;
        basicAttackRecoveryTime = 0.5f;
    }

    private float GetPatternStepGap()
    {
        return Mathf.Max(0f, patternStepGap - patternStepGapReduction);
    }

    private float GetAfterPatternEntryDashDelay()
    {
        return Mathf.Max(0f, afterPatternEntryDashDelay - afterPatternEntryDashDelayReduction);
    }

    private float GetDashPatternWarningTime(float warningTime)
    {
        if (warningTime < 0f)
            return Mathf.Max(0.12f, caster.defaultWarningTime - dashPatternWarningReduction + dashPatternWarningExtra);

        return Mathf.Max(0.05f, warningTime);
    }

    private float GetFinalDashWarningTime()
    {
        return Mathf.Max(0.05f, finalDashWarningTime - finalDashWarningReduction);
    }

    private float GetFinalSlamToDashDelay()
    {
        return Mathf.Max(0f, finalSlamToDashDelay);
    }

    private float GetAfterFinalDashDelay()
    {
        float delay = afterFinalDashDelay > 0f ? afterFinalDashDelay : afterFinalActionDelay;
        return Mathf.Max(0f, delay);
    }

    private float GetDashScreamVolumeScale()
    {
        return Mathf.Clamp01(dashScreamVolumeScale + dashScreamVolumeBoost);
    }

    private IEnumerator BasicAttackLoop()
    {
        BossPhase phaseAtLoopStart = currentPhase;
        bool enhanced = IsPhase3OrLater();
        int reps = enhanced ? 3 : 5;

        for (int i = 0; i < reps && bossHealth.currentHp > 0; i++)
        {
            yield return MoveTowardPlayerFrontCell();

            Vector2Int playerCellBeforeNormalScratch = GetPlayerOffsetCell();

            SetBossSpritePoseOffset(GetAttackPoseOffsetForFacing());
            animDriver?.PlayAttackReady(bossFacing);

            yield return caster.CastCellsWithBeforeDamage(
                caster.NormalScratchDirectional(bossCell, bossFacing),
                normalScratchWarningTime + normalScratchWarningEaseTime,
                normalScratchDamageTime,
                beforeDamage: () =>
                {
                    SetBossSpritePoseOffset(GetAttackPoseOffsetForFacing());
                    animDriver?.PlayAttack(bossFacing);
                    PlaySfx(bossAttackSfx);
                },
                fillOriginCell: bossCell
            );

            SetBossSpritePoseOffset(0f);
            animDriver?.PlayIdle(bossFacing);

            bool playerStayedAfterNormalScratch = GetPlayerOffsetCell() == playerCellBeforeNormalScratch;

            if (enhanced)
            {
                yield return DashAndEnhancedScratch();
                animDriver?.ClearEnhancedAttack();
                SetBossSpritePoseOffset(0f);
                animDriver?.PlayIdle(bossFacing);
            }

            float extraRecovery = !enhanced && !movedDuringLastApproach && playerStayedAfterNormalScratch
                ? noMoveNormalScratchRecoveryTime + noMoveNormalScratchRecoveryExtra
                : 0f;

            EvaluatePhase();
            if (currentPhase != phaseAtLoopStart)
                yield break;

            yield return WaitForBasicAttackRecovery(extraRecovery);
        }
    }

    private IEnumerator WaitForBasicAttackRecovery(float extraRecovery = 0f)
    {
        float legacyDelay = afterBasicAttackDelay + afterBasicAttackRecoveryDelay + Mathf.Max(0f, extraRecovery);
        float delay = Mathf.Max(basicAttackRecoveryTime, legacyDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
    }

    private IEnumerator MoveTowardPlayerFrontCell()
    {
        SetBossVisible(true);
        movedDuringLastApproach = false;

        while (bossHealth.currentHp > 0)
        {
            Vector2Int playerCell = GetPlayerOffsetCell();
            Vector2Int delta = playerCell - bossCell;

            // 보스 일반 추적 AI는 PlayerCell 자체를 목표로 삼지 않음. 인접 칸에서 정지.
            // 단, 정지한 뒤에는 반드시 플레이어를 바라보게 회전한다.
            if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) <= 1)
            {
                FacePlayerWithoutMoving();
                animDriver?.PlayIdle(bossFacing);
                yield break;
            }

            Vector2Int dir;

            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                dir = new Vector2Int(delta.x > 0 ? 1 : -1, 0);
            else
                dir = new Vector2Int(0, delta.y > 0 ? 1 : -1);

            SetBossFacing(dir);
            SetBossSpritePoseOffset(dir.x != 0 ? bossSpriteLRMoveOffsetY : 0f);
            animDriver?.PlayMove(bossFacing);
            bossCell = ClampCell(bossCell + dir);
            movedDuringLastApproach = true;

            yield return MoveBossRootToCell(bossCell, moveOneCellTime);
        }

        animDriver?.PlayIdle(bossFacing);
    }

    private void FacePlayerWithoutMoving()
    {
        Vector2Int playerCell = GetPlayerOffsetCell();
        Vector2Int delta = playerCell - bossCell;

        if (delta == Vector2Int.zero)
            return;

        SetBossSpritePoseOffset(0f);
        SetBossFacing(delta);
    }

    private IEnumerator DashForwardOneCell()
    {
        Vector2Int start = bossCell;
        bossCell = ClampCell(bossCell + bossFacing);

        yield return MoveBossRootToCell(bossCell, oneCellDashTime);
    }

    private IEnumerator DashAndEnhancedScratch()
    {
        Vector2Int targetCell = ClampCell(bossCell + bossFacing);

        // 강화 할퀴기 범위는 돌진 후 도착 위치 기준으로 미리 계산한다.
        // 경고장판은 짧게 표시하되, 플레이어가 최소한 반응할 수 있는 시간을 둔다.
        List<Vector2Int> enhancedCells =
            caster.EnhancedScratchDirectional(targetCell, bossFacing);

        yield return caster.CastCellsWithBeforeDamage(
            enhancedCells,
            enhancedScratchFlashWarningTime,
            enhancedScratchDamageTime,
            beforeDamage: () =>
            {
                bossCell = targetCell;
                StartCoroutine(MoveBossRootToCell(targetCell, oneCellDashTime));
                SetBossSpritePoseOffset(GetAttackPoseOffsetForFacing());
                animDriver?.PlayEnhancedAttack(bossFacing);
                PlaySfx(bossEnhancedAttackSfx);
            },
            fillOriginCell: bossCell
        );
    }

    private IEnumerator PatternEntryDash()
    {
        SetBossVisible(true);

        // 포효
        yield return ScreamRoutine();

        // 포효 후 플레이어 현재 위치로 재조준 — 방향 전환 모션 잠깐 보여주고 경고 시작
        FacePlayerWithoutMoving();
        animDriver?.PlayIdle(bossFacing);
        yield return new WaitForSeconds(0.15f);

        List<Vector2Int> cells = caster.ForwardStripe3FromCell(bossCell, bossFacing);
        Vector2Int start = bossCell;
        Vector2Int end = caster.GetDirectionalEdgeCell(start, bossFacing);

        yield return CastDashPattern(
            cells,
            bossFacing,
            patternEntryWarningTime,
            patternEntryDamageTime,
            hideAfter: true,
            explicitStart: start,
            explicitEnd: end,
            profile: DashVisualProfile.InternalQuiet
        );

        bossCell = end;

        yield return new WaitForSeconds(GetAfterPatternEntryDashDelay());
    }

    private IEnumerator RunPhaseCycle()
    {
        switch (currentPhase)
        {
            case BossPhase.Phase1:
                if (!phase1FirstDone)
                {
                    phase1FirstDone = true;
                    yield return Phase1FirstFixed();
                }
                else
                {
                    yield return RunTwoAdditionalPatterns();
                }
                break;

            case BossPhase.Phase2:
                if (!phase2FirstDone)
                {
                    phase2FirstDone = true;
                    yield return Phase2FirstFixed();
                }
                else
                {
                    yield return RunTwoAdditionalPatterns();
                }
                break;

            case BossPhase.Phase3:
                if (!phase3FirstDone)
                {
                    phase3FirstDone = true;
                    yield return Phase3FirstFixed();
                }
                else
                {
                    yield return RunTwoAdditionalPatterns();
                }
                break;
        }
    }

    private IEnumerator Phase1FirstFixed()
    {
        // Phase 1 첫 고정 패턴은 X자 대각선 2회만으로 끝내지 않는다.
        // 명세 기준으로 대각선 2회 이후 추가패턴 1을 한 번 이어서 실행한다.
        // 단, Phase 1 첫 추가패턴 1에서는 1-C(N/Z)를 금지하므로 1-A 또는 1-B만 나온다.

        // 1. 우상단 → 좌하단.
        // 대각선 폭은 3칸이지만, 보스 시각 이동은 패턴 중앙 대각선 위를 빠르게 지난다.
        yield return CastDashPattern(
            caster.Diagonal_TR_BL_3(),
            new Vector2Int(-1, -1),
            explicitStart: new Vector2Int(3, 3),
            explicitEnd: new Vector2Int(-3, -3)
        );

        yield return new WaitForSeconds(GetPatternStepGap());

        // 2. 좌상단 → 우하단.
        yield return CastDashPattern(
            caster.Diagonal_TL_BR_3(),
            new Vector2Int(1, -1),
            explicitStart: new Vector2Int(-3, 3),
            explicitEnd: new Vector2Int(3, -3)
        );

        yield return new WaitForSeconds(GetPatternStepGap());

        // 3. 추가패턴 1. Phase 1 첫 추가패턴에서는 1-C 금지.
        yield return AdditionalPattern1(allowC: false);

    }

    private IEnumerator Phase2FirstFixed()
    {
        yield return Phase2SweepOnly();

        yield return new WaitForSeconds(GetPatternStepGap());

        yield return AdditionalPhase2Plus();
    }

    private IEnumerator Phase3FirstFixed()
    {
        yield return MarkDash4();

        if (bossHealth.currentHp <= 0)
            yield break;

        yield return new WaitForSeconds(GetPatternStepGap());

        int followUp = Random.value < 0.5f ? 4 : 6;
        yield return RunAdditionalByIndex(followUp);
    }

    private IEnumerator Phase2SweepOnly()
    {
        // 시계방향 순서(좌→아래→우→위) 유지, 시작 위치 랜덤 결정
        int startIndex = Random.Range(0, 4);
        for (int i = 0; i < 4 && bossHealth.currentHp > 0; i++)
        {
            float warnTime = i == 0
                ? GetDashPatternWarningTime(-1f) + sweepFirstDashExtraWarning
                : -1f;
            int idx = (startIndex + i) % 4;
            switch (idx)
            {
                case 0: yield return CastCenteredDashPattern(caster.LeftBand4(),   Vector2Int.down,  warnTime); break;
                case 1: yield return CastCenteredDashPattern(caster.BottomBand4(), Vector2Int.right, warnTime); break;
                case 2: yield return CastCenteredDashPattern(caster.RightBand4(),  Vector2Int.up,    warnTime); break;
                case 3: yield return CastCenteredDashPattern(caster.TopBand4(),    Vector2Int.right, warnTime); break;
            }

            if (i < 3)
                yield return new WaitForSeconds(GetPatternStepGap());
        }
    }

    private IEnumerator RunTwoAdditionalPatterns()
    {
        // Phase 1 : [1-A(0), 1-B(1), N(2), Z(3)] 4개 중 2개
        // Phase 2 : [AdditionalPhase2Plus(6), Phase2SweepOnly(4)] 2개 (항상 둘 다, 순서 랜덤)
        // Phase 3 : [AdditionalPhase2Plus(6), Phase2SweepOnly(4), MarkDash4(5)] 3개 중 2개
        List<int> pool;
        if (currentPhase == BossPhase.Phase1)
            pool = new List<int> { 0, 1, 2, 3 };
        else if (currentPhase == BossPhase.Phase3)
            pool = new List<int> { 6, 4, 5 };
        else
            pool = new List<int> { 6, 4 };

        int firstIndex = Random.Range(0, pool.Count);
        int first = pool[firstIndex];
        pool.RemoveAt(firstIndex);

        int second = pool[Random.Range(0, pool.Count)];

        yield return RunAdditionalByIndex(first);

        yield return new WaitForSeconds(GetPatternStepGap());
        yield return RunAdditionalByIndex(second);
    }

    private IEnumerator RunFinalPhaseCycleAfterEntryDash()
    {
        // 파이널 고정 패턴(찍고 대시 4회)을 1개 패턴으로 보고,
        // Phase 3 패턴 풀에서 1개를 더 실행한 뒤 강화 착지로 마무리한다.
        yield return FinalPhaseRoutine();

        if (bossHealth.currentHp <= 0)
            yield break;

        currentPhase = BossPhase.Phase3;
        yield return new WaitForSeconds(GetPatternStepGap());
        yield return RunOnePhase3AdditionalPattern();

        if (bossHealth.currentHp <= 0)
            yield break;

        yield return LandingSlam();
    }

    private IEnumerator RunOnePhase3AdditionalPattern()
    {
        int[] pool = { 6, 4, 5 };
        int choice = pool[Random.Range(0, pool.Length)];
        yield return RunAdditionalByIndex(choice);
    }

    private IEnumerator RunAdditionalByIndex(int index)
    {
        switch (index)
        {
            case 0: yield return Additional1A(); break;
            case 1: yield return Additional1B(); break;
            case 2: yield return NStroke(); break;
            case 3: yield return ZStroke(); break;
            case 4: yield return Phase2SweepOnly(); break;
            case 5: yield return MarkDash4(); break;
            case 6: yield return AdditionalPhase2Plus(); break;
        }
    }

    // Phase 2+ 추가패턴: 1-A 제외, [1-B, N, Z] 균등 1/3 선택
    private IEnumerator AdditionalPhase2Plus()
    {
        int choice = Random.Range(0, 3);
        switch (choice)
        {
            case 0: yield return Additional1B(); break;
            case 1: yield return NStroke(); break;
            default: yield return ZStroke(); break;
        }
    }

    private IEnumerator AdditionalPattern1(bool allowC)
    {
        int max = allowC ? 3 : 2;
        int choice = Random.Range(0, max);

        switch (choice)
        {
            case 0:
                yield return Additional1A();
                break;

            case 1:
                yield return Additional1B();
                break;

            case 2:
                yield return Additional1C();
                break;
        }
    }

    private IEnumerator Additional1A()
    {
        // 왼쪽 벽 → 위쪽 벽 → 오른쪽 벽 → 아래쪽 벽 순환 돌진.
        // 시각 이동은 각 2칸 폭 범위의 중앙선을 지난다.
        yield return CastCenteredDashPattern(caster.LeftEdge2(), Vector2Int.up);
        yield return new WaitForSeconds(GetPatternStepGap());

        yield return CastCenteredDashPattern(caster.TopEdge2(), Vector2Int.right);
        yield return new WaitForSeconds(GetPatternStepGap());

        yield return CastCenteredDashPattern(caster.RightEdge2(), Vector2Int.down);
        yield return new WaitForSeconds(GetPatternStepGap());

        yield return CastCenteredDashPattern(caster.BottomEdge2(), Vector2Int.left);
    }

    private IEnumerator Additional1B()
    {
        yield return CastCenteredDashPattern(caster.HorizontalStripe3(0), Vector2Int.right);
        yield return new WaitForSeconds(GetPatternStepGap());

        yield return CastCenteredDashPattern(caster.VerticalStripe3(0), Vector2Int.up);
    }

    private IEnumerator Additional1C()
    {
        if (Random.value < 0.5f)
            yield return NStroke();
        else
            yield return ZStroke();
    }

    private IEnumerator NStroke()
    {
        bool reverse = Random.value < 0.5f;
        float gap = 0.05f; // 다음 경고까지 텀 최소화

        if (!reverse)
        {
            yield return CastCenteredDashPattern(caster.LeftEdge2(), Vector2Int.down);
            yield return new WaitForSeconds(gap);

            yield return CastDashPattern(
                caster.Diagonal_TR_BL_3(),
                new Vector2Int(1, 1),
                explicitStart: new Vector2Int(-3, -3),
                explicitEnd: new Vector2Int(3, 3)
            );
            yield return new WaitForSeconds(gap);

            yield return CastCenteredDashPattern(caster.RightEdge2(), Vector2Int.down);
        }
        else
        {
            yield return CastCenteredDashPattern(caster.RightEdge2(), Vector2Int.up);
            yield return new WaitForSeconds(gap);

            yield return CastDashPattern(
                caster.Diagonal_TR_BL_3(),
                new Vector2Int(-1, -1),
                explicitStart: new Vector2Int(3, 3),
                explicitEnd: new Vector2Int(-3, -3)
            );
            yield return new WaitForSeconds(gap);

            yield return CastCenteredDashPattern(caster.LeftEdge2(), Vector2Int.up);
        }
    }

    private IEnumerator ZStroke()
    {
        bool reverse = Random.value < 0.5f;
        float gap = 0.05f; // 다음 경고까지 텀 최소화

        if (!reverse)
        {
            yield return CastCenteredDashPattern(caster.TopEdge2(), Vector2Int.right);
            yield return new WaitForSeconds(gap);

            yield return CastDashPattern(
                caster.Diagonal_TR_BL_3(),
                new Vector2Int(-1, -1),
                explicitStart: new Vector2Int(3, 3),
                explicitEnd: new Vector2Int(-3, -3)
            );
            yield return new WaitForSeconds(gap);

            yield return CastCenteredDashPattern(caster.BottomEdge2(), Vector2Int.right);
        }
        else
        {
            yield return CastCenteredDashPattern(caster.BottomEdge2(), Vector2Int.left);
            yield return new WaitForSeconds(gap);

            yield return CastDashPattern(
                caster.Diagonal_TR_BL_3(),
                new Vector2Int(1, 1),
                explicitStart: new Vector2Int(-3, -3),
                explicitEnd: new Vector2Int(3, 3)
            );
            yield return new WaitForSeconds(gap);

            yield return CastCenteredDashPattern(caster.TopEdge2(), Vector2Int.left);
        }
    }

    private IEnumerator MarkDash4(bool forceNormal = false)
    {
        for (int i = 0; i < 4 && bossHealth.currentHp > 0; i++)
        {
            yield return MarkDash(forceNormal);

            yield return new WaitForSeconds(afterMarkDashDelay);
        }
    }

    private IEnumerator MarkDash(bool forceNormal = false)
    {
        diagnosticMarkDashInProgress = true;
        Vector2Int playerCell = GetPlayerOffsetCell();
        MarkDashVariant variant = PickMarkDashVariant(forceNormal);
        bool displayHorizontal = IsMarkDashDisplayHorizontal(variant);
        bool isFake = IsFakeMarkDashVariant(variant);
        bool actualHorizontal = isFake ? !displayHorizontal : displayHorizontal;

        // v3.1: 5칸 표식 사용
        List<Vector2Int> markCells = displayHorizontal
            ? caster.MarkHorizontal5(playerCell)
            : caster.MarkVertical5(playerCell);

        List<GameObject> marks = new List<GameObject>();
        bool spawnLegacyWarningTiles = false;

        foreach (Vector2Int cell in markCells)
        {
            if (!spawnLegacyWarningTiles || !caster.Inside(cell) || caster.warningTilePrefab == null)
                continue;

            GameObject mark = Instantiate(
                caster.warningTilePrefab,
                caster.CellToWorld(cell),
                Quaternion.identity
            );

            // 표식 warning tile: 모든 SpriteRenderer 비활성화 — MarkATK VFX만 표시
            foreach (var sr in mark.GetComponentsInChildren<SpriteRenderer>(true))
                sr.enabled = false;

            marks.Add(mark);
        }

        // MarkATK VFX 스폰 (판정 없음, markFlashTime 후 자동 Destroy)
        SpawnMarkDashVfx(playerCell, displayHorizontal, isFake);

        SetBossVisible(false);
        PlaySfx(bossMarkWarningSfx);

        // 표식돌진은 차오르는 경고장판이 아니라 방향 표시 후 바로 발동.
        yield return new WaitForSeconds(markFlashTime);

        foreach (GameObject mark in marks)
        {
            if (mark != null)
                Destroy(mark);
        }

        List<Vector2Int> damageCells = actualHorizontal
            ? caster.HorizontalStripe3(playerCell.y)
            : caster.VerticalStripe3(playerCell.x);

        Vector2Int dashDir = PickMarkDashDirection(actualHorizontal);

        yield return CastDashDamageOnly(damageCells, dashDir, markDamageTime, hideAfter: true);
        diagnosticMarkDashInProgress = false;
    }

    private MarkDashVariant PickMarkDashVariant(bool forceNormal = false)
    {
        if (forceNormal || !CanUseFakeMarkDash())
            return PickNormalMarkDashVariant();

        if (!testingFakeALot && skipFirstMarkDashInFakePhase && !hasSkippedFirstMarkDashInFakePhase)
        {
            hasSkippedFirstMarkDashInFakePhase = true;
            return PickNormalMarkDashVariant();
        }

        bool fakeMode = testingFakeALot || Random.value < fakeModeChance;
        if (!fakeMode)
            return PickNormalMarkDashVariant();

        switch (Random.Range(0, 4))
        {
            case 0: return MarkDashVariant.NormalVertical;
            case 1: return MarkDashVariant.NormalHorizontal;
            case 2: return MarkDashVariant.FakeVertical;
            default: return MarkDashVariant.FakeHorizontal;
        }
    }

    private MarkDashVariant PickNormalMarkDashVariant()
    {
        return Random.value < 0.5f
            ? MarkDashVariant.NormalHorizontal
            : MarkDashVariant.NormalVertical;
    }

    private bool CanUseFakeMarkDash()
    {
        return enableFakeMarkDash && ((int)currentPhase + 1) >= fakeMarkStartPhase;
    }

    private bool IsMarkDashDisplayHorizontal(MarkDashVariant variant)
    {
        return variant == MarkDashVariant.NormalHorizontal ||
               variant == MarkDashVariant.FakeHorizontal;
    }

    private bool IsFakeMarkDashVariant(MarkDashVariant variant)
    {
        return variant == MarkDashVariant.FakeVertical ||
               variant == MarkDashVariant.FakeHorizontal;
    }

    private Vector2Int PickMarkDashDirection(bool actualHorizontal)
    {
        if (actualHorizontal)
            return Random.value < 0.5f ? Vector2Int.right : Vector2Int.left;

        return Random.value < 0.5f ? Vector2Int.up : Vector2Int.down;
    }

    private void SpawnMarkDashVfx(Vector2Int playerCell, bool displayHorizontal, bool isFake)
    {
        GameObject markVfxPrefab = ResolveMarkDashVfxPrefab(isFake);
        if (markVfxPrefab == null)
            return;

        float rot = displayHorizontal ? markAtkHorizontalRotOffset : markAtkVerticalRotOffset;
        GameObject vfxGo = Instantiate(
            markVfxPrefab,
            caster.CellToWorld(playerCell),
            Quaternion.Euler(0f, 0f, rot)
        );
        ConfigureMarkAtkVfx(vfxGo);
        Destroy(vfxGo, markFlashTime);
    }

    private GameObject ResolveMarkDashVfxPrefab(bool isFake)
    {
        if (!CanUseFakeMarkDash())
            return markAtkVFXPrefab;

        if (isFake && fakeMarkVfxPrefab != null)
            return fakeMarkVfxPrefab;

        if (normalMarkVfxPrefab != null)
            return normalMarkVfxPrefab;

        return markAtkVFXPrefab;
    }

    private IEnumerator LandingSlam()
    {
        Vector2Int target = GetPlayerOffsetCell();

        bool big = currentPhase == BossPhase.Phase3 || currentPhase == BossPhase.Final;

        List<Vector2Int> cells = big
            ? caster.HollowCorner5x5(target)
            : caster.NormalScratch3(target);

        // 1단계: WarningTile만 생성 + 원형 파동 (DamageTile 아직 생성 안 함)
        yield return caster.CastWarningOnly(cells, landingWarningTime, fillOriginCell: target);

        // 2단계: 보스 등장 + 낙하 (순서 보장 — 낙하 완료 전까지 DamageTile 미생성)
        bossCell = target;
        SyncBossRootToCell();
        SetBossVisible(true);
        SetBossSpritePoseOffset(0f);
        animDriver?.PlayAppear(bossFacing);
        PlaySfx(bossAppearSfx);
        yield return DropBossSprite(slamDropHeight, slamDropDuration);

        // 3단계: 착지 순간 DamageTile 생성(시각 숨김) + SlamVFX 동시
        StartCoroutine(caster.SpawnDamageCells(cells, landingDamageTime, hideVisual: true));
        if (slamVFXPrefab != null)
            SpawnSlamVfx(target);

        // 4단계: 판정 지속
        yield return new WaitForSeconds(landingDamageTime);

        // 5단계: 착지 후 포효 + IDLE 복귀
        animDriver?.PlayScream(bossFacing);
        PlaySfx(bossSlamSfx);
        yield return new WaitForSeconds(postSlamRoarDuration);
        animDriver?.PlayIdle(bossFacing);

        yield return new WaitForSeconds(afterLandingDelay);
    }

    private IEnumerator FinalPhaseRoutine()
    {
        // PatternEntryDash 이후 실행되는 파이널 고정 패턴.
        SetBossVisible(false);

        yield return new WaitForSeconds(0.45f);

        for (int i = 0; i < 4 && bossHealth.currentHp > 0; i++)
        {
            Vector2Int slamTarget = GetPlayerOffsetCell();

            yield return caster.CastCellsWithBeforeDamage(
                caster.HollowCorner5x5(slamTarget),
                finalSlamWarningTime,
                finalDamageTime,
                beforeDamage: () =>
                {
                    bossCell = slamTarget;
                    SyncBossRootToCell();
                    SetBossVisible(true);
                    SetBossSpritePoseOffset(0f);
                    animDriver?.PlayAppear(bossFacing);
                    PlaySfx(bossAppearSfx);
                    if (slamVFXPrefab != null)
                        SpawnSlamVfx(slamTarget);
                },
                fillOriginCell: slamTarget,
                hideDamageTileVisual: true
            );

            yield return new WaitForSeconds(GetFinalSlamToDashDelay());

            // dash 단계 — 옵션에 따라 분기
            if (finalSlamUseRandomNoWarningDash)
                yield return FinalRandomNoWarningDash(slamTarget);
            else
                yield return FinalOriginalDashAfterSlam(slamTarget);

            yield return new WaitForSeconds(GetAfterFinalDashDelay());
            SetBossVisible(false);
        }

        SetBossVisible(false);
        currentPhase = BossPhase.Phase3;
    }

    private IEnumerator CastCenteredDashPattern(
        List<Vector2Int> damageCells,
        Vector2Int dashDir,
        float warningTime = -1f,
        float damageTime = -1f,
        bool hideAfter = true
    )
    {
        Vector2Int normalizedDir = caster.NormalizeDirection(dashDir);
        Vector2Int start = GetCenteredDashStart(damageCells, normalizedDir);
        Vector2Int end = GetCenteredDashEnd(start, normalizedDir);

        yield return CastDashPattern(
            damageCells,
            normalizedDir,
            warningTime,
            damageTime,
            hideAfter,
            explicitStart: start,
            explicitEnd: end
        );
    }

    private IEnumerator CastDashPattern(
        List<Vector2Int> damageCells,
        Vector2Int dashDir,
        float warningTime = -1f,
        float damageTime = -1f,
        bool hideAfter = true,
        Vector2Int? explicitStart = null,
        Vector2Int? explicitEnd = null,
        DashVisualProfile profile = DashVisualProfile.Full
    )
    {
        warningTime = GetDashPatternWarningTime(warningTime);

        if (damageTime < 0f)
            damageTime = caster.defaultDamageTime;

        Vector2Int normalizedDir = caster.NormalizeDirection(dashDir);
        Vector2Int start = explicitStart.HasValue
            ? explicitStart.Value
            : caster.GetPatternDashStart(damageCells, normalizedDir);

        Vector2Int end = explicitEnd.HasValue
            ? explicitEnd.Value
            : caster.GetPatternDashEnd(damageCells, normalizedDir);

        SetDiagnosticDashContext(start, end, damageCells);
        yield return caster.CastCellsWithBeforeDamage(
            damageCells,
            warningTime,
            damageTime,
            beforeDamage: () =>
            {
                if (playDashVisualOnPatternDamage)
                {
                    StartCoroutine(DashBossAlongArenaLine(
                        start,
                        end,
                        normalizedDir,
                        Mathf.Min(dashVisualTime, damageTime),
                        hideAfter
                    ));
                }
                ApplyDashVisuals(damageCells, start, end, profile);
            },
            fillOriginCell: start,
            hideDamageTileVisual: true,
            fillDirection: normalizedDir
        );
        ClearDiagnosticDashContext();

        bossCell = ClampCell(end);

        if (hideAfter)
            SetBossVisible(false);
    }

    private IEnumerator CastDashDamageOnly(
        List<Vector2Int> damageCells,
        Vector2Int dashDir,
        float damageTime,
        bool hideAfter = true,
        DashVisualProfile profile = DashVisualProfile.Full
    )
    {
        Vector2Int normalizedDir = caster.NormalizeDirection(dashDir);
        Vector2Int start = caster.GetPatternDashStart(damageCells, normalizedDir);
        Vector2Int end = caster.GetPatternDashEnd(damageCells, normalizedDir);

        SetDiagnosticDashContext(start, end, damageCells);
        if (playDashVisualOnPatternDamage)
        {
            StartCoroutine(DashBossAlongArenaLine(
                start,
                end,
                normalizedDir,
                Mathf.Min(dashVisualTime, damageTime),
                hideAfter
            ));
        }
        ApplyDashVisuals(damageCells, start, end, profile);

        yield return caster.CastDamageOnly(damageCells, damageTime, hideDamageTileVisual: true);
        ClearDiagnosticDashContext();

        bossCell = ClampCell(end);

        if (hideAfter)
            SetBossVisible(false);
    }

    private IEnumerator DashBossAlongArenaLine(
        Vector2Int startCell,
        Vector2Int endCell,
        Vector2Int dashDir,
        float duration,
        bool hideAfter
    )
    {
        diagnosticDashInProgress = true;
        diagnosticDashStartCell = startCell;
        diagnosticDashEndCell = endCell;
        diagnosticDashCurrentCell = startCell;
        duration = Mathf.Max(0.01f, duration);

        bossCell = ClampCell(startCell);
        SyncBossRootToCell();
        Vector2Int cardinalFacing = GetCardinalFacingFromDashDirection(dashDir);
        SetBossSpritePoseOffset(cardinalFacing.x != 0 ? bossSpriteLRMoveOffsetY : 0f);
        SetBossFacing(cardinalFacing);
        SetBossVisible(true);
        animDriver?.PlayMove(cardinalFacing);

        // 대각 돌진이면 추가 회전 적용
        if (dashDir.x != 0 && dashDir.y != 0)
        {
            float extraRot = GetDiagonalExtraRotation(dashDir);
            animDriver?.SetExtraRotation(extraRot);
        }

        Vector3 start = caster.CellToWorld(startCell);
        Vector3 end = caster.CellToWorld(endCell);
        start.z = transform.position.z;
        end.z = transform.position.z;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(start, end, t);
            if (GridManager.Instance != null)
                diagnosticDashCurrentCell = GridManager.Instance.WorldToCell(transform.position);

            if (bossSpriteTransform != null)
                bossSpriteTransform.localPosition = BossSpriteRestPos;

            yield return null;
        }

        transform.position = end;
        bossCell = ClampCell(endCell);
        diagnosticDashCurrentCell = endCell;

        if (bossSpriteTransform != null)
            bossSpriteTransform.localPosition = BossSpriteRestPos;

        // 대각 회전 복구
        animDriver?.ClearExtraRotation();

        if (hideAfter)
            SetBossVisible(false);
        diagnosticDashInProgress = false;
        diagnosticDashLaneCells.Clear();
    }

    private void SetDiagnosticDashContext(Vector2Int startCell, Vector2Int endCell, List<Vector2Int> laneCells)
    {
        diagnosticDashStartCell = startCell;
        diagnosticDashEndCell = endCell;
        diagnosticDashCurrentCell = startCell;
        diagnosticDashLaneCells.Clear();

        if (laneCells == null)
            return;

        foreach (Vector2Int cell in laneCells)
        {
            if (!diagnosticDashLaneCells.Contains(cell))
                diagnosticDashLaneCells.Add(cell);
        }
    }

    private void ClearDiagnosticDashContext()
    {
        if (!diagnosticDashInProgress)
            diagnosticDashLaneCells.Clear();
    }

    private float GetDiagonalExtraRotation(Vector2Int dir)
    {
        // (±1, ∓1) → ±45도 기준 매핑
        if (dir.x > 0 && dir.y < 0) return 45f;   // TR→BL: MOVE_DOWN + 45
        if (dir.x < 0 && dir.y < 0) return -45f;  // TL→BR: MOVE_DOWN - 45
        if (dir.x > 0 && dir.y > 0) return -45f;  // BL→TR: MOVE_UP - 45
        if (dir.x < 0 && dir.y > 0) return 45f;   // BR→TL: MOVE_UP + 45
        return 0f;
    }

    private Vector2Int GetCardinalFacingFromDashDirection(Vector2Int dir)
    {
        if (dir == Vector2Int.zero)
            return bossFacing;

        if (dir.x != 0 && dir.y != 0)
            return new Vector2Int(0, dir.y > 0 ? 1 : -1);

        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            return new Vector2Int(dir.x > 0 ? 1 : -1, 0);

        return new Vector2Int(0, dir.y > 0 ? 1 : -1);
    }

    private Vector2Int GetCenteredDashStart(List<Vector2Int> damageCells, Vector2Int dashDir)
    {
        dashDir = caster.NormalizeDirection(dashDir);

        if (damageCells == null || damageCells.Count == 0)
            return Vector2Int.zero;

        float avgX = 0f;
        float avgY = 0f;
        int count = 0;

        foreach (Vector2Int cell in damageCells)
        {
            if (!caster.Inside(cell))
                continue;

            avgX += cell.x;
            avgY += cell.y;
            count++;
        }

        if (count <= 0)
            return Vector2Int.zero;

        avgX /= count;
        avgY /= count;

        int centerX = Mathf.Clamp(Mathf.RoundToInt(avgX), Min, Max);
        int centerY = Mathf.Clamp(Mathf.RoundToInt(avgY), Min, Max);

        if (dashDir.x > 0)
            return new Vector2Int(Min, centerY);

        if (dashDir.x < 0)
            return new Vector2Int(Max, centerY);

        if (dashDir.y > 0)
            return new Vector2Int(centerX, Min);

        if (dashDir.y < 0)
            return new Vector2Int(centerX, Max);

        return new Vector2Int(centerX, centerY);
    }

    private Vector2Int GetCenteredDashEnd(Vector2Int start, Vector2Int dashDir)
    {
        dashDir = caster.NormalizeDirection(dashDir);

        if (dashDir.x > 0)
            return new Vector2Int(Max, start.y);

        if (dashDir.x < 0)
            return new Vector2Int(Min, start.y);

        if (dashDir.y > 0)
            return new Vector2Int(start.x, Max);

        if (dashDir.y < 0)
            return new Vector2Int(start.x, Min);

        return start;
    }

    private IEnumerator IntroRoutine()
    {
        if (skipIntroForTraining)
        {
            ApplyTrainingIntroSkipState();
            yield break;
        }

        CachePlayerSceneRefs();
        SetPlayerControlEnabled(false);
        SetBossVisible(false);
        SetBossHudVisible(false);
        BossBattleMusicRuntime.Instance?.BeginIntroMusic();

        if (cutsceneOverlay == null)
            cutsceneOverlay = ClickAdvanceCutsceneOverlay.EnsureInScene();

        CutsceneFreezeManager.Lock();   // 인트로 대사 중 적 동결

        if (introDialogueLines != null && introDialogueLines.Length > 0)
            yield return cutsceneOverlay.ShowLine(introDialogueLines[0], cutsceneAdvanceDelay);

        if (introDialogueLines != null && introDialogueLines.Length > 1)
        {
            PlayIntroNoise();
            for (int i = 1; i < introDialogueLines.Length; i++)
                yield return cutsceneOverlay.ShowLine(introDialogueLines[i], cutsceneAdvanceDelay);
        }

        CutsceneFreezeManager.Unlock(); // 보스 등장 전 동결 해제

        yield return MovePlayerOneCellForIntro();

        bossCell = new Vector2Int(0, 1);
        SyncBossRootToCell();
        SetBossVisible(true);
        SetBossSpritePoseOffset(0f);
        animDriver?.PlayAppear(bossFacing);
        PlaySfx(bossAppearSfx);
        yield return DropBossSprite(slamDropHeight, slamDropDuration);
        PlaySfx(bossSlamSfx);

        animDriver?.PlayScream(bossFacing);
        PlaySfx(bossIntroScreamSfx != null ? bossIntroScreamSfx : bossScreamSfx);
        yield return new WaitForSeconds(introWaitSeconds);

        BossBattleMusicRuntime.Instance?.BeginBattleMusic();
        cutsceneOverlay.HideDialogue();
        SetBossHudVisible(true);
        SetPlayerControlEnabled(true);
        animDriver?.PlayIdle(bossFacing);

        if (postIntroBattleGraceTime > 0f)
            yield return new WaitForSeconds(postIntroBattleGraceTime);
    }

    private void LateUpdate()
    {
        // 첫 프레임에 Animator가 스프라이트를 설정한 뒤 발 기준 오프셋 자동 감지
        if (!spritePivotAdjusted && bossSpriteYOffset == 0f && bossSpriteTransform != null)
        {
            var sr = bossSpriteTransform.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                bossSpriteYOffset = sr.sprite.bounds.extents.y;
                bossSpriteTransform.localPosition = BossSpriteRestPos;
                spritePivotAdjusted = true;
            }
        }
    }

    private void OnBossDead()
    {
        if (deathStarted)
            return;

        battleTimerRunning = false;
        RunRouteTracker.SetBossKilled();
        RunRouteTracker.DecideEnding();
        deathStarted = true;
        StopAllCoroutines();
        SetBossVisible(true);
        bossHealth.SetDamageable(false);
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator MoveBossRootToCell(Vector2Int targetCell, float duration)
    {
        SetBossVisible(true);

        Vector3 start = transform.position;
        Vector3 end = caster.CellToWorld(targetCell);
        end.z = start.z;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            transform.position = Vector3.Lerp(start, end, t);

            if (bossSpriteTransform != null)
                bossSpriteTransform.localPosition = BossSpriteRestPos;

            yield return null;
        }

        transform.position = end;

        if (bossSpriteTransform != null)
            bossSpriteTransform.localPosition = BossSpriteRestPos;
    }

    private void SyncBossRootToCell()
    {
        if (caster == null)
            return;

        Vector3 pos = caster.CellToWorld(bossCell);
        pos.z = transform.position.z;

        transform.position = pos;

        if (bossSpriteTransform != null)
            bossSpriteTransform.localPosition = BossSpriteRestPos;

        ApplyBossFacingVisual();
    }

    private void SetBossVisible(bool visible)
    {
        if (bossSpriteTransform != null)
            bossSpriteTransform.gameObject.SetActive(visible);

        // 중요:
        // 딜타임/취약시간/3대 제한 없음.
        // 표시 여부와 BossHealth 피격 가능 여부를 묶지 않는다.
        // 전투 중에는 BossHealth의 피격 가능 상태를 끄지 않는다.
    }

    private void SetBossFacing(Vector2Int dir)
    {
        dir = NormalizeDirection(dir);

        if (dir == Vector2Int.zero)
            return;

        bossFacing = dir;

        if (bossSpriteTransform != null)
            bossSpriteTransform.localPosition = BossSpriteRestPos;

        ApplyBossFacingVisual();
    }

    private void SetBossSpritePoseOffset(float offsetYInCells)
    {
        bossSpritePoseOffsetY = offsetYInCells;

        if (bossSpriteTransform != null)
            bossSpriteTransform.localPosition = BossSpriteRestPos;
    }

    private float GetAttackPoseOffsetForFacing()
    {
        if (bossFacing.x != 0)
            return bossSpriteHorizontalAttackExtraOffsetY;

        if (bossFacing.y > 0)
            return bossSpriteBackAttackExtraOffsetY;

        return 0f;
    }

    private void FaceDashDirection(Vector2Int dashDir)
    {
        Vector2Int facing = GetCardinalFacingFromDashDirection(dashDir);
        SetBossSpritePoseOffset(0f);
        SetBossFacing(facing);
        SetBossVisible(true);
        animDriver?.PlayIdle(facing);
    }

    private void ApplyBossFacingVisual()
    {
        if (!rotateBossVisualByFacing || bossSpriteTransform == null)
            return;

        float angle = FacingToAngle(bossFacing) + visualRotationOffsetDegrees;
        bossSpriteTransform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private float FacingToAngle(Vector2Int dir)
    {
        dir = NormalizeDirection(dir);

        if (dir == Vector2Int.up)
            return 0f;

        if (dir == Vector2Int.right)
            return -90f;

        if (dir == Vector2Int.down)
            return 180f;

        if (dir == Vector2Int.left)
            return 90f;

        return 0f;
    }

    private Vector2Int NormalizeDirection(Vector2Int dir)
    {
        if (dir == Vector2Int.zero)
            return Vector2Int.zero;

        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            return new Vector2Int(dir.x > 0 ? 1 : -1, 0);

        return new Vector2Int(0, dir.y > 0 ? 1 : -1);
    }

    private Vector2Int ClampCell(Vector2Int cell)
    {
        return new Vector2Int(
            Mathf.Clamp(cell.x, Min, Max),
            Mathf.Clamp(cell.y, Min, Max)
        );
    }

    private void CachePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        // Tag 복구가 꼬였을 때도 PlayerHealth 기준으로 한 번 더 찾는다.
        if (player == null)
        {
            PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth != null)
                player = playerHealth.gameObject;
        }

        if (player == null)
        {
            playerTransform = null;
            playerOccupant = null;
            return;
        }

        playerTransform = player.transform;
        playerOccupant = player.GetComponent<GridOccupant>();
    }

    private Vector2Int GetPlayerOffsetCell()
    {
        if (playerTransform == null)
            CachePlayer();

        if (playerTransform == null || caster == null || caster.patternOrigin == null || GridManager.Instance == null)
            return Vector2Int.zero;

        Vector2Int originCell = GridManager.Instance.WorldToCell(caster.patternOrigin.position);

        // 보스전에서는 GridOccupant.CurrentCell이 복구/자동생성 과정에서 어긋날 수 있으므로
        // 실제 Transform 위치를 우선 기준으로 삼는다.
        Vector2Int playerWorldCell = GridManager.Instance.WorldToCell(playerTransform.position);
        Vector2Int offset = playerWorldCell - originCell;

        return new Vector2Int(
            Mathf.Clamp(offset.x, Min, Max),
            Mathf.Clamp(offset.y, Min, Max)
        );
    }

    // ───────────────────────────────────────────────────────────
    // 헬퍼: ScreamRoutine
    // ───────────────────────────────────────────────────────────

    private IEnumerator ScreamRoutine()
    {
        SetBossVisible(true);
        SetBossSpritePoseOffset(0f);
        animDriver?.PlayScream(bossFacing);
        PlaySfx(bossScreamSfx);
        yield return new WaitForSeconds(screamDuration);
    }

    // ───────────────────────────────────────────────────────────
    // 헬퍼: DeathRoutine
    // ───────────────────────────────────────────────────────────

    private IEnumerator DeathRoutine()
    {
        SetPlayerControlEnabled(false);
        SetBossHudVisible(false);
        SetBossVisible(true);
        SetBossSpritePoseOffset(0f);
        animDriver?.ClearExtraRotation();
        animDriver?.ClearEnhancedAttack();
        animDriver?.PlayDie(bossFacing);
        PlaySfx(bossDeathSfx);
        if (bossScreamSfx != null && bossScreamSfx != bossDeathSfx)
            PlaySfx(bossScreamSfx, deathScreamVolumeScale);
        yield return new WaitForSeconds(bossDeathAnimDuration);

        if (cutsceneOverlay == null)
            cutsceneOverlay = ClickAdvanceCutsceneOverlay.EnsureInScene();

        CutsceneFreezeManager.Lock();   // 사망 후 대사 중 동결

        yield return cutsceneOverlay.ShowLines(postBossDialogueLines, cutsceneAdvanceDelay);
        cutsceneOverlay.HideDialogue();

        CutsceneFreezeManager.Unlock(); // 대사 종료 후 복원

        waitingForPostBossExit = true;
        SetPlayerControlEnabled(true);
    }

    private IEnumerator LoadNextSceneAfterNonKilledBossExitDialogue()
    {
        SetPlayerControlEnabled(false);

        if (cutsceneOverlay == null)
            cutsceneOverlay = ClickAdvanceCutsceneOverlay.EnsureInScene();

        DialogueLine[] lines = GetNonKilledBossExitDialogueLines();
        if (lines != null && lines.Length > 0)
        {
            CutsceneFreezeManager.Lock();
            yield return cutsceneOverlay.ShowLines(lines, cutsceneAdvanceDelay);
            cutsceneOverlay.HideDialogue();
            CutsceneFreezeManager.Unlock();
        }

        RunRouteTracker.DecideEnding();
        CutsceneFreezeManager.ForceUnlock();
        SceneManager.LoadScene(nextSceneName);
    }

    private DialogueLine[] GetNonKilledBossExitDialogueLines()
    {
        if (RunRouteTracker.GetEndingRoute() == EndingRoute.Pacifist)
        {
            return HasUsableDialogue(pacifistPostBossExitDialogueLines)
                ? pacifistPostBossExitDialogueLines
                : DefaultPacifistPostBossExitDialogueLines;
        }

        return HasUsableDialogue(normalPostBossExitDialogueLines)
            ? normalPostBossExitDialogueLines
            : DefaultNormalPostBossExitDialogueLines;
    }

    private static bool HasUsableDialogue(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0)
            return false;

        for (int i = 0; i < lines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i].body))
                return true;
        }

        return false;
    }

    // ───────────────────────────────────────────────────────────
    // 헬퍼: DropBossSprite (LandingSlam 낙하 연출)
    // ───────────────────────────────────────────────────────────

    private IEnumerator DropBossSprite(float height, float duration)
    {
        if (bossSpriteTransform == null)
            yield break;

        Vector3 dropStart = BossSpriteRestPos + Vector3.up * height;
        bossSpriteTransform.localPosition = dropStart;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            bossSpriteTransform.localPosition = Vector3.Lerp(
                dropStart,
                BossSpriteRestPos,
                Mathf.Clamp01(elapsed / duration)
            );
            yield return null;
        }

        bossSpriteTransform.localPosition = BossSpriteRestPos;
    }

    private void SpawnSlamVfx(Vector2Int cell)
    {
        if (slamVFXPrefab == null)
            return;

        GameObject go = Instantiate(slamVFXPrefab, caster.CellToWorld(cell) + slamVfxWorldOffset, Quaternion.identity);
        Destroy(go, 1.05f);
    }

    // ───────────────────────────────────────────────────────────
    // 헬퍼: SpawnScratchVFX (돌진 전용 — NormalScratch/EnhancedScratch 제외)
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// dashDir 방향에 따라 SCRATCH VFX를 회전+스케일해서 공격 범위를 꽉 채웁니다.
    /// 스프라이트는 가로(X) 방향이 기본이라고 가정합니다.
    /// </summary>
    private void SpawnScratchVFX(
        List<Vector2Int> damageCells,
        Vector2Int dashDir,
        Vector2Int startCell,
        Vector2Int endCell)
    {
        if (scratchVFXPrefab == null || damageCells == null || damageCells.Count == 0)
            return;

        Vector3 minWorld = caster.CellToWorld(damageCells[0]);
        Vector3 maxWorld = minWorld;
        foreach (var c in damageCells)
        {
            Vector3 w = caster.CellToWorld(c);
            minWorld = Vector3.Min(minWorld, w);
            maxWorld = Vector3.Max(maxWorld, w);
        }

        bool isVertical = dashDir.x == 0 && dashDir.y != 0;
        bool isDiagonal = dashDir.x != 0 && dashDir.y != 0;

        float zRot;
        Vector3 center;
        float lengthWorld;
        float widthWorld;

        if (isDiagonal)
        {
            Vector3 startWorld = caster.CellToWorld(startCell);
            Vector3 endWorld = caster.CellToWorld(endCell);
            center = (startWorld + endWorld) * 0.5f;

            Vector2 dashAxis = (Vector2)(endWorld - startWorld);
            zRot = Mathf.Atan2(dashAxis.y, dashAxis.x) * Mathf.Rad2Deg - 90f + scratchRotationOffsetDegrees;
            lengthWorld = dashAxis.magnitude + caster.CellSize + scratchDiagonalLengthPadding;
            widthWorld = scratchDiagonalWidth;
        }
        else
        {
            center = (minWorld + maxWorld) * 0.5f;
            zRot = isVertical ? scratchRotationOffsetDegrees : -90f + scratchRotationOffsetDegrees;

            if (isVertical)
            {
                lengthWorld = (maxWorld.y - minWorld.y) + caster.CellSize + scratchCardinalLengthPadding;
                widthWorld = (maxWorld.x - minWorld.x) + caster.CellSize + scratchCardinalWidthPadding;
            }
            else
            {
                lengthWorld = (maxWorld.x - minWorld.x) + caster.CellSize + scratchCardinalLengthPadding;
                widthWorld = (maxWorld.y - minWorld.y) + caster.CellSize + scratchCardinalWidthPadding;
            }
        }

        EnsureDashVfxArenaMask();

        GameObject go = Instantiate(scratchVFXPrefab, center, Quaternion.Euler(0f, 0f, zRot));

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            Vector2 spriteBounds = sr.sprite.bounds.size;
            go.transform.localScale = new Vector3(
                (widthWorld / Mathf.Max(0.001f, spriteBounds.x)) * scratchScaleMultiplier.x,
                (lengthWorld / Mathf.Max(0.001f, spriteBounds.y)) * scratchScaleMultiplier.y,
                1f
            );

            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, scratchAlpha);
            sr.maskInteraction = clipDashVfxInsideArena && dashVfxArenaMask != null
                ? SpriteMaskInteraction.VisibleInsideMask
                : SpriteMaskInteraction.None;
        }
        else
        {
            go.transform.localScale = new Vector3(
                widthWorld * scratchScaleMultiplier.x,
                lengthWorld * scratchScaleMultiplier.y,
                1f
            );
        }

        Destroy(go, 0.55f);
    }

    // helper: SpawnDashSmokeRoutine (순차 생성, 강화할퀴기 1칸 돌진 제외)
    // ───────────────────────────────────────────────────────────

    private IEnumerator SpawnDashSmokeRoutine(Vector2Int startCell, Vector2Int endCell, int count = 3)
    {
        if (dashSmokeVFXPrefab == null || count <= 0)
            yield break;

        for (int i = 0; i < count; i++)
        {
            float t = (i + 0.5f) / count;
            Vector3 pos = Vector3.Lerp(
                caster.CellToWorld(startCell),
                caster.CellToWorld(endCell),
                t
            );
            GameObject go = Instantiate(dashSmokeVFXPrefab, pos, Quaternion.identity);
            go.transform.localScale = Vector3.one * dashSmokeScale;

            EnsureDashVfxArenaMask();
            bool hasParticles = ConfigureSpawnedVfx(go, "Hazard", 18, dashSmokeAlpha);
            if (hasParticles)
                Destroy(go, dashSmokeLifetime);
            else
                StartCoroutine(FadeOutAndDestroy(go, dashSmokeFadeDuration));

            if (i < count - 1)
                yield return new WaitForSeconds(dashSmokeSpawnInterval);
        }
    }

    // ───────────────────────────────────────────────────────────
    // 헬퍼: ApplyDashVisuals (프로파일 기반 돌진 비주얼/SFX 통합)
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Full        = SCRATCH + Smoke(거리기반) + Whoosh + DashScream
    /// InternalQuiet = SCRATCH + Whoosh만 (Smoke·DashScream 없음)
    /// </summary>
    private void ApplyDashVisuals(
        List<Vector2Int> damageCells,
        Vector2Int startCell,
        Vector2Int endCell,
        DashVisualProfile profile)
    {
        // dashDir 정규화 (대각선 포함)
        Vector2Int delta = endCell - startCell;
        Vector2Int dashDir = new Vector2Int(
            delta.x == 0 ? 0 : (delta.x > 0 ? 1 : -1),
            delta.y == 0 ? 0 : (delta.y > 0 ? 1 : -1)
        );

        // 항상 재생: SCRATCH + Whoosh
        SpawnScratchVFX(damageCells, dashDir, startCell, endCell);
        PlaySfx(bossDashWhooshSfx);

        bool intenseDash = IsPhase3OrLater();

        if (intenseDash)
        {
            if (profile == DashVisualProfile.Full)
                PlayRandomDashVoice();

            TriggerPhaseDangerOverlayPulse();
        }

        // Full 프로파일: Smoke 추가
        if (profile == DashVisualProfile.Full)
        {
            int dist = Mathf.Abs(endCell.x - startCell.x) + Mathf.Abs(endCell.y - startCell.y);
            int smokeCount = dist <= 1 ? 1 : 3;
            StartCoroutine(SpawnDashSmokeRoutine(startCell, endCell, smokeCount));
        }
    }

    private void ConfigureMarkAtkVfx(GameObject vfxGo)
    {
        if (vfxGo == null)
            return;

        EnsureMarkAtkArenaMask();

        foreach (var sr in vfxGo.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.sortingLayerName = "Items";
            sr.sortingOrder = 20;
            sr.maskInteraction = useMarkAtkArenaMask
                ? SpriteMaskInteraction.VisibleInsideMask
                : SpriteMaskInteraction.None;
        }
    }

    private void EnsureDashVfxArenaMask()
    {
        if (!clipDashVfxInsideArena || caster == null || caster.patternOrigin == null || dashVfxArenaMask != null)
            return;

        var maskGo = new GameObject("DashVfxArenaMask");
        maskGo.transform.SetParent(caster.patternOrigin, false);
        maskGo.transform.localPosition = Vector3.zero;
        maskGo.transform.localScale = new Vector3(caster.CellSize * 7f, caster.CellSize * 7f, 1f);

        dashVfxArenaMask = maskGo.AddComponent<SpriteMask>();
        dashVfxArenaMask.sprite = GetRuntimeMaskSprite();
        dashVfxArenaMask.isCustomRangeActive = true;
        dashVfxArenaMask.frontSortingLayerID = SortingLayer.NameToID("Hazard");
        dashVfxArenaMask.backSortingLayerID = SortingLayer.NameToID("Hazard");
        dashVfxArenaMask.frontSortingOrder = 100;
        dashVfxArenaMask.backSortingOrder = -100;
        dashVfxArenaMask.alphaCutoff = 0.1f;
    }

    private void EnsureMarkAtkArenaMask()
    {
        if (!useMarkAtkArenaMask || caster == null || caster.patternOrigin == null || markAtkArenaMask != null)
            return;

        var maskGo = new GameObject("MarkAtkArenaMask");
        maskGo.transform.SetParent(caster.patternOrigin, false);
        maskGo.transform.localPosition = Vector3.zero;
        maskGo.transform.localScale = new Vector3(caster.CellSize * 7f, caster.CellSize * 7f, 1f);

        markAtkArenaMask = maskGo.AddComponent<SpriteMask>();
        markAtkArenaMask.sprite = GetRuntimeMaskSprite();
        markAtkArenaMask.isCustomRangeActive = true;
        markAtkArenaMask.frontSortingLayerID = SortingLayer.NameToID("Items");
        markAtkArenaMask.backSortingLayerID = SortingLayer.NameToID("Items");
        markAtkArenaMask.frontSortingOrder = Mathf.Max(markAtkMaskSortingOrder, 100);
        markAtkArenaMask.backSortingOrder = -100;
        markAtkArenaMask.alphaCutoff = 0.1f;
    }

    private static Sprite GetRuntimeMaskSprite()
    {
        if (runtimeMaskSprite != null)
            return runtimeMaskSprite;

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        runtimeMaskSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return runtimeMaskSprite;
    }

    private bool IsPhase3OrLater()
    {
        return currentPhase == BossPhase.Phase3 || currentPhase == BossPhase.Final || phase3Triggered || finalTriggered;
    }

    private void EnsurePhaseDangerOverlay()
    {
        if (!usePhaseDangerOverlay || phaseDangerOverlayImage != null)
            return;

        Canvas canvas = ResolveUiCanvas();
        if (canvas == null)
            return;

        var root = new GameObject("PhaseDangerOverlay", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        phaseDangerOverlayImage = root.GetComponent<Image>();
        phaseDangerOverlayImage.sprite = GetPhaseDangerOverlaySprite(canvas);
        phaseDangerOverlayImage.type = Image.Type.Simple;
        phaseDangerOverlayImage.preserveAspect = false;
        phaseDangerOverlayImage.raycastTarget = false;
        phaseDangerOverlayImage.color = new Color(0.75f, 0.04f, 0.04f, 0f);
    }

    private Canvas ResolveUiCanvas()
    {
        GameObject bossHpPanel = GameObject.Find("BossHPPanel");
        if (bossHpPanel != null)
        {
            Canvas panelCanvas = bossHpPanel.GetComponentInParent<Canvas>();
            if (panelCanvas != null)
                return panelCanvas;
        }

        return FindFirstObjectByType<Canvas>();
    }

    private void SetBossHudVisible(bool visible)
    {
        CacheBossHudRoot();

        if (bossHudRoot != null && bossHudRoot.activeSelf != visible)
            bossHudRoot.SetActive(visible);
    }

    private void CacheBossHudRoot()
    {
        if (bossHudRoot != null)
            return;

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform transform in transforms)
        {
            if (transform != null && transform.name == "BossHPPanel")
            {
                bossHudRoot = transform.gameObject;
                return;
            }
        }

        BossHpBar[] hpBars = FindObjectsByType<BossHpBar>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (hpBars == null || hpBars.Length == 0 || hpBars[0] == null)
            return;

        Transform root = hpBars[0].transform;
        while (root.parent != null && root.parent.GetComponent<Canvas>() == null)
            root = root.parent;

        bossHudRoot = root.gameObject;
    }

    private Sprite GetPhaseDangerOverlaySprite(Canvas canvas)
    {
        if (runtimePhaseDangerOverlaySprite != null)
            return runtimePhaseDangerOverlaySprite;

        Rect pixelRect = canvas != null ? canvas.pixelRect : new Rect(0f, 0f, Screen.width, Screen.height);
        float screenWidth = Mathf.Max(1f, pixelRect.width);
        float screenHeight = Mathf.Max(1f, pixelRect.height);
        int textureWidth = Mathf.Clamp(Mathf.RoundToInt(screenWidth / 4f), 96, 512);
        int textureHeight = Mathf.Clamp(Mathf.RoundToInt(screenHeight / 4f), 64, 384);
        float textureScaleX = textureWidth / screenWidth;
        float textureScaleY = textureHeight / screenHeight;
        float edgeThicknessX = Mathf.Max(1f, phaseDangerEdgeThickness * textureScaleX);
        float edgeThicknessY = Mathf.Max(1f, phaseDangerEdgeThickness * textureScaleY);

        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                float distanceX = Mathf.Min(x + 0.5f, textureWidth - x - 0.5f);
                float distanceY = Mathf.Min(y + 0.5f, textureHeight - y - 0.5f);
                float alphaX = Mathf.Clamp01(1f - distanceX / edgeThicknessX);
                float alphaY = Mathf.Clamp01(1f - distanceY / edgeThicknessY);
                float alpha = Mathf.Max(alphaX, alphaY);
                alpha = Mathf.SmoothStep(0f, 1f, alpha);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, true);
        runtimePhaseDangerOverlaySprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, textureWidth, textureHeight),
            new Vector2(0.5f, 0.5f),
            textureWidth
        );
        runtimePhaseDangerOverlaySprite.name = "RuntimePhaseDangerOverlay";
        return runtimePhaseDangerOverlaySprite;
    }

    private void UpdatePhaseDangerOverlayImmediate()
    {
        if (phaseDangerOverlayImage == null)
            return;

        float alpha = IsPhase3OrLater() ? phaseDangerBaseAlpha : 0f;
        Color color = phaseDangerOverlayImage.color;
        color.a = alpha;
        phaseDangerOverlayImage.color = color;
    }

    // ── Phase 3 진입 효과 ───────────────────────────────────────────────────────

    /// <summary>
    /// 3페이즈 최초 진입 시 1회 호출: 이름표 빨간색 + 붉은 플래시.
    /// HP 감소/불살 타이머 강제 진입 모두 동일하게 적용됩니다.
    /// </summary>
    private void FirePhase3EntryEffect()
    {
        // 보스 이름표 → 빨간색
        if (bossHpBarRef == null)
            bossHpBarRef = FindFirstObjectByType<BossHpBar>();
        bossHpBarRef?.SetPhase3NameColor();

        // 붉은 화면 플래시
        if (!usePhaseDangerOverlay || phaseDangerOverlayImage == null)
            return;

        if (phaseDangerPulseRoutine != null)
            StopCoroutine(phaseDangerPulseRoutine);

        phaseDangerPulseRoutine = StartCoroutine(Phase3EntryFlashRoutine());
    }

    private IEnumerator Phase3EntryFlashRoutine()
    {
        if (phaseDangerOverlayImage == null)
            yield break;

        float startAlpha = phase3EntryFlashAlpha;
        float endAlpha   = phaseDangerBaseAlpha;
        float duration   = Mathf.Max(0.1f, phase3EntryFlashFadeDuration);

        // 즉시 최고 알파로 점프
        Color c = phaseDangerOverlayImage.color;
        c.a = startAlpha;
        phaseDangerOverlayImage.color = c;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c   = phaseDangerOverlayImage.color;
            c.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            phaseDangerOverlayImage.color = c;
            yield return null;
        }

        phaseDangerPulseRoutine = null;
        UpdatePhaseDangerOverlayImmediate();
    }

    private void TriggerPhaseDangerOverlayPulse()
    {
        if (!usePhaseDangerOverlay || phaseDangerOverlayImage == null)
            return;

        if (phaseDangerPulseRoutine != null)
            StopCoroutine(phaseDangerPulseRoutine);

        phaseDangerPulseRoutine = StartCoroutine(PhaseDangerOverlayPulseRoutine());
    }

    private IEnumerator PhaseDangerOverlayPulseRoutine()
    {
        float duration = Mathf.Max(0.01f, phaseDangerPulseDuration);
        float elapsed = 0f;
        float baseAlpha = IsPhase3OrLater() ? phaseDangerBaseAlpha : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(phaseDangerDashAlpha, baseAlpha, t);

            Color color = phaseDangerOverlayImage.color;
            color.a = alpha;
            phaseDangerOverlayImage.color = color;

            yield return null;
        }

        phaseDangerPulseRoutine = null;
        UpdatePhaseDangerOverlayImmediate();
    }

    private IEnumerator FadeOutAndDestroy(GameObject go, float duration)
    {
        var sr = go != null ? go.GetComponent<SpriteRenderer>() : null;
        if (sr == null)
        {
            if (go != null) Destroy(go, duration);
            yield break;
        }

        float elapsed = 0f;
        Color c = sr.color;

        while (elapsed < duration && go != null)
        {
            elapsed += Time.deltaTime;
            sr.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0f, elapsed / duration));
            yield return null;
        }

        if (go != null)
            Destroy(go);
    }

    // ───────────────────────────────────────────────────────────
    // 헬퍼: PlaySfx
    // ───────────────────────────────────────────────────────────

    private bool ConfigureSpawnedVfx(GameObject go, string sortingLayerName, int sortingOrder, float alpha)
    {
        if (go == null)
            return false;

        go.SetActive(true);
        bool hasParticles = false;

        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color color = sr.color;
            color.a *= alpha;
            sr.color = color;
            sr.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
            sr.sortingOrder = sortingOrder;
            sr.maskInteraction = clipDashVfxInsideArena && dashVfxArenaMask != null
                ? SpriteMaskInteraction.VisibleInsideMask
                : SpriteMaskInteraction.None;
        }

        foreach (var particleRenderer in go.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            hasParticles = true;
            particleRenderer.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
            particleRenderer.sortingOrder = sortingOrder;
        }

        foreach (var particleSystem in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            hasParticles = true;
            var main = particleSystem.main;
            ParticleSystem.MinMaxGradient startColor = main.startColor;
            if (startColor.mode == ParticleSystemGradientMode.Color)
            {
                Color color = startColor.color;
                color.a *= alpha;
                main.startColor = color;
            }

            particleSystem.Play(true);
        }

        return hasParticles;
    }

    private void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (bossAudioSource == null || clip == null)
            return;

        bossAudioSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    private void LoadDashVoiceClips()
    {
        AudioClip[] loaded = Resources.LoadAll<AudioClip>(dashVoiceResourcesPath);
        var voices = new List<AudioClip>();

        if (loaded != null)
        {
            foreach (AudioClip clip in loaded)
            {
                if (clip == null || clip == bossDashWhooshSfx)
                    continue;

                string name = clip.name.ToLowerInvariant();
                if (name.Contains("whoosh"))
                    continue;

                if (!voices.Contains(clip))
                    voices.Add(clip);
            }
        }

        if (bossDashScreamSfx != null && bossDashScreamSfx != bossDashWhooshSfx && !voices.Contains(bossDashScreamSfx))
            voices.Add(bossDashScreamSfx);

        dashVoiceClips = voices.ToArray();
    }

    private void PlayRandomDashVoice()
    {
        if (dashVoiceClips == null || dashVoiceClips.Length == 0)
            LoadDashVoiceClips();

        if (dashVoiceClips == null || dashVoiceClips.Length == 0)
            return;

        int index = Random.Range(0, dashVoiceClips.Length);
        if (dashVoiceClips.Length > 1 && index == lastDashVoiceIndex)
            index = (index + Random.Range(1, dashVoiceClips.Length)) % dashVoiceClips.Length;

        lastDashVoiceIndex = index;
        PlaySfx(dashVoiceClips[index], GetDashScreamVolumeScale());
    }

    // ───────────────────────────────────────────────────────────
    // 헬퍼: FinalOriginalDashAfterSlam (기존 find-and-dash 로직 Extract)
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// finalSlamUseRandomNoWarningDash == false일 때 사용.
    /// 기존 FinalPhaseRoutine의 찍기 후 돌진 로직을 그대로 보존한다.
    /// </summary>
    private IEnumerator FinalOriginalDashAfterSlam(Vector2Int slamTarget)
    {
        // Final 돌진은 방금 찍은 위치 기준이다.
        // 플레이어가 찍기 후 이동해도 돌진 중심선은 slamTarget에서 다시 계산한다.
        Vector2Int dashDir = GetDashDirFromSlamToPlayer(slamTarget);
        FaceDashDirection(dashDir);
        List<Vector2Int> dashCells = caster.ForwardStripe3FromCell(slamTarget, dashDir);
        Vector2Int dashEnd = caster.GetDirectionalEdgeCell(slamTarget, dashDir);

        yield return CastDashPattern(
            dashCells,
            dashDir,
            GetFinalDashWarningTime(),
            finalDamageTime,
            hideAfter: false,
            explicitStart: slamTarget,
            explicitEnd: dashEnd,
            profile: DashVisualProfile.InternalQuiet
        );
    }

    // ───────────────────────────────────────────────────────────
    // 헬퍼: FinalRandomNoWarningDash (경고장판 없는 4방향 랜덤 돌진)
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// finalSlamUseRandomNoWarningDash == true일 때 사용.
    /// 경고장판 없이 4방향 중 하나를 랜덤 선택해 즉시 돌진한다.
    /// DamageTile 판정·SCRATCH·DashSmoke·dash SFX는 기존 규칙과 동일.
    /// </summary>
    private IEnumerator FinalRandomNoWarningDash(Vector2Int slamTarget)
    {
        Vector2Int dir = PickRandomCardinalDashDir();
        Debug.Log($"[Boss] Final random no-warning dash dir = {dir}");

        // 4방향 → 가로/세로 줄 계산
        Vector2Int normalizedDir = caster.NormalizeDirection(dir);
        FaceDashDirection(normalizedDir);
        List<Vector2Int> damageCells = caster.ForwardStripe3FromCell(slamTarget, normalizedDir);
        Vector2Int dashEnd = caster.GetDirectionalEdgeCell(slamTarget, normalizedDir);
        SetDiagnosticDashContext(slamTarget, dashEnd, damageCells);

        // 경고장판 없이 즉시 시각·판정 동시 시작
        if (playDashVisualOnPatternDamage)
        {
            StartCoroutine(DashBossAlongArenaLine(
                slamTarget,
                dashEnd,
                normalizedDir,
                Mathf.Min(dashVisualTime, finalDamageTime),
                hideAfter: false
            ));
        }

        ApplyDashVisuals(damageCells, slamTarget, dashEnd, DashVisualProfile.InternalQuiet);

        yield return caster.CastDamageOnly(damageCells, finalDamageTime, hideDamageTileVisual: true);
        ClearDiagnosticDashContext();

        bossCell = ClampCell(dashEnd);
    }

    private Vector2Int GetDashDirFromSlamToPlayer(Vector2Int slamTarget)
    {
        Vector2Int playerCell = GetPlayerOffsetCell();
        Vector2Int delta = playerCell - slamTarget;

        if (delta == Vector2Int.zero)
            return caster.NormalizeDirection(bossFacing);

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            return new Vector2Int(delta.x > 0 ? 1 : -1, 0);

        return new Vector2Int(0, delta.y > 0 ? 1 : -1);
    }

    // ───────────────────────────────────────────────────────────
    // 헬퍼: PickRandomCardinalDashDir
    // ───────────────────────────────────────────────────────────

    private Vector2Int PickRandomCardinalDashDir()
    {
        switch (Random.Range(0, 4))
        {
            case 0:  return Vector2Int.up;
            case 1:  return Vector2Int.down;
            case 2:  return Vector2Int.left;
            default: return Vector2Int.right;
        }
    }

    // ───────────────────────────────────────────────────────────
    // 헬퍼: OnBossHit (피격 SFX)
    // ───────────────────────────────────────────────────────────

    private void OnBossHit()
    {
        // 피격 반응만 처리한다.
        // 페이즈 전환은 기본공격 모드의 체크포인트에서만 평가해 패턴모드 중간 전환을 막는다.
        PlaySfx(bossHitSfx);
        SpawnBossHitVfx();
    }

    private void SpawnBossHitVfx()
    {
        if (bossHitVfxPrefab != null)
        {
            GameObject go = Instantiate(
                bossHitVfxPrefab,
                transform.position + bossHitVfxLocalOffset,
                Quaternion.identity);
            go.transform.localScale = Vector3.one * bossHitVfxScale;
            EnsureDashVfxArenaMask();
            ConfigureSpawnedVfx(go, "Hazard", 30, 1f);
            Destroy(go, bossHitVfxLifetime);
            return;
        }

        if (bossHitVfxFrames == null || bossHitVfxFrames.Length == 0)
            bossHitVfxFrames = Resources.LoadAll<Sprite>(BossHitSpriteResourcePath);

        if (bossHitVfxFrames == null || bossHitVfxFrames.Length == 0)
            return;

        StartCoroutine(PlayBossHitSpriteVfx());
    }

    private IEnumerator PlayBossHitSpriteVfx()
    {
        GameObject go = new GameObject("BossHitVfx", typeof(SpriteRenderer));
        go.transform.position = transform.position + bossHitVfxLocalOffset;
        go.transform.localScale = Vector3.one * bossHitVfxScale;

        EnsureDashVfxArenaMask();

        var sr = go.GetComponent<SpriteRenderer>();
        sr.sortingLayerID = SortingLayer.NameToID("Hazard");
        sr.sortingOrder = 25;
        sr.maskInteraction = clipDashVfxInsideArena && dashVfxArenaMask != null
            ? SpriteMaskInteraction.VisibleInsideMask
            : SpriteMaskInteraction.None;

        float frameTime = Mathf.Max(0.02f, bossHitVfxLifetime / bossHitVfxFrames.Length);
        for (int i = 0; i < bossHitVfxFrames.Length; i++)
        {
            sr.sprite = bossHitVfxFrames[i];
            yield return new WaitForSeconds(frameTime);
        }

        if (go != null)
            Destroy(go);
    }

    private void CachePlayerSceneRefs()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        playerController = player.GetComponent<PlayerController>();
        playerCombat = player.GetComponent<PlayerCombat>();

        playerHealth = player.GetComponent<PlayerHealth>();
        playerMover = player.GetComponent<GridMover>();
        playerOccupant = player.GetComponent<GridOccupant>();
        playerTransform = player.transform;
    }

    private void SetPlayerControlEnabled(bool enabled)
    {
        if (playerController != null)
            playerController.enabled = enabled;

        if (playerCombat != null)
            playerCombat.enabled = enabled;


    }

    private void PlayIntroNoise()
    {
        if (introNoiseClip == null)
            return;

        Vector3 playPosition = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(introNoiseClip, playPosition, introNoiseVolume);
    }

    private IEnumerator MovePlayerOneCellForIntro()
    {
        if (playerMover == null || playerOccupant == null)
            yield break;

        Vector2Int moveDir = NormalizeDirection(introAutoMoveDirection);
        if (moveDir == Vector2Int.zero)
            yield break;

        if (playerController != null)
            playerController.SetFacing(moveDir);

        if (!playerMover.CanMove(moveDir, 1, true))
            yield break;

        Vector2Int targetCell = playerOccupant.CurrentCell + moveDir;
        playerMover.ForceMoveTo(targetCell, introAutoMoveDuration);

        while (playerMover.IsMoving)
            yield return null;
    }

    private void ApplyTrainingIntroSkipState()
    {
        CachePlayerSceneRefs();
        CutsceneFreezeManager.ForceUnlock();

        Vector2Int moveDir = NormalizeDirection(introAutoMoveDirection);
        if (playerController != null && moveDir != Vector2Int.zero)
            playerController.SetFacing(moveDir);

        if (playerOccupant != null && playerTransform != null && GridManager.Instance != null && moveDir != Vector2Int.zero)
        {
            Vector2Int targetCell = playerOccupant.CurrentCell + moveDir;
            if (playerMover == null || playerMover.CanMove(moveDir, 1, true))
            {
                playerOccupant.SetCell(targetCell);
                playerTransform.position = GridManager.Instance.CellToWorld(targetCell);
            }
        }

        bossCell = new Vector2Int(0, 1);
        SetBossFacing(Vector2Int.down);
        SyncBossRootToCell();
        SetBossVisible(true);
        SetBossSpritePoseOffset(0f);
        SetBossHudVisible(true);
        SetPlayerControlEnabled(true);
        animDriver?.PlayIdle(bossFacing);
    }

    private bool IsPlayerInsidePostBossExit()
    {
        if (playerTransform == null || GridManager.Instance == null)
            return false;

        Vector2Int playerCell = GridManager.Instance.WorldToCell(playerTransform.position);
        int minX = postBossExitCenterCell.x - postBossExitSize.x / 2;
        int maxX = minX + postBossExitSize.x - 1;
        int minY = postBossExitCenterCell.y - postBossExitSize.y / 2;
        int maxY = minY + postBossExitSize.y - 1;

        return playerCell.x >= minX && playerCell.x <= maxX &&
               playerCell.y >= minY && playerCell.y <= maxY;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AssignEditorVfxReferences();
    }

    private void AssignEditorVfxReferences()
    {
        GameObject smokePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashSmokePrefabAssetPath);
        if (smokePrefab != null)
            dashSmokeVFXPrefab = smokePrefab;

        GameObject hitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossHitPrefabAssetPath);
        if (hitPrefab != null)
            bossHitVfxPrefab = hitPrefab;
    }
#endif
}
