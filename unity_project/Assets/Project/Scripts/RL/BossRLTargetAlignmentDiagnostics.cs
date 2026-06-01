using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class BossRLTargetAlignmentDiagnostics
{
    public enum HitClass
    {
        NormalVisible,
        RootVisibleOverlap,
        DashCurrentOverlap,
        StaleBossCell,
        HiddenTarget,
        OffLaneEmpty,
        UnknownRemaining
    }

    public struct BossVisualDiagnosticState
    {
        public bool visualRootExists;
        public bool visualRootActiveSelf;
        public bool visualRootActiveInHierarchy;
        public bool spriteVisible;
        public bool anyRendererDisabled;
        public bool anyRendererAlphaZero;
        public string primarySpriteObjectName;
        public bool primarySpriteRendererEnabled;
        public float primarySpriteAlpha;
        public Vector3 visualTransformPosition;
        public Vector3 rootTransformPosition;
        public bool hasRendererBoundsCenter;
        public Vector3 rendererBoundsCenter;
        public Vector2Int rootCurrentCell;
        public bool rootCurrentCellInsideAttackCells;
        public Vector2Int visualEstimatedCell;
        public bool visualCellInsideAttackCells;
        public Vector2Int spriteBoundsCenterCell;
        public bool spriteBoundsCenterCellInsideAttackCells;
        public bool bossCellInsideAttackCells;
        public bool currentlyHidden;
        public bool currentlyDashing;
        public bool currentlyMarkDash;
        public Vector2Int dashStartCell;
        public Vector2Int dashEndCell;
        public Vector2Int dashCurrentCell;
        public bool dashCurrentCellInsideAttackCells;
        public bool dashLineCellsOverlapAttackCells;
        public bool activeDamageCellsOverlapAttackCells;
        public bool activeWarningCellsOverlapAttackCells;
        public bool attackCellsOverlapAnyActiveLane;
        public HitClass hitClass;
        public Vector2Int bossGridOccupantCell;
        public bool hasBossGridOccupant;
    }

    public struct BossHitRecord
    {
        public int episode;
        public int step;
        public float time;
        public Vector2Int playerCell;
        public Vector2Int playerFacing;
        public IReadOnlyList<Vector2Int> attackCells;
        public Vector2Int bossCell;
        public Vector3 bossHealthPosition;
        public int damageAmount;
        public int bossHpBefore;
        public int bossHpAfter;
        public BossVisualDiagnosticState visualState;
    }

    public static void RecordSuccessfulBossHit(
        PlayerCombat playerCombat,
        ElevatorBossController bossController,
        BossHealth bossHealth,
        IReadOnlyList<Vector2Int> attackCells,
        Vector2Int playerCell,
        Vector2Int playerFacing,
        int bossHpBefore,
        int bossHpAfter,
        int damageAmount)
    {
        if (playerCombat == null || bossController == null || bossHealth == null)
        {
            return;
        }

        BossRLDebugLogger logger = playerCombat.GetComponent<BossRLDebugLogger>();
        if (logger == null)
        {
            return;
        }

        BossPlayerAgent agent = playerCombat.GetComponent<BossPlayerAgent>();
        int step = agent != null ? agent.StepCount : -1;

        BossHitRecord record = new BossHitRecord
        {
            episode = logger.CurrentEpisodeIndex,
            step = step,
            time = Time.time,
            playerCell = playerCell,
            playerFacing = playerFacing,
            attackCells = attackCells,
            bossCell = bossController.BossCell,
            bossHealthPosition = bossHealth.transform.position,
            damageAmount = damageAmount,
            bossHpBefore = bossHpBefore,
            bossHpAfter = bossHpAfter,
            visualState = CaptureVisualState(bossController, attackCells)
        };

        logger.RecordTargetAlignmentHit(record);
    }

    public static BossVisualDiagnosticState CaptureVisualState(
        ElevatorBossController bossController,
        IReadOnlyList<Vector2Int> attackCells)
    {
        BossVisualDiagnosticState state = new BossVisualDiagnosticState
        {
            primarySpriteObjectName = "<none>",
            primarySpriteAlpha = -1f,
            bossGridOccupantCell = Vector2Int.zero,
            visualEstimatedCell = Vector2Int.zero,
            spriteBoundsCenterCell = Vector2Int.zero,
            rootCurrentCell = Vector2Int.zero,
            dashStartCell = Vector2Int.zero,
            dashEndCell = Vector2Int.zero,
            dashCurrentCell = Vector2Int.zero,
            hitClass = HitClass.UnknownRemaining
        };

        if (bossController == null)
        {
            return state;
        }

        HashSet<Vector2Int> attackCellSet = attackCells != null
            ? new HashSet<Vector2Int>(attackCells)
            : new HashSet<Vector2Int>();

        Transform visualRoot = bossController.DiagnosticBossVisualRoot;
        state.visualRootExists = visualRoot != null;
        state.visualRootActiveSelf = visualRoot != null && visualRoot.gameObject.activeSelf;
        state.visualRootActiveInHierarchy = visualRoot != null && visualRoot.gameObject.activeInHierarchy;
        state.visualTransformPosition = visualRoot != null ? visualRoot.position : bossController.transform.position;
        state.rootTransformPosition = bossController.transform.position;
        state.bossCellInsideAttackCells = attackCellSet.Contains(bossController.BossCell);
        state.currentlyDashing = bossController.DiagnosticIsDashInProgress;
        state.currentlyMarkDash = bossController.DiagnosticIsMarkDashInProgress;
        state.dashStartCell = bossController.DiagnosticDashStartCell;
        state.dashEndCell = bossController.DiagnosticDashEndCell;
        state.dashCurrentCell = bossController.DiagnosticDashCurrentCell;
        state.dashCurrentCellInsideAttackCells = state.currentlyDashing &&
                                                attackCellSet.Contains(state.dashCurrentCell);
        state.dashLineCellsOverlapAttackCells = HasOverlap(attackCellSet, bossController.DiagnosticDashLaneCells);
        state.activeDamageCellsOverlapAttackCells = ActiveDamageCellsOverlap(attackCellSet);
        state.activeWarningCellsOverlapAttackCells = ActiveWarningCellsOverlap(attackCellSet);
        state.attackCellsOverlapAnyActiveLane = state.dashLineCellsOverlapAttackCells ||
                                               state.activeDamageCellsOverlapAttackCells ||
                                               state.activeWarningCellsOverlapAttackCells;

        GridOccupant bossOccupant = bossController.GetComponent<GridOccupant>();
        if (bossOccupant != null)
        {
            state.hasBossGridOccupant = true;
            state.bossGridOccupantCell = bossOccupant.CurrentCell;
        }

        SpriteRenderer[] renderers = visualRoot != null
            ? visualRoot.GetComponentsInChildren<SpriteRenderer>(true)
            : bossController.GetComponentsInChildren<SpriteRenderer>(true);

        SpriteRenderer primary = null;
        SpriteRenderer firstRenderer = null;
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (firstRenderer == null)
            {
                firstRenderer = renderer;
            }

            bool alphaVisible = renderer.color.a > 0.01f;
            bool rendererVisible = renderer.enabled &&
                                   renderer.gameObject.activeInHierarchy &&
                                   renderer.sprite != null &&
                                   alphaVisible;

            if (!renderer.enabled)
            {
                state.anyRendererDisabled = true;
            }

            if (!alphaVisible)
            {
                state.anyRendererAlphaZero = true;
            }

            if (rendererVisible)
            {
                state.spriteVisible = true;
                if (primary == null)
                {
                    primary = renderer;
                }
            }
        }

        if (primary == null)
        {
            primary = firstRenderer;
        }
        if (primary != null)
        {
            state.primarySpriteObjectName = primary.gameObject.name;
            state.primarySpriteRendererEnabled = primary.enabled;
            state.primarySpriteAlpha = primary.color.a;
            state.hasRendererBoundsCenter = true;
            state.rendererBoundsCenter = primary.bounds.center;
        }

        Vector3 visualPositionForCell = state.hasRendererBoundsCenter
            ? state.rendererBoundsCenter
            : state.visualTransformPosition;

        if (GridManager.Instance != null)
        {
            state.rootCurrentCell = GridManager.Instance.WorldToCell(state.rootTransformPosition);
            state.rootCurrentCellInsideAttackCells = attackCellSet.Contains(state.rootCurrentCell);
            state.visualEstimatedCell = GridManager.Instance.WorldToCell(visualPositionForCell);
            state.visualCellInsideAttackCells = attackCellSet.Contains(state.visualEstimatedCell);
            state.spriteBoundsCenterCell = state.visualEstimatedCell;
            state.spriteBoundsCenterCellInsideAttackCells = state.visualCellInsideAttackCells;
        }

        state.currentlyHidden = !state.spriteVisible;
        state.hitClass = ClassifyHit(state);
        return state;
    }

    public static string FormatHitClass(HitClass hitClass)
    {
        switch (hitClass)
        {
            case HitClass.NormalVisible:
                return "normal_visible";
            case HitClass.RootVisibleOverlap:
                return "root_visible_overlap";
            case HitClass.DashCurrentOverlap:
                return "dash_current_overlap";
            case HitClass.StaleBossCell:
                return "stale_bosscell";
            case HitClass.HiddenTarget:
                return "hidden_target";
            case HitClass.OffLaneEmpty:
                return "off_lane_empty";
            default:
                return "unknown_remaining";
        }
    }

    public static bool IsRLAttackAllowed(HitClass hitClass)
    {
        return hitClass == HitClass.NormalVisible ||
               hitClass == HitClass.RootVisibleOverlap ||
               hitClass == HitClass.DashCurrentOverlap;
    }

    private static HitClass ClassifyHit(BossVisualDiagnosticState state)
    {
        bool spriteBoundsInside = state.spriteVisible && state.spriteBoundsCenterCellInsideAttackCells;
        bool visualCellInside = state.spriteVisible && state.visualCellInsideAttackCells;
        bool visibleBodyInside = visualCellInside || spriteBoundsInside;
        bool dashBodyInside = state.currentlyDashing &&
                              (state.dashCurrentCellInsideAttackCells ||
                               state.rootCurrentCellInsideAttackCells ||
                               visibleBodyInside);
        bool dashLaneRelated = state.dashLineCellsOverlapAttackCells ||
                               state.activeDamageCellsOverlapAttackCells ||
                               state.activeWarningCellsOverlapAttackCells;
        bool currentBodyInside = state.rootCurrentCellInsideAttackCells ||
                                 visibleBodyInside ||
                                 dashBodyInside;

        if (state.spriteVisible &&
            state.spriteBoundsCenterCellInsideAttackCells &&
            state.bossCellInsideAttackCells)
        {
            return HitClass.NormalVisible;
        }

        if (dashBodyInside && dashLaneRelated)
        {
            return HitClass.DashCurrentOverlap;
        }

        if (state.spriteVisible &&
            state.rootCurrentCellInsideAttackCells &&
            state.bossCellInsideAttackCells &&
            !state.spriteBoundsCenterCellInsideAttackCells)
        {
            return HitClass.RootVisibleOverlap;
        }

        if (state.currentlyHidden && !dashBodyInside && state.bossCellInsideAttackCells)
        {
            return HitClass.HiddenTarget;
        }

        if (state.bossCellInsideAttackCells && !currentBodyInside && !state.attackCellsOverlapAnyActiveLane)
        {
            return HitClass.StaleBossCell;
        }

        if (!state.attackCellsOverlapAnyActiveLane && !currentBodyInside)
        {
            return HitClass.OffLaneEmpty;
        }

        if (state.bossCellInsideAttackCells && !currentBodyInside)
        {
            return HitClass.StaleBossCell;
        }

        return HitClass.UnknownRemaining;
    }

    private static bool HasOverlap(HashSet<Vector2Int> attackCellSet, IReadOnlyList<Vector2Int> cells)
    {
        if (attackCellSet == null || cells == null)
        {
            return false;
        }

        foreach (Vector2Int cell in cells)
        {
            if (attackCellSet.Contains(cell))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ActiveDamageCellsOverlap(HashSet<Vector2Int> attackCellSet)
    {
        if (GridManager.Instance == null || attackCellSet == null)
        {
            return false;
        }

        foreach (DamageTile tile in Object.FindObjectsByType<DamageTile>(FindObjectsSortMode.None))
        {
            if (tile != null && attackCellSet.Contains(GridManager.Instance.WorldToCell(tile.transform.position)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ActiveWarningCellsOverlap(HashSet<Vector2Int> attackCellSet)
    {
        if (GridManager.Instance == null || attackCellSet == null)
        {
            return false;
        }

        foreach (PatternTile tile in Object.FindObjectsByType<PatternTile>(FindObjectsSortMode.None))
        {
            if (tile != null && attackCellSet.Contains(GridManager.Instance.WorldToCell(tile.transform.position)))
            {
                return true;
            }
        }

        foreach (MergedPatternWarningVisual visual in Object.FindObjectsByType<MergedPatternWarningVisual>(FindObjectsSortMode.None))
        {
            if (visual == null)
            {
                continue;
            }

            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer != null && BoundsOverlapAttackCells(renderer.bounds, attackCellSet))
            {
                return true;
            }
        }

        return false;
    }

    private static bool BoundsOverlapAttackCells(Bounds bounds, HashSet<Vector2Int> attackCellSet)
    {
        if (GridManager.Instance == null)
        {
            return false;
        }

        Vector2Int min = GridManager.Instance.WorldToCell(bounds.min);
        Vector2Int max = GridManager.Instance.WorldToCell(bounds.max);
        for (int x = Mathf.Min(min.x, max.x); x <= Mathf.Max(min.x, max.x); x++)
        {
            for (int y = Mathf.Min(min.y, max.y); y <= Mathf.Max(min.y, max.y); y++)
            {
                if (attackCellSet.Contains(new Vector2Int(x, y)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static string FormatCells(IReadOnlyList<Vector2Int> cells)
    {
        if (cells == null || cells.Count == 0)
        {
            return "[]";
        }

        StringBuilder builder = new StringBuilder("[");
        for (int i = 0; i < cells.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(";");
            }

            builder.Append(cells[i].x);
            builder.Append(",");
            builder.Append(cells[i].y);
        }

        builder.Append("]");
        return builder.ToString();
    }
}
