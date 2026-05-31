using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(GridMover))]
[RequireComponent(typeof(GridOccupant))]
[RequireComponent(typeof(BossRLInputBridge))]
[RequireComponent(typeof(BossRLStateExtractor))]
[RequireComponent(typeof(BossRLReward))]
[RequireComponent(typeof(BossRLEpisodeResetter))]
[RequireComponent(typeof(BossRLDebugLogger))]
public class BossPlayerAgent : Agent
{
    // Single Discrete action space [6]:
    //   0=WAIT  1=UP  2=DOWN  3=LEFT  4=RIGHT  5=ATTACK
    // One intent per decision: move OR attack OR wait, never simultaneous.

    [SerializeField] private float maxEpisodeSeconds = 90f;

    private BossRLInputBridge     inputBridge;
    private BossRLStateExtractor  stateExtractor;
    private BossRLReward          rewardTracker;
    private BossRLEpisodeResetter episodeResetter;
    private BossRLDebugLogger     debugLogger;
    private PlayerHealth          playerHealth;

    private float episodeStartTime;
    private float cumulativeReward;
    private bool  terminalHandled;

    public override void Initialize()
    {
        inputBridge     = GetComponent<BossRLInputBridge>();
        stateExtractor  = GetComponent<BossRLStateExtractor>();
        rewardTracker   = GetComponent<BossRLReward>();
        episodeResetter = GetComponent<BossRLEpisodeResetter>();
        debugLogger     = GetComponent<BossRLDebugLogger>();
        playerHealth    = GetComponent<PlayerHealth>();

        var pc  = GetComponent<PlayerController>();
        var pcb = GetComponent<PlayerCombat>();
        var gm  = GetComponent<GridMover>();
        inputBridge.Initialize(pc, pcb, gm, playerHealth);
        stateExtractor.TryInitialize();
        rewardTracker.ResetState(stateExtractor);
        inputBridge.ResetInput();

        MaxStep = 0;
        ResetEpisodeState();
    }

    public override void OnEpisodeBegin()
    {
        inputBridge?.ResetInput();
        rewardTracker?.ResetState(stateExtractor);
        stateExtractor?.ResetTemporalState();
        ResetEpisodeState();
    }

    // ── Action masking (WriteDiscreteActionMask) ──────────────────────────────
    // SAFE ACTION MASK POLICY:
    //   WAIT (0)     : masked if player on danger + safe move exists (force escape)
    //   MOVE (1-4)   : danger-level based; safer alternative → mask higher-danger dirs
    //                  always keeps ≥1 direction open (lowest-danger escape)
    //   ATTACK (5)   : AttackReady + boss_in_range + player NOT on any danger tile
    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (terminalHandled || (episodeResetter != null && episodeResetter.ReloadQueued))
            return;
        if (stateExtractor == null || !stateExtractor.IsReady)
            return;

        // Curriculum: survival_stage > 0.5 → mask ATTACK globally (Stage 1 only)
        float survivalStage = 0f;
        try { survivalStage = Academy.Instance.EnvironmentParameters.GetWithDefault("survival_stage", 0f); }
        catch { survivalStage = 0f; }
        bool survivalStageActive = survivalStage > 0.5f;

        // ── Movement masking (actions 1-4) ────────────────────────────────────
        // Danger levels: 999=wall  3=damage  2=warning  1=recent_warn/dmg  0=safe
        // Mask directions whose danger level exceeds the minimum available level.
        Vector2Int[] dirs     = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        int[]        moveActs = { 1, 2, 3, 4 };
        int[]        dangerLevel = new int[4];

        if (stateExtractor.IsPlayerMoving)
            debugLogger?.RecordMoveBusyState();

        for (int i = 0; i < 4; i++)
            dangerLevel[i] = stateExtractor.GetDirectionDangerLevel(dirs[i]);

        int minDanger = Mathf.Min(Mathf.Min(dangerLevel[0], dangerLevel[1]),
                                   Mathf.Min(dangerLevel[2], dangerLevel[3]));
        bool safeMovePossible = (minDanger == 0);

        bool[] moveShouldMask = new bool[4];
        int warnMaskedWhenSafe = 0, recentWarnMaskedWhenSafe = 0, dmgMaskedWhenSafe = 0;

        for (int i = 0; i < 4; i++)
        {
            bool isGeo = (dangerLevel[i] == 999);
            bool maskedByBetter = (!isGeo && minDanger < dangerLevel[i]);
            moveShouldMask[i] = isGeo || maskedByBetter;

            if (maskedByBetter)
            {
                if (dangerLevel[i] == 2) warnMaskedWhenSafe++;
                else if (dangerLevel[i] == 1) recentWarnMaskedWhenSafe++;
                else if (dangerLevel[i] == 3) dmgMaskedWhenSafe++;
            }

            // Boss cell tracking (should never be geometry-blocked)
            bool nextIsBossCell = stateExtractor.IsNextCellBossCell(dirs[i]);
            if (nextIsBossCell && isGeo)
                debugLogger?.RecordBossCellTreatedAsBlocked();
            else if (nextIsBossCell)
                debugLogger?.RecordBossCellMoveAllowed();

            debugLogger?.RecordMoveMaskDecision(moveActs[i], moveShouldMask[i],
                isGeo, dangerLevel[i] == 2, dangerLevel[i] == 3,
                dangerLevel[i] == 1, false);
        }

