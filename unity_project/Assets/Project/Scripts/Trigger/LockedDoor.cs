using UnityEngine;

/// <summary>
/// A grid-blocking door that can be opened by a linked PressurePlate.
/// While locked the door registers as a GridObstacle (blocks movement).
/// When Open() is called it unblocks the cell, plays a simple visual
/// change (color → transparent), and optionally destroys itself.
/// </summary>
public class LockedDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("If true the door GameObject is destroyed after opening.")]
    public bool destroyOnOpen = false;

    [Tooltip("Color of the door while locked.")]
    public Color lockedColor = new Color(0.6f, 0.2f, 0.2f, 1f); // dark red

    [Tooltip("Color of the door once opened (set alpha 0 to vanish).")]
    public Color openedColor = new Color(0.2f, 0.8f, 0.2f, 0.15f); // faint green

    public bool IsOpen { get; private set; }

    private Vector2Int cell;
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (GridManager.Instance == null) return;

        cell = GridManager.Instance.WorldToCell(transform.position);
        transform.position = GridManager.Instance.CellToWorld(cell);

        // Block this cell
        GridManager.Instance.AddBlocked(cell);

        // Visual
        sr.color = lockedColor;
    }

    /// <summary>
    /// Called by PressurePlate when activated.
    /// </summary>
    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;

        if (GridManager.Instance != null)
            GridManager.Instance.RemoveBlocked(cell);

        sr.color = openedColor;

        if (destroyOnOpen)
            Destroy(gameObject, 0.3f);
    }

    /// <summary>
    /// Re-lock the door (optional, for toggle plates).
    /// </summary>
    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;

        if (GridManager.Instance != null)
            GridManager.Instance.AddBlocked(cell);

        sr.color = lockedColor;
    }

    private void OnDestroy()
    {
        if (GridManager.Instance != null && !IsOpen)
            GridManager.Instance.RemoveBlocked(cell);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsOpen ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.85f);
    }
}
