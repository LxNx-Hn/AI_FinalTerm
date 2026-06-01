using UnityEngine;

[DisallowMultipleComponent]
public class BossRLInputBridge : MonoBehaviour
{
    // ── Legacy single-discrete action indices [6] (kept for the Heuristic oracle
    //    and diagnostic helpers; NOT the live action spec) ─────────────────────
    public const int ACTION_WAIT   = 0;
    public const int ACTION_UP     = 1;
    public const int ACTION_DOWN   = 2;
    public const int ACTION_LEFT   = 3;
    public const int ACTION_RIGHT  = 4;
    public const int ACTION_ATTACK = 5;

    // ── Live MultiDiscrete action spec [5, 2] ─────────────────────────────────
    // Mirrors the human's two independent per-frame input channels:
    //   Branch 0 (move, like the keyboard): 0=none 1=up 2=down 3=left 4=right
    //   Branch 1 (attack, like the mouse) : 0=no-attack 1=attack
    // Move intent and attack intent are emitted together, so the agent can move
    // and attack in the same decision exactly as a human can (PlayerController
    // reads movement, PlayerCombat reads attack, independently, every frame).
    public const int MOVE_NONE    = 0;
    public const int MOVE_UP      = 1;
    public const int MOVE_DOWN    = 2;
    public const int MOVE_LEFT    = 3;
    public const int MOVE_RIGHT   = 4;
    public const int ATTACK_NONE  = 0;
    public const int ATTACK_FIRE  = 1;
    public const int MoveBranchSize   = 5;
    public const int AttackBranchSize = 2;

    public enum MoveOutcome
    {
        None,        // action is not a move (WAIT or ATTACK)
        WillMove,    // not moving, target cell is walkable
        WallBlocked, // not moving, target cell is blocked (wall / boundary)
        Busy,        // already mid-move — action not evaluated as wall blocked
    }

    private PlayerController controller;
    private PlayerCombat combat;
    private GridMover mover;
    private PlayerHealth health;

    public void Initialize(PlayerController playerController, PlayerCombat playerCombat, GridMover gridMover, PlayerHealth playerHealth)
    {
        controller = playerController;
        combat     = playerCombat;
        mover      = gridMover;
        health     = playerHealth;
    }

    public bool IsMoveReady   => mover != null && !mover.IsMoving && (health == null || !health.IsDead);
    public bool IsAttackReady => combat != null && combat.IsAttackReady;

    // ── Static helpers for single discrete action (legacy / oracle) ───────────
    public static bool IsMoveAction(int action)   => action >= ACTION_UP && action <= ACTION_RIGHT;
    public static bool IsAttackAction(int action) => action == ACTION_ATTACK;
    public static bool IsWaitAction(int action)   => action == ACTION_WAIT;

    // ── Static helpers for the MultiDiscrete move branch ──────────────────────
    public static bool IsMoveBranchActive(int moveAction) => moveAction >= MOVE_UP && moveAction <= MOVE_RIGHT;

    /// <summary>Move-branch index (0-4) → direction. Same mapping as the legacy
    /// single-action helper for indices 0-4 (0/none → zero).</summary>
    public static Vector2Int MoveBranchToDir(int moveAction) => SingleActionToMoveDir(moveAction);

    public static Vector2Int SingleActionToMoveDir(int action)
    {
        switch (action)
        {
            case ACTION_UP:    return Vector2Int.up;
            case ACTION_DOWN:  return Vector2Int.down;
            case ACTION_LEFT:  return Vector2Int.left;
            case ACTION_RIGHT: return Vector2Int.right;
            default:           return Vector2Int.zero;
        }
    }

    // ── Move outcome prediction ───────────────────────────────────────────────

    /// <summary>
    /// Predict outcome for a single discrete action (0-5).
    /// Returns None for WAIT and ATTACK actions.
    /// </summary>
    public MoveOutcome PredictMoveOutcome(int singleAction)
    {
        if (!IsMoveAction(singleAction)) return MoveOutcome.None;
        return PredictMoveOutcomeDir(SingleActionToMoveDir(singleAction));
    }

    /// <summary>
    /// Predict outcome for a given direction vector.
    /// </summary>
    public MoveOutcome PredictMoveOutcomeDir(Vector2Int dir)
    {
        if (mover == null || (health != null && health.IsDead)) return MoveOutcome.None;
        if (mover.IsMoving) return MoveOutcome.Busy;
        return mover.CanMove(dir) ? MoveOutcome.WillMove : MoveOutcome.WallBlocked;
    }

    // ── Action application ────────────────────────────────────────────────────

    /// <summary>
    /// Apply a MultiDiscrete [5,2] action: a move intent (branch 0) and an attack
    /// intent (branch 1) that are emitted together, mirroring the human's two
    /// independent input channels.
    ///   - Move and attack are NOT mutually exclusive: the agent may do both.
    ///   - A wall-blocked move still lets the attack fire (move intent is set;
    ///     PlayerController simply won't advance if the cell is blocked).
    ///   - A gate-blocked attack still leaves the move intent intact.
    /// Uses only the same PlayerController/PlayerCombat entry points as a human;
    /// no gameplay logic is changed here.
    /// </summary>
    public void ApplyMultiAction(int moveAction, int attackAction, bool allowAttack = true)
    {
        bool doMove   = IsMoveBranchActive(moveAction);
        bool doAttack = attackAction == ATTACK_FIRE && allowAttack;

        // Movement channel (keyboard equivalent) — independent of the attack channel.
        if (doMove)
            controller?.SetExternalInput(MoveBranchToDir(moveAction));
        else
            controller?.ClearExternalInput();

        // Attack channel (mouse equivalent) — independent of the movement channel.
        if (doAttack)
            combat?.RequestExternalAttack();
    }

    /// <summary>
    /// Legacy single-discrete apply [0-5] (move OR attack OR wait). Retained for
    /// reference/heuristic paths; the live policy uses ApplyMultiAction.
    /// </summary>
    public void ApplySingleAction(int action, bool allowAttack = true)
    {
        if (IsMoveAction(action))
        {
            controller?.SetExternalInput(SingleActionToMoveDir(action));
        }
        else if (IsAttackAction(action))
        {
            controller?.ClearExternalInput();
            if (allowAttack)
                combat?.RequestExternalAttack();
        }
        else  // WAIT
        {
            controller?.ClearExternalInput();
        }
    }

    public void ResetInput()
    {
        controller?.ClearExternalInput();
    }

    // ── Legacy compatibility helper ───────────────────────────────────────────
    public static Vector2Int ToDirection(int moveAction)
    {
        return SingleActionToMoveDir(moveAction);
    }
}