        // Least-danger escape: opened when no safe move exists but non-geometry move does
        bool leastDangerEscapeOpened = (minDanger > 0 && minDanger < 999);

        // All-geometry-blocked: WAIT only — record for diagnostics
        bool allMovesMasked = moveShouldMask[0] && moveShouldMask[1] && moveShouldMask[2] && moveShouldMask[3];
        if (allMovesMasked)
        {
            debugLogger?.RecordWaitOnlyState();
            bool allGeometry = dangerLevel[0] == 999 && dangerLevel[1] == 999 &&
                               dangerLevel[2] == 999 && dangerLevel[3] == 999;
            debugLogger?.RecordWaitOnlyBreakdown(allGeometry, !allGeometry);
        }

        for (int i = 0; i < 4; i++)
            if (moveShouldMask[i]) actionMask.SetActionEnabled(0, moveActs[i], false);

        // WAIT masking: force move-away when on danger and safe move available
        bool playerOnDangerForWait = stateExtractor.IsPlayerOnAnyDanger();
        bool maskWait = playerOnDangerForWait && safeMovePossible;
        if (maskWait) actionMask.SetActionEnabled(0, 0, false);

        debugLogger?.RecordMoveDangerMaskStep(
            warnMaskedWhenSafe, recentWarnMaskedWhenSafe, dmgMaskedWhenSafe,
            leastDangerEscapeOpened, maskWait, safeMovePossible);

        // ── Attack masking (action 5) ─────────────────────────────────────────
        // Hard mask: !AttackReady OR !boss_in_range OR player on any danger tile
        bool attackReady    = stateExtractor.AttackReady;
        bool bossInRangeMask = stateExtractor.IsBossInAttackRange();
        stateExtractor.GetCachedHazardAndRecentState(
            out bool playerOnWarn, out bool playerOnDmg,
            out bool playerOnRWarn, out bool playerOnRDmg);
        bool playerOnAnyDanger = playerOnWarn || playerOnDmg || playerOnRWarn || playerOnRDmg;

        bool maskedByNotReady  = !survivalStageActive && !attackReady;
        bool maskedByOOR       = !survivalStageActive && attackReady && !bossInRangeMask;
        bool maskedByWarn      = !survivalStageActive && attackReady && bossInRangeMask && playerOnWarn;
        bool maskedByDmg       = !survivalStageActive && attackReady && bossInRangeMask && !playerOnWarn && playerOnDmg;
        bool maskedByRWarn     = !survivalStageActive && attackReady && bossInRangeMask && !playerOnWarn && !playerOnDmg && playerOnRWarn;
        bool maskedByRDmg      = !survivalStageActive && attackReady && bossInRangeMask && !playerOnWarn && !playerOnDmg && !playerOnRWarn && playerOnRDmg;

        bool maskAttack = survivalStageActive || !attackReady || !bossInRangeMask || playerOnAnyDanger;
        if (maskAttack)
            actionMask.SetActionEnabled(0, 5, false);

        debugLogger?.RecordAttackMaskDecision(maskAttack, survivalStageActive,
            maskedByNotReady, maskedByOOR, maskedByWarn, maskedByDmg, maskedByRWarn, maskedByRDmg);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (terminalHandled || (episodeResetter != null && episodeResetter.ReloadQueued))
        {
            sensor.AddObservation(new float[BossRLStateExtractor.VectorObservationSize]);
            return;
        }
        stateExtractor.AppendObservations(sensor, GetElapsedNormalized());
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (terminalHandled || (episodeResetter != null && episodeResetter.ReloadQueued))
        {
            inputBridge?.ResetInput();
            return;
        }

        int singleAction = actions.DiscreteActions.Length > 0 ? actions.DiscreteActions[0] : 0;
        bool isMoveAct   = BossRLInputBridge.IsMoveAction(singleAction);
        bool isAttackAct = BossRLInputBridge.IsAttackAction(singleAction);
        bool isWaitAct   = BossRLInputBridge.IsWaitAction(singleAction);
        Vector2Int moveDir = BossRLInputBridge.SingleActionToMoveDir(singleAction);

        // Predict move outcome BEFORE applying
        BossRLInputBridge.MoveOutcome moveOutcome = isMoveAct
            ? inputBridge.PredictMoveOutcomeDir(moveDir)
            : BossRLInputBridge.MoveOutcome.None;
        bool isWallBlocked   = moveOutcome == BossRLInputBridge.MoveOutcome.WallBlocked;
        bool moveWillSucceed = moveOutcome == BossRLInputBridge.MoveOutcome.WillMove;

        // Capture attack readiness and opportunity state BEFORE applying
        bool attackWasReady = stateExtractor != null && stateExtractor.AttackReady;

