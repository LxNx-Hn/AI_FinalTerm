using UnityEngine;

[RequireComponent(typeof(GridOccupant))]
public class EnemyAttack : MonoBehaviour
{
    public int damage = 1;
    public float attackCooldown = 0.8f;
    public bool canAttack = true;
    public bool requireSettledBeforeAttack = true;
    public float maxWorldAttackDistance = 1.15f;
    public float contactWindup = 0f;

    private float timer;
    private float contactTimer;
    private GridOccupant occupant;
    private GridOccupant player;
    private GridMover mover;
    private GridMover playerMover;

    public float LastAttackTime { get; private set; } = float.NegativeInfinity;
    public Vector2Int LastAttackFacing { get; private set; } = Vector2Int.down;


    private void Awake()
    {
        occupant = GetComponent<GridOccupant>();
        mover = GetComponent<GridMover>();
    }

    private void Start()
    {
        CachePlayer();
    }

    private void Update()
    {
        if (player == null)
        {
            CachePlayer();
        }

        if (CutsceneFreezeManager.IsFrozen)
        {
            return;
        }

        if (timer > 0f)
        {
            timer -= Time.deltaTime;
        }

        if (!canAttack || player == null)
        {
            contactTimer = 0f;
            return;
        }

        if (requireSettledBeforeAttack && IsEitherMoverMoving())
        {
            contactTimer = 0f;
            return;
        }

        int distance = Mathf.Abs(player.CurrentCell.x - occupant.CurrentCell.x)
            + Mathf.Abs(player.CurrentCell.y - occupant.CurrentCell.y);

        bool hasContact = distance == 1 && IsVisuallyCloseEnough();
        if (!hasContact)
        {
            contactTimer = 0f;
            return;
        }

        contactTimer += Time.deltaTime;
        if (contactTimer < contactWindup || timer > 0f)
        {
            return;
        }

        PlayerHealth hp = player.GetComponent<PlayerHealth>();
        if (hp != null)
        {
            hp.TakeDamage(damage);
            timer = attackCooldown;
            contactTimer = 0f;
            LastAttackTime = Time.time;
            Vector2Int diff = player.CurrentCell - occupant.CurrentCell;
            LastAttackFacing = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y)
                ? (diff.x > 0 ? Vector2Int.right : Vector2Int.left)
                : (diff.y > 0 ? Vector2Int.up : Vector2Int.down);
        }
    }

    private void CachePlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<GridOccupant>();
            playerMover = playerObj.GetComponent<GridMover>();
        }
    }

    private bool IsEitherMoverMoving()
    {
        return (mover != null && mover.IsMoving)
            || (playerMover != null && playerMover.IsMoving);
    }

    private bool IsVisuallyCloseEnough()
    {
        if (maxWorldAttackDistance <= 0f || player == null)
        {
            return true;
        }

        float worldDistance = Vector2.Distance(transform.position, player.transform.position);
        return worldDistance <= maxWorldAttackDistance;
    }
}
