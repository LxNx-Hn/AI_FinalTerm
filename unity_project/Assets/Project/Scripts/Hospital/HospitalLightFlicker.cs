using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class HospitalLightFlicker : MonoBehaviour
{
    [SerializeField] private Light2D targetLight;
    [SerializeField] private float minIntensityMultiplier = 0.45f;
    [SerializeField] private float maxIntensityMultiplier = 1.25f;
    [SerializeField] private float noiseSpeed = 18f;
    [SerializeField] private float outageChancePerSecond = 0.035f;
    [SerializeField] private Vector2 outageDurationRange = new Vector2(0.08f, 0.42f);

    private float baseIntensity;
    private float seed;
    private float outageTimer;

    private void Reset()
    {
        targetLight = GetComponent<Light2D>();
    }

    private void Awake()
    {
        if (targetLight == null)
        {
            targetLight = GetComponent<Light2D>();
        }

        baseIntensity = targetLight != null ? targetLight.intensity : 1f;
        seed = Random.value * 1000f;
    }

    private void OnEnable()
    {
        if (targetLight != null && baseIntensity <= 0f)
        {
            baseIntensity = targetLight.intensity;
        }
    }

    private void Update()
    {
        if (targetLight == null)
        {
            return;
        }

        if (outageTimer > 0f)
        {
            outageTimer -= Time.deltaTime;
            targetLight.intensity = 0f;
            return;
        }

        if (Random.value < outageChancePerSecond * Time.deltaTime)
        {
            outageTimer = Random.Range(outageDurationRange.x, outageDurationRange.y);
            targetLight.intensity = 0f;
            return;
        }

        float noise = Mathf.PerlinNoise(seed, Time.time * noiseSpeed);
        float multiplier = Mathf.Lerp(minIntensityMultiplier, maxIntensityMultiplier, noise);
        targetLight.intensity = baseIntensity * multiplier;
    }
}