        bool safeOpportunity = false, dangerNearby = false, onAnyDanger = false;
        bool onWarning = false, onDamage = false, onRecentWarn = false, onRecentDmg = false;
        bool nextCellWarn = false, nextCellDmg = false, nextCellRecentWarn = false, nextCellRecentDmg = false;
        int  safeMoveCount = 0;
        if (stateExtractor != null && stateExtractor.IsReady)
        {
            safeOpportunity = stateExtractor.IsSafeAttackOpportunity();
            dangerNearby    = stateExtractor.IsDangerNearby();
            safeMoveCount   = stateExtractor.SafeMoveDirectionCount();
            onAnyDanger     = stateExtractor.IsPlayerOnAnyDanger();
            stateExtractor.GetCachedHazardAndRecentState(out onWarning, out onDamage, out onRecentWarn, out onRecentDmg);
            if (isMoveAct && moveWillSucceed)
            {
                nextCellWarn       = stateExtractor.IsNextCellWarning(moveDir);
                nextCellDmg        = stateExtractor.IsNextCellDamage(moveDir);
                nextCellRecentWarn = stateExtractor.IsNextCellRecentWarning(moveDir);
                nextCellRecentDmg  = stateExtractor.IsNextCellRecentDamage(moveDir);
            }
        }

        inputBridge.ApplySingleAction(singleAction);

        bool bossInRange    = stateExtractor != null && stateExtractor.IsBossInAttackRange();
        float manhattanDist = stateExtractor != null ? stateExtractor.ManhattanDistanceToBoss() : 99f;

        // Get full danger state for attack quality metrics (only on attack steps)
        bool onRecentWarning = onRecentWarn, onRecentDamage = onRecentDmg;
        if (stateExtractor != null && isAttackAct && !stateExtractor.IsReady)
            stateExtractor.GetFullDangerState(out _, out _, out onRecentWarning, out onRecentDamage);

        int attackActionInt    = isAttackAct ? 1 : 0;
        bool safeAttackAttempt = isAttackAct && safeOpportunity;

        BossRLReward.StepResult stepResult = rewardTracker.Evaluate(
            stateExtractor, GetElapsedSeconds(), maxEpisodeSeconds,
            isWallBlocked, attackActionInt, attackWasReady, bossInRange,
            isMoveAct && moveWillSucceed && nextCellWarn,
            isMoveAct && moveWillSucceed && nextCellRecentWarn,
            isMoveAct && moveWillSucceed && nextCellDmg,
            safeAttackAttempt);

        AddReward(stepResult.reward);
        cumulativeReward += stepResult.reward;

        bool gotHit   = stepResult.playerHitDelta > 0;
        bool attackHit = stepResult.bossDamageDelta > 0;

        debugLogger?.RecordStep(StepCount, Time.time, singleAction, isAttackAct, attackActionInt,
            moveOutcome, moveDir, stepResult, bossInRange, manhattanDist,
            onRecentWarning, onRecentDamage);

        bool choseSafeMove   = isMoveAct && moveWillSucceed &&
                              !nextCellWarn && !nextCellDmg && !nextCellRecentWarn && !nextCellRecentDmg;
        bool choseDangerMove = isMoveAct && moveWillSucceed &&
                              (nextCellWarn || nextCellDmg || nextCellRecentWarn || nextCellRecentDmg);

        debugLogger?.RecordOpportunityMetrics(
            isAttackAct, safeOpportunity, attackHit, bossInRange, attackWasReady,
            dangerNearby, safeMoveCount, onWarning, onDamage, onRecentWarn, onRecentDmg,
            isMoveAct, moveWillSucceed,
            nextCellWarn, nextCellDmg, nextCellRecentWarn, nextCellRecentDmg,
            gotHit, isWaitAct,
            Time.time, stepResult.bossDamageDelta,
            choseSafeMove, choseDangerMove);

        if (stepResult.bossDead || stepResult.playerDead || stepResult.timedOut)
        {
            string reason = stepResult.bossDead ? "boss_dead"
                          : stepResult.playerDead ? "player_dead" : "timeout";
            HandleTerminalEvent(reason);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;
        d.Clear();  // 0 = WAIT by default
        if      (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    d[0] = 1;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  d[0] = 2;
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  d[0] = 3;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) d[0] = 4;
        else if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space))      d[0] = 5;
    }

    public void HandleTerminalEvent(string reason)
    {
        if (terminalHandled) return;
        terminalHandled = true;
        debugLogger?.LogEpisodeEnd(reason, StepCount, Time.time, stateExtractor);
        EndEpisode();
        episodeResetter?.QueueSceneReload();
    }

    private void ResetEpisodeState()
    {
        episodeStartTime = Time.time;
        cumulativeReward = 0f;
        terminalHandled  = false;
        debugLogger?.ResetEpisode(stateExtractor != null ? stateExtractor.BossCurrentHp : 0, episodeStartTime);
    }

    private float GetElapsedSeconds() => Mathf.Max(0f, Time.time - episodeStartTime);
    private float GetElapsedNormalized() =>
        maxEpisodeSeconds <= 0f ? 0f : Mathf.Clamp01(GetElapsedSeconds() / maxEpisodeSeconds);
}
