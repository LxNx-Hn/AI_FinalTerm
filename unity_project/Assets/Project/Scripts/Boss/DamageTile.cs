using System.Collections.Generic;
using UnityEngine;

public class DamageTile : MonoBehaviour
{
    public int damage = 1;
    public float lifeTime = 0.15f;

    /// <summary>
    /// true로 설정하면 SpriteRenderer(자식 포함)를 모두 비활성화한다.
    /// 판정·Collider·lifeTime은 유지된다.
    /// </summary>
    public bool hideVisual = false;

    private FootHitbox footHitbox;
    private Rect tileBounds;
    private readonly HashSet<PlayerHealth> damagedPlayers = new HashSet<PlayerHealth>();

    private void Awake()
    {
        footHitbox = FindObjectOfType<FootHitbox>();
    }

    private void Start()
    {
        Vector3 p = transform.position;
        tileBounds = new Rect(p.x - 0.5f, p.y - 0.5f, 1f, 1f);
        Destroy(gameObject, lifeTime);

        if (hideVisual)
        {
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
                sr.enabled = false;
        }
    }

    private void Update()
    {
        if (footHitbox == null)
        {
            return;
        }

        Vector3 foot = footHitbox.transform.position;
        if (!tileBounds.Contains(new Vector2(foot.x, foot.y)))
        {
            return;
        }

        PlayerHealth player = footHitbox.playerHealth;
        if (player == null || damagedPlayers.Contains(player))
        {
            return;
        }

        player.TakeDamage(damage);
        damagedPlayers.Add(player);
    }
}
