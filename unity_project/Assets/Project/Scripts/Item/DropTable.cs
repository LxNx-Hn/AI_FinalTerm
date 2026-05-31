using System.Collections.Generic;
using UnityEngine;

public class DropTable : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject healItemPrefab;
    public GameObject attackBuffItemPrefab;
    public GameObject speedBuffItemPrefab;

    [Header("Drop Rates")]
    public bool guaranteedDrop = false;
    public float healRate = 0.15f;
    public float attackBuffRate = 0.10f;
    public float speedBuffRate = 0.10f;

    [Header("Drop Placement")]
    [Tooltip("Maximum search radius when the drop cell is occupied.")]
    public int maxSearchRadius = 5;

    public void TryDrop(Vector3 position)
    {
        GameObject prefab = ChooseDrop();
        if (prefab == null)
        {
            return;
        }

        Vector3 dropPos = FindEmptyDropPosition(position);
        Instantiate(prefab, dropPos, Quaternion.identity);
    }

    /// <summary>
    /// Finds a nearby empty cell to place the dropped item.
    /// Avoids stacking items on the same tile.
    /// </summary>
    private Vector3 FindEmptyDropPosition(Vector3 origin)
    {
        if (GridManager.Instance == null)
        {
            return origin;
        }

        Vector2Int originCell = GridManager.Instance.WorldToCell(origin);

        // Check if origin is free of items
        if (!HasItemAt(originCell))
        {
            return origin;
        }

        // BFS outward to find the nearest empty cell
        for (int radius = 1; radius <= maxSearchRadius; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                    {
                        continue; // only check the border of this radius
                    }

                    Vector2Int candidate = originCell + new Vector2Int(dx, dy);

                    if (GridManager.Instance.IsBlocked(candidate))
                    {
                        continue;
                    }

                    if (HasItemAt(candidate))
                    {
                        continue;
                    }

                    return GridManager.Instance.CellToWorld(candidate);
                }
            }
        }

        // Fallback: drop at origin even if occupied
        return origin;
    }

    private bool HasItemAt(Vector2Int cell)
    {
        Vector3 worldPos = GridManager.Instance.CellToWorld(cell);
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
        foreach (var hit in hits)
        {
            if (hit.GetComponent<GridItem>() != null)
            {
                return true;
            }
        }

        // Also check all GridItem objects by position (some may lack colliders)
        GridItem[] allItems = FindObjectsOfType<GridItem>();
        foreach (var item in allItems)
        {
            GridOccupant occ = item.GetComponent<GridOccupant>();
            if (occ != null && occ.CurrentCell == cell)
            {
                return true;
            }
        }

        return false;
    }

    private GameObject ChooseDrop()
    {
        if (guaranteedDrop)
        {
            int roll = Random.Range(0, 3);
            if (roll == 0)
            {
                return healItemPrefab;
            }

            if (roll == 1)
            {
                return attackBuffItemPrefab;
            }

            return speedBuffItemPrefab;
        }

        float rollValue = Random.value;
        if (rollValue < healRate)
        {
            return healItemPrefab;
        }

        if (rollValue < healRate + attackBuffRate)
        {
            return attackBuffItemPrefab;
        }

        if (rollValue < healRate + attackBuffRate + speedBuffRate)
        {
            return speedBuffItemPrefab;
        }

        return null;
    }
}
