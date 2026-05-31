using UnityEngine;

/// <summary>
/// Creates a seamless vertical scrolling background using two SpriteRenderers.
/// Move direction is top-to-bottom when scrollSpeed is positive.
/// </summary>
public class VerticalLoopingBackground : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private Sprite scrollSprite;

    [Header("Motion")]
    [SerializeField] private float scrollSpeed = 0.9f;

    [Header("Layout")]
    [SerializeField] private float targetWidthWorld = 8f;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = -50;
    [SerializeField] private float zOffset = 0f;

    private Transform tileA;
    private Transform tileB;
    private float tileHeight;

    private void Awake()
    {
        if (scrollSprite == null)
        {
            enabled = false;
            return;
        }

        EnsureTiles();
        ApplySpriteAndStyle();
        ResetTilePositions();
    }

    private void Update()
    {
        if (tileA == null || tileB == null || tileHeight <= 0f)
        {
            return;
        }

        float dy = scrollSpeed * Time.deltaTime;
        tileA.localPosition += Vector3.down * dy;
        tileB.localPosition += Vector3.down * dy;

        // When a tile moves fully below the origin, place it above the other tile.
        if (tileA.localPosition.y <= -tileHeight)
        {
            tileA.localPosition = new Vector3(0f, tileB.localPosition.y + tileHeight, zOffset);
        }
        if (tileB.localPosition.y <= -tileHeight)
        {
            tileB.localPosition = new Vector3(0f, tileA.localPosition.y + tileHeight, zOffset);
        }
    }

    private void EnsureTiles()
    {
        tileA = transform.Find("LoopTile_A");
        tileB = transform.Find("LoopTile_B");

        if (tileA == null)
        {
            var go = new GameObject("LoopTile_A");
            go.transform.SetParent(transform, false);
            tileA = go.transform;
            go.AddComponent<SpriteRenderer>();
        }

        if (tileB == null)
        {
            var go = new GameObject("LoopTile_B");
            go.transform.SetParent(transform, false);
            tileB = go.transform;
            go.AddComponent<SpriteRenderer>();
        }
    }

    private void ApplySpriteAndStyle()
    {
        var srA = tileA.GetComponent<SpriteRenderer>();
        var srB = tileB.GetComponent<SpriteRenderer>();

        srA.sprite = scrollSprite;
        srB.sprite = scrollSprite;

        srA.sortingLayerName = sortingLayerName;
        srB.sortingLayerName = sortingLayerName;
        srA.sortingOrder = sortingOrder;
        srB.sortingOrder = sortingOrder;

        float spriteWidth = scrollSprite.bounds.size.x;
        float spriteHeight = scrollSprite.bounds.size.y;
        if (spriteWidth <= 0f || spriteHeight <= 0f)
        {
            tileHeight = 0f;
            return;
        }

        float scale = targetWidthWorld > 0f ? targetWidthWorld / spriteWidth : 1f;
        Vector3 localScale = new Vector3(scale, scale, 1f);
        tileA.localScale = localScale;
        tileB.localScale = localScale;

        tileHeight = spriteHeight * scale;
    }

    private void ResetTilePositions()
    {
        tileA.localPosition = new Vector3(0f, 0f, zOffset);
        tileB.localPosition = new Vector3(0f, tileHeight, zOffset);
    }
}
