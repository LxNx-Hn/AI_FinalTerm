using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Stage01AtmosphereRuntime : MonoBehaviour
{
    private const string SceneName = "Stage01_ParkingToHospital";
    private const string BgmResourcePath = "Stage01/Audio/Stage01_BGM";
    private const string RuntimeName = "Stage01AtmosphereRuntime";
    private const string VignetteCanvasName = "Stage01_DistanceDarkness";
    private const string PropContainerName = "Stage01_RuntimeVisualBlockers";
    private const string ActualBlockerContainerName = "Stage01_RuntimeRouteBlockers";
    private const string DecalContainerName = "Stage01_RuntimeExtraDecals";
    private const string EmergencyLightContainerName = "Stage01_HospitalEmergencyLights";

    [SerializeField] private float bgmVolume = 0.9f;
    [SerializeField] private float footstepVolume = 0.48f;
    [SerializeField] private float vignetteEdgeAlpha = 0.52f;

    private AudioSource bgmSource;

    private static readonly VisualBlockerSpec[] VisualBlockers =
    {
        new VisualBlockerSpec("HospitalApproach_Barricade", new Vector2Int(30, 23), 1.05f),
        new VisualBlockerSpec("HospitalApproach_LongBarricade", new Vector2Int(36, 23), 1.0f),
        new VisualBlockerSpec("HospitalApproach_TirePile", new Vector2Int(42, 26), 1.05f),
        new VisualBlockerSpec("Start_TirePile_A", new Vector2Int(15, 23), 1.0f),
        new VisualBlockerSpec("UpperRight_Barricade", new Vector2Int(47, 26), 1.0f),
        new VisualBlockerSpec("HospitalApproach_WreckedCar", new Vector2Int(44, 19), 0.92f),
        new VisualBlockerSpec("LeftLot_WreckedCar", new Vector2Int(20, 32), 0.92f),
        new VisualBlockerSpec("FallenLight_BottomCenter", new Vector2Int(32, 12), 1.0f),
        new VisualBlockerSpec("FallenLight_BottomRight", new Vector2Int(43, 13), 1.0f),
        new VisualBlockerSpec("LeftLot_BloodyBed", new Vector2Int(18, 26), 0.95f),
        new VisualBlockerSpec("HospitalApproach_CarH", new Vector2Int(30, 36), 0.92f),
    };

    private static readonly string[] PrePlacedItemNames =
    {
        "Item_Heal_StartRoute",
        "Item_Speed_CenterBypass",
        "Item_Attack_HospitalApproach",
        "Item_Heal_BeforeEntrance",
    };

    private static readonly VisualBlockerSpec[] RouteBlockers =
    {
        new VisualBlockerSpec("Start_Barricade_A", new Vector2Int(50, 16), 0.9f),
        new VisualBlockerSpec("RoadBlock_Barricade_B", new Vector2Int(24, 21), 0.95f),
        new VisualBlockerSpec("HospitalApproach_Barricade_Right", new Vector2Int(37, 30), 0.95f),
        new VisualBlockerSpec("HospitalApproach_TirePile", new Vector2Int(31, 30), 0.9f),
        new VisualBlockerSpec("UpperRight_Bed", new Vector2Int(49, 24), 0.95f),
        new VisualBlockerSpec("FallenLight_BottomCenter", new Vector2Int(35, 14), 0.95f),
        new VisualBlockerSpec("HospitalApproach_ConeRow", new Vector2Int(33, 29), 0.95f),
        new VisualBlockerSpec("LeftLot_BloodyBed", new Vector2Int(19, 25), 0.9f),
    };

    private static readonly VisualBlockerSpec[] ExtraDecals =
    {
        new VisualBlockerSpec("Decal_Blood_01", new Vector2Int(51, 8), 1.15f),
        new VisualBlockerSpec("Decal_Glass_02", new Vector2Int(48, 13), 1.05f),
        new VisualBlockerSpec("Decal_Blood_10", new Vector2Int(27, 22), 1.05f),
        new VisualBlockerSpec("Decal_Eye_01", new Vector2Int(25, 26), 0.9f),
        new VisualBlockerSpec("Decal_Blood_14", new Vector2Int(34, 31), 1.15f),
        new VisualBlockerSpec("Decal_Glass_05", new Vector2Int(38, 35), 1.0f),
        new VisualBlockerSpec("Decal_Shoe_03", new Vector2Int(42, 28), 0.95f),
        new VisualBlockerSpec("Decal_Blood_18", new Vector2Int(45, 18), 1.05f),
        new VisualBlockerSpec("Decal_Blood_03", new Vector2Int(29, 24), 1.1f),
        new VisualBlockerSpec("Decal_Blood_06", new Vector2Int(23, 33), 1.0f),
        new VisualBlockerSpec("Decal_Glass_06", new Vector2Int(16, 31), 1.0f),
        new VisualBlockerSpec("Decal_Blood_17", new Vector2Int(48, 27), 1.05f),
        new VisualBlockerSpec("Decal_Eye_05", new Vector2Int(21, 30), 0.9f),
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneName || FindFirstObjectByType<Stage01AtmosphereRuntime>() != null)
        {
            return;
        }

        new GameObject(RuntimeName).AddComponent<Stage01AtmosphereRuntime>();
    }

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name == SceneName)
        {
            RemovePrePlacedItems();
        }
    }

    private IEnumerator Start()
    {
        yield return null;
        yield return null;

        if (SceneManager.GetActiveScene().name != SceneName)
        {
            Destroy(gameObject);
            yield break;
        }

        ApplyAudio();
        AlignPlayerVisual();
        EnsureVignette();
        AddVisualBlockers();
        AddRouteBlockers();
        AddExtraDecals();
        EnsureHospitalEmergencyLights();
    }

    private void RemovePrePlacedItems()
    {
        foreach (string itemName in PrePlacedItemNames)
        {
            GameObject item = GameObject.Find(itemName);
            if (item == null)
            {
                continue;
            }

            GridOccupant occupant = item.GetComponent<GridOccupant>();
            if (occupant != null)
            {
                occupant.Release();
            }

            Destroy(item);
        }
    }

    private void AlignPlayerVisual()
    {
        PlayerVisualAnimator2D playerVisual = FindFirstObjectByType<PlayerVisualAnimator2D>();
        if (playerVisual != null)
        {
            playerVisual.SetVisualLocalOffset(new Vector3(0f, -1.15f, 0f));
        }
    }

    private void ApplyAudio()
    {
        AudioClip bgm = Resources.Load<AudioClip>(BgmResourcePath);
        if (bgm != null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.clip = bgm;
            bgmSource.volume = bgmVolume;
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0f;
            bgmSource.priority = 160;
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning("[Stage01AtmosphereRuntime] Missing BGM at Resources/" + BgmResourcePath);
        }

        OneStepOnDirectionKey footsteps = FindFirstObjectByType<OneStepOnDirectionKey>();
        if (footsteps != null)
        {
            footsteps.SetVolume(footstepVolume);
        }
    }

    private void EnsureVignette()
    {
        if (GameObject.Find(VignetteCanvasName) != null)
        {
            return;
        }

        GameObject canvasGo = new GameObject(VignetteCanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -100;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageGo = new GameObject("DistanceVignette", typeof(RectTransform), typeof(Image));
        imageGo.transform.SetParent(canvasGo.transform, false);

        Image image = imageGo.GetComponent<Image>();
        image.sprite = Sprite.Create(CreateVignetteTexture(), new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
        image.type = Image.Type.Simple;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Texture2D CreateVignetteTexture()
    {
        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float inner = size * 0.24f;
        float outer = size * 0.72f;
        Color edge = new Color(0f, 0f, 0f, vignetteEdgeAlpha);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float t = Mathf.InverseLerp(inner, outer, distance);
                t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                texture.SetPixel(x, y, new Color(edge.r, edge.g, edge.b, edge.a * t));
            }
        }

        texture.Apply();
        return texture;
    }

    private void AddVisualBlockers()
    {
        if (GridManager.Instance == null || GameObject.Find(PropContainerName) != null)
        {
            return;
        }

        GameObject container = new GameObject(PropContainerName);
        foreach (VisualBlockerSpec spec in VisualBlockers)
        {
            GameObject template = GameObject.Find(spec.TemplateName);
            if (template == null)
            {
                continue;
            }

            if (!TryFindNearbyBlockedCell(spec.PreferredCell, 3, out Vector2Int cell))
            {
                continue;
            }

            GameObject blocker = Instantiate(template, GridManager.Instance.CellToWorld(cell), template.transform.rotation, container.transform);
            blocker.name = "Stage01_Extra_" + spec.TemplateName;
            blocker.transform.localScale = template.transform.localScale * spec.ScaleMultiplier;
        }
    }

    private void AddRouteBlockers()
    {
        if (GridManager.Instance == null || GameObject.Find(ActualBlockerContainerName) != null)
        {
            return;
        }

        GameObject container = new GameObject(ActualBlockerContainerName);
        foreach (VisualBlockerSpec spec in RouteBlockers)
        {
            GameObject template = GameObject.Find(spec.TemplateName);
            if (template == null)
            {
                continue;
            }

            if (!TryFindSafeWalkableCell(spec.PreferredCell, 4, out Vector2Int cell))
            {
                continue;
            }

            GameObject blocker = Instantiate(template, GridManager.Instance.CellToWorld(cell), template.transform.rotation, container.transform);
            blocker.name = "Stage01_Route_" + spec.TemplateName;
            blocker.transform.localScale = template.transform.localScale * spec.ScaleMultiplier;

            if (blocker.GetComponent<GridBlockedArea>() == null && blocker.GetComponent<GridObstacle>() == null)
            {
                blocker.AddComponent<GridObstacle>();
            }
        }
    }

    private void AddExtraDecals()
    {
        if (GridManager.Instance == null || GameObject.Find(DecalContainerName) != null)
        {
            return;
        }

        GameObject container = new GameObject(DecalContainerName);
        foreach (VisualBlockerSpec spec in ExtraDecals)
        {
            GameObject template = GameObject.Find(spec.TemplateName);
            if (template == null)
            {
                continue;
            }

            Vector3 position = GridManager.Instance.CellToWorld(spec.PreferredCell);
            GameObject decal = Instantiate(template, position, template.transform.rotation, container.transform);
            decal.name = "Stage01_Extra_" + spec.TemplateName;
            decal.transform.localScale = template.transform.localScale * spec.ScaleMultiplier;

            Destroy(decal.GetComponent<GridObstacle>());
            Destroy(decal.GetComponent<GridBlockedArea>());
            Destroy(decal.GetComponent<GridOccupant>());
            Collider2D collider = decal.GetComponent<Collider2D>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }
    }

    private void EnsureHospitalEmergencyLights()
    {
        if (GridManager.Instance == null || GameObject.Find(EmergencyLightContainerName) != null)
        {
            return;
        }

        GameObject container = new GameObject(EmergencyLightContainerName);
        Sprite glowSprite = CreateRadialSprite(192);
        Sprite lampSprite = CreateEmergencyLampSprite(32);

        CreateEmergencySprite(
            "Entrance_RedGlow_Wide",
            glowSprite,
            GridManager.Instance.CellToWorld(new Vector2Int(32, 39)),
            new Vector3(8.5f, 4.4f, 1f),
            new Color(1f, 0.08f, 0.04f, 0.34f),
            31,
            0.12f,
            1.4f,
            0f,
            container.transform);

        CreateEmergencySprite(
            "Entrance_RedGlow_Left",
            glowSprite,
            GridManager.Instance.CellToWorld(new Vector2Int(27, 40)),
            new Vector3(4.2f, 2.7f, 1f),
            new Color(1f, 0.04f, 0.02f, 0.28f),
            31,
            0.16f,
            1.9f,
            0.7f,
            container.transform);

        CreateEmergencySprite(
            "Entrance_RedGlow_Right",
            glowSprite,
            GridManager.Instance.CellToWorld(new Vector2Int(37, 40)),
            new Vector3(4.2f, 2.7f, 1f),
            new Color(1f, 0.04f, 0.02f, 0.28f),
            31,
            0.16f,
            1.9f,
            1.4f,
            container.transform);

        CreateEmergencySprite(
            "Entrance_EmergencyLamp_Left",
            lampSprite,
            GridManager.Instance.CellToWorld(new Vector2Int(28, 42)),
            new Vector3(0.85f, 0.85f, 1f),
            Color.white,
            34,
            0.35f,
            2.6f,
            0f,
            container.transform);

        CreateEmergencySprite(
            "Entrance_EmergencyLamp_Right",
            lampSprite,
            GridManager.Instance.CellToWorld(new Vector2Int(36, 42)),
            new Vector3(0.85f, 0.85f, 1f),
            Color.white,
            34,
            0.35f,
            2.6f,
            1.1f,
            container.transform);
    }

    private static void CreateEmergencySprite(
        string name,
        Sprite sprite,
        Vector3 position,
        Vector3 scale,
        Color color,
        int sortingOrder,
        float pulseAmount,
        float pulseSpeed,
        float phase,
        Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = scale;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;

        Stage01EmergencyLightPulse pulse = go.AddComponent<Stage01EmergencyLightPulse>();
        pulse.Configure(renderer, color, pulseAmount, pulseSpeed, phase);
    }

    private static Sprite CreateRadialSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float t = Mathf.Clamp01(1f - distance / radius);
                t = Mathf.SmoothStep(0f, 1f, t);
                texture.SetPixel(x, y, new Color(1f, 0.05f, 0.02f, t * 0.9f));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    private static Sprite CreateEmergencyLampSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color housing = new Color(0.08f, 0.08f, 0.08f, 1f);
        Color rim = new Color(0.45f, 0.45f, 0.45f, 1f);
        Color red = new Color(1f, 0.05f, 0.02f, 1f);
        Color redHot = new Color(1f, 0.42f, 0.34f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        for (int y = 9; y <= 22; y++)
        {
            for (int x = 8; x <= 23; x++)
            {
                bool edge = x == 8 || x == 23 || y == 9 || y == 22;
                bool hot = x >= 12 && x <= 19 && y >= 12 && y <= 19;
                texture.SetPixel(x, y, edge ? rim : hot ? redHot : red);
            }
        }

        for (int y = 6; y <= 8; y++)
        {
            for (int x = 10; x <= 21; x++)
            {
                texture.SetPixel(x, y, housing);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    private static bool TryFindNearbyBlockedCell(Vector2Int preferred, int radius, out Vector2Int result)
    {
        if (GridManager.Instance != null && GridManager.Instance.IsBlocked(preferred))
        {
            result = preferred;
            return true;
        }

        for (int r = 1; r <= radius; r++)
        {
            for (int y = -r; y <= r; y++)
            {
                for (int x = -r; x <= r; x++)
                {
                    Vector2Int candidate = new Vector2Int(preferred.x + x, preferred.y + y);
                    if (GridManager.Instance != null && GridManager.Instance.IsBlocked(candidate))
                    {
                        result = candidate;
                        return true;
                    }
                }
            }
        }

        result = preferred;
        return false;
    }

    private static bool TryFindSafeWalkableCell(Vector2Int preferred, int radius, out Vector2Int result)
    {
        if (IsSafeRouteBlockerCell(preferred))
        {
            result = preferred;
            return true;
        }

        for (int r = 1; r <= radius; r++)
        {
            for (int y = -r; y <= r; y++)
            {
                for (int x = -r; x <= r; x++)
                {
                    Vector2Int candidate = new Vector2Int(preferred.x + x, preferred.y + y);
                    if (IsSafeRouteBlockerCell(candidate))
                    {
                        result = candidate;
                        return true;
                    }
                }
            }
        }

        result = preferred;
        return false;
    }

    private static bool IsSafeRouteBlockerCell(Vector2Int cell)
    {
        if (GridManager.Instance == null || !GridManager.Instance.IsWalkable(cell, ignoreOccupant: true))
        {
            return false;
        }

        int walkableNeighbors = 0;
        if (GridManager.Instance.IsWalkable(cell + Vector2Int.up, ignoreOccupant: true))
        {
            walkableNeighbors++;
        }

        if (GridManager.Instance.IsWalkable(cell + Vector2Int.down, ignoreOccupant: true))
        {
            walkableNeighbors++;
        }

        if (GridManager.Instance.IsWalkable(cell + Vector2Int.left, ignoreOccupant: true))
        {
            walkableNeighbors++;
        }

        if (GridManager.Instance.IsWalkable(cell + Vector2Int.right, ignoreOccupant: true))
        {
            walkableNeighbors++;
        }

        return walkableNeighbors >= 3;
    }

    private readonly struct VisualBlockerSpec
    {
        public VisualBlockerSpec(string templateName, Vector2Int preferredCell, float scaleMultiplier)
        {
            TemplateName = templateName;
            PreferredCell = preferredCell;
            ScaleMultiplier = scaleMultiplier;
        }

        public string TemplateName { get; }
        public Vector2Int PreferredCell { get; }
        public float ScaleMultiplier { get; }
    }
}

public class Stage01EmergencyLightPulse : MonoBehaviour
{
    private SpriteRenderer target;
    private Color baseColor;
    private Vector3 baseScale;
    private float amount = 0.15f;
    private float speed = 1.5f;
    private float phase;

    public void Configure(SpriteRenderer renderer, Color color, float pulseAmount, float pulseSpeed, float pulsePhase)
    {
        target = renderer;
        baseColor = color;
        baseScale = transform.localScale;
        amount = Mathf.Max(0f, pulseAmount);
        speed = Mathf.Max(0.1f, pulseSpeed);
        phase = pulsePhase;
    }

    private void Awake()
    {
        baseScale = transform.localScale;
        if (target == null)
        {
            target = GetComponent<SpriteRenderer>();
        }

        if (target != null)
        {
            baseColor = target.color;
        }
    }

    private void Update()
    {
        if (target == null)
        {
            return;
        }

        float pulse = (Mathf.Sin((Time.time * speed) + phase) + 1f) * 0.5f;
        float flicker = Mathf.PerlinNoise(Time.time * 8.5f + phase, phase * 3.17f);
        float intensity = 1f + amount * ((pulse * 0.75f) + (flicker * 0.25f));

        Color color = baseColor;
        color.a = Mathf.Clamp01(baseColor.a * intensity);
        target.color = color;
        transform.localScale = baseScale * (1f + amount * 0.08f * pulse);
    }
}
