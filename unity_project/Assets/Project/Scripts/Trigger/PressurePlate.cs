using UnityEngine;

/// <summary>
/// A floor tile that opens one or more LockedDoors when the player steps on it.
/// Extends GridZoneTriggerBase to reuse the grid-zone detection logic.
/// The plate size defaults to 1×1 (single cell).
/// </summary>
public class PressurePlate : GridZoneTriggerBase
{
    [Header("Pressure Plate")]
    [Tooltip("Doors to open when this plate is stepped on.")]
    public LockedDoor[] linkedDoors;

    [Tooltip("Color of the plate before activation.")]
    public Color inactiveColor = new Color(0.9f, 0.75f, 0.1f, 0.7f); // dim yellow

    [Tooltip("Color of the plate after activation.")]
    public Color activeColor = new Color(0.1f, 1f, 0.3f, 0.8f); // bright green

    [Tooltip("Sorting layer for the plate sprite.")]
    public string plateSortingLayer = "Items";

    private SpriteRenderer sr;
    private bool activated;

    private void Awake()
    {
        // Default to a single-cell trigger
        size = new Vector2Int(1, 1);

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        // Create a simple 1-unit white sprite if none assigned
        if (sr.sprite == null)
            sr.sprite = CreatePlateSprite();

        sr.color = inactiveColor;
        sr.sortingLayerName = plateSortingLayer;
        sr.sortingOrder = 1;
    }

    protected override bool OnTriggered()
    {
        if (activated) return false;
        activated = true;

        sr.color = activeColor;

        if (linkedDoors != null)
        {
            foreach (var door in linkedDoors)
            {
                if (door != null) door.Open();
            }
        }

        return true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = activated ? Color.green : Color.yellow;
        Gizmos.DrawCube(transform.position, new Vector3(0.7f, 0.7f, 0.05f));
    }

    private static Sprite cachedPlateSprite;

    private static Sprite CreatePlateSprite()
    {
        if (cachedPlateSprite != null) return cachedPlateSprite;

        int s = 8;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color32 c = new Color32(255, 255, 255, 255);
        Color32[] px = new Color32[s * s];
        for (int i = 0; i < px.Length; i++) px[i] = c;
        tex.SetPixels32(px);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        cachedPlateSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        return cachedPlateSprite;
    }
}
