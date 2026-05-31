using System.Collections.Generic;
using UnityEngine;

public class GridBlockedArea : MonoBehaviour
{
    [Min(1)] public int width = 1;
    [Min(1)] public int height = 1;
    public Vector2Int pivotOffset;

    private readonly List<Vector2Int> occupiedCells = new List<Vector2Int>();
    private BoxCollider2D boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        RegisterBlockedCells();
    }

    private void OnDestroy()
    {
        ClearBlockedCells();
    }

    private void RegisterBlockedCells()
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        ClearBlockedCells();

        Vector2 centerWorld = boxCollider != null ? boxCollider.bounds.center : transform.position;
        float centerX = ((centerWorld.x - GridManager.Instance.worldOrigin.x) / GridManager.Instance.tileSize) + pivotOffset.x;
        float centerY = ((centerWorld.y - GridManager.Instance.worldOrigin.y) / GridManager.Instance.tileSize) + pivotOffset.y;

        int minX = GetMinCell(centerX, width);
        int minY = GetMinCell(centerY, height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int cell = new Vector2Int(minX + x, minY + y);
                GridManager.Instance.AddBlocked(cell);
                occupiedCells.Add(cell);
            }
        }
    }

    private void ClearBlockedCells()
    {
        if (GridManager.Instance == null)
        {
            occupiedCells.Clear();
            return;
        }

        foreach (Vector2Int cell in occupiedCells)
        {
            GridManager.Instance.RemoveBlocked(cell);
        }

        occupiedCells.Clear();
    }

    private static int GetMinCell(float center, int size)
    {
        int min = Mathf.FloorToInt(center - ((size - 1) * 0.5f));
        if (size % 2 == 0)
        {
            min += 1;
        }

        return min;
    }
}
