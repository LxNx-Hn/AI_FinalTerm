using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BossRLDebugLogger : MonoBehaviour
{
    [SerializeField] private int logEveryNSteps = 100;

    private string lastWarningKey;

    // ── Episode bookkeeping ───────────────────────────────────────────────────
    private float episodeStartTime;
    private int   episodeBossHpStart;

    // ── Reward breakdown ──────────────────────────────────────────────────────
    private float rewardTotal, rewardBossDamage, rewardHitPenalty, rewardDeathPenalty;
    private float rewardWarningTile, rewardDamageTile, rewardWallBlocked;
    private float rewardApproach, rewardMissedAttack, rewardAttackOnCooldown;
    private float rewardMovedIntoDanger, rewardSafeAttack;

    // ── Tile / hazard stats ───────────────────────────────────────────────────
    private int warningTileSteps;
    private int damageTileSteps;

    // ── Player hit stats ──────────────────────────────────────────────────────
    private int   playerHitCount;
    private float deathTime;

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
    private int bossInRangeNoAttackCount;
    private int attackOutOfRangeCount;
    private int attackWhenDangerNearbyCount;
    private int dangerNearbySteps;
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
        episodeStartTime   = startTime;
        episodeBossHpStart = bossHpStart;

        rewardTotal = rewardBossDamage = rewardHitPenalty = rewardDeathPenalty = 0f;
        rewardWarningTile = rewardDamageTile = rewardWallBlocked = 0f;
        rewardApproach = rewardMissedAttack = rewardAttackOnCooldown = 0f;
        rewardMovedIntoDanger = rewardSafeAttack = 0f;

        warningTileSteps = damageTileSteps = 0;
        playerHitCount   = 0;
        deathTime        = -1f;

        moveMaskedWallCount = moveMaskedWarningCount = moveMaskedDamageCount = 0;
        moveMaskedRecentWarningCount = moveMaskedRecentDamageCount = moveMaskedDueToBusyCount = 0;
        moveAllowedCount = movementActionCount = successfulMoveCount = dangerousMoveAttemptCount = 0;
        waitOnlyMaskStateCount = escapeActionForcedOpenCount = bossCellTreatedAsBlockedCount = 0;
        waitOnlyDueToGeometryCount = waitOnlyDueToDamageCount = 0;
        waitOnlyDueToBusyCount = waitOnlyDueToBossCellCount = 0;
        bossCellMoveAllowedCount = moveBusyStateCount = 0;
        waitActionCount = 0;

        attackMaskedNotReadyCount = attackMaskedOutOfRangeCount = 0;
        attackMaskedOnWarningCount = attackMaskedOnDamageCount = 0;
        attackMaskedOnRecentWarningCount = attackMaskedOnRecentDamageCount = 0;
        attackMaskedSurvivalStageCount = attackAllowedCount = attackMaskedTotalCount = 0;

        attackActionCount    = successfulHitSteps   = 0;
        missedAttackCount    = attackOnCooldownCount = 0;
        attackWhenOnWarning  = attackWhenOnDamage   = 0;
        attackWhenRecentWarning = attackWhenRecentDamage = 0;
        bossDamageTotal      = bossDamageBeforeFirstHit = 0;

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
        bossInRangeNoAttackCount = attackOutOfRangeCount = attackWhenDangerNearbyCount = 0;
        dangerNearbySteps = movedIntoWarningCount = movedIntoDamageCount = 0;
        movedIntoRecentWarningCount = movedIntoRecentDamageCount = 0;
        waitWhileDangerNearbyCount = hitWhileSafeMoveAvailableCount = avoidableHitCount = 0;
        hitOnWarningTileCount = hitOnDamageTileCount = hitOnRecentWarningTileCount = 0;
        hitAfterMovingIntoDangerCount = warningToHitStepSum = warningToHitStepCount = 0;
        oppInternalStep = 0;
        oppLastStepOnWarning = oppLastMoveIntoDanger = false;
        oppLastWarningEntryStep = -1;
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
        bool isAttack, bool safeOpportunity, bool attackHit, bool bossInRange, bool attackReady,
        bool dangerNearby, int safeMoveCount, bool onWarning, bool onDamage,
        bool onRecentWarn, bool onRecentDmg,
        bool isMove, bool moveWillSucceed,
        bool moveIntoWarn, bool moveIntoDmg, bool moveIntoRecentWarn, bool moveIntoRecentDmg,
        bool gotHit, bool isWait,
        float currentTime = 0f, int bossDamageDelta = 0,
        bool choseSafeMove = false, bool choseDangerMove = false)
    {
        oppInternalStep++;

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
        if (bossInRange && attackReady && !isAttack) bossInRangeNoAttackCount++;

        // ── Danger nearby tracking ────────────────────────────────────────────
        if (dangerNearby) dangerNearbySteps++;
        if (isWait && dangerNearby) waitWhileDangerNearbyCount++;

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
        rewardDeathPenalty     += result.rewardDeathPenalty;
        rewardWarningTile      += result.rewardWarningTile;
        rewardDamageTile       += result.rewardDamageTile;
        rewardWallBlocked      += result.rewardWallBlocked;
        rewardApproach         += result.rewardApproach;
        rewardMissedAttack     += result.rewardMissedAttack;
        rewardAttackOnCooldown += result.rewardAttackOnCooldown;
        rewardMovedIntoDanger  += result.rewardMovedIntoDanger;
        rewardSafeAttack       += result.rewardSafeAttack;

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
            playerHitCount++;
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

    // ── Episode summary ───────────────────────────────────────────────────────
    public void LogEpisodeEnd(string reason, int stepCount, float currentTime, BossRLStateExtractor extractor)
    {
        float survivalTime = currentTime - episodeStartTime;
        int   bossHpLeft   = extractor != null ? extractor.BossCurrentHp : 0;
        int   recentWarnCellsNow = extractor != null ? extractor.RecentWarningCellCount : 0;
        int   recentDmgCellsNow  = extractor != null ? extractor.RecentDamageCellCount  : 0;
        if (reason == "player_dead") deathTime = survivalTime;

        float wallRatio   = totalMoveAttempts > 0 ? (float)wallBlockedMoves / totalMoveAttempts : 0f;
        float avgBossDist = bossDistanceSampleCount > 0 ? bossDistanceSumAtAttack / bossDistanceSampleCount : -1f;
        float hitRate     = attackActionCount > 0 ? (float)successfulHitSteps / attackActionCount : 0f;
        float bossHpPerAtk= attackActionCount > 0 ? (float)bossDamageTotal    / attackActionCount : 0f;

        Debug.Log(
            $"[BossRL] EPISODE_END reason={reason} steps={stepCount} survival={survivalTime:F1}s " +
            $"survived10s={survived10s} survived20s={survived20s}\n" +
            $"  reward: total={rewardTotal:F3} boss={rewardBossDamage:F3} hit={rewardHitPenalty:F3} " +
            $"death={rewardDeathPenalty:F3} warn_tile={rewardWarningTile:F3} dmg_tile={rewardDamageTile:F3} " +
            $"wall={rewardWallBlocked:F3} approach={rewardApproach:F3} " +
            $"missed_atk={rewardMissedAttack:F3} cooldown_atk={rewardAttackOnCooldown:F3} " +
            $"moved_into_danger={rewardMovedIntoDanger:F3} safe_atk={rewardSafeAttack:F3}\n" +
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
            $"  attack_range: in_range_steps={bossInAttackRangeSteps} atk_in_range={attackWhenBossInRange} " +
            $"atk_out_range={attackWhenBossOutOfRange} avg_dist={avgBossDist:F2} " +
            $"first_atk_dist={bossDistanceAtFirstAttack:F2}\n" +
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
            $"  delayed_hit: safe_same_step={safeAttackHitSameStep} safe_0.3s={safeAttackHitWithin03s} " +
            $"safe_0.5s={safeAttackHitWithin05s} out_of_range_0.5s={outOfRangeAttackHitWithin05s} " +
            $"unsafe_0.5s={unsafeAttackHitWithin05s}\n" +
            $"  move_danger_mask: warn_when_safe={moveMaskedWarnWhenSafeCount} recent_warn_when_safe={moveMaskedRecentWarnWhenSafeCount} " +
            $"dmg_when_safe={moveMaskedDmgWhenSafeCount} least_danger_opened={leastDangerEscapeOpenedCount} " +
            $"wait_masked={waitMaskedDueToDangerCount} safe_avail_steps={safeMoveAvailableStepCount} " +
            $"chose_safe={choseSafeMoveCount} chose_danger={choseDangerMoveCount}\n" +
            $"  danger: nearby_steps={dangerNearbySteps} wait_while_danger={waitWhileDangerNearbyCount} " +
            $"move_into_warn={movedIntoWarningCount} move_into_dmg={movedIntoDamageCount} " +
            $"move_into_recent_warn={movedIntoRecentWarningCount} move_into_recent_dmg={movedIntoRecentDamageCount}\n" +
            $"  hit_analysis: hit_on_warn={hitOnWarningTileCount} hit_on_dmg={hitOnDamageTileCount} " +
            $"hit_on_recent_warn={hitOnRecentWarningTileCount} " +
            $"hit_safe_move_avail={hitWhileSafeMoveAvailableCount} avoidable={avoidableHitCount} " +
            $"hit_after_move_danger={hitAfterMovingIntoDangerCount} " +
            $"warn_to_hit_avg_steps={(warningToHitStepCount > 0 ? (float)warningToHitStepSum / warningToHitStepCount : -1f):F1}\n" +
            $"  player: hits={playerHitCount} first_hit_step={firstHitStep} " +
            $"first_hit_time={firstHitTime:F1}s death_time={deathTime:F1}s\n" +
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
