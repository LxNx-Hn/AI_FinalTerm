using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(GridMover))]
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerVisualAnimator2D : MonoBehaviour
{
    [Header("Idle")]
    public Sprite[] idleDown;
    public Sprite[] idleUp;
    public Sprite[] idleSide;

    [Header("Walk")]
    public Sprite[] walkDown;
    public Sprite[] walkUp;
    public Sprite[] walkLeft;
    public Sprite[] walkRight;

    [Header("Side-Scroll Override")]
    [Tooltip("Optional shared side-walk frames for side-view scenes. When present, left/right use this clip with flipX.")]
    public Sprite[] walkSide;

    [Header("Attack")]
    public Sprite[] attackDown;
    public Sprite[] attackUp;
    public Sprite[] attackLeft;
    public Sprite[] attackRight;

    [Header("Hurt")]
    public Sprite[] hurtDown;
    public Sprite[] hurtUp;
    public Sprite[] hurtLeft;
    public Sprite[] hurtRight;

    [Header("Dead")]
    public Sprite[] deadSequence;

    [Header("Timing")]
    public float idleFps = 2f;
    public float walkFps = 9f;
    public float attackFps = 12f;
    public float hurtFps = 10f;
    public float hurtDuration = 0.38f;
    public float deadFps = 10f;

    [Header("Visual Alignment")]
    [SerializeField] private Vector3 visualLocalOffset = new Vector3(0f, -1f, 0f);

    private const string VisualChildName = "PlayerVisual";

    private SpriteRenderer rootSpriteRenderer;
    private SpriteRenderer spriteRenderer;
    private PlayerController controller;
    private GridMover mover;
    private PlayerCombat combat;
    private PlayerHealth health;

    private float lastAttackTime = float.NegativeInfinity;
    private float hurtTimer;
    private float animationClock;
    private float deathClock;
    private float lastDamageTime = float.NegativeInfinity;
    private Vector2Int lastAttackFacing = Vector2Int.down;
    private Vector2Int lastHurtFacing = Vector2Int.down;
    private Vector2Int deathFacing = Vector2Int.down;
    private int lastHp;
    private bool deathStarted;
    private Vector3 previousWorldPosition;
    private bool movedByTransform;
    private bool forcedWalkEnabled;
    private Vector2Int forcedWalkFacing = Vector2Int.right;

    private void Awake()
    {
        if (!ShouldRunThisAnimator())
        {
            enabled = false;
            return;
        }

        rootSpriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer = EnsureVisualRenderer(rootSpriteRenderer);
        controller = GetComponent<PlayerController>();
        mover = GetComponent<GridMover>();
        combat = GetComponent<PlayerCombat>();
        health = GetComponent<PlayerHealth>();

        if (health != null && spriteRenderer != null)
        {
            health.spriteRenderer = spriteRenderer;
        }
    }

    private void Start()
    {
        if (health == null)
        {
            return;
        }

        lastHp = health.currentHp;
        previousWorldPosition = transform.position;
        RefreshSprite();
    }

    private void Update()
    {
        if (health == null || spriteRenderer == null)
        {
            return;
        }

        movedByTransform = (transform.position - previousWorldPosition).sqrMagnitude > 0.0001f;
        previousWorldPosition = transform.position;

        if (health.currentHp < lastHp)
        {
            hurtTimer = hurtDuration;
            lastDamageTime = Time.time;
            lastHurtFacing = controller != null ? controller.Facing : Vector2Int.down;
        }

        lastHp = health.currentHp;
        hurtTimer = Mathf.Max(0f, hurtTimer - Time.deltaTime);
        bool isWalking = forcedWalkEnabled || movedByTransform || (mover != null && mover.IsMoving);
        if (isWalking)
            animationClock += Time.unscaledDeltaTime;
        else
            animationClock = 0f;

        if (health.IsDead)
        {
            if (!deathStarted)
            {
                deathStarted = true;
                deathClock = 0f;
                deathFacing = controller != null ? controller.Facing : Vector2Int.down;
            }
            else
            {
                deathClock += Time.unscaledDeltaTime;
            }
        }

        if (combat.LastAttackTime > lastAttackTime)
        {
            lastAttackTime = combat.LastAttackTime;
            lastAttackFacing = combat.LastAttackFacing;
        }

        RefreshSprite();
    }

    public void SetForcedWalk(Vector2Int facing, bool enabled)
    {
        if (facing == Vector2Int.zero)
        {
            facing = Vector2Int.right;
        }

        forcedWalkFacing = facing;
        forcedWalkEnabled = enabled;

        if (enabled && controller != null)
        {
            controller.SetFacing(forcedWalkFacing);
        }
    }

    public void SetVisualLocalOffset(Vector3 offset)
    {
        visualLocalOffset = offset;

        Transform visualTransform = transform.Find(VisualChildName);
        if (visualTransform != null)
        {
            visualTransform.localPosition = visualLocalOffset;
        }
    }

    private void RefreshSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Vector2Int facing = forcedWalkEnabled ? forcedWalkFacing : (controller != null ? controller.Facing : Vector2Int.down);
        Sprite[] sequence;
        float fps;
        bool flipX = false;

        if (health != null && health.IsDead)
        {
            sequence = GetDeadSprites(deathFacing, out flipX);
            fps = deadFps;
        }
        else if (hurtTimer > 0f)
        {
            sequence = GetHurtSprites(lastHurtFacing, out flipX);
            fps = hurtFps;
        }
        else if (Time.time - lastAttackTime <= GetAnimationDuration(GetAttackSprites(lastAttackFacing, out _), attackFps))
        {
            sequence = GetAttackSprites(lastAttackFacing, out flipX);
            fps = attackFps;
        }
        else if (forcedWalkEnabled)
        {
            sequence = GetWalkSprites(forcedWalkFacing, out flipX);
            fps = walkFps;
        }
        else if ((mover != null && mover.IsMoving) || movedByTransform)
        {
            sequence = GetWalkSprites(facing, out flipX);
            fps = walkFps;
        }
        else
        {
            sequence = GetIdleSprites(facing, out flipX);
            fps = idleFps;
        }

        if (sequence == null || sequence.Length == 0)
        {
            return;
        }

        spriteRenderer.flipX = flipX;
        int frame;
        if (health != null && health.IsDead)
        {
            frame = Mathf.Min(sequence.Length - 1, Mathf.FloorToInt(deathClock * fps));
        }
        else if (hurtTimer > 0f)
        {
            float hurtElapsed = Mathf.Max(0f, Time.time - lastDamageTime);
            frame = Mathf.Min(sequence.Length - 1, Mathf.FloorToInt(hurtElapsed * fps));
        }
        else if (Time.time - lastAttackTime <= GetAnimationDuration(GetAttackSprites(lastAttackFacing, out _), attackFps))
        {
            float attackElapsed = Mathf.Max(0f, Time.time - lastAttackTime);
            frame = Mathf.Min(sequence.Length - 1, Mathf.FloorToInt(attackElapsed * fps));
        }
        else
        {
            frame = Mathf.FloorToInt(animationClock * fps) % sequence.Length;
        }

        spriteRenderer.sprite = sequence[frame];
    }

    private float GetAnimationDuration(Sprite[] sprites, float fps)
    {
        if (sprites == null || sprites.Length == 0 || fps <= 0f)
        {
            return 0f;
        }

        return sprites.Length / fps;
    }

    private Sprite[] GetIdleSprites(Vector2Int facing, out bool flipX)
    {
        flipX = false;

        if (facing == Vector2Int.up)
        {
            return Prefer(idleUp, idleDown);
        }

        if (facing == Vector2Int.left)
        {
            flipX = true;
            return Prefer(idleSide, idleDown);
        }

        if (facing == Vector2Int.right)
        {
            return Prefer(idleSide, idleDown);
        }

        return Prefer(idleDown, idleSide);
    }

    private Sprite[] GetWalkSprites(Vector2Int facing, out bool flipX)
    {
        flipX = false;

        if (facing == Vector2Int.up)
        {
            return Prefer(walkUp, walkDown, idleUp, idleDown);
        }

        if (facing == Vector2Int.left)
        {
            if (walkSide != null && walkSide.Length > 0)
            {
                flipX = true;
                return walkSide;
            }

            if (walkLeft != null && walkLeft.Length > 0)
                return walkLeft;

            // walkRight를 좌우 반전하여 사용
            if (walkRight != null && walkRight.Length > 0)
            {
                flipX = true;
                return walkRight;
            }

            flipX = true;
            return Prefer(idleSide, walkDown, idleDown);
        }

        if (facing == Vector2Int.right)
        {
            if (walkSide != null && walkSide.Length > 0)
            {
                return walkSide;
            }

            if (walkRight != null && walkRight.Length > 0)
                return walkRight;

            return Prefer(idleSide, walkDown, idleDown);
        }

        return Prefer(walkDown, idleDown, walkRight);
    }

    private Sprite[] GetAttackSprites(Vector2Int facing, out bool flipX)
    {
        flipX = false;

        if (facing == Vector2Int.up)
        {
            return Prefer(attackUp, attackDown);
        }

        if (facing == Vector2Int.left)
        {
            if (attackLeft != null && attackLeft.Length > 0)
            {
                return attackLeft;
            }

            if (attackRight != null && attackRight.Length > 0)
            {
                flipX = true;
                return attackRight;
            }

            return Prefer(attackDown, walkDown, idleDown);
        }

        if (facing == Vector2Int.right)
        {
            return Prefer(attackRight, attackDown, walkRight, idleSide);
        }

        return Prefer(attackDown, walkDown, idleDown);
    }

    private Sprite[] GetHurtSprites(Vector2Int facing, out bool flipX)
    {
        flipX = false;

        if (facing == Vector2Int.up)
        {
            return Prefer(hurtUp, idleUp, hurtDown);
        }

        if (facing == Vector2Int.left)
        {
            if (hurtLeft != null && hurtLeft.Length > 0)
            {
                return hurtLeft;
            }

            if (hurtRight != null && hurtRight.Length > 0)
            {
                flipX = true;
                return hurtRight;
            }

            flipX = true;
            return Prefer(idleSide, hurtDown, idleDown);
        }

        if (facing == Vector2Int.right)
        {
            return Prefer(hurtRight, idleSide, hurtDown);
        }

        return Prefer(hurtDown, idleDown);
    }

    private Sprite[] GetDeadSprites(Vector2Int facing, out bool flipX)
    {
        flipX = false;

        if (deadSequence == null || deadSequence.Length == 0)
        {
            return GetHurtSprites(facing, out flipX);
        }

        return deadSequence;
    }

    private Sprite[] Prefer(params Sprite[][] choices)
    {
        foreach (Sprite[] choice in choices)
        {
            if (choice != null && choice.Length > 0)
            {
                return choice;
            }
        }

        return null;
    }

    private bool ShouldRunThisAnimator()
    {
        PlayerVisualAnimator2D[] animators = GetComponents<PlayerVisualAnimator2D>();
        if (animators == null || animators.Length <= 1)
        {
            return true;
        }

        PlayerVisualAnimator2D selected = null;
        for (int i = animators.Length - 1; i >= 0; i--)
        {
            PlayerVisualAnimator2D candidate = animators[i];
            if (candidate != null && candidate.enabled && candidate.HasAnySpritesConfigured())
            {
                selected = candidate;
                break;
            }
        }

        if (selected == null)
        {
            selected = animators[animators.Length - 1];
        }

        return selected == this;
    }

    private bool HasAnySpritesConfigured()
    {
        return HasSprites(idleDown)
            || HasSprites(idleUp)
            || HasSprites(idleSide)
            || HasSprites(walkDown)
            || HasSprites(walkUp)
            || HasSprites(walkLeft)
            || HasSprites(walkRight)
            || HasSprites(walkSide)
            || HasSprites(attackDown)
            || HasSprites(attackUp)
            || HasSprites(attackLeft)
            || HasSprites(attackRight)
            || HasSprites(hurtDown)
            || HasSprites(hurtUp)
            || HasSprites(hurtLeft)
            || HasSprites(hurtRight)
            || HasSprites(deadSequence);
    }

    private bool HasSprites(Sprite[] sprites)
    {
        return sprites != null && sprites.Length > 0;
    }

    private SpriteRenderer EnsureVisualRenderer(SpriteRenderer source)
    {
        Transform visualTransform = transform.Find(VisualChildName);
        if (visualTransform == null)
        {
            GameObject visualObject = new GameObject(VisualChildName);
            visualTransform = visualObject.transform;
            visualTransform.SetParent(transform, false);
        }

        visualTransform.localPosition = visualLocalOffset;
        visualTransform.localRotation = Quaternion.identity;
        visualTransform.localScale = Vector3.one;

        SpriteRenderer visualRenderer = visualTransform.GetComponent<SpriteRenderer>();
        if (visualRenderer == null)
        {
            visualRenderer = visualTransform.gameObject.AddComponent<SpriteRenderer>();
        }

        if (source != null && visualRenderer != source)
        {
            CopyRendererSettings(source, visualRenderer);
            source.enabled = false;
        }

        return visualRenderer != null ? visualRenderer : source;
    }

    private void CopyRendererSettings(SpriteRenderer source, SpriteRenderer target)
    {
        target.sprite = source.sprite;
        target.color = source.color;
        target.flipX = source.flipX;
        target.flipY = source.flipY;
        target.drawMode = source.drawMode;
        target.size = source.size;
        target.maskInteraction = source.maskInteraction;
        target.sortingLayerID = source.sortingLayerID;
        target.sortingOrder = source.sortingOrder;
        target.sharedMaterial = source.sharedMaterial;
        target.enabled = true;
    }
}
