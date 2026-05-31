using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class MiniBossCharge : MonoBehaviour
{
    [Header("Start Position (world)")]
    public int startWorldX = 25;
    public int startWorldY = -24;

    [Header("Charge X Range (world)")]
    public int mapMinX = 19;
    public int mapMaxX = 29;

    [Header("Attack Area")]
    public int attackRowCount = 3;

    [Header("Timing")]
    public float warningDuration     = 1.0f;
    public float chargeDelayPerTile  = 0.008f;
    public float pauseBetweenCharges = 0.5f;
    public float initialDelay        = 1f;

    [Header("Sprite Animation")]
    public Sprite   idleSprite;
    public Sprite[] moveSpritesLeft;   // frames 4-7
    public Sprite[] moveSpritesRight;  // frames 8-11
    public float    moveFps = 8f;

    [Header("Damage")]
    public int chargeDamage = 99;
    public GameObject damageTilePrefab;
    public GameObject scratchVFXPrefab;

    [Header("Warning Visual (boss style)")]
    public string warningSortingLayer = "Items";
    public int    warningSortingOrder = 20;
    public int    warningPixelsPerCell = 48;
    public float  warningFillAlpha    = 0.42f;
    public float  warningBorderAlpha  = 0.9f;
    public float  warningBorderWidth  = 0.035f;

    private SpriteRenderer sr;
    private Coroutine moveAnimCoroutine;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null && idleSprite == null)
            idleSprite = sr.sprite;
    }

    private void OnEnable()
    {
        StartCoroutine(ChargeLoop());
    }

    private IEnumerator ChargeLoop()
    {
        yield return new WaitForSeconds(initialDelay);

        int currentY = startWorldY;
        bool firstCharge = true;

        while (true)
        {
            int fromX = firstCharge ? startWorldX : mapMaxX;
            firstCharge = false;
            yield return ChargeRow(currentY, fromX, mapMinX, -1);
            currentY += attackRowCount;
            yield return new WaitForSeconds(pauseBetweenCharges);
            if (!gameObject.activeSelf) yield break;

            yield return ChargeRow(currentY, mapMinX, mapMaxX, 1);
            currentY += attackRowCount;
            yield return new WaitForSeconds(pauseBetweenCharges);
            if (!gameObject.activeSelf) yield break;
        }
    }

    private IEnumerator ChargeRow(int rowWorldY, int fromWorldX, int toWorldX, int dir)
    {
        if (GridManager.Instance == null) yield break;

        var gm = GridManager.Instance;
        int cellStartX = Mathf.RoundToInt((fromWorldX - gm.worldOrigin.x) / gm.tileSize);
        int cellEndX   = Mathf.RoundToInt((toWorldX   - gm.worldOrigin.x) / gm.tileSize);
        int cellY      = Mathf.RoundToInt((rowWorldY  - gm.worldOrigin.y) / gm.tileSize);

        // 메인 행 셀 수집
        var mainCells = new List<Vector2Int>();
        for (int cx = cellStartX; dir > 0 ? cx <= cellEndX : cx >= cellEndX; cx += dir)
        {
            var cell = new Vector2Int(cx, cellY);
            if (!gm.IsWalkable(cell, ignoreOccupant: true))
            {
                if (mainCells.Count == 0) continue;
                break;
            }
            mainCells.Add(cell);
        }

        if (mainCells.Count == 0) yield break;

        // 3줄 전체 셀 목록
        int half = attackRowCount / 2;
        var allCells = new List<Vector2Int>();
        foreach (var cell in mainCells)
            for (int ro = -half; ro <= half; ro++)
                allCells.Add(new Vector2Int(cell.x, cell.y + ro));

        transform.position = gm.CellToWorld(new Vector2Int(cellStartX, cellY));

        // 방향별 스프라이트로 전환 (flipX 불필요)
        if (sr != null)
        {
            sr.flipX = false;
            var frames = dir < 0 ? moveSpritesLeft : moveSpritesRight;
            if (frames != null && frames.Length > 0)
            {
                if (moveAnimCoroutine != null) StopCoroutine(moveAnimCoroutine);
                moveAnimCoroutine = StartCoroutine(PlayMoveAnim(frames));
            }
        }

        // GridManager 셀 좌표 = patternOrigin(-32,-32) + cell 벡터 → 월드 좌표 일치
        Vector3 patternOrigin = new Vector3(gm.worldOrigin.x, gm.worldOrigin.y, 0f);

        // ── 경고 비주얼 (보스 스타일 merged) ──
        var warningVisual = MergedPatternWarningVisual.Spawn(
            allCells, patternOrigin, warningDuration,
            new Vector2Int(cellStartX, cellY),   // 시작점부터 채워지는 fill 효과
            warningSortingLayer, warningSortingOrder,
            warningPixelsPerCell, warningFillAlpha, warningBorderAlpha, warningBorderWidth,
            warningDuration * 0.45f, true,
            new Vector2Int(dir, 0));              // 돌진 방향으로 fill 진행

        yield return new WaitForSeconds(warningDuration);

        if (warningVisual != null) Destroy(warningVisual.gameObject);

        // ── 데미지 비주얼 (보스 스타일 merged) + 스윕 타일 ──
        SpawnScratchVFX(allCells, dir);

        foreach (var cell in mainCells)
        {
            if (!gameObject.activeSelf) yield break;
            yield return SmoothMoveTo(gm.CellToWorld(cell), chargeDelayPerTile);

            if (damageTilePrefab != null)
            {
                for (int ro = -half; ro <= half; ro++)
                {
                    var c = new Vector2Int(cell.x, cell.y + ro);
                    var go = Instantiate(damageTilePrefab, gm.CellToWorld(c), Quaternion.identity);
                    var dt = go.GetComponent<DamageTile>();
                    if (dt != null)
                    {
                        dt.damage = chargeDamage;
                        dt.lifeTime = chargeDelayPerTile * 4f;
                        dt.hideVisual = true;
                    }
                }
            }
        }

        // 돌진 끝 → idle 스프라이트 복귀
        if (moveAnimCoroutine != null) { StopCoroutine(moveAnimCoroutine); moveAnimCoroutine = null; }
        if (sr != null) { sr.sprite = idleSprite; sr.flipX = false; }
    }

    private IEnumerator PlayMoveAnim(Sprite[] frames)
    {
        int idx = 0;
        float interval = 1f / Mathf.Max(0.01f, moveFps);
        while (true)
        {
            if (sr != null) sr.sprite = frames[idx % frames.Length];
            idx++;
            yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnScratchVFX(List<Vector2Int> cells, int dir)
    {
        if (scratchVFXPrefab == null || cells == null || cells.Count == 0) return;
        var gm = GridManager.Instance;
        if (gm == null) return;

        Vector3 minW = gm.CellToWorld(cells[0]);
        Vector3 maxW = minW;
        foreach (var c in cells)
        {
            Vector3 w = gm.CellToWorld(c);
            minW = Vector3.Min(minW, w);
            maxW = Vector3.Max(maxW, w);
        }

        Vector3 center    = (minW + maxW) * 0.5f;
        float lengthWorld = (maxW.x - minW.x) + 1f + 1.8f; // 수평 길이
        float widthWorld  = (maxW.y - minW.y) + 1f + 0.9f; // 수직 폭 (3줄)

        var go = Instantiate(scratchVFXPrefab, center, Quaternion.Euler(0f, 0f, -90f));
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            Vector2 sb = sr.sprite.bounds.size;
            go.transform.localScale = new Vector3(
                (widthWorld  / Mathf.Max(0.001f, sb.x)) * 1.18f,
                (lengthWorld / Mathf.Max(0.001f, sb.y)) * 1.12f,
                1f);
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.42f);
        }
        else
        {
            go.transform.localScale = new Vector3(widthWorld * 1.18f, lengthWorld * 1.12f, 1f);
        }
        Destroy(go, 0.55f);
    }

    private IEnumerator SmoothMoveTo(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        transform.position = target;
    }
}
