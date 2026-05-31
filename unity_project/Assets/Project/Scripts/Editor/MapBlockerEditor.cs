#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Scene-view tool to add/remove walkable cells.
/// Window > Map Blocker Editor
/// </summary>
public class MapBlockerEditor : EditorWindow
{
    private HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();
    private bool editing;
    private bool showOverlay = true;
    private Vector2 origin = new Vector2(-32f, -32f);
    private float tileSize = 1f;
    private Vector2Int mapMin = new Vector2Int(0, 0);
    private Vector2Int mapMax = new Vector2Int(64, 64);
    private bool dirty;

    private const string ScriptPath = "Assets/Project/Scripts/Grid/MapBlockerBootstrap.cs";

    [MenuItem("Window/Map Blocker Editor")]
    public static void Open()
    {
        var w = GetWindow<MapBlockerEditor>("Map Blocker");
        w.Reload();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        Reload();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Blocked cells: " + blocked.Count + (dirty ? " (unsaved)" : ""));
        editing = EditorGUILayout.ToggleLeft("Edit mode (click cells in Scene view to toggle)", editing);
        showOverlay = EditorGUILayout.ToggleLeft("Show overlay", showOverlay);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
        origin = EditorGUILayout.Vector2Field("World origin", origin);
        tileSize = EditorGUILayout.FloatField("Tile size", tileSize);
        mapMin = EditorGUILayout.Vector2IntField("Map min cell", mapMin);
        mapMax = EditorGUILayout.Vector2IntField("Map max cell", mapMax);

        EditorGUILayout.Space();
        if (GUILayout.Button("Reload from script"))
        {
            Reload();
        }
        GUI.enabled = dirty;
        if (GUILayout.Button("Save to script"))
        {
            SaveToScript();
        }
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Red overlay = blocked cell (player can't walk).\n" +
            "In Edit mode: left-click empty cell = add block; left-click blocked cell = remove block.\n" +
            "Hold Shift to box-select. Save to write changes to MapBlockerBootstrap.cs.",
            MessageType.Info);

        SceneView.RepaintAll();
    }

    private void Reload()
    {
        blocked.Clear();
        if (File.Exists(ScriptPath))
        {
            string text = File.ReadAllText(ScriptPath);
            var rx = new System.Text.RegularExpressions.Regex(@"new Vector2Int\(\s*(-?\d+)\s*,\s*(-?\d+)\s*\)");
            foreach (System.Text.RegularExpressions.Match m in rx.Matches(text))
            {
                blocked.Add(new Vector2Int(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)));
            }
        }
        dirty = false;
        Repaint();
    }

    private void SaveToScript()
    {
        // Sort by x then y for stable diffs
        var list = new List<Vector2Int>(blocked);
        list.Sort((a, b) =>
        {
            int c = a.x.CompareTo(b.x);
            return c != 0 ? c : a.y.CompareTo(b.y);
        });

        var sb = new StringBuilder();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        sb.AppendLine("[DefaultExecutionOrder(50)]");
        sb.AppendLine("public class MapBlockerBootstrap : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    private static readonly Vector2Int[] Cells = new Vector2Int[]");
        sb.AppendLine("    {");
        for (int i = 0; i < list.Count; i++)
        {
            sb.Append("        new Vector2Int(").Append(list[i].x).Append(", ").Append(list[i].y).Append(")");
            if (i < list.Count - 1) sb.Append(",");
            sb.AppendLine();
        }
        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (GridManager.Instance == null) return;");
        sb.AppendLine("        for (int i = 0; i < Cells.Length; i++)");
        sb.AppendLine("            GridManager.Instance.AddBlocked(Cells[i]);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public int CellCount => Cells.Length;");
        sb.AppendLine("}");

        File.WriteAllText(ScriptPath, sb.ToString());
        AssetDatabase.ImportAsset(ScriptPath);
        dirty = false;
        Debug.Log("Saved " + list.Count + " cells to " + ScriptPath);
    }

    private void OnSceneGUI(SceneView sv)
    {
        if (!showOverlay) return;
        Event e = Event.current;

        // Draw overlays for cells within map range
        for (int x = mapMin.x - 1; x <= mapMax.x + 1; x++)
        {
            for (int y = mapMin.y - 1; y <= mapMax.y + 1; y++)
            {
                var c = new Vector2Int(x, y);
                if (!blocked.Contains(c)) continue;
                DrawCell(c, new Color(1f, 0.15f, 0.15f, 0.45f));
            }
        }

        if (!editing) return;

        // Convert mouse position to world cell
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Vector3 mw = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
            int cx = Mathf.RoundToInt((mw.x - origin.x) / tileSize);
            int cy = Mathf.RoundToInt((mw.y - origin.y) / tileSize);
            var cell = new Vector2Int(cx, cy);
            if (blocked.Contains(cell)) blocked.Remove(cell);
            else blocked.Add(cell);
            dirty = true;
            e.Use();
            sv.Repaint();
            Repaint();
        }
    }

    private void DrawCell(Vector2Int cell, Color color)
    {
        Vector3 c = new Vector3(origin.x + cell.x * tileSize, origin.y + cell.y * tileSize, 0f);
        float half = tileSize * 0.48f;
        Vector3[] verts = new Vector3[]
        {
            new Vector3(c.x - half, c.y - half, 0),
            new Vector3(c.x + half, c.y - half, 0),
            new Vector3(c.x + half, c.y + half, 0),
            new Vector3(c.x - half, c.y + half, 0),
        };
        Handles.DrawSolidRectangleWithOutline(verts, color, new Color(color.r, color.g, color.b, 1f));
    }
}
#endif
