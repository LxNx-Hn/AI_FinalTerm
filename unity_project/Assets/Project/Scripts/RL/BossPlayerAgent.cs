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
    // RELAXED MASK POLICY:
    //   WAIT (0)     : always allowed
    //   MOVE (1-4)   : hard mask only for wall/arena-out + active damage tile
    //                  warning/recent danger → observation+reward only, NOT masked
    //                  escape exception: if all 4 moves masked, force-open best escape
    //   ATTACK (5)   : hard mask only when !AttackReady (cooldown)
    //                  bossInRange/danger tile conditions → observation+reward only
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
        // Hard mask: wall/arena-out (isWall) + active damage tile (nextDamage)
        // NOT masked: warning, recent_warning, recent_damage (use obs+reward)
        Vector2Int[] dirs     = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        int[]        moveActs = { 1, 2, 3, 4 };
        bool[]       isWallArr   = new bool[4];
        bool[]       isDamageArr = new bool[4];
        bool[]       shouldMask  = new bool[4];

        for (int i = 0; i < 4; i++)
        {
            bool isWall     = !stateExtractor.CanMoveInDirection(dirs[i]);
            bool nextDamage = stateExtractor.IsNextCellDamage(dirs[i]);
            // Debug: check if boss cell is wrongly treated as wall
            bool nextIsBossCell = stateExtractor.IsNextCellBossCell(dirs[i]);
            if (nextIsBossCell && isWall)
                debugLogger?.RecordBossCellTreatedAsBlocked();
            isWallArr[i]   = isWall;
            isDamageArr[i] = nextDamage;
            shouldMask[i]  = isWall || nextDamage;
            // Record with warning=false/recent=false (those are no longer hard-masked)
            debugLogger?.RecordMoveMaskDecision(moveActs[i], shouldMask[i],
                isWall, false, nextDamage, false, false);
        }

        // Escape exception: if ALL 4 moves are masked, force open at least one non-wall move
        bool allMovesMasked = shouldMask[0] && shouldMask[1] && shouldMask[2] && shouldMask[3];
        if (allMovesMasked)
        {
            debugLogger?.RecordWaitOnlyState();
            for (int i = 0; i < 4; i++)
            {
                if (!isWallArr[i] && isDamageArr[i])
                {
                    shouldMask[i] = false;  // damage tile but not permanent wall → allow escape
                    debugLogger?.RecordEscapeActionForcedOpen();
                    break;
                }
            }
        }

        for (int i = 0; i < 4; i++)
            if (shouldMask[i]) actionMask.SetActionEnabled(0, moveActs[i], false);

        // ── Attack masking (action 5) ─────────────────────────────────────────
        // Hard mask ONLY: !AttackReady (cooldown)
        // bossInRange, danger tiles → handled by observation + reward
        bool attackReady = stateExtractor.AttackReady;
        bool maskAttack  = survivalStageActive || !attackReady;

        if (maskAttack)
            actionMask.SetActionEnabled(0, 5, false);

        debugLogger?.RecordAttackMaskDecision(maskAttack, survivalStageActive,
            !attackReady, false, false, false, false, false);
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
        Vector2Int moveDir = BossRLInputBridge.SingleActionToMoveDir(singleAction);

        // Predict move outcome BEFORE applying
        BossRLInputBridge.MoveOutcome moveOutcome = isMoveAct
            ? inputBridge.PredictMoveOutcomeDir(moveDir)
            : BossRLInputBridge.MoveOutcome.None;
        bool isWallBlocked = moveOutcome == BossRLInputBridge.MoveOutcome.WallBlocked;

        // Capture attack readiness BEFORE applying
        bool attackWasReady = stateExtractor != null && stateExtractor.AttackReady;

        inputBridge.ApplySingleAction(singleAction);

        bool bossInRange    = stateExtractor != null && stateExtractor.IsBossInAttackRange();
        float manhattanDist = stateExtractor != null ? stateExtractor.ManhattanDistanceToBoss() : 99f;

        // Get full danger state for attack quality metrics
        bool onRecentWarning = false, onRecentDamage = false;
        if (stateExtractor != null && isAttackAct)
            stateExtractor.GetFullDangerState(out _, out _, out onRecentWarning, out onRecentDamage);

        int attackActionInt = isAttackAct ? 1 : 0;
        BossRLReward.StepResult stepResult = rewardTracker.Evaluate(
            stateExtractor, GetElapsedSeconds(), maxEpisodeSeconds,
            isWallBlocked, attackActionInt, attackWasReady, bossInRange);

        AddReward(stepResult.reward);
        cumulativeReward += stepResult.reward;

        debugLogger?.RecordStep(StepCount, Time.time, singleAction, isAttackAct, attackActionInt,
            moveOutcome, moveDir, stepResult, bossInRange, manhattanDist,
            onRecentWarning, onRecentDamage);

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
