using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridOccupant))]
public class RangedEnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    public int damage = 1;
    public float attackCooldown = 2.5f;
    public int minAttackRange = 2;
    public int maxAttackRange = 7;
    public bool canAttack = true;

    [Header("Pattern Tiles")]
    public GameObject warningTilePrefab;
    public GameObject damageTilePrefab;

    [Header("Visual Projectile")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 15f;
    public int projectileCount = 1;
    public int projectileLaneSpacing = 1;

    [Header("Timing")]
    public float warningTime = 0.8f;
    public float damageTime = 0.15f;
    public float sequenceDelay = 0.05f;

    private float timer;
    private bool attacking;
    private GridOccupant occupant;
    private GridOccupant player;

    public float LastAttackTime { get; private set; } = float.NegativeInfinity;
    public Vector2Int LastAttackFacing { get; private set; } = Vector2Int.down;

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
        if (player == null) CachePlayer();
        if (CutsceneFreezeManager.IsFrozen) return;
        if (timer > 0f) timer -= Time.deltaTime;
        if (!canAttack || player == null || attacking) return;
        if (timer > 0f) return;

        int dist = ManhattanDistance();
        if (dist < minAttackRange || dist > maxAttackRange) return;

        Vector2Int diff = player.CurrentCell - occupant.CurrentCell;
        Vector2Int facing = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y)
            ? new Vector2Int(diff.x > 0 ? 1 : -1, 0)
            : new Vector2Int(0, diff.y > 0 ? 1 : -1);

        LastAttackFacing = facing;
        LastAttackTime = Time.time;
        timer = attackCooldown;
        attacking = true;

        StartCoroutine(FireLineAttack(occupant.CurrentCell, facing));
    }

    private IEnumerator FireLineAttack(Vector2Int origin, Vector2Int dir)
    {
        List<List<Vector2Int>> lanes = BuildLanes(origin, dir);

        if (lanes.Count == 0)
        {
            attacking = false;
            yield break;
        }

        // 경고 타일: 전체 경로에 동시에 표시
        List<GameObject> warnings = new List<GameObject>();
        foreach (var lane in lanes)
        {
            foreach (var cell in lane)
            {
                if (warningTilePrefab == null) break;
                var go = Instantiate(warningTilePrefab, GridManager.Instance.CellToWorld(cell), Quaternion.identity);
                var pt = go.GetComponent<PatternTile>();
                if (pt != null) pt.lifeTime = warningTime;
                warnings.Add(go);
            }
        }

        yield return new WaitForSeconds(warningTime);

        foreach (var w in warnings)
            if (w != null) Destroy(w);

        // 시각적 투사체 (데미지=0, 실제 데미지는 DamageTile이 처리)
        if (projectilePrefab != null)
        {
            foreach (var lane in lanes)
            {
                if (lane.Count == 0)
                    continue;

                Vector3 spawnPos = GridManager.Instance.CellToWorld(lane[0] - dir);
                Vector3 stopPos  = GridManager.Instance.CellToWorld(lane[lane.Count - 1]);
                var proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
                var rp = proj.GetComponent<RangedProjectile>();
                if (rp != null)
                {
                    rp.speed = projectileSpeed;
                    rp.SetStopPosition(stopPos);
                    rp.Init(new Vector2(dir.x, dir.y), 0);
                }
            }
        }

        // 데미지 타일: 적에서 플레이어 방향으로 순서대로 날아옴
        int maxLaneLength = 0;
        foreach (var lane in lanes)
            maxLaneLength = Mathf.Max(maxLaneLength, lane.Count);

        for (int i = 0; i < maxLaneLength; i++)
        {
            foreach (var lane in lanes)
            {
                if (i >= lane.Count)
                    continue;

                if (damageTilePrefab != null)
                {
                    var go = Instantiate(damageTilePrefab, GridManager.Instance.CellToWorld(lane[i]), Quaternion.identity);
                    var dt = go.GetComponent<DamageTile>();
                    if (dt != null) { dt.damage = damage; dt.lifeTime = damageTime; }
                }
            }
            yield return new WaitForSeconds(sequenceDelay);
        }

        attacking = false;
    }

    private List<List<Vector2Int>> BuildLanes(Vector2Int origin, Vector2Int dir)
    {
        var lanes = new List<List<Vector2Int>>();
        int count = Mathf.Max(1, projectileCount);
        int spacing = Mathf.Max(1, projectileLaneSpacing);
        Vector2Int perpendicular = dir.x != 0 ? Vector2Int.up : Vector2Int.right;
        int centerOffset = count / 2;

        for (int i = 0; i < count; i++)
        {
            int offset = (i - centerOffset) * spacing;
            Vector2Int laneOrigin = origin + perpendicular * offset;
            List<Vector2Int> lane = BuildLine(laneOrigin, dir, aimAtPlayer: i == centerOffset);
            if (lane.Count > 0)
                lanes.Add(lane);
        }

        return lanes;
    }

    private List<Vector2Int> BuildLine(Vector2Int origin, Vector2Int dir, bool aimAtPlayer)
    {
        var cells = new List<Vector2Int>();

        // 좌우 발사: 플레이어의 실제 행(y)으로 조준 (대각선 위치도 맞게)
        if (aimAtPlayer && dir.y == 0 && player != null)
            origin = new Vector2Int(origin.x, player.CurrentCell.y);
        // 상하 발사: 플레이어의 실제 열(x)으로 조준
        else if (aimAtPlayer && dir.x == 0 && player != null)
            origin = new Vector2Int(player.CurrentCell.x, origin.y);

        Vector2Int cur = origin + dir;

        for (int i = 0; i < maxAttackRange; i++)
        {
            if (GridManager.Instance != null && !GridManager.Instance.IsWalkable(cur, ignoreOccupant: true))
                break;
            cells.Add(cur);
            cur += dir;
        }

        return cells;
    }

    private void CachePlayer()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.GetComponent<GridOccupant>();
    }

    private int ManhattanDistance()
    {
        if (player == null || occupant == null) return int.MaxValue;
        return Mathf.Abs(player.CurrentCell.x - occupant.CurrentCell.x)
             + Mathf.Abs(player.CurrentCell.y - occupant.CurrentCell.y);
    }
}
