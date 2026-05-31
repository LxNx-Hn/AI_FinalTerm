using UnityEngine;
using UnityEngine.Rendering.Universal;

public class HospitalPlayerSightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Light2D coneLight;
    [SerializeField] private Light2D haloLight;

    [Header("Cone")]
    [SerializeField] private float coneNormalRadius = 8f;
    [SerializeField] private float coneLowHpRadius = 5.5f;
    [SerializeField] private float coneNormalIntensity = 0.95f;
    [SerializeField] private float coneLowHpIntensity = 0.65f;

    [Header("Halo")]
    [SerializeField] private float haloNormalRadius = 2.6f;
    [SerializeField] private float haloLowHpRadius = 1.75f;
    [SerializeField] private float haloNormalIntensity = 0.42f;
    [SerializeField] private float haloLowHpIntensity = 0.28f;

    [Header("Pulse")]
    [SerializeField] private float pulseSpeed = 2.8f;
    [SerializeField] private float pulseAmount = 0.08f;

    private void Reset()
    {
        playerController = GetComponentInParent<PlayerController>();
        playerHealth = GetComponentInParent<PlayerHealth>();
    }

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerController>();
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponentInParent<PlayerHealth>();
        }
    }

    private void LateUpdate()
    {
        ApplyFacing();
        ApplyLightState();
    }

    private void ApplyFacing()
    {
        if (playerController == null || coneLight == null)
        {
            return;
        }

        Vector2Int facing = playerController.Facing;
        float z = 180f;

        if (facing == Vector2Int.up)
        {
            z = 0f;
        }
        else if (facing == Vector2Int.left)
        {
            z = 90f;
        }
        else if (facing == Vector2Int.right)
        {
            z = 270f;
        }

        coneLight.transform.localRotation = Quaternion.Euler(0f, 0f, z);
    }

    private void ApplyLightState()
    {
        float health01 = 1f;
        if (playerHealth != null && playerHealth.maxHp > 0)
        {
            health01 = Mathf.Clamp01((float)playerHealth.currentHp / playerHealth.maxHp);
        }

        float distress = 1f - health01;
        float pulse = 1f + Mathf.Sin(Time.time * Mathf.Lerp(pulseSpeed, pulseSpeed * 2.1f, distress)) * pulseAmount;

        if (coneLight != null)
        {
            coneLight.pointLightOuterRadius = Mathf.Lerp(coneLowHpRadius, coneNormalRadius, health01);
            coneLight.intensity = Mathf.Lerp(coneLowHpIntensity, coneNormalIntensity, health01) * pulse;
        }

        if (haloLight != null)
        {
            haloLight.pointLightOuterRadius = Mathf.Lerp(haloLowHpRadius, haloNormalRadius, health01);
            haloLight.intensity = Mathf.Lerp(haloLowHpIntensity, haloNormalIntensity, health01) * pulse;
        }
    }
}
