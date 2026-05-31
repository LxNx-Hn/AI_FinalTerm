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

    // ── Episode reset ─────────────────────────────────────────────────────────
    public void ResetEpisode(int bossHpStart, float startTime)
    {
        episodeStartTime   = startTime;
        episodeBossHpStart = bossHpStart;

        rewardTotal = rewardBossDamage = rewardHitPenalty = rewardDeathPenalty = 0f;
        rewardWarningTile = rewardDamageTile = rewardWallBlocked = 0f;
        rewardApproach = rewardMissedAttack = rewardAttackOnCooldown = 0f;

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
    }

    // ── Escape/wait state recording ───────────────────────────────────────────
    public void RecordWaitOnlyState()              { waitOnlyMaskStateCount++; }
    public void RecordEscapeActionForcedOpen()     { escapeActionForcedOpenCount++; }
    public void RecordBossCellTreatedAsBlocked()   { bossCellTreatedAsBlockedCount++; }
    public void RecordBossCellMoveAllowed()        { bossCellMoveAllowedCount++; }
    public void RecordMoveBusyState()              { moveBusyStateCount++; }

    public void RecordWaitOnlyBreakdown(bool dueToGeometry, bool dueToDamage)
    {
        if (dueToGeometry) waitOnlyDueToGeometryCount++;
        if (dueToDamage)   waitOnlyDueToDamageCount++;
        // busy and boss_cell are no longer mask criteria → always 0, never incremented
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
            $"missed_atk={rewardMissedAttack:F3} cooldown_atk={rewardAttackOnCooldown:F3}\n" +
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
            $"  attack_mask: total_masked={attackMaskedTotalCount} allowed={attackAllowedCount} " +
            $"survival_stage={attackMaskedSurvivalStageCount} not_ready={attackMaskedNotReadyCount} " +
            $"out_of_range={attackMaskedOutOfRangeCount}(should=0) on_warn={attackMaskedOnWarningCount}(should=0) " +
            $"on_dmg={attackMaskedOnDamageCount}(should=0)\n" +
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
