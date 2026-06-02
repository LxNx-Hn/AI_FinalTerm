using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPatternCaster : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject warningTilePrefab;
    public GameObject damageTilePrefab;
    public GameObject safeTilePrefab;

    [Header("Origin")]
    public Transform patternOrigin;

    [Header("Timing")]
    public float defaultWarningTime = 0.55f;
    public float defaultDamageTime = 0.15f;
    [SerializeField] private float defaultWarningFillTime = 0.25f;

    [Header("Merged Warning Visual")]
    [SerializeField] private bool useMergedWarningVisual = true;
    [SerializeField] private string mergedWarningSortingLayerName = "Items";
    [SerializeField] private int mergedWarningSortingOrder = 20;
    [SerializeField] private int mergedWarningPixelsPerCell = 48;
    [SerializeField] private float mergedWarningFillAlpha = 0.42f;
    [SerializeField] private float mergedWarningBorderAlpha = 0.9f;
    [SerializeField] private float mergedWarningBorderWidth = 0.035f;
    [SerializeField] private bool useMergedDamageVisual = true;
    [SerializeField] private string mergedDamageSortingLayerName = "Hazard";
    [SerializeField] private int mergedDamageSortingOrder = 0;
    [SerializeField] private float mergedDamageFillAlpha = 0.78f;
    [SerializeField] private float mergedDamageBorderAlpha = 0.95f;

    [Header("Direct Player Damage")]
    [Tooltip("DamageTile 트리거가 플레이어를 놓치는 경우를 막기 위해, 피해 시간 동안 플레이어 셀이 타격 범위와 겹치면 직접 피해를 줍니다.")]
    public bool applyDirectPlayerDamage = true;

    [Tooltip("보스 패턴 1회 타격당 플레이어에게 줄 피해량입니다.")]
    public int directPlayerDamage = 1;

    private const int Min = -3;
    private const int Max = 3;

    /// <summary>셀 하나의 월드 크기 (항상 1f).</summary>
    public float CellSize => 1f;

    public Vector3 CellToWorld(Vector2Int cell)
    {
        Vector3 origin = patternOrigin != null ? patternOrigin.position : Vector3.zero;
        return origin + new Vector3(cell.x, cell.y, 0f);
    }

    public bool Inside(Vector2Int cell)
    {
        return cell.x >= Min && cell.x <= Max && cell.y >= Min && cell.y <= Max;
    }

    public Vector2Int ClampArenaCell(Vector2Int cell)
    {
        return new Vector2Int(
            Mathf.Clamp(cell.x, Min, Max),
            Mathf.Clamp(cell.y, Min, Max)
        );
    }

    public Vector2Int NormalizeDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
            return Vector2Int.down;

        // Diagonal movement is allowed for dash visuals.
        int x = direction.x == 0 ? 0 : direction.x > 0 ? 1 : -1;
        int y = direction.y == 0 ? 0 : direction.y > 0 ? 1 : -1;

        return new Vector2Int(x, y);
    }

    public Vector2Int GetDirectionalEdgeCell(Vector2Int start, Vector2Int direction)
    {
        direction = NormalizeDirection(direction);
        start = ClampArenaCell(start);

        int x = start.x;
        int y = start.y;

        while (true)
        {
            Vector2Int next = new Vector2Int(x + direction.x, y + direction.y);

            if (!Inside(next))
                break;

            x = next.x;
            y = next.y;
        }

        return new Vector2Int(x, y);
    }

    public Vector2Int GetPatternDashStart(List<Vector2Int> cells, Vector2Int direction)
    {
        return GetPatternDashExtreme(cells, direction, wantEnd: false);
    }

    public Vector2Int GetPatternDashEnd(List<Vector2Int> cells, Vector2Int direction)
    {
        return GetPatternDashExtreme(cells, direction, wantEnd: true);
    }

    private Vector2Int GetPatternDashExtreme(List<Vector2Int> cells, Vector2Int direction, bool wantEnd)
    {
        direction = NormalizeDirection(direction);
        List<Vector2Int> unique = UniqueInside(cells);

        if (unique.Count == 0)
            return Vector2Int.zero;

        Vector2 center = Vector2.zero;
        foreach (Vector2Int cell in unique)
            center += cell;
        center /= unique.Count;

        Vector2Int best = unique[0];
        int bestDot = Dot(best, direction);
        float bestTie = TieDistance(best, center, direction);

        for (int i = 1; i < unique.Count; i++)
        {
            Vector2Int cell = unique[i];
            int dot = Dot(cell, direction);
            float tie = TieDistance(cell, center, direction);

            bool better = wantEnd
                ? dot > bestDot || (dot == bestDot && tie < bestTie)
                : dot < bestDot || (dot == bestDot && tie < bestTie);

            if (better)
            {
                best = cell;
                bestDot = dot;
                bestTie = tie;
            }
        }

        return best;
    }

    private int Dot(Vector2Int a, Vector2Int b)
    {
        return a.x * b.x + a.y * b.y;
    }

    private float TieDistance(Vector2Int cell, Vector2 center, Vector2Int direction)
    {
        // Tie breaker: pick the cell closest to the center line of the pattern width.
        Vector2 dir = new Vector2(direction.x, direction.y).normalized;
        Vector2 side = new Vector2(-dir.y, dir.x);
        Vector2 delta = new Vector2(cell.x, cell.y) - center;
        return Mathf.Abs(Vector2.Dot(delta, side));
    }

    public IEnumerator CastCells(List<Vector2Int> cells, float warningTime = -1f, float damageTime = -1f)
    {
        yield return CastCellsWithBeforeDamage(cells, warningTime, damageTime, null);
    }

    public IEnumerator CastCellsWithBeforeDamage(
        List<Vector2Int> cells,
        float warningTime = -1f,
        float damageTime = -1f,
        Action beforeDamage = null,
        Vector2Int? fillOriginCell = null,
        bool hideDamageTileVisual = false,
        bool animateWarningFill = true,
        Vector2Int? fillDirection = null,
        string damageSource = "Pattern",
        string damageSourceGroup = "pattern"
    )
    {
        if (warningTime < 0f)
            warningTime = defaultWarningTime;

        if (damageTime < 0f)
            damageTime = defaultDamageTime;

        cells = UniqueInside(cells);

        List<GameObject> warnings = SpawnWarningVisuals(cells, warningTime, fillOriginCell, animateWarningFill, fillDirection);

        yield return new WaitForSeconds(warningTime);

        foreach (GameObject warning in warnings)
        {
            if (warning != null)
                Destroy(warning);
        }

        beforeDamage?.Invoke();

        bool hideIndividualDamageVisual = hideDamageTileVisual;
        if (!hideDamageTileVisual && useMergedDamageVisual)
        {
            SpawnMergedDamageVisual(cells, damageTime);
            hideIndividualDamageVisual = true;
        }

        foreach (Vector2Int cell in cells)
        {
            if (damageTilePrefab == null)
                continue;

            GameObject damage = Instantiate(
                damageTilePrefab,
                CellToWorld(cell),
                Quaternion.identity
            );

            DamageTile damageTile = damage.GetComponent<DamageTile>();
            if (damageTile != null)
            {
                damageTile.lifeTime = damageTime;
                damageTile.hideVisual = hideIndividualDamageVisual;
                damageTile.damageSource = damageSource;
                damageTile.damageSourceGroup = damageSourceGroup;
            }
        }

        yield return MonitorDirectPlayerDamageDuringDamageTime(cells, damageTime, damageSource, damageSourceGroup);
    }

    /// <summary>
    /// LandingSlam 전용: WarningTile만 생성하고 warningTime 후 제거한다.
    /// DamageTile은 생성하지 않는다.
    /// </summary>
    public IEnumerator CastWarningOnly(
        List<Vector2Int> cells,
        float warningTime = -1f,
        Vector2Int? fillOriginCell = null,
        bool animateWarningFill = true,
        Vector2Int? fillDirection = null
    )
    {
        if (warningTime < 0f)
            warningTime = defaultWarningTime;

        cells = UniqueInside(cells);

        List<GameObject> warnings = SpawnWarningVisuals(cells, warningTime, fillOriginCell, animateWarningFill, fillDirection);

        yield return new WaitForSeconds(warningTime);

        foreach (GameObject warning in warnings)
        {
            if (warning != null)
                Destroy(warning);
        }
    }

    /// <summary>
    /// LandingSlam 착지 순간 전용: DamageTile만 즉시 생성한다.
    /// MonitorDirectPlayerDamage도 시작한다.
    /// </summary>
    public IEnumerator SpawnDamageCells(
        List<Vector2Int> cells,
        float damageTime = -1f,
        bool hideVisual = false,
        string damageSource = "SpawnDamageCells",
        string damageSourceGroup = "pattern"
    )
    {
        if (damageTime < 0f)
            damageTime = defaultDamageTime;

        cells = UniqueInside(cells);

        foreach (Vector2Int cell in cells)
        {
            if (damageTilePrefab == null)
                continue;

            GameObject damage = Instantiate(
                damageTilePrefab,
                CellToWorld(cell),
                Quaternion.identity
            );

            DamageTile damageTile = damage.GetComponent<DamageTile>();
            if (damageTile != null)
            {
                damageTile.lifeTime = damageTime;
                damageTile.hideVisual = hideVisual;
                damageTile.damageSource = damageSource;
                damageTile.damageSourceGroup = damageSourceGroup;
            }
        }

        yield return MonitorDirectPlayerDamageDuringDamageTime(cells, damageTime, damageSource, damageSourceGroup);
    }

    // 8방향 이웃 벡터 (외곽 판정용)
    private static readonly Vector2Int[] Dirs8 =
    {
        new Vector2Int( 1,  0), new Vector2Int(-1,  0),
        new Vector2Int( 0,  1), new Vector2Int( 0, -1),
        new Vector2Int( 1,  1), new Vector2Int( 1, -1),
        new Vector2Int(-1,  1), new Vector2Int(-1, -1)
    };

    private void ApplyFillAnimation(
        List<Vector2Int> cells,
        List<GameObject> warningGOs,
        float warningTime,
        Vector2Int origin
    )
    {
        var cellSet = new HashSet<Vector2Int>(cells);

        float maxDist = 0f;
        foreach (var c in cells)
            maxDist = Mathf.Max(maxDist,
                Mathf.Sqrt((c.x - origin.x) * (c.x - origin.x) +
                           (c.y - origin.y) * (c.y - origin.y)));
        maxDist = Mathf.Max(maxDist, 0.01f);

        Color innerColor = new Color(1f,    0.35f, 0.35f, 0.55f);
        Color outerColor = new Color(0.85f, 0.05f, 0.05f, 0.90f);

        for (int i = 0; i < cells.Count && i < warningGOs.Count; i++)
        {
            Vector2Int c = cells[i];
            float dist  = Mathf.Sqrt((c.x - origin.x) * (c.x - origin.x) +
                                     (c.y - origin.y) * (c.y - origin.y));
            float ratio = dist / maxDist;
            float delay = ratio * warningTime * 0.6f;

            bool isOuter = false;
            foreach (var d in Dirs8)
                if (!cellSet.Contains(c + d)) { isOuter = true; break; }

            Color finalColor = isOuter
                ? outerColor
                : Color.Lerp(innerColor, outerColor, ratio * 0.5f);

            var go = warningGOs[i];
            if (go != null)
                go.GetComponent<PatternTile>()?.InitFill(delay, warningTime, finalColor);
        }
    }

    private List<GameObject> SpawnWarningVisuals(
        List<Vector2Int> cells,
        float warningTime,
        Vector2Int? fillOriginCell,
        bool animateWarningFill,
        Vector2Int? fillDirection
    )
    {
        List<GameObject> warnings = new List<GameObject>();

        if (useMergedWarningVisual)
        {
            var visual = MergedPatternWarningVisual.Spawn(
                cells,
                patternOrigin != null ? patternOrigin.position : Vector3.zero,
                warningTime,
                fillOriginCell,
                mergedWarningSortingLayerName,
                mergedWarningSortingOrder,
                mergedWarningPixelsPerCell,
                mergedWarningFillAlpha,
                mergedWarningBorderAlpha,
                mergedWarningBorderWidth,
                GetWarningFillTime(warningTime),
                animateWarningFill,
                fillDirection
            );

            if (visual != null)
                warnings.Add(visual.gameObject);

            return warnings;
        }

        foreach (Vector2Int cell in cells)
        {
            if (warningTilePrefab == null)
                continue;

            GameObject warning = Instantiate(
                warningTilePrefab,
                CellToWorld(cell),
                Quaternion.identity
            );

            PatternTile patternTile = warning.GetComponent<PatternTile>();
            if (patternTile != null)
                patternTile.lifeTime = warningTime;

            warnings.Add(warning);
        }

        if (fillOriginCell.HasValue)
            ApplyFillAnimation(cells, warnings, warningTime, fillOriginCell.Value);

        return warnings;
    }

    private void SpawnMergedDamageVisual(List<Vector2Int> cells, float damageTime)
    {
        MergedPatternWarningVisual.Spawn(
            cells,
            patternOrigin != null ? patternOrigin.position : Vector3.zero,
            damageTime,
            null,
            mergedDamageSortingLayerName,
            mergedDamageSortingOrder,
            mergedWarningPixelsPerCell,
            mergedDamageFillAlpha,
            mergedDamageBorderAlpha,
            mergedWarningBorderWidth,
            damageTime,
            animateFill: false,
            fillDirection: null
        );
    }

    private float GetWarningFillTime(float warningTime)
    {
        return Mathf.Clamp(defaultWarningFillTime, 0.05f, Mathf.Max(0.05f, warningTime));
    }

    public IEnumerator CastDamageOnly(
        List<Vector2Int> cells,
        float damageTime = -1f,
        bool hideDamageTileVisual = false,
        string damageSource = "DamageOnly",
        string damageSourceGroup = "pattern"
    )
    {
        if (damageTime < 0f)
            damageTime = defaultDamageTime;

        cells = UniqueInside(cells);

        foreach (Vector2Int cell in cells)
        {
            if (damageTilePrefab == null)
                continue;

            GameObject damage = Instantiate(
                damageTilePrefab,
                CellToWorld(cell),
                Quaternion.identity
            );

            DamageTile damageTile = damage.GetComponent<DamageTile>();
            if (damageTile != null)
            {
                damageTile.lifeTime = damageTime;
                damageTile.hideVisual = hideDamageTileVisual;
                damageTile.damageSource = damageSource;
                damageTile.damageSourceGroup = damageSourceGroup;
            }
        }

        yield return MonitorDirectPlayerDamageDuringDamageTime(cells, damageTime, damageSource, damageSourceGroup);
    }

    private IEnumerator MonitorDirectPlayerDamageDuringDamageTime(
        List<Vector2Int> arenaCells,
        float damageTime,
        string damageSource,
        string damageSourceGroup
    )
    {
        if (!applyDirectPlayerDamage)
        {
            yield return new WaitForSeconds(damageTime);
            yield break;
        }

        float elapsed = 0f;
        bool damaged = false;

        while (elapsed < damageTime)
        {
            if (!damaged && IsPlayerInArenaCells(arenaCells))
            {
                DamagePlayerDirectly(damageSource, damageSourceGroup);
                damaged = true;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private bool IsPlayerInArenaCells(List<Vector2Int> arenaCells)
    {
        if (arenaCells == null || arenaCells.Count == 0)
            return false;

        if (GridManager.Instance == null || patternOrigin == null)
            return false;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return false;

        GridOccupant playerOccupant = player.GetComponent<GridOccupant>();
        if (playerOccupant == null)
            return false;

        Vector2Int originCell = GridManager.Instance.WorldToCell(patternOrigin.position);
        Vector2Int playerArenaCell = playerOccupant.CurrentCell - originCell;

        return arenaCells.Contains(playerArenaCell);
    }

    private void DamagePlayerDirectly(string damageSource, string damageSourceGroup)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.TakeDamage(directPlayerDamage, damageSource, damageSourceGroup);
    }

    public List<Vector2Int> NormalScratch3(Vector2Int center)
    {
        return Rect(center, 1, 1);
    }

    public List<Vector2Int> NormalScratchDirectional(Vector2Int center, Vector2Int facing)
    {
        NormalizeFacing(ref facing);

        Vector2Int side = new Vector2Int(-facing.y, facing.x);

        List<Vector2Int> cells = new List<Vector2Int>();

        for (int forward = 0; forward <= 2; forward++)
        {
            for (int s = -1; s <= 1; s++)
            {
                cells.Add(center + facing * forward + side * s);
            }
        }

        return UniqueInside(cells);
    }

    public List<Vector2Int> EnhancedScratchDirectional(Vector2Int center, Vector2Int facing)
    {
        NormalizeFacing(ref facing);

        Vector2Int side = new Vector2Int(-facing.y, facing.x);

        List<Vector2Int> cells = new List<Vector2Int>();

        for (int forward = 0; forward <= 2; forward++)
        {
            int sideHalf = forward == 2 ? 1 : 2;

            for (int s = -sideHalf; s <= sideHalf; s++)
            {
                cells.Add(center + facing * forward + side * s);
            }
        }

        return UniqueInside(cells);
    }

    public List<Vector2Int> HollowCorner5x5(Vector2Int center)
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                if (Mathf.Abs(dx) == 2 && Mathf.Abs(dy) == 2)
                    continue;

                cells.Add(center + new Vector2Int(dx, dy));
            }
        }

        return UniqueInside(cells);
    }

    public List<Vector2Int> EnhancedScratch5(Vector2Int center)
    {
        return HollowCorner5x5(center);
    }

    public List<Vector2Int> Rect(Vector2Int center, int halfX, int halfY)
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int dx = -halfX; dx <= halfX; dx++)
        {
            for (int dy = -halfY; dy <= halfY; dy++)
            {
                cells.Add(center + new Vector2Int(dx, dy));
            }
        }

        return UniqueInside(cells);
    }

    public List<Vector2Int> HorizontalStripe3(int centerY)
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int dy = -1; dy <= 1; dy++)
        {
            int y = centerY + dy;

            for (int x = Min; x <= Max; x++)
            {
                cells.Add(new Vector2Int(x, y));
            }
        }

        return UniqueInside(cells);
    }

    public List<Vector2Int> VerticalStripe3(int centerX)
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int dx = -1; dx <= 1; dx++)
        {
            int x = centerX + dx;

            for (int y = Min; y <= Max; y++)
            {
                cells.Add(new Vector2Int(x, y));
            }
        }

        return UniqueInside(cells);
    }

    public List<Vector2Int> ForwardStripe3FromCell(Vector2Int start, Vector2Int facing)
    {
        NormalizeFacing(ref facing);

        List<Vector2Int> cells = new List<Vector2Int>();

        if (facing.x != 0)
        {
            int step = facing.x > 0 ? 1 : -1;

            for (int x = start.x; x >= Min && x <= Max; x += step)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    cells.Add(new Vector2Int(x, start.y + dy));
                }
            }
        }
        else
        {
            int step = facing.y > 0 ? 1 : -1;

            for (int y = start.y; y >= Min && y <= Max; y += step)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    cells.Add(new Vector2Int(start.x + dx, y));
                }
            }
        }

        return UniqueInside(cells);
    }

    public List<Vector2Int> MarkHorizontal3(Vector2Int center)
    {
        List<Vector2Int> cells = new List<Vector2Int>
        {
            new Vector2Int(center.x - 1, center.y),
            new Vector2Int(center.x, center.y),
            new Vector2Int(center.x + 1, center.y)
        };

        return UniqueInside(cells);
    }

    public List<Vector2Int> MarkVertical3(Vector2Int center)
    {
        List<Vector2Int> cells = new List<Vector2Int>
        {
            new Vector2Int(center.x, center.y + 1),
            new Vector2Int(center.x, center.y),
            new Vector2Int(center.x, center.y - 1)
        };

        return UniqueInside(cells);
    }

    public List<Vector2Int> MarkHorizontal5(Vector2Int center)
    {
        List<Vector2Int> cells = new List<Vector2Int>
        {
            new Vector2Int(center.x - 2, center.y),
            new Vector2Int(center.x - 1, center.y),
            new Vector2Int(center.x,     center.y),
            new Vector2Int(center.x + 1, center.y),
            new Vector2Int(center.x + 2, center.y)
        };

        return UniqueInside(cells);
    }

    public List<Vector2Int> MarkVertical5(Vector2Int center)
    {
        List<Vector2Int> cells = new List<Vector2Int>
        {
            new Vector2Int(center.x, center.y + 2),
            new Vector2Int(center.x, center.y + 1),
            new Vector2Int(center.x, center.y),
            new Vector2Int(center.x, center.y - 1),
            new Vector2Int(center.x, center.y - 2)
        };

        return UniqueInside(cells);
    }

    public List<Vector2Int> Diagonal_TR_BL_3()
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int x = Min; x <= Max; x++)
        {
            for (int y = Min; y <= Max; y++)
            {
                if (Mathf.Abs(x - y) <= 1)
                    cells.Add(new Vector2Int(x, y));
            }
        }

        return UniqueInside(cells);
    }

    public List<Vector2Int> Diagonal_TL_BR_3()
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int x = Min; x <= Max; x++)
        {
            for (int y = Min; y <= Max; y++)
            {
                if (Mathf.Abs(x + y) <= 1)
                    cells.Add(new Vector2Int(x, y));
            }
        }

        return UniqueInside(cells);
    }

    public List<Vector2Int> DiagonalTLBR3()
    {
        return Diagonal_TL_BR_3();
    }

    public List<Vector2Int> DiagonalTRBL3()
    {
        return Diagonal_TR_BL_3();
    }

    public List<Vector2Int> LeftBand4()
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int x = Min; x <= 0; x++)
        {
            for (int y = Min; y <= Max; y++)
            {
                cells.Add(new Vector2Int(x, y));
            }
        }

        return UniqueInside(cells);
    }

    public List<Vector2Int> BottomBand4()
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int x = Min; x <= Max; x++)
        {
            for (int y = Min; y <= 0; y++)
            {
                cells.Add(new Vector2Int(x, y));
            }
        }

        return UniqueInside(cells);
    }

    public List<Vector2Int> RightBand4()
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int x = 0; x <= Max; x++)
        {
            for (int y = Min; y <= Max; y++)
            {
                cells.Add(new Vector2Int(x, y));
            }
        }

        return UniqueInside(cells);
    }

    public List<Vector2Int> TopBand4()
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int x = Min; x <= Max; x++)
        {
            for (int y = 0; y <= Max; y++)
            {
                cells.Add(new Vector2Int(x, y));
            }
        }

        return UniqueInside(cells);
    }

    public List<Vector2Int> LeftEdge2()
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int x = Min; x <= Min + 1; x++)
        {
            for (int y = Min; y <= Max; y++)
            {
                cells.Add(new Vector2Int(x, y));
            }
        }

        return UniqueInside(cells);
    }

    public List<Vector2Int> TopEdge2()
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int x = Min; x <= Max; x++)
        {
            for (int y = Max - 1; y <= Max; y++)
            {
                cells.Add(new Vector2Int(x, y));
            }
        }

        return UniqueInside(cells);
    }

    public List<Vector2Int> RightEdge2()
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int x = Max - 1; x <= Max; x++)
        {
            for (int y = Min; y <= Max; y++)
            {
                cells.Add(new Vector2Int(x, y));
            }
        }

        return UniqueInside(cells);
    }

    public List<Vector2Int> BottomEdge2()
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int x = Min; x <= Max; x++)
        {
            for (int y = Min; y <= Min + 1; y++)
            {
                cells.Add(new Vector2Int(x, y));
            }
        }

        return UniqueInside(cells);
    }

    public List<Vector2Int> NPattern()
    {
        HashSet<Vector2Int> set = new HashSet<Vector2Int>();

        foreach (Vector2Int cell in LeftEdge2())
            set.Add(cell);

        foreach (Vector2Int cell in Diagonal_TR_BL_3())
            set.Add(cell);

        foreach (Vector2Int cell in RightEdge2())
            set.Add(cell);

        return new List<Vector2Int>(set);
    }

    public List<Vector2Int> ZPattern()
    {
        HashSet<Vector2Int> set = new HashSet<Vector2Int>();

        foreach (Vector2Int cell in TopEdge2())
            set.Add(cell);

        foreach (Vector2Int cell in Diagonal_TR_BL_3())
            set.Add(cell);

        foreach (Vector2Int cell in BottomEdge2())
            set.Add(cell);

        return new List<Vector2Int>(set);
    }

    private void NormalizeFacing(ref Vector2Int facing)
    {
        if (facing == Vector2Int.zero)
        {
            facing = Vector2Int.down;
            return;
        }

        if (Mathf.Abs(facing.x) >= Mathf.Abs(facing.y))
        {
            facing = new Vector2Int(facing.x > 0 ? 1 : -1, 0);
        }
        else
        {
            facing = new Vector2Int(0, facing.y > 0 ? 1 : -1);
        }
    }

    private List<Vector2Int> UniqueInside(List<Vector2Int> cells)
    {
        HashSet<Vector2Int> set = new HashSet<Vector2Int>();

        foreach (Vector2Int cell in cells)
        {
            if (Inside(cell))
                set.Add(cell);
        }

        return new List<Vector2Int>(set);
    }
}
