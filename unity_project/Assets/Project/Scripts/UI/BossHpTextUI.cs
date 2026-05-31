using TMPro;
using UnityEngine;

public class BossHpTextUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private GameObject container;

    private void Awake()
    {
        if (hpText != null)
        {
            hpText.enabled = false;
            hpText.raycastTarget = false;
        }
    }

    private void Start()
    {
        if (bossHealth == null)
        {
            bossHealth = FindFirstObjectByType<BossHealth>();
        }

        if (bossHealth == null && container != null)
            container.SetActive(false);
    }
}
