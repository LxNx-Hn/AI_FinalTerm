using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid")]
    public float tileSize = 1f;
    public Vector2 worldOrigin = Vector2.zero;

    private readonly HashSet<Vector2Int> blockedCells = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, GridOccupant> occupants = new Dictionary<Vector2Int, GridOccupant>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(
            worldOrigin.x + cell.x * tileSize,
            worldOrigin.y + cell.y * tileSize,
            0f);
    }

    public Vector2Int WorldToCell(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt((worldPosition.x - worldOrigin.x) / tileSize);
        int y = Mathf.RoundToInt((worldPosition.y - worldOrigin.y) / tileSize);
        return new Vector2Int(x, y);
    }

    public bool IsBlocked(Vector2Int cell)
    {
        return blockedCells.Contains(cell);
    }

    public void AddBlocked(Vector2Int cell)
    {
        blockedCells.Add(cell);
    }

    public void RemoveBlocked(Vector2Int cell)
    {
        blockedCells.Remove(cell);
    }

    public bool HasOccupant(Vector2Int cell)
    {
        return occupants.ContainsKey(cell) && occupants[cell] != null;
    }

    public GridOccupant GetOccupant(Vector2Int cell)
    {
        occupants.TryGetValue(cell, out GridOccupant result);
        return result;
    }

    public bool IsWalkable(Vector2Int cell, bool ignoreOccupant = false)
    {
        if (IsBlocked(cell))
        {
            return false;
        }

        if (!ignoreOccupant && HasOccupant(cell))
        {
            return false;
        }

        return true;
    }

    public void Register(GridOccupant occupant, Vector2Int cell)
    {
        if (occupant == null)
        {
            return;
        }

        occupants[cell] = occupant;
    }

    public void Move(GridOccupant occupant, Vector2Int from, Vector2Int to)
    {
        if (occupant == null)
        {
            return;
        }

        if (occupants.ContainsKey(from) && occupants[from] == occupant)
        {
            occupants.Remove(from);
        }

        occupants[to] = occupant;
    }

    public void Unregister(GridOccupant occupant, Vector2Int cell)
    {
        if (occupant == null)
        {
            return;
        }

        if (occupants.ContainsKey(cell) && occupants[cell] == occupant)
        {
            occupants.Remove(cell);
        }
    }
}
