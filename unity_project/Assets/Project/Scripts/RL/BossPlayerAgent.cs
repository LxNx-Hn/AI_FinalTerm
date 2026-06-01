using System.Collections.Generic;
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

    [SerializeField] private float maxEpisodeSeconds = 210f;

    private BossRLInputBridge     inputBridge;
    private BossRLStateExtractor  stateExtractor;
    private BossRLReward          rewardTracker;
    private BossRLEpisodeResetter episodeResetter;
    private BossRLDebugLogger     debugLogger;
    private PlayerHealth          playerHealth;

    private float episodeStartTime;
    private float cumulativeReward;
    private bool  terminalHandled;
    private bool  terminalRewardApplied;
    private readonly List<float> pendingMissedSafeOpportunityTimes = new List<float>();

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

    // ── Action masking (WriteDiscreteActionMask) — MultiDiscrete [5,2] ─────────
    // SAFE ACTION MASK POLICY:
    //   Branch 0 MOVE:
    //     none (0)   : masked if player on danger + safe move exists (force escape)
    //     dir  (1-4) : danger-level based; safer alternative → mask higher-danger dirs
    //                  always keeps ≥1 branch-0 action open (lowest-danger escape / none)
    //   Branch 1 ATTACK:
    //     no-attack (0): never masked
    //     attack    (1): AttackReady + boss_in_range + player NOT on any danger tile
    //                    + RL target gate (visible-overlap only)
    // Move and attack masks are independent, so the agent can be forced to dodge
    // (branch 0) while still being free to attack (branch 1) in the same step.
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
        bool rlTargetAllowed = true;
        BossRLTargetAlignmentDiagnostics.HitClass maskTargetClass =
            BossRLTargetAlignmentDiagnostics.HitClass.UnknownRemaining;
        if (!survivalStageActive && attackReady && bossInRangeMask && !playerOnAnyDanger)
        {
            rlTargetAllowed = stateExtractor.IsCurrentRLAttackTargetAllowed(out maskTargetClass);
            debugLogger?.RecordRLAttackTargetGate(maskTargetClass, rlTargetAllowed, blockedAtExternalRequest: false);
        }

        bool maskAttack = survivalStageActive || !attackReady || !bossInRangeMask || playerOnAnyDanger || !rlTargetAllowed;
        if (maskAttack)
            actionMask.SetActionEnabled(1, BossRLInputBridge.ATTACK_FIRE, false);  // branch 1, action 1

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

        // MultiDiscrete [5,2]: branch 0 = move intent, branch 1 = attack intent.
        int moveAction   = actions.DiscreteActions.Length > 0 ? actions.DiscreteActions[0] : 0;
        int attackBranch = actions.DiscreteActions.Length > 1 ? actions.DiscreteActions[1] : 0;
        bool isMoveAct      = BossRLInputBridge.IsMoveBranchActive(moveAction);
        bool isAttackIntent = attackBranch == BossRLInputBridge.ATTACK_FIRE;
        Vector2Int moveDir  = BossRLInputBridge.MoveBranchToDir(moveAction);

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
        int  distanceToBossBeforeAction = 99;
        int  moveDistanceDelta = 0;
        int  bossHpBeforeAction = 0;
        bool remainedInAttackRangeAfterMove = false;
        Vector2Int playerCellBeforeAction = Vector2Int.zero;
        Vector2Int bossCellBeforeAction = Vector2Int.zero;
        Vector2Int bossFacingBeforeAction = Vector2Int.down;
        if (stateExtractor != null && stateExtractor.IsReady)
        {
            safeOpportunity = stateExtractor.IsSafeAttackOpportunity();
            dangerNearby    = stateExtractor.IsDangerNearby();
            safeMoveCount   = stateExtractor.SafeMoveDirectionCount();
            onAnyDanger     = stateExtractor.IsPlayerOnAnyDanger();
            distanceToBossBeforeAction = stateExtractor.ManhattanDistanceToBoss();
            bossHpBeforeAction = stateExtractor.BossCurrentHp;
            playerCellBeforeAction = stateExtractor.PlayerWorldCell;
            bossCellBeforeAction = stateExtractor.BossWorldCell;
            bossFacingBeforeAction = stateExtractor.BossFacing;
            stateExtractor.GetCachedHazardAndRecentState(out onWarning, out onDamage, out onRecentWarn, out onRecentDmg);
            if (isMoveAct && moveWillSucceed)
            {
                nextCellWarn       = stateExtractor.IsNextCellWarning(moveDir);
                nextCellDmg        = stateExtractor.IsNextCellDamage(moveDir);
                nextCellRecentWarn = stateExtractor.IsNextCellRecentWarning(moveDir);
                nextCellRecentDmg  = stateExtractor.IsNextCellRecentDamage(moveDir);
                moveDistanceDelta  = stateExtractor.ManhattanDistanceToBossAfterMove(moveDir) - distanceToBossBeforeAction;
                remainedInAttackRangeAfterMove = stateExtractor.IsBossInAttackRangeAfterMove(moveDir);
            }
        }

        bool allowExternalAttack = true;
        BossRLTargetAlignmentDiagnostics.HitClass actionTargetClass =
            BossRLTargetAlignmentDiagnostics.HitClass.UnknownRemaining;
        if (isAttackIntent && stateExtractor != null && stateExtractor.IsReady)
        {
            allowExternalAttack = stateExtractor.IsCurrentRLAttackTargetAllowed(out actionTargetClass);
            if (!allowExternalAttack)
                debugLogger?.RecordRLAttackTargetGate(actionTargetClass, allowed: false, blockedAtExternalRequest: true);
        }

        // Capture "was mid-move" before applying, for the attack-while-moving metric.
        bool playerWasMoving = stateExtractor != null && stateExtractor.IsPlayerMoving;

        inputBridge.ApplyMultiAction(moveAction, attackBranch, allowExternalAttack);

        if (terminalHandled || (episodeResetter != null && episodeResetter.ReloadQueued))
        {
            inputBridge?.ResetInput();
            return;
        }

        bool bossInRange    = stateExtractor != null && stateExtractor.IsBossInAttackRange();
        float manhattanDist = stateExtractor != null ? stateExtractor.ManhattanDistanceToBoss() : 99f;

        // Get full danger state for attack quality metrics (only on attack steps)
        bool onRecentWarning = onRecentWarn, onRecentDamage = onRecentDmg;
        if (stateExtractor != null && isAttackIntent && !stateExtractor.IsReady)
            stateExtractor.GetFullDangerState(out _, out _, out onRecentWarning, out onRecentDamage);

        bool appliedAttackAct = isAttackIntent && allowExternalAttack;
        bool isWaitAct         = !isMoveAct && !appliedAttackAct;  // no move + no attack
        int attackActionInt    = appliedAttackAct ? 1 : 0;
        bool safeAttackAttempt = appliedAttackAct && safeOpportunity;
        bool missedSafeAttackOpportunity = safeOpportunity && !appliedAttackAct && !dangerNearby;

        BossRLReward.StepResult stepResult = rewardTracker.Evaluate(
            stateExtractor, GetElapsedSeconds(), maxEpisodeSeconds,
            isWallBlocked, attackActionInt, attackWasReady, bossInRange,
            isMoveAct && moveWillSucceed && nextCellWarn,
            isMoveAct && moveWillSucceed && nextCellRecentWarn,
            isMoveAct && moveWillSucceed && nextCellDmg,
            safeAttackAttempt,
            false);

        AddReward(stepResult.reward);
        cumulativeReward += stepResult.reward;

        bool gotHit   = stepResult.playerHitDelta > 0;
        bool attackHit = stepResult.bossDamageDelta > 0;
        ProcessPendingMissedSafeOpportunityPenalties(Time.time, gotHit);
        if (missedSafeAttackOpportunity)
            pendingMissedSafeOpportunityTimes.Add(Time.time);

        // Per-step recording: pass the move-branch index for move/wait histogram
        // and the APPLIED attack (gate-allowed) for attack tracking.
        debugLogger?.RecordStep(StepCount, Time.time, moveAction, appliedAttackAct, attackActionInt,
            moveOutcome, moveDir, stepResult, bossInRange, manhattanDist,
            onRecentWarning, onRecentDamage);

        // MultiDiscrete simultaneity metrics (move+attack in one decision).
        debugLogger?.RecordActionSpaceStep(
            isMoveAct, isAttackIntent, appliedAttackAct, allowExternalAttack, playerWasMoving);

        bool choseSafeMove   = isMoveAct && moveWillSucceed &&
                              !nextCellWarn && !nextCellDmg && !nextCellRecentWarn && !nextCellRecentDmg;
        bool choseDangerMove = isMoveAct && moveWillSucceed &&
                              (nextCellWarn || nextCellDmg || nextCellRecentWarn || nextCellRecentDmg);

        debugLogger?.RecordOpportunityMetrics(
            moveAction,
            appliedAttackAct, safeOpportunity, attackHit, bossInRange, attackWasReady,
            dangerNearby, safeMoveCount, onWarning, onDamage, onRecentWarn, onRecentDmg,
            isMoveAct, moveWillSucceed,
            nextCellWarn, nextCellDmg, nextCellRecentWarn, nextCellRecentDmg,
            gotHit, isWaitAct,
            Time.time, stepResult.bossDamageDelta,
            choseSafeMove, choseDangerMove,
            distanceToBossBeforeAction, moveDistanceDelta,
            playerCellBeforeAction, bossCellBeforeAction,
            remainedInAttackRangeAfterMove, bossHpBeforeAction, stateExtractor != null ? stateExtractor.BossCurrentHp : 0,
            bossFacingBeforeAction);

        if (stepResult.bossDead || stepResult.playerDead || stepResult.timedOut)
        {
            string reason = stepResult.bossDead ? "boss_dead"
                          : stepResult.playerDead ? "player_dead" : "timeout";
            bool terminalRewardAlreadyApplied = stepResult.bossDead || stepResult.playerDead;
            HandleTerminalEvent(reason, terminalRewardAlreadyApplied);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;
        d.Clear();  // (move=none, attack=no) by default

        int intent;
        if (!TryGetHeuristicOracleIntent(out intent))
        {
            intent = BossRLInputBridge.ACTION_WAIT;
            if      (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    intent = BossRLInputBridge.ACTION_UP;
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  intent = BossRLInputBridge.ACTION_DOWN;
            else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  intent = BossRLInputBridge.ACTION_LEFT;
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) intent = BossRLInputBridge.ACTION_RIGHT;
            else if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space))      intent = BossRLInputBridge.ACTION_ATTACK;
        }
        WriteIntentToBranches(d, intent);
    }

    /// <summary>Translate a legacy single-action intent (0-5) into MultiDiscrete [5,2] branches.</summary>
    private static void WriteIntentToBranches(ActionSegment<int> d, int intent)
    {
        if (BossRLInputBridge.IsAttackAction(intent))
        {
            d[0] = BossRLInputBridge.MOVE_NONE;
            d[1] = BossRLInputBridge.ATTACK_FIRE;
        }
        else if (BossRLInputBridge.IsMoveAction(intent))
        {
            d[0] = intent;                            // ACTION_UP..ACTION_RIGHT == MOVE_UP..MOVE_RIGHT
            d[1] = BossRLInputBridge.ATTACK_NONE;
        }
        else
        {
            d[0] = BossRLInputBridge.MOVE_NONE;
            d[1] = BossRLInputBridge.ATTACK_NONE;
        }
    }

    private bool TryGetHeuristicOracleIntent(out int intent)
    {
        intent = BossRLInputBridge.ACTION_WAIT;
        if (stateExtractor == null || !stateExtractor.IsReady) return false;

        stateExtractor.GetCachedHazardAndRecentState(
            out bool onWarning, out bool onDamage, out bool onRecentWarn, out bool onRecentDmg);
        bool onAnyDanger = onWarning || onDamage || onRecentWarn || onRecentDmg;

        if (onAnyDanger)
        {
            intent = SelectSafestMoveAction(allowLeastDanger: true);
            return true;
        }

        if (stateExtractor.AttackReady && stateExtractor.IsBossInAttackRange())
        {
            intent = BossRLInputBridge.ACTION_ATTACK;
            return true;
        }

        intent = SelectApproachOrSafestMoveAction();
        return true;
    }

    private int SelectApproachOrSafestMoveAction()
    {
        int bestAction = BossRLInputBridge.ACTION_WAIT;
        int bestScore = int.MinValue;
        int currentDistance = stateExtractor.ManhattanDistanceToBoss();

        for (int action = BossRLInputBridge.ACTION_UP; action <= BossRLInputBridge.ACTION_RIGHT; action++)
        {
            Vector2Int dir = BossRLInputBridge.SingleActionToMoveDir(action);
            int danger = stateExtractor.GetDirectionDangerLevel(dir);
            if (danger != 0) continue;
            if (inputBridge.PredictMoveOutcomeDir(dir) != BossRLInputBridge.MoveOutcome.WillMove) continue;

            bool inRangeAfter = stateExtractor.IsBossInAttackRangeAfterMove(dir);
            int distanceAfter = stateExtractor.ManhattanDistanceToBossAfterMove(dir);
            int score = 0;
            if (inRangeAfter) score += 1000;
            score += Mathf.Clamp(currentDistance - distanceAfter, -5, 5) * 20;
            score -= distanceAfter;

            if (score > bestScore)
            {
                bestScore = score;
                bestAction = action;
            }
        }

        if (bestAction != BossRLInputBridge.ACTION_WAIT)
            return bestAction;

        return SelectSafestMoveAction(allowLeastDanger: true);
    }

    private int SelectSafestMoveAction(bool allowLeastDanger)
    {
        int bestAction = BossRLInputBridge.ACTION_WAIT;
        int bestDanger = int.MaxValue;
        int bestDistance = int.MaxValue;

        for (int action = BossRLInputBridge.ACTION_UP; action <= BossRLInputBridge.ACTION_RIGHT; action++)
        {
            Vector2Int dir = BossRLInputBridge.SingleActionToMoveDir(action);
            int danger = stateExtractor.GetDirectionDangerLevel(dir);
            if (!allowLeastDanger && danger != 0) continue;
            if (danger >= 999) continue;
            if (inputBridge.PredictMoveOutcomeDir(dir) != BossRLInputBridge.MoveOutcome.WillMove) continue;

            int distanceAfter = stateExtractor.ManhattanDistanceToBossAfterMove(dir);
            bool better = danger < bestDanger || (danger == bestDanger && distanceAfter < bestDistance);
            if (better)
            {
                bestDanger = danger;
                bestDistance = distanceAfter;
                bestAction = action;
            }
        }

        return bestAction;
    }

    public void HandleTerminalEvent(string reason, bool terminalRewardAlreadyApplied = false)
    {
        if (terminalHandled) return;
        terminalHandled = true;

        if (terminalRewardAlreadyApplied)
        {
            terminalRewardApplied = true;
        }
        else
        {
            ApplyTerminalReward(reason);
        }

        debugLogger?.LogEpisodeEnd(reason, StepCount, Time.time, stateExtractor);
        EndEpisode();
        episodeResetter?.QueueSceneReload();
    }

    private void ResetEpisodeState()
    {
        episodeStartTime = Time.time;
        cumulativeReward = 0f;
        terminalHandled  = false;
        terminalRewardApplied = false;
        pendingMissedSafeOpportunityTimes.Clear();
        debugLogger?.ResetEpisode(stateExtractor != null ? stateExtractor.BossCurrentHp : 0, episodeStartTime);
    }

    private void ApplyTerminalReward(string reason)
    {
        if (terminalRewardApplied) return;

        terminalRewardApplied = true;
        float terminalReward = BossRLReward.TerminalRewardForReason(reason);
        AddReward(terminalReward);
        cumulativeReward += terminalReward;
        debugLogger?.RecordTerminalReward(reason, terminalReward);
    }

    private void ProcessPendingMissedSafeOpportunityPenalties(float currentTime, bool gotHit)
    {
        for (int i = pendingMissedSafeOpportunityTimes.Count - 1; i >= 0; i--)
        {
            float elapsed = currentTime - pendingMissedSafeOpportunityTimes[i];
            if (gotHit && elapsed <= 1.0f)
            {
                pendingMissedSafeOpportunityTimes.RemoveAt(i);
                continue;
            }

            if (elapsed >= 1.0f)
            {
                float penalty = BossRLReward.MissedSafeAttackOpportunityPenalty;
                AddReward(penalty);
                cumulativeReward += penalty;
                debugLogger?.RecordDelayedMissedSafeOpportunityPenalty(penalty);
                pendingMissedSafeOpportunityTimes.RemoveAt(i);
            }
        }
    }

    private float GetElapsedSeconds() => Mathf.Max(0f, Time.time - episodeStartTime);
    private float GetElapsedNormalized() =>
        maxEpisodeSeconds <= 0f ? 0f : Mathf.Clamp01(GetElapsedSeconds() / maxEpisodeSeconds);
}
