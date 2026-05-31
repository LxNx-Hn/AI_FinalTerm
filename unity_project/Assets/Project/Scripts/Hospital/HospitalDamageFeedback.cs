using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[DefaultExecutionOrder(10000)]
public class HospitalDamageFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Volume volume;
    [SerializeField] private CanvasGroup deathScarePanel;
    [SerializeField] private Image deathScareImage;

    [Header("Damage Shake")]
    [SerializeField] private float shakeDuration = 0.18f;
    [SerializeField] private float shakeStrength = 0.24f;
    [SerializeField] private float lowHpShakeMultiplier = 1.45f;

    [Header("Damage Lens")]
    [SerializeField] private float damagePulseDuration = 0.42f;
    [SerializeField] private float vignetteBoost = 0.2f;
    [SerializeField] private float chromaticBoost = 0.32f;
    [SerializeField] private float exposureDrop = -0.32f;

    [Header("Death Flash")]
    [SerializeField] private float deathFlashDuration = 1.2f;
    [SerializeField] private Color deathFlashColor = new Color(0.06f, 0f, 0f, 0.92f);

    private Vignette vignette;
    private ChromaticAberration chromaticAberration;
    private ColorAdjustments colorAdjustments;
    private float baseVignetteIntensity;
    private float baseChromaticIntensity;
    private float basePostExposure;
    private int lastHp = -1;
    private float shakeTimer;
    private float shakeCurrentStrength;
    private float pulseTimer;
    private Vector3 cameraShakeOffset;
    private Coroutine deathRoutine;

    private void Reset()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        targetCamera = Camera.main;
        volume = FindFirstObjectByType<Volume>();
    }

    private void Awake()
    {
        ResolveReferences();
        CacheVolumeDefaults();
        PrepareDeathPanel();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (playerHealth != null)
        {
            lastHp = playerHealth.currentHp;
            playerHealth.onHpChanged.AddListener(OnHpChanged);
            playerHealth.onDead.AddListener(OnDead);
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.onHpChanged.RemoveListener(OnHpChanged);
            playerHealth.onDead.RemoveListener(OnDead);
        }

        RestoreVolumeDefaults();
    }

    private void Update()
    {
        UpdateDamageLens();
    }

    private void LateUpdate()
    {
        UpdateCameraShake();
    }

    private void ResolveReferences()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (volume == null)
        {
            volume = FindFirstObjectByType<Volume>();
        }
    }

    private void CacheVolumeDefaults()
    {
        if (volume == null || volume.profile == null)
        {
            return;
        }

        volume.profile.TryGet(out vignette);
        volume.profile.TryGet(out chromaticAberration);
        volume.profile.TryGet(out colorAdjustments);

        if (vignette != null)
        {
            baseVignetteIntensity = vignette.intensity.value;
        }

        if (chromaticAberration != null)
        {
            baseChromaticIntensity = chromaticAberration.intensity.value;
        }

        if (colorAdjustments != null)
        {
            basePostExposure = colorAdjustments.postExposure.value;
        }
    }

    private void PrepareDeathPanel()
    {
        if (deathScarePanel == null)
        {
            GameObject panel = GameObject.Find("DeathScarePanel");
            if (panel != null)
            {
                deathScarePanel = panel.GetComponent<CanvasGroup>();
                deathScareImage = panel.GetComponent<Image>();
            }
        }

        if (deathScarePanel != null)
        {
            deathScarePanel.alpha = 0f;
            deathScarePanel.gameObject.SetActive(false);
        }
    }

    private void OnHpChanged(int currentHp, int maxHp)
    {
        if (lastHp >= 0 && currentHp < lastHp)
        {
            float health01 = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 1f;
            TriggerDamageFeedback(1f + (1f - health01) * lowHpShakeMultiplier);
        }

        lastHp = currentHp;
    }

    private void TriggerDamageFeedback(float strengthMultiplier)
    {
        shakeTimer = shakeDuration;
        shakeCurrentStrength = shakeStrength * strengthMultiplier;
        pulseTimer = damagePulseDuration;
    }

    private void OnDead()
    {
        TriggerDamageFeedback(lowHpShakeMultiplier + 0.8f);

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
        }

        deathRoutine = StartCoroutine(DeathFlashRoutine());
    }

    private void UpdateCameraShake()
    {
        if (targetCamera == null || shakeTimer <= 0f)
        {
            if (targetCamera != null && cameraShakeOffset != Vector3.zero)
            {
                targetCamera.transform.position -= cameraShakeOffset;
            }

            cameraShakeOffset = Vector3.zero;
            return;
        }

        shakeTimer -= Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(shakeTimer / Mathf.Max(0.0001f, shakeDuration));
        float strength = shakeCurrentStrength * t * t;
        Vector2 random = Random.insideUnitCircle * strength;
        Vector3 nextOffset = new Vector3(random.x, random.y, 0f);
        targetCamera.transform.position += nextOffset - cameraShakeOffset;
        cameraShakeOffset = nextOffset;
    }

    private void UpdateDamageLens()
    {
        if (pulseTimer <= 0f)
        {
            RestoreVolumeDefaults();
            return;
        }

        pulseTimer -= Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(pulseTimer / Mathf.Max(0.0001f, damagePulseDuration));
        float pulse = Mathf.Sin(t * Mathf.PI);

        if (vignette != null)
        {
            vignette.intensity.Override(baseVignetteIntensity + vignetteBoost * pulse);
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.Override(baseChromaticIntensity + chromaticBoost * pulse);
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.Override(basePostExposure + exposureDrop * pulse);
        }
    }

    private IEnumerator DeathFlashRoutine()
    {
        if (deathScarePanel == null)
        {
            yield break;
        }

        deathScarePanel.gameObject.SetActive(true);
        if (deathScareImage != null)
        {
            deathScareImage.color = deathFlashColor;
        }

        float timer = 0f;
        while (timer < deathFlashDuration)
        {
            timer += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(timer / deathFlashDuration);
            float flicker = Mathf.Abs(Mathf.Sin(normalized * Mathf.PI * 9f));
            deathScarePanel.alpha = Mathf.Lerp(0.15f, deathFlashColor.a, flicker) * (1f - normalized * 0.25f);
            yield return null;
        }

        deathScarePanel.alpha = deathFlashColor.a;
        deathRoutine = null;
    }

    private void RestoreVolumeDefaults()
    {
        if (vignette != null)
        {
            vignette.intensity.Override(baseVignetteIntensity);
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.Override(baseChromaticIntensity);
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.Override(basePostExposure);
        }
    }
}
