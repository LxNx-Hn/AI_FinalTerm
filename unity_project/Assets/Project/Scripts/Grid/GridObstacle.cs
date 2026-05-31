using UnityEngine;

public class GridObstacle : MonoBehaviour
{
    private Vector2Int cell;

    private void Start()
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        cell = GridManager.Instance.WorldToCell(transform.position);
        transform.position = GridManager.Instance.CellToWorld(cell);
        GridManager.Instance.AddBlocked(cell);
    }

    private void OnDestroy()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.RemoveBlocked(cell);
        }
    }
}
