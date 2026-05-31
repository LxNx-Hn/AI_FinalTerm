using UnityEngine;

public class RangedProjectile : MonoBehaviour
{
    [Header("Stats")]
    public float speed = 5f;
    public float maxLifetime = 4f;

    [Header("Fly Animation")]
    public Sprite[] flyFrames;
    public float flyFps = 10f;

    [Header("Impact Animation")]
    public Sprite[] impactFrames;
    public float impactFps = 12f;

    private Vector2 dir;
    private int damage;
    private SpriteRenderer sr;
    private bool exploding;
    private float animClock;
    private int frameIdx;
    private float lifetime;
    private PlayerHealth playerHealth;
    private Transform playerTransform;
    private Vector3 stopPos;
    private bool hasStopPos;
    private bool reflected;

    public bool CanBeDeflected => !exploding;

    public void SetStopPosition(Vector3 worldPos)
    {
        stopPos = worldPos;
        hasStopPos = true;
    }

    public void Init(Vector2 direction, int dmg)
    {
        dir = direction.normalized;
        damage = dmg;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        sr.flipX = false;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        if (flyFrames != null && flyFrames.Length > 0)
            sr.sprite = flyFrames[0];
    }

    public void DestroyByPlayerAttack()
    {
        if (exploding)
            return;

        StartImpact();
    }

    public void Reflect(Vector2 reflectedDirection, int reflectedDamage)
    {
        if (exploding)
            return;

        reflected = true;
        hasStopPos = false;
        lifetime = 0f;
        damage = Mathf.Max(1, reflectedDamage);
        playerHealth = null;
        playerTransform = null;
        Init(reflectedDirection, damage);
    }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerHealth = playerObj.GetComponent<PlayerHealth>();
            playerTransform = playerObj.transform;
        }
    }

    private void Update()
    {
        lifetime += Time.deltaTime;

        if (exploding)
        {
            UpdateImpact();
            return;
        }

        if (lifetime > maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        // 이동
        transform.position += (Vector3)(dir * speed * Time.deltaTime);

        // 마지막 타일 도달 시 폭발
        if (hasStopPos)
        {
            Vector2 toStop = (Vector2)stopPos - (Vector2)transform.position;
            if (Vector2.Dot(toStop, dir) <= 0f)
            {
                transform.position = stopPos;
                StartImpact();
                return;
            }
        }

        // 비행 애니메이션
        if (flyFrames != null && flyFrames.Length > 0)
        {
            animClock += Time.deltaTime;
            float interval = 1f / flyFps;
            while (animClock >= interval)
            {
                animClock -= interval;
                frameIdx = (frameIdx + 1) % flyFrames.Length;
            }
            sr.sprite = flyFrames[frameIdx];
        }

        // 플레이어 히트 판정
        if (!reflected && playerHealth != null && playerTransform != null)
        {
            if (Vector2.Distance(transform.position, playerTransform.position) < 0.55f)
            {
                playerHealth.TakeDamage(damage);
                StartImpact();
                return;
            }
        }

        if (reflected)
        {
            foreach (var enemy in FindObjectsOfType<EnemyHealth>())
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy)
                    continue;

                if (Vector2.Distance(transform.position, enemy.transform.position) < 0.6f)
                {
                    enemy.TakeDamage(damage);
                    StartImpact();
                    return;
                }
            }
        }

        // 벽 충돌 (현재 셀이 걷기 불가면 폭발)
        if (GridManager.Instance != null)
        {
            Vector2Int cell = GridManager.Instance.WorldToCell(transform.position);
            if (!GridManager.Instance.IsWalkable(cell, ignoreOccupant: true))
            {
                StartImpact();
            }
        }
    }

    private void StartImpact()
    {
        exploding = true;
        frameIdx = 0;
        animClock = 0f;
        sr.flipX = false;
        transform.rotation = Quaternion.identity;
        if (impactFrames != null && impactFrames.Length > 0)
            sr.sprite = impactFrames[0];
    }

    private void UpdateImpact()
    {
        if (impactFrames == null || impactFrames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }
        animClock += Time.deltaTime;
        float interval = 1f / impactFps;
        while (animClock >= interval)
        {
            animClock -= interval;
            frameIdx++;
            if (frameIdx >= impactFrames.Length)
            {
                Destroy(gameObject);
                return;
            }
            sr.sprite = impactFrames[frameIdx];
        }
    }
}
