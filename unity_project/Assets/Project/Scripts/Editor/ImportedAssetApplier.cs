using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot importer that copies extracted assets from _Imported/ into clean
/// project paths, reconfigures their TextureImporter as Sprite, and assigns
/// them to the relevant prefabs.
///
/// Trigger:
///   Manual: Project Tools → Apply Imported Assets
///   Auto:   create file Temp/ApplyImportedAssets.trigger
/// </summary>
[InitializeOnLoad]
public static class ImportedAssetApplier
{
    static readonly string TriggerPath = Path.Combine(
        Path.GetDirectoryName(Application.dataPath),
        "Temp", "ApplyImportedAssets.trigger");

    const string OUT_CHAR = "Assets/Project/Art/Sprites/Characters";
    const string OUT_ITEM = "Assets/Project/Art/Sprites/Items";

    static ImportedAssetApplier()
    {
        EditorApplication.delayCall += CheckTrigger;
    }

    static void CheckTrigger()
    {
        if (!File.Exists(TriggerPath)) return;
        try
        {
            File.Delete(TriggerPath);
            Debug.Log("[ImportedAssetApplier] Trigger detected — running ApplyAll()");
            ApplyAll();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ImportedAssetApplier] Failed: {ex.Message}");
        }
    }

    [MenuItem("Project Tools/Apply Imported Assets")]
    public static void ApplyAll()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Project/Art/Sprites/Characters"));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Project/Art/Sprites/Items"));

        // ── Player idle ────────────────────────────────────────────────────────
        // Keep center pivot to match existing placeholder convention; FootHitbox
        // child anchors damage detection separately.
        ApplySprite(
            srcPath:    "Assets/Project/Art/_Imported/Evelyn/evelyn sprites/idle.png",
            dstPath:    $"{OUT_CHAR}/Evelyn_Idle.png",
            prefabPath: "Assets/Project/Prefabs/Player/Player.prefab",
            ppu:        32f,
            pivot:      new Vector2(0.5f, 0.5f),
            sortingLayer: "Characters",
            sortingOrder: 0);

        // ── Items ──────────────────────────────────────────────────────────────
        ApplySprite(
            srcPath:    "Assets/Project/Art/_Imported/NewFolder/새 폴더/potion.png",
            dstPath:    $"{OUT_ITEM}/Potion.png",
            prefabPath: "Assets/Project/Prefabs/Item/HealItem.prefab",
            ppu:        32f,
            pivot:      new Vector2(0.5f, 0.5f),
            sortingLayer: "Items",
            sortingOrder: 0);

        ApplySprite(
            srcPath:    "Assets/Project/Art/_Imported/NewFolder/새 폴더/speed.png",
            dstPath:    $"{OUT_ITEM}/Speed.png",
            prefabPath: "Assets/Project/Prefabs/Item/SpeedBuffItem.prefab",
            ppu:        32f,
            pivot:      new Vector2(0.5f, 0.5f),
            sortingLayer: "Items",
            sortingOrder: 0);

        ApplySprite(
            srcPath:    "Assets/Project/Art/_Imported/NewFolder/새 폴더/strongstrong.png",
            dstPath:    $"{OUT_ITEM}/AttackBuff.png",
            prefabPath: "Assets/Project/Prefabs/Item/AttackBuffItem.prefab",
            ppu:        32f,
            pivot:      new Vector2(0.5f, 0.5f),
            sortingLayer: "Items",
            sortingOrder: 0);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ImportedAssetApplier] ✅ Done.");
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    static void ApplySprite(string srcPath, string dstPath, string prefabPath,
                            float ppu, Vector2 pivot,
                            string sortingLayer, int sortingOrder)
    {
        // 1. Copy source PNG into clean destination path (overwrite each run)
        var projectRoot = Path.GetDirectoryName(Application.dataPath);
        var srcFull     = Path.Combine(projectRoot, srcPath);
        var dstFull     = Path.Combine(projectRoot, dstPath);

        if (!File.Exists(srcFull))
        {
            Debug.LogWarning($"[Imported] source missing: {srcPath}");
            return;
        }

        File.Copy(srcFull, dstFull, true);
        AssetDatabase.ImportAsset(dstPath);

        // 2. Configure as Sprite
        var imp = (TextureImporter)AssetImporter.GetAtPath(dstPath);
        if (imp == null)
        {
            Debug.LogWarning($"[Imported] no importer for {dstPath}");
            return;
        }

        imp.textureType         = TextureImporterType.Sprite;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode          = FilterMode.Point;
        imp.textureCompression  = TextureImporterCompression.Uncompressed;
        imp.alphaIsTransparency = true;

        var settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        settings.spritePivot     = pivot;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        imp.SetTextureSettings(settings);

        EditorUtility.SetDirty(imp);
        imp.SaveAndReimport();

        // 3. Assign to prefab
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(dstPath);
        if (sprite == null)
        {
            Debug.LogWarning($"[Imported] sprite load failed: {dstPath}");
            return;
        }

        if (!File.Exists(Path.Combine(projectRoot, prefabPath)))
        {
            Debug.LogWarning($"[Imported] prefab missing: {prefabPath}");
            return;
        }

        using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
        var root = scope.prefabContentsRoot;
        var sr   = root.GetComponent<SpriteRenderer>();
        if (sr == null) sr = root.AddComponent<SpriteRenderer>();
        sr.sprite           = sprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder     = sortingOrder;

        Debug.Log($"[Imported] {Path.GetFileNameWithoutExtension(dstPath)} → {Path.GetFileName(prefabPath)}");
    }
}
