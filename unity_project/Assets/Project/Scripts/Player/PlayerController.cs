using UnityEngine;

[RequireComponent(typeof(GridMover))]
public class PlayerController : MonoBehaviour
{
    public Vector2Int Facing { get; private set; } = Vector2Int.down;

    /// <summary>씬 초기화 시 초기 방향을 강제 설정 (RooftopEndingSequenceRuntime 등에서 사용)</summary>
    public void SetFacing(Vector2Int dir) => Facing = dir;

    public void SetExternalInput(Vector2Int input)
    {
        useExternalInput = true;
        externalInput = input;
    }

    public void ClearExternalInput()
    {
        useExternalInput = false;
        externalInput = Vector2Int.zero;
    }

    private GridMover mover;
    private Vector2Int bufferedDirection;
    private PlayerHealth health;
    private bool useExternalInput;
    private Vector2Int externalInput;

    private void Awake()
    {
        mover = GetComponent<GridMover>();
        health = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            bufferedDirection = Vector2Int.zero;
            return;
        }

        Vector2Int input = ReadInput();

        if (input != Vector2Int.zero)
        {
            bufferedDirection = input;
            Facing = input;
        }
        else
        {
            bufferedDirection = Vector2Int.zero;
        }

        if (!mover.IsMoving && bufferedDirection != Vector2Int.zero)
        {
            if (mover.CanMove(bufferedDirection))
            {
                mover.Move(bufferedDirection);
            }
            else
            {
                bufferedDirection = Vector2Int.zero;
            }
        }
    }

    private Vector2Int ReadInput()
    {
        if (useExternalInput)
        {
            // One-shot: consume and clear immediately so RL input doesn't repeat across frames.
            Vector2Int value = externalInput;
            useExternalInput = false;
            externalInput = Vector2Int.zero;
            return value;
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            return Vector2Int.up;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            return Vector2Int.down;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            return Vector2Int.left;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            return Vector2Int.right;
        }

        return Vector2Int.zero;
    }
}
