using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Stage01DirectionalEnemyAnimator : MonoBehaviour
{
    [SerializeField] private string variant = "iv";
    [SerializeField] private float frameInterval = 0.12f;
    [SerializeField] private Transform root;

    private SpriteRenderer spriteRenderer;
    private Sprite[] downFrames;
    private Sprite[] leftFrames;
    private Sprite[] rightFrames;
    private Sprite[] upFrames;
    private Vector3 lastRootPosition;
    private Vector2Int facing = Vector2Int.down;
    private int frameIndex;
    private float frameTimer;

    public void Configure(string newVariant, Transform newRoot)
    {
        variant = string.IsNullOrWhiteSpace(newVariant) ? "iv" : newVariant;
        root = newRoot;
        LoadFrames();
        ApplyFrame(0);
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (root == null)
        {
            root = transform.parent != null ? transform.parent : transform;
        }

        LoadFrames();
        lastRootPosition = root.position;
        ApplyFrame(0);
    }

    private void Update()
    {
        if (root == null)
        {
            return;
        }

        Vector3 movementDelta = root.position - lastRootPosition;
        bool moving = movementDelta.sqrMagnitude > 0.0001f;
        if (!FacePlayer())
        {
            if (moving)
            {
                facing = DirectionFromDelta(movementDelta);
            }
        }

        frameTimer -= Time.deltaTime;
        if (frameTimer <= 0f)
        {
            Sprite[] frames = CurrentFrames();
            int frameCount = Mathf.Max(1, frames.Length);
            frameIndex = moving ? (frameIndex + 1) % frameCount : 0;
            frameTimer = Mathf.Max(0.04f, frameInterval);
            ApplyFrame(frameIndex);
        }

        lastRootPosition = root.position;
    }

    private void LoadFrames()
    {
        downFrames = LoadDirection("down");
        leftFrames = LoadDirection("left");
        rightFrames = LoadDirection("right");
        upFrames = LoadDirection("up");
    }

    private Sprite[] LoadDirection(string direction)
    {
        Sprite[] frames = new Sprite[4];
        for (int i = 0; i < frames.Length; i++)
        {
            frames[i] = Resources.Load<Sprite>($"Stage01/Enemies/Directional/{variant}/{variant}_{direction}_{i}");
        }

        return frames;
    }

    private bool FacePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return false;
        }

        Vector3 delta = player.transform.position - root.position;
        if (delta.sqrMagnitude > 0.01f)
        {
            facing = DirectionFromDelta(delta);
            return true;
        }

        return false;
    }

    private static Vector2Int DirectionFromDelta(Vector3 delta)
    {
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
        {
            return delta.x >= 0f ? Vector2Int.right : Vector2Int.left;
        }

        return delta.y >= 0f ? Vector2Int.up : Vector2Int.down;
    }

    private Sprite[] CurrentFrames()
    {
        if (facing == Vector2Int.left)
            return leftFrames;
        if (facing == Vector2Int.right)
            return rightFrames;
        if (facing == Vector2Int.up)
            return upFrames;

        return downFrames;
    }

    private void ApplyFrame(int index)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        Sprite[] frames = CurrentFrames();
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        index = Mathf.Clamp(index, 0, frames.Length - 1);
        if (frames[index] != null)
        {
            spriteRenderer.sprite = frames[index];
        }
    }
}
