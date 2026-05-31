using UnityEngine;

[DisallowMultipleComponent]
public class BossRLInputBridge : MonoBehaviour
{
    // ── Single Discrete action indices [6] ────────────────────────────────────
    public const int ACTION_WAIT   = 0;
    public const int ACTION_UP     = 1;
    public const int ACTION_DOWN   = 2;
    public const int ACTION_LEFT   = 3;
    public const int ACTION_RIGHT  = 4;
    public const int ACTION_ATTACK = 5;

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

    // ── Static helpers for single discrete action ─────────────────────────────
    public static bool IsMoveAction(int action)   => action >= ACTION_UP && action <= ACTION_RIGHT;
    public static bool IsAttackAction(int action) => action == ACTION_ATTACK;
    public static bool IsWaitAction(int action)   => action == ACTION_WAIT;

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
    /// Apply a single discrete action [0-5].
    /// Move actions only move (no attack). ATTACK only attacks (no move). WAIT does nothing.
    /// </summary>
    public void ApplySingleAction(int action)
    {
        if (IsMoveAction(action))
        {
            controller?.SetExternalInput(SingleActionToMoveDir(action));
        }
        else if (IsAttackAction(action))
        {
            controller?.ClearExternalInput();
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
