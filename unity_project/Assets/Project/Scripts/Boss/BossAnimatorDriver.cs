using UnityEngine;

/// <summary>
/// Boss Animator helper.
/// Uses Animator.Play(stateName, -1, 0f) directly and keeps state switching simple.
/// </summary>
public class BossAnimatorDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer bossSpriteRenderer;

    public void PlayIdle(Vector2Int facing)        => Play("IDLE", facing);
    public void PlayMove(Vector2Int facing)        => Play("MOVE", facing);
    public void PlayAttackReady(Vector2Int facing) => Play("ATTACK_READY", facing);
    public void PlayAttack(Vector2Int facing)      => Play("ATTACK", facing);
    public void PlayScream(Vector2Int facing)      => Play("SCREAM", facing);
    public void PlayAppear(Vector2Int facing)      => Play("APPEAR", facing);
    public void PlayDie(Vector2Int facing)         => Play("DIE", facing);

    public void PlayMarkAtk()
    {
        if (animator == null)
            return;

        animator.Play("MARKATK", -1, 0f);
    }

    public void PlayEnhancedAttack(Vector2Int facing)
    {
        Vector2Int mirrorFacing = facing;
        if (facing == Vector2Int.left)
            mirrorFacing = Vector2Int.right;
        if (facing == Vector2Int.right)
            mirrorFacing = Vector2Int.left;

        Play("ATTACK", mirrorFacing);
        SetFlipX(true);
    }

    public void ClearEnhancedAttack() => SetFlipX(false);

    public void SetExtraRotation(float deg)
    {
        if (bossSpriteRenderer == null)
            return;

        bossSpriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, deg);
    }

    public void ClearExtraRotation()
    {
        if (bossSpriteRenderer == null)
            return;

        bossSpriteRenderer.transform.localRotation = Quaternion.identity;
    }

    public void SetFlipX(bool flip)
    {
        if (bossSpriteRenderer != null)
            bossSpriteRenderer.flipX = flip;
    }

    private void Play(string state, Vector2Int facing)
    {
        if (animator == null)
            return;

        string stateName = $"{state}_{FacingToDir(facing)}";
        animator.Play(stateName, -1, 0f);
    }

    private static string FacingToDir(Vector2Int facing)
    {
        if (facing.y < 0)
            return "DOWN";
        if (facing.y > 0)
            return "UP";
        if (facing.x < 0)
            return "LEFT";
        if (facing.x > 0)
            return "RIGHT";
        return "DOWN";
    }
}
