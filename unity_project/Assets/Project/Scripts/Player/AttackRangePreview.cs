using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shows semi-transparent yellow tiles on attack cells ONLY when the player attacks.
/// Tiles flash briefly then disappear.
/// </summary>
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(GridOccupant))]
public class AttackRangePreview : MonoBehaviour
{
    [Header("Flash Visuals")]
    [Tooltip("Overlay color when attack fires.")]
    public Color flashColor = new Color(1f, 1f, 0f, 0.45f);

    [Tooltip("How long the flash stays visible (seconds).")]
    public float flashDuration = 0.18f;

    [Tooltip("Sorting layer for the flash quads.")]
    public string sortingLayerName = "Items";
    public int sortingOrder = 10;

    [Tooltip("Quad scale relative to one grid tile.")]
    public float tileScale = 1.0f;

    private PlayerCombat combat;
    private PlayerController controller;
    private GridOccupant occupant;

    private readonly List<GameObject> pool = new List<GameObject>();
    private Sprite cachedSprite;
    private Coroutine flashRoutine;

    private void Awake()
    {
        if (!ShouldRunThisPreview())
        {
            enabled = false;
            return;
        }

        combat = GetComponent<PlayerCombat>();
        controller = GetComponent<PlayerController>();
        occupant = GetComponent<GridOccupant>();
    }

    private void Update()
    {
        if (combat == null || controller == null || occupant == null || !combat.enabled)
        {
            return;
        }

        // Trigger flash on the same input the combat script uses
        if (Input.GetMouseButtonDown(0))
        {
            ShowFlash();
        }
    }

    /// <summary>
    /// Display the attack range tiles briefly.
    /// </summary>
    public void ShowFlash()
    {
        if (GridManager.Instance == null) return;

        Vector2Int origin = occupant.CurrentCell;
        Vector2Int facing = controller.Facing;
        List<Vector2Int> cells = combat.GetAttackCells(origin, facing);

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine(cells));
    }

    private IEnumerator FlashRoutine(List<Vector2Int> cells)
    {
        // Grow pool if needed
        while (pool.Count < cells.Count)
            pool.Add(CreateQuad(pool.Count));

        // Show quads at attack positions
        for (int i = 0; i < cells.Count; i++)
        {
            Vector3 pos = GridManager.Instance.CellToWorld(cells[i]);
            pos.z = 0f;
            pool[i].transform.position = pos;
            float gt = GridManager.Instance != null ? GridManager.Instance.tileSize : 1f;
            pool[i].transform.localScale = new Vector3(tileScale * gt, tileScale * gt, 1f);
            pool[i].SetActive(true);
        }

        // Hide extras
        for (int i = cells.Count; i < pool.Count; i++)
            pool[i].SetActive(false);

        // Wait then hide all
        yield return new WaitForSeconds(flashDuration);

        HideAll();
        flashRoutine = null;
    }

    private void HideAll()
    {
        foreach (var q in pool)
            if (q != null) q.SetActive(false);
    }

    private GameObject CreateQuad(int index)
    {
        GameObject go = new GameObject($"AtkFlash_{index}");
        // Do NOT parent to Player — Player has localScale != 1, which would shrink the indicator.
        // Keep in world space so quads stay exactly one tile in size.

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSprite();
        sr.color = flashColor;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;

        float gridTile = GridManager.Instance != null ? GridManager.Instance.tileSize : 1f;
        go.transform.localScale = new Vector3(tileScale * gridTile, tileScale * gridTile, 1f);
        go.SetActive(false);
        return go;
    }

    private Sprite GetSprite()
    {
        if (cachedSprite != null) return cachedSprite;

        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color32 w = new Color32(255, 255, 255, 255);
        Color32[] px = new Color32[16];
        for (int i = 0; i < 16; i++) px[i] = w;
        tex.SetPixels32(px);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        cachedSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return cachedSprite;
    }

    private bool ShouldRunThisPreview()
    {
        AttackRangePreview[] previews = GetComponents<AttackRangePreview>();
        return previews == null || previews.Length == 0 || previews[0] == this;
    }

    private void OnDestroy()
    {
        if (cachedSprite != null)
        {
            Destroy(cachedSprite.texture);
            Destroy(cachedSprite);
        }
    }
}
