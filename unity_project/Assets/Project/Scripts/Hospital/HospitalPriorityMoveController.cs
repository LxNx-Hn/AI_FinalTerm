using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class HospitalPriorityMoveController : MonoBehaviour
{
    [SerializeField] private PlayerController facingController;
    [SerializeField] private GridMover mover;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private Vector2 collisionProbeSize = new Vector2(0.86f, 0.86f);

    private readonly List<Vector2Int> heldDirections = new List<Vector2Int>(4);

    private void Reset()
    {
        facingController = GetComponent<PlayerController>();
        mover = GetComponent<GridMover>();
        health = GetComponent<PlayerHealth>();
    }

    private void Awake()
    {
        if (facingController == null)
        {
            facingController = GetComponent<PlayerController>();
        }

        if (mover == null)
        {
            mover = GetComponent<GridMover>();
        }

        if (health == null)
        {
            health = GetComponent<PlayerHealth>();
        }
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            heldDirections.Clear();
            return;
        }

        RefreshHeldDirections();

        Vector2Int direction = GetCurrentDirection();
        if (direction == Vector2Int.zero)
        {
            return;
        }

        facingController?.SetFacing(direction);

        if (mover != null && !mover.IsMoving && mover.CanMove(direction) && !HasEnemyColliderInNextCell(direction))
        {
            mover.Move(direction);
        }
    }

    private void RefreshHeldDirections()
    {
        RegisterPress(KeyCode.W, Vector2Int.up);
        RegisterPress(KeyCode.UpArrow, Vector2Int.up);
        RegisterPress(KeyCode.S, Vector2Int.down);
        RegisterPress(KeyCode.DownArrow, Vector2Int.down);
        RegisterPress(KeyCode.A, Vector2Int.left);
        RegisterPress(KeyCode.LeftArrow, Vector2Int.left);
        RegisterPress(KeyCode.D, Vector2Int.right);
        RegisterPress(KeyCode.RightArrow, Vector2Int.right);

        RemoveReleasedDirection(Vector2Int.up, KeyCode.W, KeyCode.UpArrow);
        RemoveReleasedDirection(Vector2Int.down, KeyCode.S, KeyCode.DownArrow);
        RemoveReleasedDirection(Vector2Int.left, KeyCode.A, KeyCode.LeftArrow);
        RemoveReleasedDirection(Vector2Int.right, KeyCode.D, KeyCode.RightArrow);
    }

    private void RegisterPress(KeyCode key, Vector2Int direction)
    {
        if (!Input.GetKeyDown(key))
        {
            return;
        }

        heldDirections.Remove(direction);
        heldDirections.Add(direction);
    }

    private void RemoveReleasedDirection(Vector2Int direction, KeyCode primary, KeyCode alternate)
    {
        if (Input.GetKey(primary) || Input.GetKey(alternate))
        {
            return;
        }

        heldDirections.Remove(direction);
    }

    private Vector2Int GetCurrentDirection()
    {
        for (int i = heldDirections.Count - 1; i >= 0; i--)
        {
            Vector2Int direction = heldDirections[i];
            if (IsDirectionHeld(direction))
            {
                return direction;
            }

            heldDirections.RemoveAt(i);
        }

        return Vector2Int.zero;
    }

    private static bool IsDirectionHeld(Vector2Int direction)
    {
        if (direction == Vector2Int.up)
        {
            return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        }

        if (direction == Vector2Int.down)
        {
            return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        }

        if (direction == Vector2Int.left)
        {
            return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        }

        if (direction == Vector2Int.right)
        {
            return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        }

        return false;
    }

    private bool HasEnemyColliderInNextCell(Vector2Int direction)
    {
        if (mover == null || GridManager.Instance == null)
        {
            return false;
        }

        GridOccupant occupant = mover.GetComponent<GridOccupant>();
        if (occupant == null)
        {
            return false;
        }

        Vector2Int targetCell = occupant.CurrentCell + direction;
        Vector3 targetWorld = GridManager.Instance.CellToWorld(targetCell);
        Collider2D[] hits = Physics2D.OverlapBoxAll(targetWorld, collisionProbeSize, 0f);

        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.isTrigger || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.GetComponentInParent<EnemyHealth>() != null)
            {
                return true;
            }
        }

        return false;
    }
}
