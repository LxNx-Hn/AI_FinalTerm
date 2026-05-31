using UnityEngine;

public abstract class GridZoneTriggerBase : MonoBehaviour
{
    public Vector2Int size = new Vector2Int(3, 3);
    public bool triggerOnce = true;
    public bool ignoreCutsceneFreeze;

    private bool triggered;
    protected GridOccupant player;

    protected virtual void Start()
    {
        CachePlayer();
    }

    protected virtual void Update()
    {
        if (player == null)
        {
            CachePlayer();
        }

        if (player == null || GridManager.Instance == null)
        {
            return;
        }

        if (triggerOnce && triggered)
        {
            return;
        }

        if (!ignoreCutsceneFreeze && CutsceneFreezeManager.IsFrozen)
        {
            return;
        }

        if (Contains(player.CurrentCell) && OnTriggered())
        {
            triggered = true;
        }
    }

    protected bool Contains(Vector2Int cell)
    {
        Vector2Int center = GridManager.Instance.WorldToCell(transform.position);
        int minX = center.x - size.x / 2;
        int maxX = minX + size.x - 1;
        int minY = center.y - size.y / 2;
        int maxY = minY + size.y - 1;

        return cell.x >= minX && cell.x <= maxX && cell.y >= minY && cell.y <= maxY;
    }

    protected abstract bool OnTriggered();

    private void CachePlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<GridOccupant>();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0.1f));
    }
}
