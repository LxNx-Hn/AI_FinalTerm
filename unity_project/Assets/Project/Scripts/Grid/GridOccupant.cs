using UnityEngine;

public class GridOccupant : MonoBehaviour
{
    public Vector2Int CurrentCell { get; private set; }

    [Header("Options")]
    public bool blocksMovement = true;

    private void Start()
    {
        SnapToGridAndRegister();
    }

    public void SnapToGridAndRegister()
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        if (blocksMovement)
        {
            GridManager.Instance.Unregister(this, CurrentCell);
        }

        CurrentCell = GridManager.Instance.WorldToCell(transform.position);
        transform.position = GridManager.Instance.CellToWorld(CurrentCell);

        if (blocksMovement)
        {
            GridManager.Instance.Register(this, CurrentCell);
        }
    }

    public void SetCell(Vector2Int newCell)
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        Vector2Int oldCell = CurrentCell;
        CurrentCell = newCell;

        if (blocksMovement)
        {
            GridManager.Instance.Move(this, oldCell, newCell);
        }
    }

    public void Release()
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        if (blocksMovement)
        {
            GridManager.Instance.Unregister(this, CurrentCell);
        }
    }
}
