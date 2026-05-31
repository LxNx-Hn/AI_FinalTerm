using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class KidAnimator : MonoBehaviour
{
    [Header("Walk Frames")]
    public Sprite[] walkFront;
    public Sprite[] walkSide;
    public Sprite[] walkBack;

    [Header("Attack Frames")]
    public Sprite[] attackFront;
    public Sprite[] attackSide;
    public Sprite[] attackBack;

    [Header("Timing")]
    public float walkFps = 8f;
    public float attackFps = 10f;

    private SpriteRenderer sr;
    private GridMover mover;
    private EnemyAttack enemyAttack;
    private Vector2Int facing = Vector2Int.down;
    private Vector3 lastPos;
    private float animClock;
    private float lastAttackTime = float.NegativeInfinity;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mover = GetComponent<GridMover>();
        enemyAttack = GetComponent<EnemyAttack>();
    }

    private void Start()
    {
        lastPos = transform.position;
        if (sr.sprite == null)
        {
            Sprite[] idle = GetWalkFrames(facing, out _);
            if (idle != null && idle.Length > 0)
                sr.sprite = idle[0];
        }
    }

    private void Update()
    {
        Vector3 dp = transform.position - lastPos;
        lastPos = transform.position;

        if (Mathf.Abs(dp.x) > 0.001f || Mathf.Abs(dp.y) > 0.001f)
        {
            if (Mathf.Abs(dp.x) >= Mathf.Abs(dp.y))
                facing = dp.x > 0f ? Vector2Int.right : Vector2Int.left;
            else
                facing = dp.y > 0f ? Vector2Int.up : Vector2Int.down;
        }

        if (enemyAttack != null && enemyAttack.LastAttackTime > lastAttackTime)
        {
            lastAttackTime = enemyAttack.LastAttackTime;
            facing = enemyAttack.LastAttackFacing;
        }

        animClock += Time.deltaTime;

        bool flipX;
        Sprite[] attackFrames = GetAttackFrames(facing, out _);
        bool isAttacking = (Time.time - lastAttackTime) < GetDuration(attackFrames, attackFps);

        if (isAttacking)
        {
            Sprite[] frames = GetAttackFrames(facing, out flipX);
            if (frames == null || frames.Length == 0) return;
            float elapsed = Mathf.Max(0f, Time.time - lastAttackTime);
            int frame = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(elapsed * attackFps));
            sr.flipX = flipX;
            sr.sprite = frames[frame];
            return;
        }

        bool isMoving = mover != null && mover.IsMoving;
        Sprite[] walkFrames = GetWalkFrames(facing, out flipX);
        if (walkFrames == null || walkFrames.Length == 0) return;

        sr.flipX = flipX;
        if (isMoving)
            sr.sprite = walkFrames[Mathf.FloorToInt(animClock * walkFps) % walkFrames.Length];
        else
            sr.sprite = walkFrames[0];
    }

private Sprite[] GetWalkFrames(Vector2Int dir, out bool flipX)
    {
        flipX = false;
        if (dir == Vector2Int.up)    return Prefer(walkBack, walkFront);
        if (dir == Vector2Int.left)  return Prefer(walkSide, walkFront);
        if (dir == Vector2Int.right) { flipX = true; return Prefer(walkSide, walkFront); }
        return Prefer(walkFront, walkSide);
    }

private Sprite[] GetAttackFrames(Vector2Int dir, out bool flipX)
    {
        flipX = false;
        if (dir == Vector2Int.up)    return Prefer(attackBack, attackFront);
        if (dir == Vector2Int.left)  return Prefer(attackSide, attackFront);
        if (dir == Vector2Int.right) { flipX = true; return Prefer(attackSide, attackFront); }
        return Prefer(attackFront, attackSide);
    }

    private float GetDuration(Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0 || fps <= 0f) return 0f;
        return frames.Length / fps;
    }

    private Sprite[] Prefer(params Sprite[][] choices)
    {
        foreach (Sprite[] c in choices)
            if (c != null && c.Length > 0) return c;
        return null;
    }
}
