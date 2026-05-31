using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class ElevatorLoopBackgroundRuntime : MonoBehaviour
{
    [Header("Texture")]
    [SerializeField] private string resourcesTexturePath = "Boss/ElevatorLoop";

    [Header("Scroll")]
    [SerializeField] private float scrollSpeed = 1.35f;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = -200;
    [SerializeField] private float zPosition = 8f;

    private Transform tileA;
    private Transform tileB;
    private float tileHeight;

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void EnsureEditorBackground()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying) return;
            var scene = SceneManager.GetActiveScene();
            if (scene.name == "Boss01_Elevator")
            {
                if (FindObjectOfType<ElevatorLoopBackgroundRuntime>() == null)
                {
                    GameObject root = new GameObject("ElevatorLoopBackground");
                    root.AddComponent<ElevatorLoopBackgroundRuntime>();
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
        };
    }
#endif

    // ---------------------------------------------------------------
    // RuntimeInitializeOnLoadMethod → sceneLoaded 이벤트로 전환.
    // 씬 재로드(리트라이) 시에도 배경이 다시 생성됩니다.
    // ---------------------------------------------------------------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Boss01_Elevator")
            return;

        if (FindObjectOfType<ElevatorLoopBackgroundRuntime>() != null)
            return;

        GameObject root = new GameObject("ElevatorLoopBackground");
        root.AddComponent<ElevatorLoopBackgroundRuntime>();
    }

    private void OnEnable()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        Texture2D tex = Resources.Load<Texture2D>(resourcesTexturePath);
        if (tex == null)
        {
            Debug.LogWarning("[ElevatorLoopBackgroundRuntime] Missing texture at Resources/" + resourcesTexturePath);
            enabled = false;
            return;
        }

        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic)
        {
            Debug.LogWarning("[ElevatorLoopBackgroundRuntime] Main Camera missing or not orthographic.");
            enabled = false;
            return;
        }

        float spriteWorldWidth = sprite.bounds.size.x;
        float spriteWorldHeight = sprite.bounds.size.y;

        float cameraWorldWidth = cam.orthographicSize * 2f * cam.aspect;
        // Multiply by 1.5f to ensure the background is wide enough to cover camera panning
        float uniformScale = (cameraWorldWidth / Mathf.Max(0.0001f, spriteWorldWidth)) * 1.5f;
        tileHeight = spriteWorldHeight * uniformScale;

        tileA = CreateTile("TileA", sprite, uniformScale, 0f);
        tileB = CreateTile("TileB", sprite, uniformScale, tileHeight);
    }

    private void Update()
    {
        if (tileA == null || tileB == null)
            return;

        float dy = scrollSpeed * Time.deltaTime;
        tileA.position += Vector3.down * dy;
        tileB.position += Vector3.down * dy;

        Camera cam = Camera.main;
        float camY = cam != null ? cam.transform.position.y : 0f;

        // Use strict < and a camera-based threshold to prevent the two tiles from infinitely leapfrogging each other
        if (tileA.position.y < camY - tileHeight)
        {
            tileA.position = new Vector3(tileA.position.x, tileB.position.y + tileHeight, tileA.position.z);
        }
        else if (tileB.position.y < camY - tileHeight)
        {
            tileB.position = new Vector3(tileB.position.x, tileA.position.y + tileHeight, tileB.position.z);
        }
    }

    private Transform CreateTile(string name, Sprite sprite, float uniformScale, float y)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(0f, y, zPosition);
        go.transform.localScale = Vector3.one * uniformScale;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;

        return go.transform;
    }
}
