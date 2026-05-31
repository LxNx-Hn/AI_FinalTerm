using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MonsterSpawnScareEffect : MonoBehaviour
{
    private const string RunnerName = "MonsterSpawnScareEffectRunner";

    private static MonsterSpawnScareEffect instance;
    private static Texture2D ringTexture;

    private Image overlay;
    private Coroutine cameraRoutine;
    private Coroutine overlayRoutine;
    private Vector3 cameraBasePosition;

    public static void Play(GameObject enemy, float intensity = 1f)
    {
        if (enemy == null)
        {
            return;
        }

        MonsterSpawnScareEffect runner = GetOrCreateRunner();
        runner.PlayInternal(enemy, Mathf.Max(0.1f, intensity));
    }

    private static MonsterSpawnScareEffect GetOrCreateRunner()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject runnerObject = GameObject.Find(RunnerName);
        if (runnerObject == null)
        {
            runnerObject = new GameObject(RunnerName);
            DontDestroyOnLoad(runnerObject);
        }

        instance = runnerObject.GetComponent<MonsterSpawnScareEffect>();
        if (instance == null)
        {
            instance = runnerObject.AddComponent<MonsterSpawnScareEffect>();
        }

        instance.EnsureOverlay();
        return instance;
    }

    private void PlayInternal(GameObject enemy, float intensity)
    {
        StartCoroutine(EnemyPulseRoutine(enemy.transform, intensity));

        SpriteRenderer renderer = enemy.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
        {
            StartCoroutine(SpriteFlickerRoutine(renderer, intensity));
        }

        if (overlayRoutine != null)
        {
            StopCoroutine(overlayRoutine);
        }
        overlayRoutine = StartCoroutine(OverlayPulseRoutine(intensity));

        Camera cam = Camera.main;
        if (cam != null)
        {
            if (cameraRoutine != null)
            {
                StopCoroutine(cameraRoutine);
                cam.transform.position = cameraBasePosition;
            }

            cameraRoutine = StartCoroutine(CameraShakeRoutine(cam, intensity));
        }
    }

    private void EnsureOverlay()
    {
        if (overlay != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("MonsterScareOverlayCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject overlayObject = new GameObject("MonsterScareOverlay");
        overlayObject.transform.SetParent(canvasObject.transform, false);

        overlay = overlayObject.AddComponent<Image>();
        overlay.color = Color.clear;
        overlay.raycastTarget = false;

        RectTransform rect = overlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private IEnumerator EnemyPulseRoutine(Transform target, float intensity)
    {
        if (target == null)
        {
            yield break;
        }

        GameObject ring = new GameObject("MonsterSpawnScarePulse");
        ring.transform.position = target.position + new Vector3(0f, 0f, -0.05f);

        SpriteRenderer ringRenderer = ring.AddComponent<SpriteRenderer>();
        ringRenderer.sprite = GetRingSprite();
        ringRenderer.sortingLayerName = "Hazard";
        ringRenderer.sortingOrder = 250;

        float duration = 0.45f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 0.25f;
        Vector3 endScale = Vector3.one * (2.2f + intensity * 0.4f);
        Color startColor = new Color(0.85f, 0.02f, 0.02f, 0.72f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            ring.transform.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
            ringRenderer.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));

            yield return null;
        }

        Destroy(ring);
    }

    private IEnumerator SpriteFlickerRoutine(SpriteRenderer renderer, float intensity)
    {
        if (renderer == null)
        {
            yield break;
        }

        Transform target = renderer.transform;
        Color originalColor = renderer.color;
        Vector3 originalScale = target.localScale;
        Vector3 popScale = originalScale * (1.18f + intensity * 0.04f);

        float duration = 0.34f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(t * Mathf.PI);
            float flicker = Mathf.Sin(t * Mathf.PI * 12f) > 0f ? 1f : 0f;

            target.localScale = Vector3.LerpUnclamped(originalScale, popScale, pulse);
            renderer.color = Color.Lerp(originalColor, new Color(1f, 0.15f, 0.1f, originalColor.a), flicker * 0.55f);

            yield return null;
        }

        if (target != null)
        {
            target.localScale = originalScale;
        }

        if (renderer != null)
        {
            renderer.color = originalColor;
        }
    }

    private IEnumerator OverlayPulseRoutine(float intensity)
    {
        EnsureOverlay();

        float duration = 0.38f;
        float elapsed = 0f;
        float peakAlpha = Mathf.Clamp01(0.22f + intensity * 0.08f);
        Color color = new Color(0.12f, 0f, 0f, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Sin(t * Mathf.PI) * peakAlpha;
            overlay.color = new Color(color.r, color.g, color.b, alpha);

            yield return null;
        }

        overlay.color = Color.clear;
        overlayRoutine = null;
    }

    private IEnumerator CameraShakeRoutine(Camera cam, float intensity)
    {
        Transform camTransform = cam.transform;
        cameraBasePosition = camTransform.position;

        float duration = 0.24f;
        float elapsed = 0f;
        float strength = 0.12f + intensity * 0.04f;

        while (elapsed < duration && camTransform != null)
        {
            elapsed += Time.deltaTime;
            float fade = 1f - Mathf.Clamp01(elapsed / duration);
            Vector2 offset = Random.insideUnitCircle * strength * fade;
            camTransform.position = cameraBasePosition + new Vector3(offset.x, offset.y, 0f);

            yield return null;
        }

        if (camTransform != null)
        {
            camTransform.position = cameraBasePosition;
        }

        cameraRoutine = null;
    }

    private static Sprite GetRingSprite()
    {
        if (ringTexture == null)
        {
            const int size = 96;
            const float inner = 0.34f;
            const float outer = 0.48f;
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / size;
                    float alpha = Mathf.SmoothStep(0f, 1f, distance - inner) * Mathf.SmoothStep(0f, 1f, outer - distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha * 5f));
                }
            }

            ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            ringTexture.name = "RuntimeMonsterSpawnScareRing";
            ringTexture.filterMode = FilterMode.Bilinear;
            ringTexture.wrapMode = TextureWrapMode.Clamp;
            ringTexture.SetPixels(pixels);
            ringTexture.Apply();
        }

        return Sprite.Create(ringTexture, new Rect(0f, 0f, ringTexture.width, ringTexture.height), new Vector2(0.5f, 0.5f), ringTexture.width);
    }
}
