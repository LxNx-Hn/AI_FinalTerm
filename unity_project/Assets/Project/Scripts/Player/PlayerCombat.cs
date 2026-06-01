using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridOccupant))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Attack")]
    public int baseDamage = 1;
    public float attackCooldown = 0.5f;

    [Header("Visuals")]
    [Tooltip("Prefab spawned briefly on each attack cell so the player can see the swing range.")]
    public GameObject hitboxFlashPrefab;
    public float hitboxFlashDuration = 0.2f;

    [Header("Projectile Defense")]
    public float projectileCellRadius = 0.55f;

    [Header("Audio")]
    public AudioClip attackSfx;
    public float attackSfxVolume = 0.9f;

    
[Header("Debug")]
    public bool drawAttackGizmo = true;

    private float cooldownTimer;
    private GridOccupant occupant;
    private PlayerController controller;
    private BuffController buff;
    private PlayerHealth health;
    private List<Vector2Int> lastAttackCells = new List<Vector2Int>();
    private AudioSource audioSource;


    // Boss hit: boss has blocksMovement=false so it won't appear in the occupant dict.
    // Cache the boss controller and check BossCell directly.
    private BossHealth           cachedBossHealth;
    private ElevatorBossController cachedBossCtrl;
    private bool externalAttackRequested;

    public float LastAttackTime { get; private set; } = float.NegativeInfinity;
    public Vector2Int LastAttackFacing { get; private set; } = Vector2Int.down;
    public bool IsAttackReady => cooldownTimer <= 0f && (health == null || !health.IsDead);

    private void Awake()
    {
        occupant = GetComponent<GridOccupant>();
        controller = GetComponent<PlayerController>();
        buff = GetComponent<BuffController>();
        health = GetComponent<PlayerHealth>();
        audioSource = GetComponent<AudioSource>();

        if (GetComponent<AttackRangePreview>() == null)
        {
            gameObject.AddComponent<AttackRangePreview>();
        }
    }

    private void Start()
    {
        cachedBossHealth = FindFirstObjectByType<BossHealth>();
        if (cachedBossHealth != null)
            cachedBossCtrl = cachedBossHealth.GetComponent<ElevatorBossController>();
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            return;
        }

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        bool attackRequested = externalAttackRequested || Input.GetMouseButtonDown(0);
        externalAttackRequested = false;

        if (attackRequested && cooldownTimer <= 0f)
        {
            Attack();
            cooldownTimer = attackCooldown;
        }
    }

    public void RequestExternalAttack()
    {
        externalAttackRequested = true;
    }

    private void Attack()
    {
        if (GridManager.Instance == null || controller == null)
        {
            return;
        }

        LastAttackTime = Time.time;
        LastAttackFacing = controller.Facing;

        int damage = baseDamage;
        if (buff != null && buff.AttackBuffActive)
        {
            damage *= 2;
        }

        Vector2Int origin = occupant.CurrentCell;
        Vector2Int direction = controller.Facing;
        List<Vector2Int> cells = GetAttackCells(origin, direction);
        lastAttackCells = cells;

        if (hitboxFlashPrefab != null) StartCoroutine(FlashHitbox(cells));

        // Trigger visual-only attack range preview for both mouse and RL agent attacks
        GetComponent<AttackRangePreview>()?.ShowFlash();

        bool hitAny = false;
        var hitEnemies = new HashSet<EnemyHealth>();
        var cellSet = new HashSet<Vector2Int>(cells);

        if (DestroyProjectilesInCells(cellSet))
            hitAny = true;

        // 1차: occupant dict 기반 (정지 상태 적)
        foreach (Vector2Int cell in cells)
        {
            GridOccupant target = GridManager.Instance.GetOccupant(cell);
            if (target == null) continue;

            EnemyHealth enemy = target.GetComponent<EnemyHealth>();
            if (enemy != null) { enemy.TakeDamage(damage); hitAny = true; hitEnemies.Add(enemy); continue; }

            BossHealth bossViaOccupant = target.GetComponent<BossHealth>();
            if (bossViaOccupant != null) { bossViaOccupant.TakeDamage(damage); hitAny = true; }
        }

        // 2차: 스프라이트 바운드 기반
        // — 이동 보간 중이거나 스프라이트가 여러 타일을 차지하는 적을 모두 포착
        foreach (var enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            if (hitEnemies.Contains(enemy)) continue;

            bool hit = false;
            var sr = GetAttackBoundsRenderer(enemy);
            if (sr != null)
            {
                Bounds b = sr.bounds;
                float ts = GridManager.Instance.tileSize;
                Vector2 orig = GridManager.Instance.worldOrigin;
                int x0 = Mathf.RoundToInt((b.min.x - orig.x) / ts);
                int x1 = Mathf.RoundToInt((b.max.x - orig.x) / ts);
                int y0 = Mathf.RoundToInt((b.min.y - orig.y) / ts);
                int y1 = Mathf.RoundToInt((b.max.y - orig.y) / ts);
                for (int cx = x0; cx <= x1 && !hit; cx++)
                    for (int cy = y0; cy <= y1 && !hit; cy++)
                        if (cellSet.Contains(new Vector2Int(cx, cy)))
                            hit = true;
            }
            else
            {
                hit = cellSet.Contains(GridManager.Instance.WorldToCell(enemy.transform.position));
            }

            if (hit)
            {
                enemy.TakeDamage(damage);
                hitAny = true;
                hitEnemies.Add(enemy);
            }
        }

        // Boss is non-blocking (blocksMovement=false) so it's not in the occupant dict.
        // Check its tracked BossCell directly.
        if (cachedBossCtrl != null && cachedBossHealth != null)
        {
            if (cells.Contains(cachedBossCtrl.BossCell))
            {
                int bossHpBefore = cachedBossHealth.currentHp;
                cachedBossHealth.TakeDamage(damage);
                int bossDamageDelta = Mathf.Max(0, bossHpBefore - cachedBossHealth.currentHp);
                if (bossDamageDelta > 0)
                {
                    BossRLTargetAlignmentDiagnostics.RecordSuccessfulBossHit(
                        this,
                        cachedBossCtrl,
                        cachedBossHealth,
                        cells,
                        origin,
                        direction,
                        bossHpBefore,
                        cachedBossHealth.currentHp,
                        bossDamageDelta);
                }
                hitAny = true;
            }
        }

        if (hitAny && attackSfx != null)
        {
            if (audioSource != null)
                audioSource.PlayOneShot(attackSfx, attackSfxVolume);
            else
                AudioSource.PlayClipAtPoint(attackSfx, transform.position, attackSfxVolume);
        }
    }

    private static SpriteRenderer GetAttackBoundsRenderer(EnemyHealth enemy)
    {
        if (enemy == null)
        {
            return null;
        }

        SpriteRenderer direct = enemy.GetComponent<SpriteRenderer>();
        if (direct != null && direct.enabled)
        {
            return direct;
        }

        foreach (SpriteRenderer child in enemy.GetComponentsInChildren<SpriteRenderer>())
        {
            if (child != null && child.enabled)
            {
                return child;
            }
        }

        return direct;
    }

    public List<Vector2Int> GetAttackCells(Vector2Int origin, Vector2Int direction)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        Vector2Int side = new Vector2Int(-direction.y, direction.x);

        for (int forward = 1; forward <= 2; forward++)
        {
            for (int offset = -1; offset <= 1; offset++)
            {
                result.Add(origin + direction * forward + side * offset);
            }
        }

        return result;
    }

    private IEnumerator FlashHitbox(List<Vector2Int> cells)
    {
        if (GridManager.Instance == null) yield break;
        var spawned = new List<GameObject>();
        foreach (var cell in cells)
        {
            var go = Instantiate(hitboxFlashPrefab, GridManager.Instance.CellToWorld(cell), Quaternion.identity);
            spawned.Add(go);
        }
        yield return new WaitForSeconds(hitboxFlashDuration);
        foreach (var go in spawned) if (go) Destroy(go);
    }

    private bool DestroyProjectilesInCells(HashSet<Vector2Int> cellSet)
    {
        bool hitAny = false;
        foreach (var projectile in FindObjectsByType<RangedProjectile>(FindObjectsSortMode.None))
        {
            if (projectile == null || !projectile.CanBeDeflected)
                continue;

            if (!IsProjectileInCells(projectile, cellSet))
                continue;

            projectile.DestroyByPlayerAttack();
            hitAny = true;
        }

        return hitAny;
    }

    private bool IsProjectileInCells(RangedProjectile projectile, HashSet<Vector2Int> cellSet)
    {
        if (GridManager.Instance == null || projectile == null)
            return false;

        Vector2 projectilePosition = projectile.transform.position;
        foreach (Vector2Int cell in cellSet)
        {
            Vector2 cellPosition = GridManager.Instance.CellToWorld(cell);
            if (Vector2.Distance(projectilePosition, cellPosition) <= projectileCellRadius)
                return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawAttackGizmo || GridManager.Instance == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        foreach (Vector2Int cell in lastAttackCells)
        {
            Gizmos.DrawWireCube(GridManager.Instance.CellToWorld(cell), Vector3.one * 0.9f);
        }
    }

}
