using UnityEngine;

[RequireComponent(typeof(GridOccupant))]
public class GridItem : MonoBehaviour
{
    public ItemType itemType;
    public float attackBuffDuration = 8f;
    public float speedBuffDuration = 6f;
    [SerializeField] private float pickupDistance = 0.65f;

    private GridOccupant occupant;
    private GridOccupant player;
    private bool pickedUp;

    private void Awake()
    {
        occupant = GetComponent<GridOccupant>();
    }

    private void Start()
    {
        CachePlayer();
    }

    private void Update()
    {
        if (pickedUp)
        {
            return;
        }

        if (player == null)
        {
            CachePlayer();
        }

        if (player == null)
        {
            return;
        }

        if (occupant == null)
        {
            occupant = GetComponent<GridOccupant>();
        }

        bool sameCell = occupant != null && player.CurrentCell == occupant.CurrentCell;
        float pickupDistanceSq = pickupDistance * pickupDistance;
        bool closeEnough = (player.transform.position - transform.position).sqrMagnitude <= pickupDistanceSq;

        if (sameCell || closeEnough)
        {
            PickUp(player.gameObject);
        }
    }

    private void CachePlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<GridOccupant>();
        }
    }

    private void PickUp(GameObject playerObj)
    {
        if (pickedUp)
        {
            return;
        }

        pickedUp = true;

        if (itemType == ItemType.Heal)
        {
            PlayerHealth hp = playerObj.GetComponent<PlayerHealth>();
            if (hp != null)
            {
                hp.Heal(1);
            }
        }
        else if (itemType == ItemType.AttackBuff)
        {
            BuffController buff = playerObj.GetComponent<BuffController>();
            if (buff != null)
            {
                buff.ApplyAttackBuff(attackBuffDuration);
            }
        }
        else if (itemType == ItemType.SpeedBuff)
        {
            BuffController buff = playerObj.GetComponent<BuffController>();
            if (buff != null)
            {
                buff.ApplySpeedBuff(speedBuffDuration);
            }
        }
        else if (itemType == ItemType.Keycard && GameState.Instance != null)
        {
            GameState.Instance.hasKeycard = true;
        }

        if (occupant != null)
        {
            occupant.Release();
        }

        Destroy(gameObject);
    }
}
