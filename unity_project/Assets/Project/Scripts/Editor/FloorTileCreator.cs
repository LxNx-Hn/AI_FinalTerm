using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;

public static class FloorTileCreator
{
    [MenuItem("Project Tools/Create FloorTile Placeholders")]
    public static void CreateFloorTilePlaceholders()
    {
        CreateTile("FloorTile_A", new Color32(0x7A, 0x7A, 0x7A, 0xFF), new Color32(0x40, 0x40, 0x40, 0xFF));
        CreateTile("FloorTile_B", new Color32(0x85, 0x85, 0x85, 0xFF), new Color32(0x40, 0x40, 0x40, 0xFF));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FloorTileCreator] FloorTile_A and FloorTile_B created (PNG + Tile asset + Prefab).");
    }

    private static void CreateTile(string name, Color32 fill, Color32 border)
    {
        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x == 0 || x == size - 1 || y == 0 || y == size - 1;
                pixels[y * size + x] = isBorder ? border : fill;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        string texPath = $"Assets/Project/Art/Tiles/{name}.png";
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(texPath));
        File.WriteAllBytes(texPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(texPath);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spritePivot = new Vector2(0.5f, 0.5f);
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        importer.SetTextureSettings(settings);
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);

        // Create Tile ScriptableObject for Tilemap use
        string tileAssetPath = $"Assets/Project/Art/Tiles/{name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(tileAssetPath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            AssetDatabase.CreateAsset(tile, tileAssetPath);
        }
        else
        {
            tile.sprite = sprite;
            EditorUtility.SetDirty(tile);
        }

        string prefabPath = $"Assets/Project/Prefabs/Tiles/{name}.prefab";
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(prefabPath));
        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Floor";
        sr.sortingOrder = 0;

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
    }
}
