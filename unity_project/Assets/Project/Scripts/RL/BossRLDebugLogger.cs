using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BossRLDebugLogger : MonoBehaviour
{
    [SerializeField] private int logEveryNSteps = 250;
    private const int MaxTraceSamplesPerEpisode = 5;
    private const int MaxTraceSamplesPerRun = 50;
    private static int globalSafeOppMoveTraceSamples;

    private string lastWarningKey;
    public int CurrentEpisodeIndex => episodeIndex;

    // ── Episode bookkeeping ───────────────────────────────────────────────────
    private float episodeStartTime;
    private int   episodeBossHpStart;

    // ── Reward breakdown ──────────────────────────────────────────────────────
    private float rewardTotal, rewardBossDamage, rewardHitPenalty, rewardCriticalHealthPenalty, rewardDeathPenalty;
    private float rewardFastClear;
    private float rewardWarningTile, rewardDamageTile, rewardWallBlocked;
    private float rewardApproach, rewardMissedAttack, rewardAttackOnCooldown;
    private float rewardMovedIntoDanger, rewardSafeAttack, rewardMissedSafeAttackOpportunity;

    // ── Tile / hazard stats ───────────────────────────────────────────────────
    private int warningTileSteps;
    private int damageTileSteps;

    // ── Player hit stats ──────────────────────────────────────────────────────
    private int   playerHitCount;
    private int   playerDamageStepCount;
    private int   maxPlayerHpLossInSingleStep;
    private float deathTime;
    private PlayerHealth cachedPlayerHealth;
    private readonly Dictionary<string, int> damageSourceHitCounts = new Dictionary<string, int>();
    private readonly Dictionary<string, int> damageSourceGroupHitCounts = new Dictionary<string, int>();

    // ── Movement mask metrics ─────────────────────────────────────────────────
    private int moveMaskedWallCount;           // geometry wall masked (pure blockedCells check)
    private int moveMaskedWarningCount;        // should be 0 after relaxation
    private int moveMaskedDamageCount;         // active damage tile masked
    private int moveMaskedRecentWarningCount;  // should be 0 after relaxation
    private int moveMaskedRecentDamageCount;   // should be 0 after relaxation
    private int moveMaskedDueToBusyCount;      // must be 0 — busy no longer used as wall
    private int moveAllowedCount;
    private int movementActionCount;
    private int successfulMoveCount;
    private int dangerousMoveAttemptCount;
    private int waitOnlyMaskStateCount;        // all 4 moves masked → only WAIT available
    private int waitOnlyDueToGeometryCount;    // wait-only because all 4 dirs geometry blocked
    private int waitOnlyDueToDamageCount;      // wait-only because damage blocking non-geometry dirs
    private int waitOnlyDueToBusyCount;        // must be 0 — busy no longer a mask criterion
    private int waitOnlyDueToBossCellCount;    // must be 0 — boss cell no longer blocked
    private int escapeActionForcedOpenCount;   // escape exception triggered
    private int bossCellTreatedAsBlockedCount; // boss cell wrongly blocked (must be 0 after fix)
    private int bossCellMoveAllowedCount;      // boss cell correctly allowed (tracks fix working)
    private int moveBusyStateCount;            // times IsMoving=true at mask call (informational)
    private int waitActionCount;               // times action=0 (WAIT) was selected
    private int actionWaitCount;
    private int actionMoveUpCount;
    private int actionMoveDownCount;
    private int actionMoveLeftCount;
    private int actionMoveRightCount;
    private int actionAttackCount;

    // ── Attack mask metrics ───────────────────────────────────────────────────
    private int attackMaskedNotReadyCount;
    private int attackMaskedOutOfRangeCount;
    private int attackMaskedOnWarningCount;
    private int attackMaskedOnDamageCount;
    private int attackMaskedOnRecentWarningCount;
    private int attackMaskedOnRecentDamageCount;
    private int attackMaskedSurvivalStageCount;
    private int attackAllowedCount;
    private int attackMaskedTotalCount;

    // ── Attack quality metrics ────────────────────────────────────────────────
    private int   attackActionCount;
    private int   successfulHitSteps;
    private int   missedAttackCount;
    private int   attackOnCooldownCount;
    private int   attackWhenOnWarning;
    private int   attackWhenOnDamage;
    private int   attackWhenRecentWarning;
    private int   attackWhenRecentDamage;
    private int   bossDamageTotal;
    private int   bossDamageBeforeFirstHit;

    // BossCell vs visible boss sprite diagnostic metrics.
    private int bossSpriteVisibleHitCount;
    private int bossSpriteInvisibleHitCount;
    private int bossSpriteOffButBossCellHitCount;
    private int bossVisualRootInactiveHitCount;
    private int bossRendererDisabledHitCount;
    private int bossAlphaZeroHitCount;
    private int bossCellInAttackRangeButSpriteNotVisibleCount;
    private int bossCellInAttackRangeButSpriteCellOutOfRangeCount;
    private int spriteVisibleCellInAttackRangeCount;
    private int spriteVisibleCellOutOfAttackRangeCount;
    private int hiddenBossAttackRewardCount;
    private int targetAlignmentLogCount;
    private int hitClassNormalVisibleCount;
    private int hitClassRootVisibleOverlapCount;
    private int hitClassDashCurrentOverlapCount;
    private int hitClassStaleBossCellCount;
    private int hitClassHiddenTargetCount;
    private int hitClassOffLaneEmptyCount;
    private int hitClassUnknownRemainingCount;
    private int rootCellInAttackRangeCount;
    private int spriteBoundsCellInAttackRangeCount;
    private int bossCellInAttackRangeCount;
    private int rootInRangeSpriteBoundsOutCount;
    private int rootInRangeSpriteInvisibleCount;
    private int spriteBoundsOutButRootVisibleOverlapCount;
    private int dashCurrentRootOverlapCount;
    private int dashCurrentVisualOverlapCount;
    private int dashAllowedCandidateCount;
    private int dashStaleBossCellOnlyCount;
    private int dashOffLaneEmptyCount;
    private int dashHitAllowedCandidateCount;
    private int dashHitCurrentVisualOverlapCount;
    private int dashHitBossCellOnlyCount;
    private int dashHitPreviousPositionSuspectCount;
    private int dashHitFutureEndCellSuspectCount;
    private int dashHitOutsideActiveLaneCount;
    private int dashHitInsideActiveDamageLaneCount;
    private int dashHitInsideActiveWarningLaneCount;

    // Diagonal blindspot wiggle diagnostic metrics. These are logging-only and
    // do not feed reward, action masking, or observations.
    private int diagonalBlindspotBossCellSteps;
    private int diagonalBlindspotRootSteps;
    private int diagonalBlindspotVisualSteps;
    private int diagonalBlindspotAttackCount;
    private int diagonalBlindspotAttackHitCount;
    private int diagonalBlindspotMoveCount;
    private int diagonalBlindspotBackAndForthCount;
    private int diagonalBlindspotAxisExitCount;
    private int diagonalBlindspotKeptSafeCount;
    private int diagonalBlindspotThenPlayerHitCount;
    private int diagonalBlindspotBossMeleeWhiffCandidateCount;
    private int attackAfterDiagonalWiggleCount;
    private int axisPositionStepCount;
    private int axisPositionPlayerHitCount;
    private int axisPositionBossMeleeHitCount;
    private int diagonalBlindspotPlayerHitCount;
    private int diagonalBlindspotBossMeleeHitCount;
    private int playerInBossFrontAxisCount;
    private int playerInBossBackAxisCount;
    private int playerInBossLeftAxisCount;
    private int playerInBossRightAxisCount;
    private int playerInBossDiagonalBlindspotCount;
    private int diagonalWiggleStreak;
    private float lastDiagonalWiggleWindowTime = -999f;
    private bool hasLastDiagonalMove;
    private Vector2Int lastDiagonalMoveFrom;
    private Vector2Int lastDiagonalMoveTo;
    private bool hasLastBossPoseForWiggle;
    private Vector2Int lastBossCellForWiggle;
    private Vector2Int lastBossFacingForWiggle;
    private ElevatorBossController cachedBossControllerForWiggle;

    // ── Attack range metrics ──────────────────────────────────────────────────
    private int   bossInAttackRangeSteps;
    private int   attackWhenBossInRange;
    private int   attackWhenBossOutOfRange;
    private float bossDistanceSumAtAttack;
    private int   bossDistanceSampleCount;
    private float bossDistanceAtFirstAttack;
    private bool  firstAttackRecorded;

    // ── Wall blocked (move attempted, but wall) ───────────────────────────────
    private int wallBlockedMoves;
    private int wallBlockedUp, wallBlockedDown, wallBlockedLeft, wallBlockedRight;
    private int totalMoveAttempts;

    // ── First hit tracking ────────────────────────────────────────────────────
    private bool  firstHitRecorded;
    private int   firstHitStep;
    private float firstHitTime;

    // ── Recent danger presence tracking ──────────────────────────────────────
    private int playerOnRecentWarningSteps;
    private int playerOnRecentDamageSteps;

    // ── Warning → damage re-entry tracking ───────────────────────────────────
    private const int ReentryWindow = 4;
    private bool prevOnWarning;
    private int  lastWarningExitStep;
    private int  recentWarningReentryCount;
    private int  damageSoonAfterWarning;
    private int  warningToDamageHitCount;
    private int  hitAfterWarningAvoidCount;

    // ── Survival milestones ───────────────────────────────────────────────────
    private bool survived10s;
    private bool survived20s;

    // ── Opportunity / danger metrics ──────────────────────────────────────────
    private int safeAttackOpportunitySteps;
    private int safeAttackTakenCount;
    private int safeAttackMissedCount;
    private int safeAttackHitCount;
    private int safeOppWaitCount;
    private int safeOppMoveCount;
    private int safeOppMoveTowardBossCount;
    private int safeOppMoveAwayFromBossCount;
    private int safeOppMoveLateralCount;
    private int bossInRangeNoAttackCount;
    private int attackOutOfRangeCount;
    private int attackWhenDangerNearbyCount;
    private int dangerNearbySteps;
    private int dangerNearbySafeMoveCount;
    private int dangerNearbyDangerMoveCount;
    private int movedIntoWarningCount;
    private int movedIntoDamageCount;
    private int movedIntoRecentWarningCount;
    private int movedIntoRecentDamageCount;
    private int waitWhileDangerNearbyCount;
    private int hitWhileSafeMoveAvailableCount;
    private int avoidableHitCount;
    private int hitOnWarningTileCount;
    private int hitOnDamageTileCount;
    private int hitOnRecentWarningTileCount;
    private int hitAfterMovingIntoDangerCount;
    private int warningToHitStepSum;
    private int warningToHitStepCount;

    // ── Distance / positioning metrics ───────────────────────────────────────
    private float distanceToBossSum;
    private int   distanceToBossSampleCount;
    private int   minDistanceToBoss;
    private int   maxDistanceToBoss;
    private int   timeInAttackRangeSteps;
    private int   timeOutOfAttackRangeSteps;
    private int   enteredAttackRangeCount;
    private int   leftAttackRangeCount;
    private bool  hasPreviousAttackRangeState;
    private bool  previousAttackRangeState;

    // ── Per-action attack-allowed metrics ────────────────────────────────────
    private int attackAllowedActionStepCount;
    private int chosenAttackWhenAllowedCount;
    private int missedAllowedAttackCount;

    // ── Safe opportunity MOVE trace ──────────────────────────────────────────
    private struct SafeOppMoveTrace
    {
        public int episodeIndex;
        public int step;
        public string actionName;
        public Vector2Int playerCell;
        public Vector2Int bossCell;
        public int distanceBefore;
        public int distanceAfter;
        public bool remainedInAttackRangeAfterMove;
        public bool leftAttackRangeAfterMove;
        public bool movedTowardBoss;
        public bool movedAwayFromBoss;
        public bool movedLateral;
        public bool dangerNearby;
        public bool nextWarning;
        public bool nextDamage;
        public bool nextRecentWarning;
        public bool nextRecentDamage;
        public int safeMoveAlternatives;
        public bool attackAllowed;
        public int bossHpBefore;
        public float timestamp;
        public bool hitWithin1s;
        public bool hitWithin2s;
        public bool bossHpAfter05Recorded;
        public int bossHpAfter05;
    }
    private readonly List<SafeOppMoveTrace> pendingSafeOppMoveTraces = new List<SafeOppMoveTrace>();
    private int episodeIndex;
    private int safeOppMoveTraceSampleCount;
    private int safeOppMoveTraceTotalCount;
    private int safeOppMoveUsefulEscapeCount;
    private int safeOppMoveKeptAttackRangeCount;
    private int safeOppMoveLeftAttackRangeCount;
    private int safeOppMoveAwayWithoutDangerCount;
    private int safeOppMoveThenHitWithin2sCount;
    private int safeOppMoveThenNoHitNoDamageCount;
    private int safeOppAttackWouldHaveBeenAllowedCount;

    // ── Warning-on-player / boss-facing diagnostics ─────────────────────────
    private int warningSpawnOnPlayerCount;
    private int warningSpawnOnPlayerSafeMoveAvailableCount;
    private int warningSpawnOnPlayerEscapeSuccessCount;
    private int warningSpawnOnPlayerEscapeFailCount;
    private int warningSpawnOnPlayerHitCount;
    private int warningSpawnOnPlayerChosenWaitCount;
    private int warningSpawnOnPlayerChosenAttackCount;
    private int warningSpawnOnPlayerChosenMoveCount;
    private int warningSpawnToDamageFrameSum;
    private int warningSpawnToDamageFrameCount;
    private int decisionAvailableBeforeDamageCount;
    private int noDecisionBeforeDamageCount;
    private int bossFacingChangedBetweenWarningAndDamageCount;
    private int hitWhenBossFacingChangedCount;
    private int hitByOriginalWarningDirectionCount;
    private int hitByRotatedDamageDirectionCount;
    private int patternHitUnknownCount;
    private bool pendingWarningOnPlayer;
    private int pendingWarningFrame;
    private int pendingWarningDecisionStep;
    private Vector2Int pendingWarningBossFacing;

    // cross-step state for opportunity tracking
    private int  oppInternalStep;
    private bool oppLastStepOnWarning;
    private int  oppLastWarningEntryStep;
    private bool oppLastMoveIntoDanger;

    // ── Danger movement mask metrics ─────────────────────────────────────────
    private int moveMaskedWarnWhenSafeCount;
    private int moveMaskedRecentWarnWhenSafeCount;
    private int moveMaskedDmgWhenSafeCount;
    private int leastDangerEscapeOpenedCount;
    private int waitMaskedDueToDangerCount;
    private int safeMoveAvailableStepCount;
    private int choseSafeMoveCount;
    private int choseDangerMoveCount;

    // ── Delayed hit attribution ───────────────────────────────────────────────
    private struct PendingAttackRecord
    {
        public float timestamp;
        public bool  isSafe;
        public bool  isOutOfRange;
    }
    private readonly List<PendingAttackRecord> pendingAttacks = new List<PendingAttackRecord>();
    private int safeAttackHitSameStep;
    private int safeAttackHitWithin03s;
    private int safeAttackHitWithin05s;
    private int outOfRangeAttackHitWithin05s;
    private int unsafeAttackHitWithin05s;

    // ── Episode reset ─────────────────────────────────────────────────────────
    public void ResetEpisode(int bossHpStart, float startTime)
    {
        episodeIndex++;
        episodeStartTime   = startTime;
        episodeBossHpStart = bossHpStart;

        rewardTotal = rewardBossDamage = rewardHitPenalty = rewardCriticalHealthPenalty = rewardDeathPenalty = 0f;
        rewardFastClear = 0f;
        rewardWarningTile = rewardDamageTile = rewardWallBlocked = 0f;
        rewardApproach = rewardMissedAttack = rewardAttackOnCooldown = 0f;
        rewardMovedIntoDanger = rewardSafeAttack = rewardMissedSafeAttackOpportunity = 0f;

        warningTileSteps = damageTileSteps = 0;
        playerHitCount   = 0;
        playerDamageStepCount = 0;
        maxPlayerHpLossInSingleStep = 0;
        deathTime        = -1f;
        cachedPlayerHealth = FindFirstObjectByType<PlayerHealth>();
        damageSourceHitCounts.Clear();
        damageSourceGroupHitCounts.Clear();

        moveMaskedWallCount = moveMaskedWarningCount = moveMaskedDamageCount = 0;
        moveMaskedRecentWarningCount = moveMaskedRecentDamageCount = moveMaskedDueToBusyCount = 0;
        moveAllowedCount = movementActionCount = successfulMoveCount = dangerousMoveAttemptCount = 0;
        waitOnlyMaskStateCount = escapeActionForcedOpenCount = bossCellTreatedAsBlockedCount = 0;
        waitOnlyDueToGeometryCount = waitOnlyDueToDamageCount = 0;
        waitOnlyDueToBusyCount = waitOnlyDueToBossCellCount = 0;
        bossCellMoveAllowedCount = moveBusyStateCount = 0;
        waitActionCount = 0;
        actionWaitCount = actionMoveUpCount = actionMoveDownCount = 0;
        actionMoveLeftCount = actionMoveRightCount = actionAttackCount = 0;

        attackMaskedNotReadyCount = attackMaskedOutOfRangeCount = 0;
        attackMaskedOnWarningCount = attackMaskedOnDamageCount = 0;
        attackMaskedOnRecentWarningCount = attackMaskedOnRecentDamageCount = 0;
        attackMaskedSurvivalStageCount = attackAllowedCount = attackMaskedTotalCount = 0;

        attackActionCount    = successfulHitSteps   = 0;
        missedAttackCount    = attackOnCooldownCount = 0;
        attackWhenOnWarning  = attackWhenOnDamage   = 0;
        attackWhenRecentWarning = attackWhenRecentDamage = 0;
        bossDamageTotal      = bossDamageBeforeFirstHit = 0;
        bossSpriteVisibleHitCount = bossSpriteInvisibleHitCount = 0;
        bossSpriteOffButBossCellHitCount = bossVisualRootInactiveHitCount = 0;
        bossRendererDisabledHitCount = bossAlphaZeroHitCount = 0;
        bossCellInAttackRangeButSpriteNotVisibleCount = 0;
        bossCellInAttackRangeButSpriteCellOutOfRangeCount = 0;
        spriteVisibleCellInAttackRangeCount = spriteVisibleCellOutOfAttackRangeCount = 0;
        hiddenBossAttackRewardCount = targetAlignmentLogCount = 0;
        hitClassNormalVisibleCount = hitClassRootVisibleOverlapCount = 0;
        hitClassDashCurrentOverlapCount = 0;
        hitClassStaleBossCellCount = hitClassHiddenTargetCount = 0;
        hitClassOffLaneEmptyCount = hitClassUnknownRemainingCount = 0;
        rootCellInAttackRangeCount = spriteBoundsCellInAttackRangeCount = 0;
        bossCellInAttackRangeCount = rootInRangeSpriteBoundsOutCount = 0;
        rootInRangeSpriteInvisibleCount = spriteBoundsOutButRootVisibleOverlapCount = 0;
        dashCurrentRootOverlapCount = dashCurrentVisualOverlapCount = 0;
        dashAllowedCandidateCount = dashStaleBossCellOnlyCount = dashOffLaneEmptyCount = 0;
        dashHitAllowedCandidateCount = dashHitCurrentVisualOverlapCount = 0;
        dashHitBossCellOnlyCount = dashHitPreviousPositionSuspectCount = 0;
        dashHitFutureEndCellSuspectCount = dashHitOutsideActiveLaneCount = 0;
        dashHitInsideActiveDamageLaneCount = dashHitInsideActiveWarningLaneCount = 0;
        diagonalBlindspotBossCellSteps = diagonalBlindspotRootSteps = diagonalBlindspotVisualSteps = 0;
        diagonalBlindspotAttackCount = diagonalBlindspotAttackHitCount = diagonalBlindspotMoveCount = 0;
        diagonalBlindspotBackAndForthCount = diagonalBlindspotAxisExitCount = 0;
        diagonalBlindspotKeptSafeCount = diagonalBlindspotThenPlayerHitCount = 0;
        diagonalBlindspotBossMeleeWhiffCandidateCount = attackAfterDiagonalWiggleCount = 0;
        axisPositionStepCount = axisPositionPlayerHitCount = axisPositionBossMeleeHitCount = 0;
        diagonalBlindspotPlayerHitCount = diagonalBlindspotBossMeleeHitCount = 0;
        playerInBossFrontAxisCount = playerInBossBackAxisCount = 0;
        playerInBossLeftAxisCount = playerInBossRightAxisCount = 0;
        playerInBossDiagonalBlindspotCount = 0;
        diagonalWiggleStreak = 0;
        lastDiagonalWiggleWindowTime = -999f;
        hasLastDiagonalMove = false;
        hasLastBossPoseForWiggle = false;

        bossInAttackRangeSteps   = attackWhenBossInRange    = attackWhenBossOutOfRange = 0;
        bossDistanceSumAtAttack  = 0f;
        bossDistanceSampleCount  = 0;
        bossDistanceAtFirstAttack = -1f;
        firstAttackRecorded      = false;

        wallBlockedMoves = wallBlockedUp = wallBlockedDown = wallBlockedLeft = wallBlockedRight = 0;
        totalMoveAttempts = 0;

        firstHitRecorded = false;
        firstHitStep     = -1;
        firstHitTime     = -1f;

        playerOnRecentWarningSteps = 0;
        playerOnRecentDamageSteps  = 0;

        prevOnWarning             = false;
        lastWarningExitStep       = -9999;
        recentWarningReentryCount = 0;
        damageSoonAfterWarning    = 0;
        warningToDamageHitCount   = 0;
        hitAfterWarningAvoidCount = 0;

        survived10s = survived20s = false;

        safeAttackOpportunitySteps = safeAttackTakenCount = safeAttackMissedCount = safeAttackHitCount = 0;
        safeOppWaitCount = safeOppMoveCount = 0;
        safeOppMoveTowardBossCount = safeOppMoveAwayFromBossCount = safeOppMoveLateralCount = 0;
        bossInRangeNoAttackCount = attackOutOfRangeCount = attackWhenDangerNearbyCount = 0;
        dangerNearbySafeMoveCount = dangerNearbyDangerMoveCount = 0;
        dangerNearbySteps = movedIntoWarningCount = movedIntoDamageCount = 0;
        movedIntoRecentWarningCount = movedIntoRecentDamageCount = 0;
        waitWhileDangerNearbyCount = hitWhileSafeMoveAvailableCount = avoidableHitCount = 0;
        hitOnWarningTileCount = hitOnDamageTileCount = hitOnRecentWarningTileCount = 0;
        hitAfterMovingIntoDangerCount = warningToHitStepSum = warningToHitStepCount = 0;
        oppInternalStep = 0;
        oppLastStepOnWarning = oppLastMoveIntoDanger = false;
        oppLastWarningEntryStep = -1;
        distanceToBossSum = 0f;
        distanceToBossSampleCount = 0;
        minDistanceToBoss = int.MaxValue;
        maxDistanceToBoss = int.MinValue;
        timeInAttackRangeSteps = timeOutOfAttackRangeSteps = 0;
        enteredAttackRangeCount = leftAttackRangeCount = 0;
        hasPreviousAttackRangeState = false;
        previousAttackRangeState = false;
        attackAllowedActionStepCount = chosenAttackWhenAllowedCount = missedAllowedAttackCount = 0;
        pendingSafeOppMoveTraces.Clear();
        safeOppMoveTraceSampleCount = 0;
        safeOppMoveTraceTotalCount = 0;
        safeOppMoveUsefulEscapeCount = 0;
        safeOppMoveKeptAttackRangeCount = 0;
        safeOppMoveLeftAttackRangeCount = 0;
        safeOppMoveAwayWithoutDangerCount = 0;
        safeOppMoveThenHitWithin2sCount = 0;
        safeOppMoveThenNoHitNoDamageCount = 0;
        safeOppAttackWouldHaveBeenAllowedCount = 0;
        warningSpawnOnPlayerCount = 0;
        warningSpawnOnPlayerSafeMoveAvailableCount = 0;
        warningSpawnOnPlayerEscapeSuccessCount = 0;
        warningSpawnOnPlayerEscapeFailCount = 0;
        warningSpawnOnPlayerHitCount = 0;
        warningSpawnOnPlayerChosenWaitCount = 0;
        warningSpawnOnPlayerChosenAttackCount = 0;
        warningSpawnOnPlayerChosenMoveCount = 0;
        warningSpawnToDamageFrameSum = 0;
        warningSpawnToDamageFrameCount = 0;
        decisionAvailableBeforeDamageCount = 0;
        noDecisionBeforeDamageCount = 0;
        bossFacingChangedBetweenWarningAndDamageCount = 0;
        hitWhenBossFacingChangedCount = 0;
        hitByOriginalWarningDirectionCount = 0;
        hitByRotatedDamageDirectionCount = 0;
        patternHitUnknownCount = 0;
        pendingWarningOnPlayer = false;
        pendingWarningFrame = -1;
        pendingWarningDecisionStep = -1;
        pendingWarningBossFacing = Vector2Int.zero;
        pendingAttacks.Clear();
        safeAttackHitSameStep = safeAttackHitWithin03s = safeAttackHitWithin05s = 0;
        outOfRangeAttackHitWithin05s = unsafeAttackHitWithin05s = 0;
        moveMaskedWarnWhenSafeCount = moveMaskedRecentWarnWhenSafeCount = moveMaskedDmgWhenSafeCount = 0;
        leastDangerEscapeOpenedCount = waitMaskedDueToDangerCount = safeMoveAvailableStepCount = 0;
        choseSafeMoveCount = choseDangerMoveCount = 0;
    }

    // ── Escape/wait state recording ───────────────────────────────────────────
    public void RecordWaitOnlyState()              { waitOnlyMaskStateCount++; }
    public void RecordEscapeActionForcedOpen()     { escapeActionForcedOpenCount++; }
    public void RecordBossCellTreatedAsBlocked()   { bossCellTreatedAsBlockedCount++; }
    public void RecordBossCellMoveAllowed()        { bossCellMoveAllowedCount++; }
    public void RecordMoveBusyState()              { moveBusyStateCount++; }

    public void RecordMoveDangerMaskStep(
        int warnMaskedWhenSafe, int recentWarnMaskedWhenSafe, int dmgMaskedWhenSafe,
        bool leastDangerOpened, bool waitMasked, bool safeMovePossible)
    {
        moveMaskedWarnWhenSafeCount       += warnMaskedWhenSafe;
        moveMaskedRecentWarnWhenSafeCount += recentWarnMaskedWhenSafe;
        moveMaskedDmgWhenSafeCount        += dmgMaskedWhenSafe;
        if (leastDangerOpened) leastDangerEscapeOpenedCount++;
        if (waitMasked)        waitMaskedDueToDangerCount++;
        if (safeMovePossible)  safeMoveAvailableStepCount++;
    }

    public void RecordWaitOnlyBreakdown(bool dueToGeometry, bool dueToDamage)
    {
        if (dueToGeometry) waitOnlyDueToGeometryCount++;
        if (dueToDamage)   waitOnlyDueToDamageCount++;
        // busy and boss_cell are no longer mask criteria → always 0, never incremented
    }

    // ── Opportunity metric recording (called from OnActionReceived) ───────────

    public void RecordOpportunityMetrics(
        int singleAction,
        bool isAttack, bool safeOpportunity, bool attackHit, bool bossInRange, bool attackReady,
        bool dangerNearby, int safeMoveCount, bool onWarning, bool onDamage,
        bool onRecentWarn, bool onRecentDmg,
        bool isMove, bool moveWillSucceed,
        bool moveIntoWarn, bool moveIntoDmg, bool moveIntoRecentWarn, bool moveIntoRecentDmg,
        bool gotHit, bool isWait,
        float currentTime = 0f, int bossDamageDelta = 0,
        bool choseSafeMove = false, bool choseDangerMove = false,
        int currentDistanceToBoss = 99, int moveDistanceDelta = 0,
        Vector2Int playerCell = default(Vector2Int), Vector2Int bossCell = default(Vector2Int),
        bool remainedInAttackRangeAfterMove = false, int bossHpBefore = 0, int currentBossHp = 0,
        Vector2Int bossFacing = default(Vector2Int))
    {
        oppInternalStep++;
        UpdatePendingSafeOppMoveTraces(currentTime, gotHit, currentBossHp);
        UpdateWarningOnPlayerDiagnostics(singleAction, safeMoveCount, onWarning, onDamage, gotHit, bossFacing);

        if (singleAction == 0) actionWaitCount++;
        else if (singleAction == 1) actionMoveUpCount++;
        else if (singleAction == 2) actionMoveDownCount++;
        else if (singleAction == 3) actionMoveLeftCount++;
        else if (singleAction == 4) actionMoveRightCount++;
        else if (singleAction == 5) actionAttackCount++;

        int clampedDistance = Mathf.Max(0, currentDistanceToBoss);
        distanceToBossSum += clampedDistance;
        distanceToBossSampleCount++;
        minDistanceToBoss = Mathf.Min(minDistanceToBoss, clampedDistance);
        maxDistanceToBoss = Mathf.Max(maxDistanceToBoss, clampedDistance);

        UpdateMeleeWiggleDiagnostics(
            singleAction,
            isAttack,
            attackHit,
            isMove,
            moveWillSucceed,
            moveDistanceDelta,
            gotHit,
            currentTime,
            clampedDistance,
            playerCell,
            bossCell,
            bossFacing,
            onWarning,
            onDamage,
            onRecentWarn,
            onRecentDmg,
            moveIntoWarn,
            moveIntoDmg,
            moveIntoRecentWarn,
            moveIntoRecentDmg);

        if (bossInRange) timeInAttackRangeSteps++;
        else             timeOutOfAttackRangeSteps++;

        if (hasPreviousAttackRangeState)
        {
            if (!previousAttackRangeState && bossInRange) enteredAttackRangeCount++;
            if (previousAttackRangeState && !bossInRange) leftAttackRangeCount++;
        }
        hasPreviousAttackRangeState = true;
        previousAttackRangeState = bossInRange;

        // ── Attack opportunity tracking ───────────────────────────────────────
        if (safeOpportunity)  safeAttackOpportunitySteps++;
        if (isAttack)
        {
            if (safeOpportunity)
            {
                safeAttackTakenCount++;
                if (attackHit)  safeAttackHitCount++;
                else            safeAttackMissedCount++;
            }
            if (!bossInRange)   attackOutOfRangeCount++;
            if (dangerNearby)   attackWhenDangerNearbyCount++;
        }
        if (safeOpportunity)
        {
            if (isWait) safeOppWaitCount++;
            if (isMove)
            {
                safeOppMoveCount++;
                if (moveDistanceDelta < 0)      safeOppMoveTowardBossCount++;
                else if (moveDistanceDelta > 0) safeOppMoveAwayFromBossCount++;
                else                            safeOppMoveLateralCount++;
            }
            if (isAttack) chosenAttackWhenAllowedCount++;
            else          missedAllowedAttackCount++;
            attackAllowedActionStepCount++;
        }
        if (bossInRange && attackReady && !isAttack) bossInRangeNoAttackCount++;

        // ── Danger nearby tracking ────────────────────────────────────────────
        if (dangerNearby) dangerNearbySteps++;
        if (isWait && dangerNearby) waitWhileDangerNearbyCount++;
        if (dangerNearby && choseSafeMove) dangerNearbySafeMoveCount++;
        if (dangerNearby && choseDangerMove) dangerNearbyDangerMoveCount++;

        if (safeOpportunity && isMove)
        {
            RecordSafeOppMoveTrace(singleAction, currentTime, playerCell, bossCell,
                currentDistanceToBoss, currentDistanceToBoss + moveDistanceDelta,
                bossInRange, remainedInAttackRangeAfterMove, moveDistanceDelta,
                dangerNearby, moveIntoWarn, moveIntoDmg, moveIntoRecentWarn, moveIntoRecentDmg,
                safeMoveCount, bossHpBefore);
        }

        // ── Move into danger tracking ─────────────────────────────────────────
        bool movedIntoDangerThisStep = false;
        if (isMove && moveWillSucceed)
        {
            if (moveIntoWarn)      { movedIntoWarningCount++;       movedIntoDangerThisStep = true; }
            if (moveIntoDmg)       { movedIntoDamageCount++;        movedIntoDangerThisStep = true; }
            if (moveIntoRecentWarn){ movedIntoRecentWarningCount++; movedIntoDangerThisStep = true; }
            if (moveIntoRecentDmg) { movedIntoRecentDamageCount++;  movedIntoDangerThisStep = true; }
        }

        // ── Hit analysis ──────────────────────────────────────────────────────
        if (gotHit)
        {
            if (onWarning)            hitOnWarningTileCount++;
            if (onDamage)             hitOnDamageTileCount++;
            if (onRecentWarn)         hitOnRecentWarningTileCount++;
            if (safeMoveCount > 0)    hitWhileSafeMoveAvailableCount++;
            if ((onWarning || onDamage) && safeMoveCount > 0) avoidableHitCount++;
            if (oppLastMoveIntoDanger) hitAfterMovingIntoDangerCount++;

            // time from warning entry to hit
            if (oppLastWarningEntryStep >= 0)
            {
                warningToHitStepSum += oppInternalStep - oppLastWarningEntryStep;
                warningToHitStepCount++;
                oppLastWarningEntryStep = -1;
            }
        }

        // ── Cross-step state update ───────────────────────────────────────────
        // warning entry/exit tracking
        bool enteredWarning = !oppLastStepOnWarning && onWarning;
        bool exitedWarning  = oppLastStepOnWarning  && !onWarning;
        if (enteredWarning) oppLastWarningEntryStep = oppInternalStep;
        if (exitedWarning)  oppLastWarningEntryStep = -1;
        oppLastStepOnWarning = onWarning;

        // move-into-danger flag (1 step lookback)
        oppLastMoveIntoDanger = movedIntoDangerThisStep;

        // chose safe/danger move tracking
        if (choseSafeMove)   choseSafeMoveCount++;
        if (choseDangerMove) choseDangerMoveCount++;

        // ── Delayed hit attribution ───────────────────────────────────────────
        // Register new attack in pending list
        if (isAttack && attackReady)
        {
            pendingAttacks.Add(new PendingAttackRecord
            {
                timestamp   = currentTime,
                isSafe      = safeOpportunity,
                isOutOfRange = !bossInRange
            });
        }

        // Attribute boss damage to oldest pending attack within 0.55s window
        if (bossDamageDelta > 0 && pendingAttacks.Count > 0)
        {
            for (int i = 0; i < pendingAttacks.Count; i++)
            {
                float elapsed = currentTime - pendingAttacks[i].timestamp;
                if (elapsed > 0.55f) continue; // outside window
                var pa = pendingAttacks[i];
                if (pa.isSafe)
                {
                    if (elapsed < 0.02f) safeAttackHitSameStep++;
                    if (elapsed <= 0.30f) safeAttackHitWithin03s++;
                    if (elapsed <= 0.50f) safeAttackHitWithin05s++;
                }
                else if (pa.isOutOfRange)
                {
                    if (elapsed <= 0.50f) outOfRangeAttackHitWithin05s++;
                }
                else
                {
                    if (elapsed <= 0.50f) unsafeAttackHitWithin05s++;
                }
                pendingAttacks.RemoveAt(i);
                break; // attribute to oldest only
            }
        }

        // Prune stale pending attacks older than 0.6s
        for (int i = pendingAttacks.Count - 1; i >= 0; i--)
            if (currentTime - pendingAttacks[i].timestamp > 0.6f)
                pendingAttacks.RemoveAt(i);
    }

    private void UpdateMeleeWiggleDiagnostics(
        int singleAction,
        bool isAttack,
        bool attackHit,
        bool isMove,
        bool moveWillSucceed,
        int moveDistanceDelta,
        bool gotHit,
        float currentTime,
        int currentDistanceToBoss,
        Vector2Int playerCell,
        Vector2Int bossCell,
        Vector2Int bossFacing,
        bool onWarning,
        bool onDamage,
        bool onRecentWarn,
        bool onRecentDmg,
        bool moveIntoWarn,
        bool moveIntoDmg,
        bool moveIntoRecentWarn,
        bool moveIntoRecentDmg)
    {
        GetBossReferenceCells(bossCell, out Vector2Int bossCellRef, out Vector2Int rootCellRef, out Vector2Int visualCellRef);
        bool diagonalBossCell = IsDiagonalBlindspot1(playerCell, bossCellRef);
        bool diagonalRoot = IsDiagonalBlindspot1(playerCell, rootCellRef);
        bool diagonalVisual = IsDiagonalBlindspot1(playerCell, visualCellRef);
        bool diagonalAny = diagonalBossCell || diagonalRoot || diagonalVisual;
        bool axisPosition = IsAxisPosition(playerCell, bossCellRef);
        bool currentSafe = !onWarning && !onDamage && !onRecentWarn && !onRecentDmg;
        bool bossPoseChanged = hasLastBossPoseForWiggle &&
                               (bossCell != lastBossCellForWiggle ||
                                bossFacing != lastBossFacingForWiggle);

        if (diagonalBossCell) diagonalBlindspotBossCellSteps++;
        if (diagonalRoot) diagonalBlindspotRootSteps++;
        if (diagonalVisual) diagonalBlindspotVisualSteps++;

        if (axisPosition)
        {
            axisPositionStepCount++;
            if (gotHit) axisPositionPlayerHitCount++;
        }
        RecordBossFacingAxisPosition(playerCell, bossCellRef, bossFacing, diagonalAny);

        if (diagonalAny)
        {
            if (gotHit) diagonalBlindspotPlayerHitCount++;
            else if (currentSafe) diagonalBlindspotKeptSafeCount++;
        }

        if (diagonalAny && isAttack)
        {
            diagonalBlindspotAttackCount++;
            if (attackHit) diagonalBlindspotAttackHitCount++;
        }

        if (diagonalAny && !gotHit && bossPoseChanged && currentDistanceToBoss <= 2)
        {
            diagonalBlindspotBossMeleeWhiffCandidateCount++;
        }

        if (gotHit && currentDistanceToBoss <= 2)
        {
            if (axisPosition) axisPositionBossMeleeHitCount++;
            if (diagonalAny) diagonalBlindspotBossMeleeHitCount++;
        }

        if (diagonalAny && currentTime - lastDiagonalWiggleWindowTime <= 2.0f && gotHit)
        {
            diagonalBlindspotThenPlayerHitCount++;
            lastDiagonalWiggleWindowTime = -999f;
        }

        bool diagonalWiggleMove = false;
        if (diagonalAny && isMove && moveWillSucceed)
        {
            Vector2Int moveDir = BossRLInputBridge.SingleActionToMoveDir(singleAction);
            Vector2Int nextPlayerCell = playerCell + moveDir;
            bool nextSafe = !moveIntoWarn && !moveIntoDmg && !moveIntoRecentWarn && !moveIntoRecentDmg;
            bool nextDiagonalAny = IsDiagonalBlindspotAny(nextPlayerCell, bossCellRef, rootCellRef, visualCellRef);
            bool nextAxisPosition = IsAxisPosition(nextPlayerCell, bossCellRef);
            bool nextAdjacentSafe = nextSafe &&
                                    !nextAxisPosition &&
                                    MinManhattanDistance(nextPlayerCell, bossCellRef, rootCellRef, visualCellRef) <= 2;
            bool backAndForth = hasLastDiagonalMove &&
                                lastDiagonalMoveFrom == nextPlayerCell &&
                                lastDiagonalMoveTo == playerCell &&
                                (nextDiagonalAny || nextAdjacentSafe);

            diagonalBlindspotMoveCount++;

            if (backAndForth)
            {
                diagonalBlindspotBackAndForthCount++;
                diagonalWiggleMove = true;
            }

            if (nextAxisPosition)
            {
                diagonalBlindspotAxisExitCount++;
            }

            if (diagonalWiggleMove || (nextDiagonalAny && Mathf.Abs(moveDistanceDelta) <= 1))
            {
                diagonalWiggleStreak++;
                if (diagonalWiggleStreak >= 2)
                    lastDiagonalWiggleWindowTime = currentTime;
            }
            else diagonalWiggleStreak = 0;

            lastDiagonalMoveFrom = playerCell;
            lastDiagonalMoveTo = nextPlayerCell;
            hasLastDiagonalMove = true;
        }
        else if (!diagonalAny)
        {
            diagonalWiggleStreak = 0;
            hasLastDiagonalMove = false;
        }

        if (isAttack && attackHit && currentTime - lastDiagonalWiggleWindowTime <= 1.0f)
        {
            attackAfterDiagonalWiggleCount++;
            lastDiagonalWiggleWindowTime = -999f;
        }

        lastBossCellForWiggle = bossCell;
        lastBossFacingForWiggle = bossFacing;
        hasLastBossPoseForWiggle = true;
    }

    private void GetBossReferenceCells(
        Vector2Int fallbackBossCell,
        out Vector2Int bossCellRef,
        out Vector2Int rootCellRef,
        out Vector2Int visualCellRef)
    {
        bossCellRef = fallbackBossCell;
        rootCellRef = fallbackBossCell;
        visualCellRef = fallbackBossCell;

        ElevatorBossController bossController = GetBossControllerForWiggle();
        if (bossController == null || GridManager.Instance == null)
            return;

        bossCellRef = bossController.BossCell;
        rootCellRef = GridManager.Instance.WorldToCell(bossController.transform.position);

        Transform visualRoot = bossController.DiagnosticBossVisualRoot;
        if (visualRoot == null)
        {
            visualCellRef = rootCellRef;
            return;
        }

        Vector3 visualPosition = visualRoot.position;
        foreach (SpriteRenderer renderer in visualRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer != null &&
                renderer.enabled &&
                renderer.gameObject.activeInHierarchy &&
                renderer.sprite != null &&
                renderer.color.a > 0.01f)
            {
                visualPosition = renderer.bounds.center;
                break;
            }
        }

        visualCellRef = GridManager.Instance.WorldToCell(visualPosition);
    }

    private ElevatorBossController GetBossControllerForWiggle()
    {
        if (cachedBossControllerForWiggle != null)
            return cachedBossControllerForWiggle;

        cachedBossControllerForWiggle = UnityEngine.Object.FindFirstObjectByType<ElevatorBossController>();
        return cachedBossControllerForWiggle;
    }

    private void RecordBossFacingAxisPosition(
        Vector2Int playerCell,
        Vector2Int bossRefCell,
        Vector2Int bossFacing,
        bool diagonalBlindspot)
    {
        Vector2Int facing = NormalizeAxisFacing(bossFacing);
        Vector2Int delta = playerCell - bossRefCell;
        if (delta == Vector2Int.zero)
            return;

        Vector2Int left = new Vector2Int(-facing.y, facing.x);
        int frontBack = delta.x * facing.x + delta.y * facing.y;
        int leftRight = delta.x * left.x + delta.y * left.y;

        if (leftRight == 0)
        {
            if (frontBack > 0) playerInBossFrontAxisCount++;
            else if (frontBack < 0) playerInBossBackAxisCount++;
        }

        if (frontBack == 0)
        {
            if (leftRight > 0) playerInBossLeftAxisCount++;
            else if (leftRight < 0) playerInBossRightAxisCount++;
        }

        if (diagonalBlindspot)
            playerInBossDiagonalBlindspotCount++;
    }

    private static Vector2Int NormalizeAxisFacing(Vector2Int facing)
    {
        if (Mathf.Abs(facing.x) >= Mathf.Abs(facing.y) && facing.x != 0)
            return new Vector2Int(facing.x > 0 ? 1 : -1, 0);
        if (facing.y != 0)
            return new Vector2Int(0, facing.y > 0 ? 1 : -1);
        return Vector2Int.down;
    }

    private static bool IsDiagonalBlindspotAny(
        Vector2Int playerCell,
        Vector2Int bossCellRef,
        Vector2Int rootCellRef,
        Vector2Int visualCellRef)
    {
        return IsDiagonalBlindspot1(playerCell, bossCellRef) ||
               IsDiagonalBlindspot1(playerCell, rootCellRef) ||
               IsDiagonalBlindspot1(playerCell, visualCellRef);
    }

    private static bool IsDiagonalBlindspot1(Vector2Int playerCell, Vector2Int bossRefCell)
    {
        int dx = playerCell.x - bossRefCell.x;
        int dy = playerCell.y - bossRefCell.y;
        return Mathf.Abs(dx) == 1 && Mathf.Abs(dy) == 1;
    }

    private static bool IsAxisPosition(Vector2Int playerCell, Vector2Int bossRefCell)
    {
        int dx = playerCell.x - bossRefCell.x;
        int dy = playerCell.y - bossRefCell.y;
        return dx == 0 || dy == 0;
    }

    private static int MinManhattanDistance(
        Vector2Int playerCell,
        Vector2Int bossCellRef,
        Vector2Int rootCellRef,
        Vector2Int visualCellRef)
    {
        int d0 = Mathf.Abs(playerCell.x - bossCellRef.x) + Mathf.Abs(playerCell.y - bossCellRef.y);
        int d1 = Mathf.Abs(playerCell.x - rootCellRef.x) + Mathf.Abs(playerCell.y - rootCellRef.y);
        int d2 = Mathf.Abs(playerCell.x - visualCellRef.x) + Mathf.Abs(playerCell.y - visualCellRef.y);
        return Mathf.Min(d0, Mathf.Min(d1, d2));
    }

    private float AxisVsDiagonalHitRateDelta()
    {
        float axisHitRate = axisPositionStepCount > 0
            ? (float)axisPositionPlayerHitCount / axisPositionStepCount
            : 0f;
        int diagonalSteps = Mathf.Max(diagonalBlindspotBossCellSteps,
            Mathf.Max(diagonalBlindspotRootSteps, diagonalBlindspotVisualSteps));
        float diagonalHitRate = diagonalSteps > 0
            ? (float)diagonalBlindspotPlayerHitCount / diagonalSteps
            : 0f;
        return axisHitRate - diagonalHitRate;
    }

    private void RecordSafeOppMoveTrace(
        int singleAction, float currentTime, Vector2Int playerCell, Vector2Int bossCell,
        int distanceBefore, int distanceAfter, bool wasInAttackRangeBeforeMove,
        bool remainedInAttackRangeAfterMove, int moveDistanceDelta,
        bool dangerNearby, bool nextWarning, bool nextDamage,
        bool nextRecentWarning, bool nextRecentDamage,
        int safeMoveAlternatives, int bossHpBefore)
    {
        safeOppMoveTraceTotalCount++;
        if (remainedInAttackRangeAfterMove) safeOppMoveKeptAttackRangeCount++;
        if (wasInAttackRangeBeforeMove && !remainedInAttackRangeAfterMove) safeOppMoveLeftAttackRangeCount++;
        if (moveDistanceDelta > 0 && !dangerNearby && !nextWarning && !nextDamage && !nextRecentWarning && !nextRecentDamage)
            safeOppMoveAwayWithoutDangerCount++;
        safeOppAttackWouldHaveBeenAllowedCount++;

        if (safeOppMoveTraceSampleCount >= MaxTraceSamplesPerEpisode ||
            globalSafeOppMoveTraceSamples >= MaxTraceSamplesPerRun)
            return;

        var trace = new SafeOppMoveTrace
        {
            episodeIndex = episodeIndex,
            step = oppInternalStep,
            actionName = ActionName(singleAction),
            playerCell = playerCell,
            bossCell = bossCell,
            distanceBefore = distanceBefore,
            distanceAfter = distanceAfter,
            remainedInAttackRangeAfterMove = remainedInAttackRangeAfterMove,
            leftAttackRangeAfterMove = wasInAttackRangeBeforeMove && !remainedInAttackRangeAfterMove,
            movedTowardBoss = moveDistanceDelta < 0,
            movedAwayFromBoss = moveDistanceDelta > 0,
            movedLateral = moveDistanceDelta == 0,
            dangerNearby = dangerNearby,
            nextWarning = nextWarning,
            nextDamage = nextDamage,
            nextRecentWarning = nextRecentWarning,
            nextRecentDamage = nextRecentDamage,
            safeMoveAlternatives = safeMoveAlternatives,
            attackAllowed = true,
            bossHpBefore = bossHpBefore,
            timestamp = currentTime,
            bossHpAfter05Recorded = false,
            bossHpAfter05 = bossHpBefore
        };

        pendingSafeOppMoveTraces.Add(trace);
        safeOppMoveTraceSampleCount++;
        globalSafeOppMoveTraceSamples++;

        Debug.Log(
            $"[BossRL] safe_opp_move_trace episode={trace.episodeIndex} step={trace.step} " +
            $"action={trace.actionName} player_cell={trace.playerCell} boss_cell={trace.bossCell} " +
            $"dist_before={trace.distanceBefore} dist_after={trace.distanceAfter} " +
            $"remained_in_attack_range_after_move={trace.remainedInAttackRangeAfterMove} " +
            $"left_attack_range_after_move={trace.leftAttackRangeAfterMove} " +
            $"toward={trace.movedTowardBoss} away={trace.movedAwayFromBoss} lateral={trace.movedLateral} " +
            $"danger_nearby={trace.dangerNearby} next_warn={trace.nextWarning} next_dmg={trace.nextDamage} " +
            $"next_recent_warn={trace.nextRecentWarning} next_recent_dmg={trace.nextRecentDamage} " +
            $"safe_move_alternatives={trace.safeMoveAlternatives} attack_allowed={trace.attackAllowed} " +
            $"boss_hp_before={trace.bossHpBefore}");
    }

    private void UpdatePendingSafeOppMoveTraces(float currentTime, bool gotHit, int currentBossHp)
    {
        for (int i = pendingSafeOppMoveTraces.Count - 1; i >= 0; i--)
        {
            SafeOppMoveTrace trace = pendingSafeOppMoveTraces[i];
            float elapsed = currentTime - trace.timestamp;
            if (gotHit && elapsed <= 1.0f) trace.hitWithin1s = true;
            if (gotHit && elapsed <= 2.0f) trace.hitWithin2s = true;
            if (!trace.bossHpAfter05Recorded && elapsed >= 0.5f)
            {
                trace.bossHpAfter05Recorded = true;
                trace.bossHpAfter05 = currentBossHp;
            }

            if (elapsed >= 2.0f)
            {
                FinalizeSafeOppMoveTrace(trace);
                pendingSafeOppMoveTraces.RemoveAt(i);
            }
            else
            {
                pendingSafeOppMoveTraces[i] = trace;
            }
        }
    }

    private void FinalizePendingSafeOppMoveTracesAtEpisodeEnd(float currentTime, int currentBossHp)
    {
        UpdatePendingSafeOppMoveTraces(currentTime + 2.1f, false, currentBossHp);
        for (int i = pendingSafeOppMoveTraces.Count - 1; i >= 0; i--)
        {
            SafeOppMoveTrace trace = pendingSafeOppMoveTraces[i];
            if (!trace.bossHpAfter05Recorded)
            {
                trace.bossHpAfter05Recorded = true;
                trace.bossHpAfter05 = currentBossHp;
            }
            FinalizeSafeOppMoveTrace(trace);
            pendingSafeOppMoveTraces.RemoveAt(i);
        }
    }

    private void FinalizeSafeOppMoveTrace(SafeOppMoveTrace trace)
    {
        bool tookDangerCell = trace.nextWarning || trace.nextDamage || trace.nextRecentWarning || trace.nextRecentDamage;
        int damageAfter05 = trace.bossHpBefore - trace.bossHpAfter05;
        bool noHitNoDamage = !trace.hitWithin2s && damageAfter05 <= 0;

        if (trace.dangerNearby && !trace.hitWithin2s && !tookDangerCell)
            safeOppMoveUsefulEscapeCount++;
        if (trace.hitWithin2s)
            safeOppMoveThenHitWithin2sCount++;
        if (noHitNoDamage)
            safeOppMoveThenNoHitNoDamageCount++;

        Debug.Log(
            $"[BossRL] safe_opp_move_trace_result episode={trace.episodeIndex} step={trace.step} " +
            $"boss_hp_before={trace.bossHpBefore} boss_hp_after_0.5s={trace.bossHpAfter05} " +
            $"boss_damage_after_0.5s={damageAfter05} hit_within_1s={trace.hitWithin1s} " +
            $"hit_within_2s={trace.hitWithin2s} useful_escape={trace.dangerNearby && !trace.hitWithin2s && !tookDangerCell} " +
            $"no_hit_no_damage={noHitNoDamage}");
    }

    private static string ActionName(int action)
    {
        switch (action)
        {
            case 1: return "MOVE_UP";
            case 2: return "MOVE_DOWN";
            case 3: return "MOVE_LEFT";
            case 4: return "MOVE_RIGHT";
            case 5: return "ATTACK";
            default: return "WAIT";
        }
    }

    private void UpdateWarningOnPlayerDiagnostics(
        int singleAction, int safeMoveCount,
        bool onWarning, bool onDamage, bool gotHit, Vector2Int bossFacing)
    {
        int frame = Time.frameCount;
        bool warningSpawnObserved = !pendingWarningOnPlayer && !oppLastStepOnWarning && onWarning;
        if (warningSpawnObserved)
        {
            pendingWarningOnPlayer = true;
            pendingWarningFrame = frame;
            pendingWarningDecisionStep = oppInternalStep;
            pendingWarningBossFacing = bossFacing;
            warningSpawnOnPlayerCount++;
            if (safeMoveCount > 0) warningSpawnOnPlayerSafeMoveAvailableCount++;
            if (BossRLInputBridge.IsWaitAction(singleAction)) warningSpawnOnPlayerChosenWaitCount++;
            if (BossRLInputBridge.IsAttackAction(singleAction)) warningSpawnOnPlayerChosenAttackCount++;
            if (BossRLInputBridge.IsMoveAction(singleAction)) warningSpawnOnPlayerChosenMoveCount++;
        }

        if (!pendingWarningOnPlayer) return;

        if (onDamage || gotHit)
        {
            int frameDelta = Mathf.Max(0, frame - pendingWarningFrame);
            warningSpawnToDamageFrameSum += frameDelta;
            warningSpawnToDamageFrameCount++;
            if (oppInternalStep == pendingWarningDecisionStep) noDecisionBeforeDamageCount++;
            else decisionAvailableBeforeDamageCount++;

            bool facingChanged = bossFacing != pendingWarningBossFacing;
            if (facingChanged) bossFacingChangedBetweenWarningAndDamageCount++;

            if (gotHit)
            {
                warningSpawnOnPlayerHitCount++;
                warningSpawnOnPlayerEscapeFailCount++;
                patternHitUnknownCount++;
                if (facingChanged)
                {
                    hitWhenBossFacingChangedCount++;
                    hitByRotatedDamageDirectionCount++;
                }
                else
                {
                    hitByOriginalWarningDirectionCount++;
                }
            }
            else if (!onWarning)
            {
                warningSpawnOnPlayerEscapeSuccessCount++;
            }

            pendingWarningOnPlayer = false;
            return;
        }

        if (!onWarning)
        {
            warningSpawnOnPlayerEscapeSuccessCount++;
            pendingWarningOnPlayer = false;
        }
    }

    // ── Mask decision recording ───────────────────────────────────────────────

    public void RecordMoveMaskDecision(int action, bool masked,
        bool isWall, bool nextWarning, bool nextDamage,
        bool nextRecentWarning, bool nextRecentDamage)
    {
        if (masked)
        {
            if (isWall)               moveMaskedWallCount++;
            else if (nextDamage)      { moveMaskedDamageCount++;        dangerousMoveAttemptCount++; }
            else if (nextWarning)     { moveMaskedWarningCount++;       dangerousMoveAttemptCount++; }
            else if (nextRecentDamage){ moveMaskedRecentDamageCount++;  dangerousMoveAttemptCount++; }
            else if (nextRecentWarning){ moveMaskedRecentWarningCount++; dangerousMoveAttemptCount++; }
        }
        else
        {
            moveAllowedCount++;
        }
    }

    public void RecordTargetAlignmentHit(BossRLTargetAlignmentDiagnostics.BossHitRecord record)
    {
        BossRLTargetAlignmentDiagnostics.BossVisualDiagnosticState visual = record.visualState;
        targetAlignmentLogCount++;
        bool visibleBodyOverlap = visual.spriteVisible &&
                                  (visual.visualCellInsideAttackCells ||
                                   visual.spriteBoundsCenterCellInsideAttackCells);
        bool currentVisualOverlap = visual.rootCurrentCellInsideAttackCells ||
                                    visibleBodyOverlap ||
                                    (visual.currentlyDashing && visual.dashCurrentCellInsideAttackCells);
        bool dashVisualOverlap = visual.currentlyDashing &&
                                 (visual.dashCurrentCellInsideAttackCells ||
                                  visual.rootCurrentCellInsideAttackCells ||
                                  visibleBodyOverlap);

        switch (visual.hitClass)
        {
            case BossRLTargetAlignmentDiagnostics.HitClass.NormalVisible:
                hitClassNormalVisibleCount++;
                break;
            case BossRLTargetAlignmentDiagnostics.HitClass.RootVisibleOverlap:
                hitClassRootVisibleOverlapCount++;
                break;
            case BossRLTargetAlignmentDiagnostics.HitClass.DashCurrentOverlap:
                hitClassDashCurrentOverlapCount++;
                dashAllowedCandidateCount++;
                dashHitAllowedCandidateCount++;
                break;
            case BossRLTargetAlignmentDiagnostics.HitClass.StaleBossCell:
                hitClassStaleBossCellCount++;
                if (visual.currentlyDashing) dashStaleBossCellOnlyCount++;
                break;
            case BossRLTargetAlignmentDiagnostics.HitClass.HiddenTarget:
                hitClassHiddenTargetCount++;
                break;
            case BossRLTargetAlignmentDiagnostics.HitClass.OffLaneEmpty:
                hitClassOffLaneEmptyCount++;
                if (visual.currentlyDashing) dashOffLaneEmptyCount++;
                break;
            default:
                hitClassUnknownRemainingCount++;
                break;
        }

        if (visual.rootCurrentCellInsideAttackCells) rootCellInAttackRangeCount++;
        if (visual.spriteBoundsCenterCellInsideAttackCells) spriteBoundsCellInAttackRangeCount++;
        if (visual.bossCellInsideAttackCells) bossCellInAttackRangeCount++;
        if (visual.rootCurrentCellInsideAttackCells && !visual.spriteBoundsCenterCellInsideAttackCells)
            rootInRangeSpriteBoundsOutCount++;
        if (visual.rootCurrentCellInsideAttackCells && !visual.spriteVisible)
            rootInRangeSpriteInvisibleCount++;
        if (visual.hitClass == BossRLTargetAlignmentDiagnostics.HitClass.RootVisibleOverlap)
            spriteBoundsOutButRootVisibleOverlapCount++;
        if (visual.currentlyDashing && visual.rootCurrentCellInsideAttackCells)
            dashCurrentRootOverlapCount++;
        if (dashVisualOverlap)
            dashCurrentVisualOverlapCount++;

        if (visual.spriteVisible) bossSpriteVisibleHitCount++;
        else bossSpriteInvisibleHitCount++;

        if (visual.bossCellInsideAttackCells && !visual.spriteVisible)
        {
            bossSpriteOffButBossCellHitCount++;
            bossCellInAttackRangeButSpriteNotVisibleCount++;
        }

        if (visual.visualRootExists &&
            (!visual.visualRootActiveSelf || !visual.visualRootActiveInHierarchy))
        {
            bossVisualRootInactiveHitCount++;
        }

        if (!visual.primarySpriteRendererEnabled)
        {
            bossRendererDisabledHitCount++;
        }

        if (visual.anyRendererAlphaZero)
        {
            bossAlphaZeroHitCount++;
        }

        if (visual.bossCellInsideAttackCells &&
            visual.spriteVisible &&
            !visual.visualCellInsideAttackCells)
        {
            bossCellInAttackRangeButSpriteCellOutOfRangeCount++;
        }

        if (visual.spriteVisible && visual.visualCellInsideAttackCells)
        {
            spriteVisibleCellInAttackRangeCount++;
        }
        else if (visual.spriteVisible)
        {
            spriteVisibleCellOutOfAttackRangeCount++;
        }

        if (visual.currentlyHidden || !visual.spriteVisible)
        {
            hiddenBossAttackRewardCount++;
        }

        if (visual.currentlyDashing && currentVisualOverlap)
        {
            dashHitCurrentVisualOverlapCount++;
        }

        if (visual.bossCellInsideAttackCells && !currentVisualOverlap)
        {
            dashHitBossCellOnlyCount++;
        }

        if (visual.currentlyDashing &&
            attackCellContains(record.attackCells, visual.dashStartCell) &&
            !visual.dashCurrentCellInsideAttackCells)
        {
            dashHitPreviousPositionSuspectCount++;
        }

        if (visual.currentlyDashing &&
            attackCellContains(record.attackCells, visual.dashEndCell) &&
            !visual.dashCurrentCellInsideAttackCells)
        {
            dashHitFutureEndCellSuspectCount++;
        }

        if (!visual.attackCellsOverlapAnyActiveLane)
        {
            dashHitOutsideActiveLaneCount++;
        }

        if (visual.activeDamageCellsOverlapAttackCells)
        {
            dashHitInsideActiveDamageLaneCount++;
        }

        if (visual.activeWarningCellsOverlapAttackCells)
        {
            dashHitInsideActiveWarningLaneCount++;
        }

        string rendererBoundsCenter = visual.hasRendererBoundsCenter
            ? visual.rendererBoundsCenter.ToString()
            : "<none>";
        string gridOccupantCell = visual.hasBossGridOccupant
            ? $"({visual.bossGridOccupantCell.x},{visual.bossGridOccupantCell.y})"
            : "<none>";

        Debug.Log(
            $"[BossRL] target_alignment_hit episode={record.episode} step={record.step} " +
            $"hit_class={BossRLTargetAlignmentDiagnostics.FormatHitClass(visual.hitClass)} " +
            $"time={record.time:F3} player_cell=({record.playerCell.x},{record.playerCell.y}) " +
            $"player_facing=({record.playerFacing.x},{record.playerFacing.y}) " +
            $"attack_cells={BossRLTargetAlignmentDiagnostics.FormatCells(record.attackCells)} " +
            $"boss_cell=({record.bossCell.x},{record.bossCell.y}) " +
            $"bosscell_in_attack={visual.bossCellInsideAttackCells} " +
            $"sprite_visible={visual.spriteVisible} visual_root_active_self={visual.visualRootActiveSelf} " +
            $"visual_root_active_hierarchy={visual.visualRootActiveInHierarchy} " +
            $"sprite_renderer_enabled={visual.primarySpriteRendererEnabled} " +
            $"sprite_alpha={visual.primarySpriteAlpha:F3} sprite_object={visual.primarySpriteObjectName} " +
            $"boss_visual_pos={visual.visualTransformPosition} visual_cell=({visual.visualEstimatedCell.x},{visual.visualEstimatedCell.y}) " +
            $"visual_cell_in_attack={visual.visualCellInsideAttackCells} " +
            $"sprite_bounds_cell=({visual.spriteBoundsCenterCell.x},{visual.spriteBoundsCenterCell.y}) " +
            $"sprite_bounds_cell_in_attack={visual.spriteBoundsCenterCellInsideAttackCells} " +
            $"boss_health_pos={record.bossHealthPosition} " +
            $"boss_root_pos={visual.rootTransformPosition} renderer_bounds_center={rendererBoundsCenter} " +
            $"root_cell=({visual.rootCurrentCell.x},{visual.rootCurrentCell.y}) root_cell_in_attack={visual.rootCurrentCellInsideAttackCells} " +
            $"grid_occupant_cell={gridOccupantCell} " +
            $"hidden={visual.currentlyHidden} dashing={visual.currentlyDashing} markdash={visual.currentlyMarkDash} " +
            $"dash_start=({visual.dashStartCell.x},{visual.dashStartCell.y}) dash_end=({visual.dashEndCell.x},{visual.dashEndCell.y}) " +
            $"dash_current=({visual.dashCurrentCell.x},{visual.dashCurrentCell.y}) dash_current_in_attack={visual.dashCurrentCellInsideAttackCells} " +
            $"dash_line_overlap_attack={visual.dashLineCellsOverlapAttackCells} " +
            $"active_damage_overlap_attack={visual.activeDamageCellsOverlapAttackCells} " +
            $"active_warning_overlap_attack={visual.activeWarningCellsOverlapAttackCells} " +
            $"attack_cells_overlap_any_active_lane={visual.attackCellsOverlapAnyActiveLane} " +
            $"boss_hp_before={record.bossHpBefore} boss_hp_after={record.bossHpAfter} damage={record.damageAmount}");
    }

    private static bool attackCellContains(IReadOnlyList<Vector2Int> attackCells, Vector2Int cell)
    {
        if (attackCells == null)
        {
            return false;
        }

        for (int i = 0; i < attackCells.Count; i++)
        {
            if (attackCells[i] == cell)
            {
                return true;
            }
        }

        return false;
    }

    public void RecordAttackMaskDecision(bool masked, bool survivalStage,
        bool notReady, bool outOfRange, bool onWarning, bool onDamage,
        bool onRecentWarning, bool onRecentDamage)
    {
        if (masked)
        {
            attackMaskedTotalCount++;
            if (survivalStage)        attackMaskedSurvivalStageCount++;
            else if (notReady)        attackMaskedNotReadyCount++;
            else if (outOfRange)      attackMaskedOutOfRangeCount++;
            else if (onWarning)       attackMaskedOnWarningCount++;
            else if (onDamage)        attackMaskedOnDamageCount++;
            else if (onRecentWarning) attackMaskedOnRecentWarningCount++;
            else if (onRecentDamage)  attackMaskedOnRecentDamageCount++;
        }
        else
        {
            attackAllowedCount++;
        }
    }

    // ── Per-step recording ────────────────────────────────────────────────────

    private PlayerHealth GetPlayerHealth()
    {
        if (cachedPlayerHealth == null)
            cachedPlayerHealth = FindFirstObjectByType<PlayerHealth>();

        return cachedPlayerHealth;
    }

    private static void AddCount(Dictionary<string, int> counts, string key, int amount)
    {
        if (amount <= 0)
            return;

        key = string.IsNullOrEmpty(key) ? "unknown" : key;
        counts.TryGetValue(key, out int current);
        counts[key] = current + amount;
    }

    private static string FormatCounts(Dictionary<string, int> counts)
    {
        if (counts == null || counts.Count == 0)
            return "none";

        string output = "";
        foreach (KeyValuePair<string, int> kv in counts)
        {
            if (output.Length > 0)
                output += ",";

            output += $"{kv.Key}:{kv.Value}";
        }

        return output;
    }

    public void RecordStep(
        int stepCount, float currentTime,
        int singleAction, bool isAttackAction, int attackActionInt,
        BossRLInputBridge.MoveOutcome moveOutcome, Vector2Int moveDirection,
        BossRLReward.StepResult result,
        bool bossInRange = false, float manhattanDist = 99f,
        bool onRecentWarning = false, bool onRecentDamage = false)
    {
        // Reward accumulators
        rewardTotal            += result.reward;
        rewardBossDamage       += result.rewardBossDamage;
        rewardHitPenalty       += result.rewardHitPenalty;
        rewardCriticalHealthPenalty += result.rewardCriticalHealthPenalty;
        rewardDeathPenalty     += result.rewardDeathPenalty;
        rewardFastClear        += result.rewardFastClear;
        rewardWarningTile      += result.rewardWarningTile;
        rewardDamageTile       += result.rewardDamageTile;
        rewardWallBlocked      += result.rewardWallBlocked;
        rewardApproach         += result.rewardApproach;
        rewardMissedAttack     += result.rewardMissedAttack;
        rewardAttackOnCooldown += result.rewardAttackOnCooldown;
        rewardMovedIntoDanger  += result.rewardMovedIntoDanger;
        rewardSafeAttack       += result.rewardSafeAttack;
        rewardMissedSafeAttackOpportunity += result.rewardMissedSafeAttackOpportunity;

        // Boss damage
        if (!firstHitRecorded) bossDamageBeforeFirstHit += result.bossDamageDelta;
        bossDamageTotal += result.bossDamageDelta;
        if (result.bossDamageDelta > 0) successfulHitSteps++;

        // Tile presence
        if (result.onWarningTile) warningTileSteps++;
        if (result.onDamageTile)  damageTileSteps++;

        // Player hit
        if (result.playerHitDelta > 0)
        {
            playerHitCount += result.playerHitDelta;
            playerDamageStepCount++;
            maxPlayerHpLossInSingleStep = Mathf.Max(maxPlayerHpLossInSingleStep, result.playerHitDelta);
            PlayerHealth playerHealth = GetPlayerHealth();
            AddCount(damageSourceHitCounts, playerHealth?.LastDamageSource ?? "unknown", result.playerHitDelta);
            AddCount(damageSourceGroupHitCounts, playerHealth?.LastDamageSourceGroup ?? "unknown", result.playerHitDelta);
            if (!firstHitRecorded)
            {
                firstHitRecorded = true;
                firstHitStep     = stepCount;
                firstHitTime     = currentTime - episodeStartTime;
            }
        }

        // Survival milestones
        float elapsed = currentTime - episodeStartTime;
        if (!survived10s && elapsed >= 10f) survived10s = true;
        if (!survived20s && elapsed >= 20f) survived20s = true;

        // Attack range
        if (bossInRange) bossInAttackRangeSteps++;

        // Wait action tracking
        if (BossRLInputBridge.IsWaitAction(singleAction)) waitActionCount++;

        // Attack tracking
        if (isAttackAction)
        {
            attackActionCount++;
            if (result.onWarningTile) attackWhenOnWarning++;
            if (result.onDamageTile)  attackWhenOnDamage++;
            if (onRecentWarning)      attackWhenRecentWarning++;
            if (onRecentDamage)       attackWhenRecentDamage++;
            if (result.missedAttack)      missedAttackCount++;
            if (result.attackOnCooldown)  attackOnCooldownCount++;
            if (bossInRange) attackWhenBossInRange++;
            else             attackWhenBossOutOfRange++;
            bossDistanceSumAtAttack += manhattanDist;
            bossDistanceSampleCount++;
            if (!firstAttackRecorded)
            {
                firstAttackRecorded        = true;
                bossDistanceAtFirstAttack  = manhattanDist;
            }
        }

        // Move tracking
        bool isMoveAction = BossRLInputBridge.IsMoveAction(singleAction);
        if (isMoveAction)
        {
            movementActionCount++;
            if (moveOutcome == BossRLInputBridge.MoveOutcome.WillMove)
            {
                successfulMoveCount++;
            }
            else if (moveOutcome == BossRLInputBridge.MoveOutcome.WallBlocked)
            {
                wallBlockedMoves++;
                totalMoveAttempts++;
                if      (moveDirection == Vector2Int.up)    wallBlockedUp++;
                else if (moveDirection == Vector2Int.down)  wallBlockedDown++;
                else if (moveDirection == Vector2Int.left)  wallBlockedLeft++;
                else if (moveDirection == Vector2Int.right) wallBlockedRight++;
            }
            if (moveOutcome != BossRLInputBridge.MoveOutcome.Busy &&
                moveOutcome != BossRLInputBridge.MoveOutcome.None)
                totalMoveAttempts++;
        }

        // Recent danger presence
        if (onRecentWarning) playerOnRecentWarningSteps++;
        if (onRecentDamage)  playerOnRecentDamageSteps++;

        // Warning → damage re-entry
        if (prevOnWarning && !result.onWarningTile)
            lastWarningExitStep = stepCount;

        bool inReentryWindow = (stepCount - lastWarningExitStep) <= ReentryWindow && lastWarningExitStep >= 0;
        if (inReentryWindow)
        {
            if (result.onDamageTile || result.onWarningTile) recentWarningReentryCount++;
            if (result.onDamageTile) damageSoonAfterWarning++;
            if (result.playerHitDelta > 0) { warningToDamageHitCount++; hitAfterWarningAvoidCount++; }
        }
        prevOnWarning = result.onWarningTile;

        // Periodic log
        if (logEveryNSteps > 0 && stepCount > 0 && stepCount % logEveryNSteps == 0)
        {
            float wallRatio = totalMoveAttempts > 0 ? (float)wallBlockedMoves / totalMoveAttempts : 0f;
            Debug.Log(
                $"[BossRL] step={stepCount} r={result.reward:F3} " +
                $"onWarn={result.onWarningTile} onDmg={result.onDamageTile} " +
                $"act={singleAction}(atk={isAttackAction}) inRange={bossInRange} " +
                $"move={moveOutcome} wallRatio={wallRatio:P0} " +
                $"warnReentry={recentWarningReentryCount}");
        }
    }

    public void RecordTerminalReward(string reason, float reward)
    {
        rewardTotal += reward;
        if (reason == "player_dead")
        {
            rewardDeathPenalty += reward;
        }
        else if (reason == "boss_dead")
        {
            rewardBossDamage += reward;
        }

        Debug.Log($"[BossRL] terminal_reward reason={reason} value={reward:F3}");
    }

    public void RecordDelayedMissedSafeOpportunityPenalty(float penalty)
    {
        rewardTotal += penalty;
        rewardMissedSafeAttackOpportunity += penalty;
    }

    // ── Episode summary ───────────────────────────────────────────────────────
    public void LogEpisodeEnd(string reason, int stepCount, float currentTime, BossRLStateExtractor extractor)
    {
        float survivalTime = currentTime - episodeStartTime;
        int   bossHpLeft   = extractor != null ? extractor.BossCurrentHp : 0;
        int   recentWarnCellsNow = extractor != null ? extractor.RecentWarningCellCount : 0;
        int   recentDmgCellsNow  = extractor != null ? extractor.RecentDamageCellCount  : 0;
        PlayerHealth playerHealth = GetPlayerHealth();
        string lastDamageSource = playerHealth != null ? playerHealth.LastDamageSource : "unknown";
        string lastDamageSourceGroup = playerHealth != null ? playerHealth.LastDamageSourceGroup : "unknown";
        string deathSource = reason == "player_dead" ? lastDamageSource : "none";
        string deathSourceGroup = reason == "player_dead" ? lastDamageSourceGroup : "none";
        if (reason == "player_dead") deathTime = survivalTime;
        FinalizePendingSafeOppMoveTracesAtEpisodeEnd(currentTime, bossHpLeft);

        float wallRatio   = totalMoveAttempts > 0 ? (float)wallBlockedMoves / totalMoveAttempts : 0f;
        float avgBossDist = bossDistanceSampleCount > 0 ? bossDistanceSumAtAttack / bossDistanceSampleCount : -1f;
        float avgDistanceToBoss = distanceToBossSampleCount > 0 ? distanceToBossSum / distanceToBossSampleCount : -1f;
        int minDist = distanceToBossSampleCount > 0 ? minDistanceToBoss : -1;
        int maxDist = distanceToBossSampleCount > 0 ? maxDistanceToBoss : -1;
        float hitRate     = attackActionCount > 0 ? (float)successfulHitSteps / attackActionCount : 0f;
        float bossHpPerAtk= attackActionCount > 0 ? (float)bossDamageTotal    / attackActionCount : 0f;
        float safeOppAttackRatio = safeAttackOpportunitySteps > 0 ? (float)safeAttackTakenCount / safeAttackOpportunitySteps : 0f;
        float safeOppWaitRatio   = safeAttackOpportunitySteps > 0 ? (float)safeOppWaitCount / safeAttackOpportunitySteps : 0f;
        float safeOppMoveRatio   = safeAttackOpportunitySteps > 0 ? (float)safeOppMoveCount / safeAttackOpportunitySteps : 0f;
        float warningToDamageFramesAvg = warningSpawnToDamageFrameCount > 0
            ? (float)warningSpawnToDamageFrameSum / warningSpawnToDamageFrameCount
            : -1f;

        Debug.Log(
            $"[BossRL] EPISODE_END reason={reason} steps={stepCount} survival={survivalTime:F1}s " +
            $"survived10s={survived10s} survived20s={survived20s}\n" +
            $"  reward: total={rewardTotal:F3} boss={rewardBossDamage:F3} hit={rewardHitPenalty:F3} " +
            $"critical_hp={rewardCriticalHealthPenalty:F3} death={rewardDeathPenalty:F3} " +
            $"fast_clear={rewardFastClear:F3} warn_tile={rewardWarningTile:F3} dmg_tile={rewardDamageTile:F3} " +
            $"wall={rewardWallBlocked:F3} approach={rewardApproach:F3} " +
            $"missed_atk={rewardMissedAttack:F3} cooldown_atk={rewardAttackOnCooldown:F3} " +
            $"moved_into_danger={rewardMovedIntoDanger:F3} safe_atk={rewardSafeAttack:F3} " +
            $"missed_safe_opp={rewardMissedSafeAttackOpportunity:F3}\n" +
            $"  boss: hp_start={episodeBossHpStart} hp_left={bossHpLeft} dmg_dealt={bossDamageTotal} " +
            $"dmg_before_first_hit={bossDamageBeforeFirstHit}\n" +
            $"  move_mask: wall(geometry)={moveMaskedWallCount} warn={moveMaskedWarningCount}(should=0) " +
            $"dmg={moveMaskedDamageCount} recent_warn={moveMaskedRecentWarningCount}(should=0) " +
            $"recent_dmg={moveMaskedRecentDamageCount}(should=0) " +
            $"due_to_busy={moveMaskedDueToBusyCount}(must=0) allowed={moveAllowedCount} " +
            $"dangerous_attempts={dangerousMoveAttemptCount}\n" +
            $"  move_escape: wait_only_states={waitOnlyMaskStateCount} " +
            $"geo={waitOnlyDueToGeometryCount} dmg={waitOnlyDueToDamageCount} " +
            $"busy={waitOnlyDueToBusyCount}(must=0) boss_cell={waitOnlyDueToBossCellCount}(must=0) " +
            $"escape_forced_open={escapeActionForcedOpenCount} " +
            $"boss_cell_blocked={bossCellTreatedAsBlockedCount}(must=0) " +
            $"boss_cell_allowed={bossCellMoveAllowedCount} " +
            $"busy_at_mask={moveBusyStateCount} " +
            $"wait_actions={waitActionCount}\n" +
            $"  attack_mask: total_masked={attackMaskedTotalCount} allowed(safe_in_range)={attackAllowedCount} " +
            $"survival_stage={attackMaskedSurvivalStageCount} not_ready={attackMaskedNotReadyCount} " +
            $"out_of_range={attackMaskedOutOfRangeCount} on_warn={attackMaskedOnWarningCount} " +
            $"on_dmg={attackMaskedOnDamageCount} on_recent_warn={attackMaskedOnRecentWarningCount} " +
            $"on_recent_dmg={attackMaskedOnRecentDamageCount}\n" +
            $"  attack_quality: actions={attackActionCount} hits={successfulHitSteps} " +
            $"missed={missedAttackCount} cooldown={attackOnCooldownCount} " +
            $"hit_rate={hitRate:P1} dmg_per_atk={bossHpPerAtk:F3}\n" +
            $"  target_alignment: logs={targetAlignmentLogCount} " +
            $"boss_sprite_visible_hit_count={bossSpriteVisibleHitCount} " +
            $"boss_sprite_invisible_hit_count={bossSpriteInvisibleHitCount} " +
            $"boss_sprite_off_but_bosscell_hit_count={bossSpriteOffButBossCellHitCount} " +
            $"boss_visual_root_inactive_hit_count={bossVisualRootInactiveHitCount} " +
            $"boss_renderer_disabled_hit_count={bossRendererDisabledHitCount} " +
            $"boss_alpha_zero_hit_count={bossAlphaZeroHitCount} " +
            $"bosscell_in_attack_range_but_sprite_not_visible_count={bossCellInAttackRangeButSpriteNotVisibleCount} " +
            $"bosscell_in_attack_range_but_sprite_cell_out_of_range_count={bossCellInAttackRangeButSpriteCellOutOfRangeCount} " +
            $"sprite_visible_cell_in_attack_range_count={spriteVisibleCellInAttackRangeCount} " +
            $"sprite_visible_cell_out_of_attack_range_count={spriteVisibleCellOutOfAttackRangeCount} " +
            $"hidden_boss_attack_reward_count={hiddenBossAttackRewardCount}\n" +
            $"  dash_target_alignment: hit_class_normal_visible_count={hitClassNormalVisibleCount} " +
            $"hit_class_root_visible_overlap_count={hitClassRootVisibleOverlapCount} " +
            $"hit_class_dash_current_overlap_count={hitClassDashCurrentOverlapCount} " +
            $"hit_class_stale_bosscell_count={hitClassStaleBossCellCount} " +
            $"hit_class_hidden_target_count={hitClassHiddenTargetCount} " +
            $"hit_class_off_lane_empty_count={hitClassOffLaneEmptyCount} " +
            $"hit_class_unknown_remaining_count={hitClassUnknownRemainingCount} " +
            $"root_cell_in_attack_range_count={rootCellInAttackRangeCount} " +
            $"sprite_bounds_cell_in_attack_range_count={spriteBoundsCellInAttackRangeCount} " +
            $"boss_cell_in_attack_range_count={bossCellInAttackRangeCount} " +
            $"root_in_range_sprite_bounds_out_count={rootInRangeSpriteBoundsOutCount} " +
            $"root_in_range_sprite_invisible_count={rootInRangeSpriteInvisibleCount} " +
            $"sprite_bounds_out_but_root_visible_overlap_count={spriteBoundsOutButRootVisibleOverlapCount} " +
            $"dash_current_root_overlap_count={dashCurrentRootOverlapCount} " +
            $"dash_current_visual_overlap_count={dashCurrentVisualOverlapCount} " +
            $"dash_allowed_candidate_count={dashAllowedCandidateCount} " +
            $"dash_stale_bosscell_only_count={dashStaleBossCellOnlyCount} " +
            $"dash_off_lane_empty_count={dashOffLaneEmptyCount} " +
            $"dash_hit_allowed_candidate_count={dashHitAllowedCandidateCount} " +
            $"dash_hit_current_visual_overlap_count={dashHitCurrentVisualOverlapCount} " +
            $"dash_hit_bosscell_only_count={dashHitBossCellOnlyCount} " +
            $"dash_hit_previous_position_suspect_count={dashHitPreviousPositionSuspectCount} " +
            $"dash_hit_future_endcell_suspect_count={dashHitFutureEndCellSuspectCount} " +
            $"dash_hit_outside_active_lane_count={dashHitOutsideActiveLaneCount} " +
            $"dash_hit_inside_active_damage_lane_count={dashHitInsideActiveDamageLaneCount} " +
            $"dash_hit_inside_active_warning_lane_count={dashHitInsideActiveWarningLaneCount}\n" +
            $"  diagonal_blindspot_wiggle_diag: diagonal_blindspot_bosscell_steps={diagonalBlindspotBossCellSteps} " +
            $"diagonal_blindspot_root_steps={diagonalBlindspotRootSteps} " +
            $"diagonal_blindspot_visual_steps={diagonalBlindspotVisualSteps} " +
            $"diagonal_blindspot_attack_count={diagonalBlindspotAttackCount} " +
            $"diagonal_blindspot_attack_hit_count={diagonalBlindspotAttackHitCount} " +
            $"diagonal_blindspot_move_count={diagonalBlindspotMoveCount} " +
            $"diagonal_blindspot_back_and_forth_count={diagonalBlindspotBackAndForthCount} " +
            $"diagonal_blindspot_axis_exit_count={diagonalBlindspotAxisExitCount} " +
            $"diagonal_blindspot_kept_safe_count={diagonalBlindspotKeptSafeCount} " +
            $"diagonal_blindspot_then_player_hit_count={diagonalBlindspotThenPlayerHitCount} " +
            $"diagonal_blindspot_boss_melee_whiff_candidate_count={diagonalBlindspotBossMeleeWhiffCandidateCount} " +
            $"attack_after_diagonal_wiggle_count={attackAfterDiagonalWiggleCount} " +
            $"axis_position_step_count={axisPositionStepCount} " +
            $"axis_position_player_hit_count={axisPositionPlayerHitCount} " +
            $"axis_position_boss_melee_hit_count={axisPositionBossMeleeHitCount} " +
            $"diagonal_blindspot_player_hit_count={diagonalBlindspotPlayerHitCount} " +
            $"diagonal_blindspot_boss_melee_hit_count={diagonalBlindspotBossMeleeHitCount} " +
            $"axis_vs_diagonal_hit_rate_delta={AxisVsDiagonalHitRateDelta():F3} " +
            $"player_in_boss_front_axis={playerInBossFrontAxisCount} " +
            $"player_in_boss_back_axis={playerInBossBackAxisCount} " +
            $"player_in_boss_left_axis={playerInBossLeftAxisCount} " +
            $"player_in_boss_right_axis={playerInBossRightAxisCount} " +
            $"player_in_boss_diagonal_blindspot={playerInBossDiagonalBlindspotCount}\n" +
            $"  action_histogram: wait={actionWaitCount} move_up={actionMoveUpCount} " +
            $"move_down={actionMoveDownCount} move_left={actionMoveLeftCount} " +
            $"move_right={actionMoveRightCount} attack={actionAttackCount}\n" +
            $"  attack_range: in_range_steps={bossInAttackRangeSteps} atk_in_range={attackWhenBossInRange} " +
            $"atk_out_range={attackWhenBossOutOfRange} avg_dist={avgBossDist:F2} " +
            $"first_atk_dist={bossDistanceAtFirstAttack:F2}\n" +
            $"  distance_position: avg_distance_to_boss={avgDistanceToBoss:F2} " +
            $"min_distance_to_boss={minDist} max_distance_to_boss={maxDist} " +
            $"time_in_attack_range_steps={timeInAttackRangeSteps} " +
            $"time_out_of_attack_range_steps={timeOutOfAttackRangeSteps} " +
            $"entered_attack_range_count={enteredAttackRangeCount} " +
            $"left_attack_range_count={leftAttackRangeCount}\n" +
            $"  hazard: warn_steps={warningTileSteps} dmg_steps={damageTileSteps} " +
            $"atk_on_warn={attackWhenOnWarning} atk_on_dmg={attackWhenOnDamage} " +
            $"atk_on_recent_warn={attackWhenRecentWarning} atk_on_recent_dmg={attackWhenRecentDamage}\n" +
            $"  recent_danger: on_recent_warn_steps={playerOnRecentWarningSteps} " +
            $"on_recent_dmg_steps={playerOnRecentDamageSteps} " +
            $"recent_warn_cells_now={recentWarnCellsNow} recent_dmg_cells_now={recentDmgCellsNow}\n" +
            $"  warn_reentry: reentry={recentWarningReentryCount} dmg_soon={damageSoonAfterWarning} " +
            $"hit_soon={warningToDamageHitCount} hit_after_avoid={hitAfterWarningAvoidCount}\n" +
            $"  opportunity: safe_opp_steps={safeAttackOpportunitySteps} " +
            $"safe_taken={safeAttackTakenCount} safe_hit={safeAttackHitCount} safe_missed={safeAttackMissedCount} " +
            $"in_range_no_attack={bossInRangeNoAttackCount} atk_out_of_range={attackOutOfRangeCount} " +
            $"atk_danger_nearby={attackWhenDangerNearbyCount}\n" +
            $"  safe_opp_action: total={safeAttackOpportunitySteps} attack={safeAttackTakenCount} " +
            $"wait={safeOppWaitCount} move={safeOppMoveCount} missed={missedAllowedAttackCount} " +
            $"attack_ratio={safeOppAttackRatio:P1} wait_ratio={safeOppWaitRatio:P1} move_ratio={safeOppMoveRatio:P1} " +
            $"move_toward_boss={safeOppMoveTowardBossCount} move_away_from_boss={safeOppMoveAwayFromBossCount} " +
            $"move_lateral={safeOppMoveLateralCount}\n" +
            $"  safe_opp_move_trace_summary: samples={safeOppMoveTraceSampleCount} total_moves={safeOppMoveTraceTotalCount} " +
            $"useful_escape={safeOppMoveUsefulEscapeCount} kept_attack_range={safeOppMoveKeptAttackRangeCount} " +
            $"left_attack_range={safeOppMoveLeftAttackRangeCount} away_without_danger={safeOppMoveAwayWithoutDangerCount} " +
            $"hit_within_2s={safeOppMoveThenHitWithin2sCount} no_hit_no_damage={safeOppMoveThenNoHitNoDamageCount} " +
            $"attack_would_have_been_allowed={safeOppAttackWouldHaveBeenAllowedCount}\n" +
            $"  warning_on_player: spawn_count={warningSpawnOnPlayerCount} " +
            $"safe_move_available={warningSpawnOnPlayerSafeMoveAvailableCount} " +
            $"escape_success={warningSpawnOnPlayerEscapeSuccessCount} escape_fail={warningSpawnOnPlayerEscapeFailCount} " +
            $"hit={warningSpawnOnPlayerHitCount} chosen_wait={warningSpawnOnPlayerChosenWaitCount} " +
            $"chosen_attack={warningSpawnOnPlayerChosenAttackCount} chosen_move={warningSpawnOnPlayerChosenMoveCount} " +
            $"spawn_to_damage_frames_avg={warningToDamageFramesAvg:F1} " +
            $"decision_before_damage={decisionAvailableBeforeDamageCount} " +
            $"no_decision_before_damage={noDecisionBeforeDamageCount}\n" +
            $"  boss_facing_diag: facing_changed_warning_to_damage={bossFacingChangedBetweenWarningAndDamageCount} " +
            $"hit_when_facing_changed={hitWhenBossFacingChangedCount} " +
            $"hit_by_original_warning_direction={hitByOriginalWarningDirectionCount} " +
            $"hit_by_rotated_damage_direction={hitByRotatedDamageDirectionCount} " +
            $"pattern_hit_unknown={patternHitUnknownCount}\n" +
            $"  attack_allowed_action: allowed_steps={attackAllowedActionStepCount} " +
            $"chosen_attack_when_allowed={chosenAttackWhenAllowedCount} " +
            $"missed_allowed_attack={missedAllowedAttackCount}\n" +
            $"  delayed_hit: safe_same_step={safeAttackHitSameStep} safe_0.3s={safeAttackHitWithin03s} " +
            $"safe_0.5s={safeAttackHitWithin05s} out_of_range_0.5s={outOfRangeAttackHitWithin05s} " +
            $"unsafe_0.5s={unsafeAttackHitWithin05s}\n" +
            $"  move_danger_mask: warn_when_safe={moveMaskedWarnWhenSafeCount} recent_warn_when_safe={moveMaskedRecentWarnWhenSafeCount} " +
            $"dmg_when_safe={moveMaskedDmgWhenSafeCount} least_danger_opened={leastDangerEscapeOpenedCount} " +
            $"wait_masked={waitMaskedDueToDangerCount} safe_avail_steps={safeMoveAvailableStepCount} " +
            $"chose_safe={choseSafeMoveCount} chose_danger={choseDangerMoveCount}\n" +
            $"  danger: nearby_steps={dangerNearbySteps} wait_while_danger={waitWhileDangerNearbyCount} " +
            $"safe_move={dangerNearbySafeMoveCount} danger_move={dangerNearbyDangerMoveCount} " +
            $"attack={attackWhenDangerNearbyCount} " +
            $"move_into_warn={movedIntoWarningCount} move_into_dmg={movedIntoDamageCount} " +
            $"move_into_recent_warn={movedIntoRecentWarningCount} move_into_recent_dmg={movedIntoRecentDamageCount}\n" +
            $"  hit_analysis: hit_on_warn={hitOnWarningTileCount} hit_on_dmg={hitOnDamageTileCount} " +
            $"hit_on_recent_warn={hitOnRecentWarningTileCount} " +
            $"hit_safe_move_avail={hitWhileSafeMoveAvailableCount} avoidable={avoidableHitCount} " +
            $"hit_after_move_danger={hitAfterMovingIntoDangerCount} " +
            $"warn_to_hit_avg_steps={(warningToHitStepCount > 0 ? (float)warningToHitStepSum / warningToHitStepCount : -1f):F1}\n" +
            $"  player: hits={playerHitCount} first_hit_step={firstHitStep} " +
            $"first_hit_time={firstHitTime:F1}s death_time={deathTime:F1}s " +
            $"hp_after={extractor?.PlayerCurrentHp ?? -1} hits_remaining_est={Mathf.Max(0, extractor?.PlayerCurrentHp ?? 0)} " +
            $"damage_steps={playerDamageStepCount} max_hp_loss_step={maxPlayerHpLossInSingleStep} " +
            $"death_source={deathSource} death_source_group={deathSourceGroup} " +
            $"damage_source_hits={FormatCounts(damageSourceHitCounts)} " +
            $"damage_source_group_hits={FormatCounts(damageSourceGroupHitCounts)}\n" +
            $"  move: actions={movementActionCount} success={successfulMoveCount} " +
            $"wall_blocked={wallBlockedMoves} wall_ratio={wallRatio:P0} " +
            $"(U={wallBlockedUp} D={wallBlockedDown} L={wallBlockedLeft} R={wallBlockedRight}) " +
            $"wait_actions={waitActionCount}");
    }

    // ── Legacy stubs ──────────────────────────────────────────────────────────
    public void LogStep(int stepCount, float reward, BossRLStateExtractor extractor) { }

    public void LogEpisodeEnd(string reason, int stepCount, float cumulativeReward)
    {
        Debug.Log($"[BossRL] episode_end reason={reason} step={stepCount} cumR={cumulativeReward:F3}");
    }

    public void LogWarningOnce(string key, string message)
    {
        if (string.IsNullOrEmpty(key) || lastWarningKey == key) return;
        lastWarningKey = key;
        Debug.LogWarning(message);
    }

    // ── Legacy RecordMaskDecision (old attack-only signature) ─────────────────
    public void RecordMaskDecision(bool maskAttack, bool notReady, bool outOfRange, bool onWarning, bool onDamage)
    {
        RecordAttackMaskDecision(maskAttack, false, notReady, outOfRange, onWarning, onDamage, false, false);
    }
}
