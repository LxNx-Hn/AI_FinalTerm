using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates colored placeholder sprites and assigns them to every prefab that
/// needs a visible sprite for playtesting. Run once (or after a clean checkout):
///   Project Tools → Create Placeholder Sprites
/// </summary>
public static class PlaceholderSpriteCreator
{
    const string OUT_DIR = "Assets/Project/Art/Placeholders";

    // ── Entry point ─────────────────────────────────────────────────────────────
    [MenuItem("Project Tools/Create Placeholder Sprites")]
    public static void CreateAll()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Project/Art/Placeholders"));

        // ── 1. Generate sprite PNGs ─────────────────────────────────────────────
        MakeSprite("Player_PH",       32, 32,
            new Color32(0x5B, 0xD3, 0xFF, 0xFF),   // cyan body
            new Color32(0x00, 0x7A, 0xCC, 0xFF));   // dark-cyan border

        MakeSprite("SmallEnemy_PH",   32, 32,
            new Color32(0xFF, 0x44, 0x44, 0xFF),    // red body
            new Color32(0xAA, 0x00, 0x00, 0xFF));   // dark-red border

        MakeSprite("MediumEnemy_PH",  48, 48,
            new Color32(0xCC, 0x55, 0x00, 0xFF),    // orange body
            new Color32(0x88, 0x22, 0x00, 0xFF));   // dark-orange border

        MakeSprite("Boss_PH",         64, 64,
            new Color32(0xAA, 0x22, 0xFF, 0xFF),    // purple body
            new Color32(0x55, 0x00, 0xAA, 0xFF));   // dark-purple border

        MakeSprite("WarningTile_PH",  32, 32,
            new Color32(0xFF, 0xEE, 0x00, 0xAA),    // yellow semi-transparent
            new Color32(0xFF, 0x99, 0x00, 0xFF));

        MakeSprite("DamageTile_PH",   32, 32,
            new Color32(0xFF, 0x22, 0x22, 0xAA),    // red semi-transparent
            new Color32(0xCC, 0x00, 0x00, 0xFF));

        MakeSprite("SafeTile_PH",     32, 32,
            new Color32(0x22, 0xFF, 0x66, 0xAA),    // green semi-transparent
            new Color32(0x00, 0xAA, 0x33, 0xFF));

        MakeSprite("Wall_PH",         32, 32,
            new Color32(0x55, 0x55, 0x55, 0xFF),    // dark-gray body
            new Color32(0x22, 0x22, 0x22, 0xFF));   // near-black border

        MakeSprite("HealItem_PH",     24, 24,
            new Color32(0x44, 0xFF, 0x88, 0xFF),    // mint-green body
            new Color32(0x00, 0x99, 0x33, 0xFF));

        MakeSprite("KeyCard_PH",      24, 24,
            new Color32(0xFF, 0xFF, 0x55, 0xFF),    // bright-yellow body
            new Color32(0xAA, 0x88, 0x00, 0xFF));

        MakeSprite("AttackHitbox_PH", 32, 32,
            new Color32(0xFF, 0xEE, 0x00, 0x88),    // semi-transparent yellow body
            new Color32(0xFF, 0xCC, 0x00, 0xFF));   // strong yellow border

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── 2. Assign sprites to prefabs ─────────────────────────────────────────
        Assign("Assets/Project/Prefabs/Player/Player.prefab",
               "Player_PH",       "Characters", 0);
        Assign("Assets/Project/Prefabs/Enemy/SmallEnemy.prefab",
               "SmallEnemy_PH",   "Characters", 0);
        Assign("Assets/Project/Prefabs/Enemy/MediumEnemy.prefab",
               "MediumEnemy_PH",  "Characters", 1);
        Assign("Assets/Project/Prefabs/Pattern/WarningTile.prefab",
               "WarningTile_PH",  "PatternWarning", 0);
        Assign("Assets/Project/Prefabs/Pattern/DamageTile.prefab",
               "DamageTile_PH",   "Hazard",         0);
        Assign("Assets/Project/Prefabs/Pattern/SafeTile.prefab",
               "SafeTile_PH",     "Floor",           1);
        Assign("Assets/Project/Prefabs/System/Wall.prefab",
               "Wall_PH",         "Props",           0);
        Assign("Assets/Project/Prefabs/Item/HealItem.prefab",
               "HealItem_PH",     "Items",           0);
        Assign("Assets/Project/Prefabs/Item/KeyCard.prefab",
               "KeyCard_PH",      "Items",           0);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PlaceholderSpriteCreator] ✅ Done — all placeholder sprites created and assigned.");
    }

    // ── Public API (called from BoxPrototypeBuilder for in-scene boss) ──────────

    /// <summary>Returns the sprite at the given name, or null.</summary>
    public static Sprite GetSprite(string spriteName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{OUT_DIR}/{spriteName}.png");
    }

    // ── PNG generation ───────────────────────────────────────────────────────────

    static void MakeSprite(string name, int w, int h, Color32 fill, Color32 border)
    {
        string assetPath = $"{OUT_DIR}/{name}.png";
        string fullPath  = Path.Combine(Application.dataPath, $"Project/Art/Placeholders/{name}.png");

        // Always regenerate so colors are up-to-date
        var tex    = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = (x == 0 || x == w - 1 || y == 0 || y == h - 1) ? border : fill;
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(assetPath);
        var imp = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        imp.textureType              = TextureImporterType.Sprite;
        imp.spritePixelsPerUnit      = 32;
        imp.filterMode               = FilterMode.Point;
        imp.textureCompression       = TextureImporterCompression.Uncompressed;
        imp.alphaIsTransparency      = true;

        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spritePivot     = new Vector2(0.5f, 0.5f);
        s.spriteAlignment = (int)SpriteAlignment.Center;
        imp.SetTextureSettings(s);
        EditorUtility.SetDirty(imp);
        imp.SaveAndReimport();
    }

    // ── Prefab assignment ─────────────────────────────────────────────────────────

    static void Assign(string prefabPath, string spriteName, string sortingLayer, int sortingOrder)
    {
        var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (pfb == null)
        {
            Debug.LogWarning($"[Placeholder] Prefab not found: {prefabPath}");
            return;
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{OUT_DIR}/{spriteName}.png");
        if (sprite == null)
        {
            Debug.LogWarning($"[Placeholder] Sprite not found: {spriteName}");
            return;
        }

        using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
        var root = scope.prefabContentsRoot;

        var sr = root.GetComponent<SpriteRenderer>();
        if (sr == null) sr = root.AddComponent<SpriteRenderer>();

        sr.sprite           = sprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder     = sortingOrder;
    }
}
