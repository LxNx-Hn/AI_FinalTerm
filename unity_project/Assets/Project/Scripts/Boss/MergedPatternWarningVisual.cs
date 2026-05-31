using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MergedPatternWarningVisual : MonoBehaviour
{
    private const float CellSize = 1f;

    private readonly HashSet<Vector2Int> cellSet = new HashSet<Vector2Int>();
    private readonly List<Vector2Int> cells = new List<Vector2Int>();

    private SpriteRenderer spriteRenderer;
    private Texture2D texture;
    private Sprite sprite;

    private int minX;
    private int minY;
    private int widthCells;
    private int heightCells;
    private int pixelsPerCell;
    private float maxRevealDistance;
    private bool animateFill;
    private float fillDuration;
    private bool useDirectionalFill;
    private Vector2 fillDirection;
    private float minProjection;
    private float maxProjection;
    private Vector2 revealOrigin;
    private Color fillColor;
    private Color borderColor;
    private float borderWidthInCells;

    public static MergedPatternWarningVisual Spawn(
        List<Vector2Int> sourceCells,
        Vector3 patternOrigin,
        float lifetime,
        Vector2Int? fillOriginCell,
        string sortingLayerName,
        int sortingOrder,
        int pixelsPerCell,
        float fillAlpha,
        float borderAlpha,
        float borderWidthInCells,
        float fillDuration,
        bool animateFill,
        Vector2Int? fillDirection
    )
    {
        if (sourceCells == null || sourceCells.Count == 0)
            return null;

        GameObject go = new GameObject("MergedPatternWarning");
        var visual = go.AddComponent<MergedPatternWarningVisual>();
        visual.Initialize(
            sourceCells,
            patternOrigin,
            lifetime,
            fillOriginCell,
            sortingLayerName,
            sortingOrder,
            pixelsPerCell,
            fillAlpha,
            borderAlpha,
            borderWidthInCells,
            fillDuration,
            animateFill,
            fillDirection
        );
        return visual;
    }

    private void Initialize(
        List<Vector2Int> sourceCells,
        Vector3 patternOrigin,
        float lifetime,
        Vector2Int? fillOriginCell,
        string sortingLayerName,
        int sortingOrder,
        int requestedPixelsPerCell,
        float fillAlpha,
        float borderAlpha,
        float requestedBorderWidthInCells,
        float requestedFillDuration,
        bool animateFill,
        Vector2Int? fillDirection
    )
    {
        foreach (Vector2Int cell in sourceCells)
        {
            if (cellSet.Add(cell))
                cells.Add(cell);
        }

        CalculateBounds();

        pixelsPerCell = Mathf.Clamp(requestedPixelsPerCell, 16, 96);
        borderWidthInCells = Mathf.Clamp(requestedBorderWidthInCells, 0.025f, 0.2f);
        this.animateFill = animateFill;
        this.fillDuration = Mathf.Clamp(requestedFillDuration, 0.01f, Mathf.Max(0.01f, lifetime));
        this.fillColor = new Color(1f, 0.05f, 0.05f, Mathf.Clamp01(fillAlpha));
        this.borderColor = new Color(1f, 0f, 0f, Mathf.Clamp01(borderAlpha));
        revealOrigin = fillOriginCell.HasValue ? fillOriginCell.Value : CalculateCellCenter();
        maxRevealDistance = CalculateMaxRevealDistance();
        ConfigureDirectionalFill(fillDirection);

        int textureWidth = Mathf.Max(1, widthCells * pixelsPerCell);
        int textureHeight = Mathf.Max(1, heightCells * pixelsPerCell);
        texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        sprite = Sprite.Create(
            texture,
            new Rect(0, 0, textureWidth, textureHeight),
            new Vector2(0.5f, 0.5f),
            pixelsPerCell
        );

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = sortingOrder;

        Vector3 lowerLeft = patternOrigin + new Vector3(minX - 0.5f, minY - 0.5f, 0f);
        transform.position = lowerLeft + new Vector3(widthCells * 0.5f, heightCells * 0.5f, 0f);

        Render(animateFill ? 0f : 1f);
        StartCoroutine(Animate(lifetime));
    }

    private void CalculateBounds()
    {
        minX = cells[0].x;
        int maxX = cells[0].x;
        minY = cells[0].y;
        int maxY = cells[0].y;

        for (int i = 1; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];
            minX = Mathf.Min(minX, cell.x);
            maxX = Mathf.Max(maxX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxY = Mathf.Max(maxY, cell.y);
        }

        widthCells = maxX - minX + 1;
        heightCells = maxY - minY + 1;
    }

    private Vector2 CalculateCellCenter()
    {
        Vector2 total = Vector2.zero;
        foreach (Vector2Int cell in cells)
            total += cell;

        return total / cells.Count;
    }

    private float CalculateMaxRevealDistance()
    {
        float maxDistance = 0.01f;
        foreach (Vector2Int cell in cells)
        {
            for (int ix = -1; ix <= 1; ix += 2)
            {
                for (int iy = -1; iy <= 1; iy += 2)
                {
                    Vector2 corner = new Vector2(cell.x + ix * 0.5f, cell.y + iy * 0.5f);
                    maxDistance = Mathf.Max(maxDistance, Vector2.Distance(revealOrigin, corner));
                }
            }
        }

        return maxDistance;
    }

    private void ConfigureDirectionalFill(Vector2Int? requestedDirection)
    {
        if (!requestedDirection.HasValue || requestedDirection.Value == Vector2Int.zero)
        {
            useDirectionalFill = false;
            return;
        }

        fillDirection = new Vector2(requestedDirection.Value.x, requestedDirection.Value.y).normalized;
        useDirectionalFill = true;
        minProjection = float.PositiveInfinity;
        maxProjection = float.NegativeInfinity;

        foreach (Vector2Int cell in cells)
        {
            for (int ix = -1; ix <= 1; ix += 2)
            {
                for (int iy = -1; iy <= 1; iy += 2)
                {
                    Vector2 corner = new Vector2(cell.x + ix * 0.5f, cell.y + iy * 0.5f);
                    float projection = Vector2.Dot(corner, fillDirection);
                    minProjection = Mathf.Min(minProjection, projection);
                    maxProjection = Mathf.Max(maxProjection, projection);
                }
            }
        }

        if (maxProjection <= minProjection)
            maxProjection = minProjection + 0.01f;
    }

    private IEnumerator Animate(float lifetime)
    {
        float duration = Mathf.Max(0.01f, lifetime);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Render(animateFill ? Mathf.Clamp01(elapsed / fillDuration) : 1f);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void Render(float reveal)
    {
        int width = texture.width;
        int height = texture.height;
        Color clear = Color.clear;
        float radius = Mathf.SmoothStep(0f, maxRevealDistance, reveal);
        float softEdge = Mathf.Max(0.05f, CellSize / pixelsPerCell * 3f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 arenaPoint = PixelToArenaPoint(x, y);
                Vector2Int cell = ArenaPointToCell(arenaPoint);

                if (!cellSet.Contains(cell))
                {
                    texture.SetPixel(x, y, clear);
                    continue;
                }

                Color pixel = clear;
                float fillAmount = useDirectionalFill
                    ? CalculateDirectionalFillAmount(arenaPoint, reveal, softEdge)
                    : CalculateRadialFillAmount(arenaPoint, radius, softEdge);

                if (fillAmount > 0f)
                {
                    pixel = fillColor;
                    pixel.a *= fillAmount;
                }

                if (IsOuterBorderPixel(arenaPoint, cell))
                    pixel = AlphaOver(pixel, borderColor);

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply(false);
    }

    private float CalculateRadialFillAmount(Vector2 arenaPoint, float radius, float softEdge)
    {
        float distance = Vector2.Distance(revealOrigin, arenaPoint);
        return Mathf.Clamp01((radius - distance + softEdge) / softEdge);
    }

    private float CalculateDirectionalFillAmount(Vector2 arenaPoint, float reveal, float softEdge)
    {
        float threshold = Mathf.Lerp(minProjection, maxProjection, Mathf.SmoothStep(0f, 1f, reveal));
        float projection = Vector2.Dot(arenaPoint, fillDirection);
        return Mathf.Clamp01((threshold - projection + softEdge) / softEdge);
    }

    private Vector2 PixelToArenaPoint(int x, int y)
    {
        float arenaX = minX - 0.5f + (x + 0.5f) / pixelsPerCell;
        float arenaY = minY - 0.5f + (y + 0.5f) / pixelsPerCell;
        return new Vector2(arenaX, arenaY);
    }

    private Vector2Int ArenaPointToCell(Vector2 arenaPoint)
    {
        return new Vector2Int(
            Mathf.FloorToInt(arenaPoint.x + 0.5f),
            Mathf.FloorToInt(arenaPoint.y + 0.5f)
        );
    }

    private bool IsOuterBorderPixel(Vector2 arenaPoint, Vector2Int cell)
    {
        float left = cell.x - 0.5f;
        float right = cell.x + 0.5f;
        float bottom = cell.y - 0.5f;
        float top = cell.y + 0.5f;

        if (arenaPoint.x - left <= borderWidthInCells && !cellSet.Contains(cell + Vector2Int.left))
            return true;

        if (right - arenaPoint.x <= borderWidthInCells && !cellSet.Contains(cell + Vector2Int.right))
            return true;

        if (arenaPoint.y - bottom <= borderWidthInCells && !cellSet.Contains(cell + Vector2Int.down))
            return true;

        if (top - arenaPoint.y <= borderWidthInCells && !cellSet.Contains(cell + Vector2Int.up))
            return true;

        return false;
    }

    private Color AlphaOver(Color under, Color over)
    {
        float outA = over.a + under.a * (1f - over.a);
        if (outA <= 0f)
            return Color.clear;

        return new Color(
            (over.r * over.a + under.r * under.a * (1f - over.a)) / outA,
            (over.g * over.a + under.g * under.a * (1f - over.a)) / outA,
            (over.b * over.a + under.b * under.a * (1f - over.a)) / outA,
            outA
        );
    }

    private void OnDestroy()
    {
        if (sprite != null)
            Destroy(sprite);

        if (texture != null)
            Destroy(texture);
    }
}
