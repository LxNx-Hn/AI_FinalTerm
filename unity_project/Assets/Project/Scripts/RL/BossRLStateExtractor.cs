using System.Collections.Generic;
using System.Reflection;
using Unity.MLAgents.Sensors;
using UnityEngine;

[DisallowMultipleComponent]
public class BossRLStateExtractor : MonoBehaviour
{
    public const int ArenaHalfExtent = 3;
    public const int ArenaSize = ArenaHalfExtent * 2 + 1;
    public const int MaskObservationSize = ArenaSize * ArenaSize;  // 49

    // Observation breakdown (193 total):
    //   Base 163: player_pos(2) + facing(4) + hp×2(2) + boss_vis+pos(3)
    //             + warningMask(49) + damageMask(49) + ready×2+elapsed(3)
    //             + prevWarningMask(49) + bossInRange+manhattan(2)
    //   New  30:  recent_danger(2) + directional 4×7(28)
    public const int VectorObservationSize = 193;
    private static readonly float[] ZeroObservations = new float[VectorObservationSize];

    private static readonly FieldInfo MergedWarningCellsField =
        typeof(MergedPatternWarningVisual).GetField("cells", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly Vector2Int[] CardinalDirs =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    // ── Current hazard state ──────────────────────────────────────────────────
    private readonly HashSet<Vector2Int> warningCells = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> damageCells  = new HashSet<Vector2Int>();
    private readonly float[] warningMask = new float[MaskObservationSize];
    private readonly float[] damageMask  = new float[MaskObservationSize];
    private readonly float[] prevWarningMask = new float[MaskObservationSize];

    // ── Recent danger memory (Time.time TTL-based) ────────────────────────────
    // warning TTL 0.65s: covers full warning→damage window so agent remembers danger zone
    // damage TTL 0.30s: brief memory to avoid stepping back in immediately
    private const float RecentWarningTTL = 0.65f;
    private const float RecentDamageTTL  = 0.30f;
    private readonly Dictionary<Vector2Int, float> recentWarningExpireTime = new Dictionary<Vector2Int, float>();
    private readonly Dictionary<Vector2Int, float> recentDamageExpireTime  = new Dictionary<Vector2Int, float>();
    private readonly HashSet<Vector2Int> recentWarningCells = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> recentDamageCells  = new HashSet<Vector2Int>();
    // Frame guard to avoid double-updating in the same frame
    private int lastRecentDangerFrame = -1;

    // ── References ───────────────────────────────────────────────────────────
    private GridManager gridManager;
    private GridOccupant playerOccupant;
    private GridMover playerMover;
    private PlayerController playerController;
    private PlayerCombat playerCombat;
    private PlayerHealth playerHealth;
    private BossHealth bossHealth;
    private ElevatorBossController bossController;
    private BossPatternCaster patternCaster;
    private Renderer[] bossRenderers;
    private BossRLDebugLogger debugLogger;

    public bool TryInitialize()
    {
        gridManager      = GridManager.Instance;
        playerOccupant   = GetComponent<GridOccupant>();
        playerMover      = GetComponent<GridMover>();
        playerController = GetComponent<PlayerController>();
        playerCombat     = GetComponent<PlayerCombat>();
        playerHealth     = GetComponent<PlayerHealth>();

        bossController = FindFirstObjectByType<ElevatorBossController>();
        bossHealth     = bossController != null ? bossController.GetComponent<BossHealth>() : FindFirstObjectByType<BossHealth>();
        patternCaster  = bossController != null ? bossController.GetComponent<BossPatternCaster>() : FindFirstObjectByType<BossPatternCaster>();
        bossRenderers  = bossController != null ? bossController.GetComponentsInChildren<Renderer>(true) : null;
        debugLogger    = GetComponent<BossRLDebugLogger>();

        return HasCoreReferences();
    }

    // ── Public properties ─────────────────────────────────────────────────────
    public bool IsReady => EnsureReady(logWarning: false);
    public int  PlayerCurrentHp => playerHealth != null ? playerHealth.currentHp : 0;
    public int  PlayerMaxHp     => playerHealth != null ? Mathf.Max(1, playerHealth.maxHp) : 1;
    public bool PlayerIsDead    => playerHealth != null && playerHealth.IsDead;
    public int  BossCurrentHp   => bossHealth != null ? bossHealth.currentHp : 0;
    public int  BossMaxHp       => bossHealth != null ? Mathf.Max(1, bossHealth.maxHp) : 1;
    public bool BossIsDead      => bossHealth != null && bossHealth.currentHp <= 0;
    public bool MoveReady              => playerMover != null && !playerMover.IsMoving && !PlayerIsDead;
    public bool AttackReady            => playerCombat != null && playerCombat.IsAttackReady;
    public bool IsPlayerMoving         => playerMover != null && playerMover.IsMoving;
    public int  RecentWarningCellCount => recentWarningCells.Count;
    public int  RecentDamageCellCount  => recentDamageCells.Count;

    public void ResetTemporalState()
    {
        System.Array.Clear(prevWarningMask, 0, prevWarningMask.Length);
        recentWarningExpireTime.Clear();
        recentDamageExpireTime.Clear();
        recentWarningCells.Clear();
        recentDamageCells.Clear();
        lastRecentDangerFrame = -1;
    }

    // ── Observation collection ────────────────────────────────────────────────
    public void AppendObservations(VectorSensor sensor, float elapsedNormalized)
    {
        if (!EnsureReady(logWarning: true))
        {
            sensor.AddObservation(ZeroObservations);
            return;
        }

        Vector2Int playerArenaCell = GetPlayerArenaCell();
        Vector2Int bossArenaCell   = GetBossArenaCell();
        bool bossVisible           = IsBossVisible();

        // ── Base 163 (unchanged structure) ────────────────────────────────────
        sensor.AddObservation(NormalizeArenaCoord(playerArenaCell.x));  // 1
        sensor.AddObservation(NormalizeArenaCoord(playerArenaCell.y));  // 2
        AddFacingObservation(sensor, playerController != null ? playerController.Facing : Vector2Int.down); // 4 → 6
        sensor.AddObservation((float)PlayerCurrentHp / PlayerMaxHp);    // 1 → 7
        sensor.AddObservation((float)BossCurrentHp   / BossMaxHp);      // 1 → 8
        sensor.AddObservation(bossVisible);                              // 1 → 9
        sensor.AddObservation(bossVisible ? NormalizeArenaCoord(bossArenaCell.x) : 0f); // 1 → 10
        sensor.AddObservation(bossVisible ? NormalizeArenaCoord(bossArenaCell.y) : 0f); // 1 → 11

        System.Array.Copy(warningMask, prevWarningMask, MaskObservationSize);
        RefreshHazardMasks();
        TryUpdateRecentDanger();

        sensor.AddObservation(warningMask);   // 49 → 60
        sensor.AddObservation(damageMask);    // 49 → 109
        sensor.AddObservation(AttackReady);   // 1  → 110
        sensor.AddObservation(MoveReady);     // 1  → 111
        sensor.AddObservation(Mathf.Clamp01(elapsedNormalized)); // 1 → 112

        sensor.AddObservation(prevWarningMask); // 49 → 161

        bool bossInRange = IsBossInAttackRangeInternal(playerArenaCell);
        sensor.AddObservation(bossInRange);   // 1 → 162
        float manhattanNorm = GetManhattanDistanceToBossNormalized(playerArenaCell, bossArenaCell);
        sensor.AddObservation(manhattanNorm); // 1 → 163

        // ── Recent danger (+2 = 165) ──────────────────────────────────────────
        sensor.AddObservation(recentWarningCells.Contains(playerArenaCell)); // 164
        sensor.AddObservation(recentDamageCells.Contains(playerArenaCell));  // 165

        // ── Directional observations 4×7 (+28 = 193) ─────────────────────────
        AddDirectionalObservations(sensor, playerArenaCell, bossArenaCell);
    }

    // ── Public hazard / masking helpers ───────────────────────────────────────

    public void GetHazardState(out bool onWarning, out bool onDamage)
    {
        // Use cached hazard state — AppendObservations already calls RefreshHazardMasks each step.
        // Do NOT call RefreshHazardMasks here; calling it from WriteDiscreteActionMask would
        // overwrite warningMask BEFORE AppendObservations copies it to prevWarningMask,
        // breaking the temporal observation AND doubling expensive FindObjectsByType calls.
        if (!EnsureReady(logWarning: false)) { onWarning = onDamage = false; return; }
        Vector2Int p = GetPlayerArenaCell();
        onWarning = warningCells.Contains(p);
        onDamage  = damageCells.Contains(p);
    }

    public void GetFullDangerState(out bool onWarning, out bool onDamage,
                                   out bool onRecentWarning, out bool onRecentDamage)
    {
        if (!EnsureReady(logWarning: false))
        {
            onWarning = onDamage = onRecentWarning = onRecentDamage = false;
            return;
        }
        RefreshHazardMasks();
        TryUpdateRecentDanger();
        Vector2Int p = GetPlayerArenaCell();
        onWarning       = warningCells.Contains(p);
        onDamage        = damageCells.Contains(p);
        onRecentWarning = recentWarningCells.Contains(p);
        onRecentDamage  = recentDamageCells.Contains(p);
    }

    public bool IsPlayerStandingOnWarningTile()
    {
        if (!EnsureReady(logWarning: false)) return false;
        RefreshHazardMasks();
        return warningCells.Contains(GetPlayerArenaCell());
    }

    public bool IsPlayerStandingOnDamageTile()
    {
        if (!EnsureReady(logWarning: false)) return false;
        RefreshHazardMasks();
        return damageCells.Contains(GetPlayerArenaCell());
    }

    // ── Direction-based helpers for action masking ────────────────────────────

    /// <summary>True when moving in dir is NOT wall-blocked (player not busy).</summary>
    public bool CanMoveInDirection(Vector2Int dir)
    {
        if (playerMover == null) return false;
        if (playerMover.IsMoving) return false;
        return playerMover.CanMove(dir);
    }

    /// <summary>Geometry-only wall check for action masking.
    /// Does NOT use IsMoving (busy ≠ wall). Boss cell is always passable.
    /// Use this for movement hard-mask; never use CanMoveInDirection for wall detection.</summary>
    public bool IsGeometryBlockedDirection(Vector2Int dir)
    {
        if (!EnsureReady(logWarning: false) || playerOccupant == null || GridManager.Instance == null)
            return true;

        // Boss cell is not a wall – player can attempt to move into it
        if (IsNextCellBossCell(dir))
            return false;

        Vector2Int next = playerOccupant.CurrentCell + dir;
        return !GridManager.Instance.IsWalkable(next, ignoreOccupant: true);
    }

    public bool IsNextCellWarning(Vector2Int dir)
    {
        if (!EnsureReady(logWarning: false)) return false;
        return warningCells.Contains(GetPlayerArenaCell() + dir);
    }

    public bool IsNextCellDamage(Vector2Int dir)
    {
        if (!EnsureReady(logWarning: false)) return false;
        return damageCells.Contains(GetPlayerArenaCell() + dir);
    }

    public bool IsNextCellRecentWarning(Vector2Int dir)
    {
        if (!EnsureReady(logWarning: false)) return false;
        return recentWarningCells.Contains(GetPlayerArenaCell() + dir);
    }

    public bool IsNextCellRecentDamage(Vector2Int dir)
    {
        if (!EnsureReady(logWarning: false)) return false;
        return recentDamageCells.Contains(GetPlayerArenaCell() + dir);
    }

    /// <summary>True when the next cell in dir is the boss's current world cell.</summary>
    public bool IsNextCellBossCell(Vector2Int dir)
    {
        if (!EnsureReady(logWarning: false) || playerOccupant == null || bossController == null) return false;
        Vector2Int nextWorldCell = playerOccupant.CurrentCell + dir;
        return nextWorldCell == bossController.BossCell;
    }

    // ── Opportunity / danger diagnostic helpers ───────────────────────────────

    /// <summary>True when attack is ready, boss is in range, and player is on no danger tile (including recent).</summary>
    public bool IsSafeAttackOpportunity()
    {
        if (!EnsureReady(logWarning: false)) return false;
        if (!AttackReady || !IsBossInAttackRange()) return false;
        Vector2Int p = GetPlayerArenaCell();
        return !warningCells.Contains(p) && !damageCells.Contains(p) &&
               !recentWarningCells.Contains(p) && !recentDamageCells.Contains(p);
    }

    /// <summary>True when any cardinal-adjacent arena cell contains a warning/damage/recent hazard.</summary>
    public bool IsDangerNearby()
    {
        if (!EnsureReady(logWarning: false)) return false;
        Vector2Int p = GetPlayerArenaCell();
        foreach (Vector2Int dir in CardinalDirs)
        {
            Vector2Int n = p + dir;
            if (warningCells.Contains(n) || damageCells.Contains(n) ||
                recentWarningCells.Contains(n) || recentDamageCells.Contains(n))
                return true;
        }
        return false;
    }

    /// <summary>Number of directions the player can move to without hitting geometry wall or any hazard.</summary>
    public int SafeMoveDirectionCount()
    {
        if (!EnsureReady(logWarning: false)) return 0;
        int count = 0;
        foreach (Vector2Int dir in CardinalDirs)
        {
            if (!IsGeometryBlockedDirection(dir) &&
                !IsNextCellWarning(dir) && !IsNextCellDamage(dir) &&
                !IsNextCellRecentWarning(dir) && !IsNextCellRecentDamage(dir))
                count++;
        }
        return count;
    }

    /// <summary>Danger level of the cell in the given direction:
    /// 999=geometry-blocked  3=active_damage  2=active_warning  1=recent_warn/dmg  0=safe</summary>
    public int GetDirectionDangerLevel(Vector2Int dir)
    {
        if (IsGeometryBlockedDirection(dir)) return 999;
        bool nextDmg   = IsNextCellDamage(dir);
        bool nextWarn  = IsNextCellWarning(dir);
        bool nextRWarn = IsNextCellRecentWarning(dir);
        bool nextRDmg  = IsNextCellRecentDamage(dir);
        if (nextDmg)               return 3;
        if (nextWarn)              return 2;
        if (nextRWarn || nextRDmg) return 1;
        return 0;
    }

    /// <summary>True when the player is currently on any warning/damage/recent tile.</summary>
    public bool IsPlayerOnAnyDanger()
    {
        if (!EnsureReady(logWarning: false)) return false;
        Vector2Int p = GetPlayerArenaCell();
        return warningCells.Contains(p) || damageCells.Contains(p) ||
               recentWarningCells.Contains(p) || recentDamageCells.Contains(p);
    }

    /// <summary>Returns cached hazard and recent-danger state without calling RefreshHazardMasks.
    /// Safe to call from OnActionReceived after CollectObservations has run.</summary>
    public void GetCachedHazardAndRecentState(out bool onWarning, out bool onDamage,
                                              out bool onRecentWarning, out bool onRecentDamage)
    {
        if (!EnsureReady(logWarning: false))
        {
            onWarning = onDamage = onRecentWarning = onRecentDamage = false;
            return;
        }
        Vector2Int p = GetPlayerArenaCell();
        onWarning       = warningCells.Contains(p);
        onDamage        = damageCells.Contains(p);
        onRecentWarning = recentWarningCells.Contains(p);
        onRecentDamage  = recentDamageCells.Contains(p);
    }

    // ── Attack range helpers ──────────────────────────────────────────────────

    public bool IsBossInAttackRange()
    {
        if (!EnsureReady(logWarning: false) || playerOccupant == null || playerController == null)
            return false;
        return IsBossInAttackRangeInternal(GetPlayerArenaCell());
    }

    public int ManhattanDistanceToBoss()
    {
        if (!EnsureReady(logWarning: false) || playerOccupant == null || bossController == null)
            return 99;
        Vector2Int p = playerOccupant.CurrentCell;
        Vector2Int b = bossController.BossCell;
        return Mathf.Abs(p.x - b.x) + Mathf.Abs(p.y - b.y);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void AddDirectionalObservations(VectorSensor sensor, Vector2Int playerArenaCell, Vector2Int bossArenaCell)
    {
        // 4 directions × 7 observations = 28 total
        foreach (Vector2Int dir in CardinalDirs)
        {
            Vector2Int nextArena = playerArenaCell + dir;

            bool canMove         = CanMoveInDirection(dir);
            bool nextWarning     = IsInsideArena(nextArena) && warningCells.Contains(nextArena);
            bool nextDamage      = IsInsideArena(nextArena) && damageCells.Contains(nextArena);
            bool nextRecentWarn  = IsInsideArena(nextArena) && recentWarningCells.Contains(nextArena);
            bool nextRecentDmg   = IsInsideArena(nextArena) && recentDamageCells.Contains(nextArena);

            int  nextDist     = Mathf.Abs(nextArena.x - bossArenaCell.x) + Mathf.Abs(nextArena.y - bossArenaCell.y);
            float nextDistNorm = Mathf.Clamp01(nextDist / 12f);

            bool bossInRangeAfter = GetBossInRangeAfterMove(dir);

            sensor.AddObservation(canMove);           // 1
            sensor.AddObservation(nextWarning);        // 1
            sensor.AddObservation(nextDamage);         // 1
            sensor.AddObservation(nextRecentWarn);     // 1
            sensor.AddObservation(nextRecentDmg);      // 1
            sensor.AddObservation(nextDistNorm);       // 1
            sensor.AddObservation(bossInRangeAfter);   // 1
        }
    }

    private bool GetBossInRangeAfterMove(Vector2Int moveDir)
    {
        if (playerCombat == null || playerOccupant == null || bossController == null) return false;
        Vector2Int nextWorldCell = playerOccupant.CurrentCell + moveDir;
        List<Vector2Int> cells = playerCombat.GetAttackCells(nextWorldCell, moveDir);
        return cells.Contains(bossController.BossCell);
    }

    private void TryUpdateRecentDanger()
    {
        if (Time.frameCount == lastRecentDangerFrame) return;
        lastRecentDangerFrame = Time.frameCount;
        UpdateRecentDanger();
    }

    private void UpdateRecentDanger()
    {
        float now = Time.time;

        // Refresh/add current hazard cells with TTL from now
        foreach (Vector2Int cell in warningCells)
            recentWarningExpireTime[cell] = now + RecentWarningTTL;
        foreach (Vector2Int cell in damageCells)
            recentDamageExpireTime[cell] = now + RecentDamageTTL;

        // Expire old cells
        var wKeys = new List<Vector2Int>(recentWarningExpireTime.Keys);
        foreach (var k in wKeys)
            if (now >= recentWarningExpireTime[k]) recentWarningExpireTime.Remove(k);
        var dKeys = new List<Vector2Int>(recentDamageExpireTime.Keys);
        foreach (var k in dKeys)
            if (now >= recentDamageExpireTime[k]) recentDamageExpireTime.Remove(k);

        // Rebuild sets
        recentWarningCells.Clear();
        foreach (var k in recentWarningExpireTime.Keys) recentWarningCells.Add(k);
        recentDamageCells.Clear();
        foreach (var k in recentDamageExpireTime.Keys)  recentDamageCells.Add(k);
    }

    private bool IsBossInAttackRangeInternal(Vector2Int playerArenaCell)
    {
        if (playerCombat == null || bossController == null) return false;
        Vector2Int playerCell = playerOccupant != null ? playerOccupant.CurrentCell : Vector2Int.zero;
        Vector2Int facing     = playerController != null ? playerController.Facing : Vector2Int.down;
        List<Vector2Int> cells = playerCombat.GetAttackCells(playerCell, facing);
        return cells.Contains(bossController.BossCell);
    }

    private float GetManhattanDistanceToBossNormalized(Vector2Int playerArena, Vector2Int bossArena)
    {
        int dist = Mathf.Abs(playerArena.x - bossArena.x) + Mathf.Abs(playerArena.y - bossArena.y);
        return Mathf.Clamp01(dist / 12f);
    }

    private void RefreshHazardMasks()
    {
        warningCells.Clear();
        damageCells.Clear();
        System.Array.Clear(warningMask, 0, warningMask.Length);
        System.Array.Clear(damageMask,  0, damageMask.Length);

        foreach (PatternTile pt in FindObjectsByType<PatternTile>(FindObjectsSortMode.None))
            warningCells.Add(WorldToArenaCell(pt.transform.position));

        foreach (MergedPatternWarningVisual mpv in FindObjectsByType<MergedPatternWarningVisual>(FindObjectsSortMode.None))
        {
            if (MergedWarningCellsField == null) continue;
            List<Vector2Int> cells = MergedWarningCellsField.GetValue(mpv) as List<Vector2Int>;
            if (cells == null) continue;
            foreach (Vector2Int cell in cells)
                if (IsInsideArena(cell)) warningCells.Add(cell);
        }

        foreach (DamageTile dt in FindObjectsByType<DamageTile>(FindObjectsSortMode.None))
            damageCells.Add(WorldToArenaCell(dt.transform.position));

        foreach (Vector2Int cell in warningCells) warningMask[ToMaskIndex(cell)] = 1f;
        foreach (Vector2Int cell in damageCells)  damageMask[ToMaskIndex(cell)]  = 1f;
    }

    private Vector2Int GetPlayerArenaCell()
    {
        Vector3 pos = transform.position;
        if (playerOccupant != null && gridManager != null)
            pos = gridManager.CellToWorld(playerOccupant.CurrentCell);
        return WorldToArenaCell(pos);
    }

    private Vector2Int GetBossArenaCell()
    {
        if (bossController == null) return Vector2Int.zero;
        return WorldCellToArenaCell(bossController.BossCell);
    }

    private bool IsBossVisible()
    {
        if (bossRenderers == null || bossRenderers.Length == 0)
            return bossController != null && bossController.gameObject.activeInHierarchy;
        foreach (Renderer r in bossRenderers)
            if (r != null && r.enabled && r.gameObject.activeInHierarchy) return true;
        return false;
    }

    private bool EnsureReady(bool logWarning)
    {
        if (HasCoreReferences()) return true;
        TryInitialize();
        if (HasCoreReferences()) return true;
        if (logWarning)
        {
            string missing = GetMissingReferenceSummary();
            debugLogger = debugLogger != null ? debugLogger : GetComponent<BossRLDebugLogger>();
            if (debugLogger != null)
                debugLogger.LogWarningOnce("BossRLStateExtractor.MissingRefs",
                    $"[BossRL] StateExtractor missing: {missing}");
            else
                Debug.LogWarning($"[BossRL] StateExtractor missing: {missing}");
        }
        return false;
    }

    private bool HasCoreReferences() =>
        gridManager != null && playerOccupant != null && playerMover != null &&
        playerController != null && playerCombat != null && playerHealth != null &&
        bossHealth != null && bossController != null && patternCaster != null;

    private string GetMissingReferenceSummary()
    {
        var m = new List<string>();
        if (gridManager == null)      m.Add(nameof(gridManager));
        if (playerOccupant == null)   m.Add(nameof(playerOccupant));
        if (playerMover == null)      m.Add(nameof(playerMover));
        if (playerController == null) m.Add(nameof(playerController));
        if (playerCombat == null)     m.Add(nameof(playerCombat));
        if (playerHealth == null)     m.Add(nameof(playerHealth));
        if (bossHealth == null)       m.Add(nameof(bossHealth));
        if (bossController == null)   m.Add(nameof(bossController));
        if (patternCaster == null)    m.Add(nameof(patternCaster));
        return m.Count > 0 ? string.Join(", ", m) : "none";
    }

    private void AddFacingObservation(VectorSensor sensor, Vector2Int facing)
    {
        Vector2Int n = NormalizeCardinal(facing);
        sensor.AddObservation(n == Vector2Int.up);
        sensor.AddObservation(n == Vector2Int.down);
        sensor.AddObservation(n == Vector2Int.left);
        sensor.AddObservation(n == Vector2Int.right);
    }

    private Vector2Int WorldToArenaCell(Vector3 worldPosition)
    {
        if (gridManager == null || patternCaster == null || patternCaster.patternOrigin == null)
            return Vector2Int.zero;
        Vector2Int worldCell = gridManager.WorldToCell(worldPosition);
        return WorldCellToArenaCell(worldCell);
    }

    private Vector2Int WorldCellToArenaCell(Vector2Int worldCell)
    {
        if (gridManager == null || patternCaster == null || patternCaster.patternOrigin == null)
            return Vector2Int.zero;
        Vector2Int originCell = gridManager.WorldToCell(patternCaster.patternOrigin.position);
        return worldCell - originCell;
    }

    private static Vector2Int NormalizeCardinal(Vector2Int d)
    {
        if (d == Vector2Int.zero) return Vector2Int.down;
        if (Mathf.Abs(d.x) >= Mathf.Abs(d.y))
            return new Vector2Int(d.x > 0 ? 1 : -1, 0);
        return new Vector2Int(0, d.y > 0 ? 1 : -1);
    }

    private static float NormalizeArenaCoord(int value) =>
        Mathf.Clamp(value / (float)ArenaHalfExtent, -1f, 1f);

    private static bool IsInsideArena(Vector2Int cell) =>
        cell.x >= -ArenaHalfExtent && cell.x <= ArenaHalfExtent &&
        cell.y >= -ArenaHalfExtent && cell.y <= ArenaHalfExtent;

    private static int ToMaskIndex(Vector2Int cell)
    {
        int x = Mathf.Clamp(cell.x + ArenaHalfExtent, 0, ArenaSize - 1);
        int y = Mathf.Clamp(cell.y + ArenaHalfExtent, 0, ArenaSize - 1);
        return y * ArenaSize + x;
    }
}
