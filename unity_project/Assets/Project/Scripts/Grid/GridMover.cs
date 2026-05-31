using System.Collections;
using UnityEngine;

[RequireComponent(typeof(GridOccupant))]
public class GridMover : MonoBehaviour
{
    [Header("Movement")]
    public float moveDuration = 0.23f;

    public bool IsMoving { get; private set; }

    private GridOccupant occupant;

    private void Awake()
    {
        occupant = GetComponent<GridOccupant>();
    }

    public bool CanMove(Vector2Int direction, int distance = 1, bool ignoreOccupants = false)
    {
        if (IsMoving || direction == Vector2Int.zero || GridManager.Instance == null)
        {
            return false;
        }

        for (int i = 1; i <= distance; i++)
        {
            Vector2Int next = occupant.CurrentCell + direction * i;
            if (!GridManager.Instance.IsWalkable(next, ignoreOccupants))
            {
                return false;
            }
        }

        return true;
    }

    public void Move(Vector2Int direction, int distance = 1, bool ignoreOccupants = false)
    {
        if (!CanMove(direction, distance, ignoreOccupants))
        {
            return;
        }

        Vector2Int target = occupant.CurrentCell + direction * distance;
        StartCoroutine(MoveRoutine(target, moveDuration));
    }

    public void ForceMoveTo(Vector2Int targetCell, float overrideDuration = -1f)
    {
        if (IsMoving || GridManager.Instance == null)
        {
            return;
        }

        float duration = overrideDuration >= 0f ? overrideDuration : moveDuration;
        StartCoroutine(MoveRoutine(targetCell, duration));
    }

    private IEnumerator MoveRoutine(Vector2Int targetCell, float duration)
    {
        IsMoving = true;

        Vector3 start = transform.position;
        Vector3 end = GridManager.Instance.CellToWorld(targetCell);

        occupant.SetCell(targetCell);

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(timer / duration);
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        transform.position = end;
        IsMoving = false;
    }
}
