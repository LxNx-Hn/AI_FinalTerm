using UnityEngine;

public class HospitalEnvironmentalPulse : MonoBehaviour
{
    [Header("Alpha")]
    [SerializeField] private bool pulseAlpha;
    [SerializeField] private float alphaAmount = 0.12f;

    [Header("Scale")]
    [SerializeField] private bool pulseScale;
    [SerializeField] private float scaleAmount = 0.03f;

    [Header("Motion")]
    [SerializeField] private Vector2 positionAmount;
    [SerializeField] private float rotationAmount;

    [Header("Timing")]
    [SerializeField] private float speed = 1f;
    [SerializeField] private float phaseOffset;
    [SerializeField] private bool randomizePhase = true;

    private SpriteRenderer spriteRenderer;
    private Color baseColor;
    private Vector3 basePosition;
    private Vector3 baseScale;
    private Quaternion baseRotation;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
        }

        basePosition = transform.localPosition;
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;

        if (randomizePhase)
        {
            phaseOffset += Random.value * 10f;
        }
    }

    private void OnEnable()
    {
        basePosition = transform.localPosition;
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
    }

    private void Update()
    {
        float t = Time.time * speed + phaseOffset;
        float wave = Mathf.Sin(t);
        float softWave = Mathf.Sin(t * 0.53f + 1.7f);

        if (spriteRenderer != null && pulseAlpha)
        {
            Color color = baseColor;
            color.a = Mathf.Clamp01(baseColor.a + wave * alphaAmount);
            spriteRenderer.color = color;
        }

        if (pulseScale)
        {
            float scale = 1f + wave * scaleAmount;
            transform.localScale = new Vector3(baseScale.x * scale, baseScale.y * scale, baseScale.z);
        }

        if (positionAmount != Vector2.zero)
        {
            transform.localPosition = basePosition + new Vector3(positionAmount.x * wave, positionAmount.y * softWave, 0f);
        }

        if (rotationAmount > 0f)
        {
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, rotationAmount * wave);
        }
    }

    private void OnDisable()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }

        transform.localPosition = basePosition;
        transform.localScale = baseScale;
        transform.localRotation = baseRotation;
    }
}
