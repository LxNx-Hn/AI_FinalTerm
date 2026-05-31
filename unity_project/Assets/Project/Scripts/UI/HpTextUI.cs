using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DreamNoms.HeartSystem;

public class HpTextUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private bool useSimpleText = false;
    [SerializeField] private RectTransform heartRoot;
    [SerializeField] private GameObject heartContainerPrefab;
    [SerializeField] private bool hideLegacyPanelBackground = true;
    [SerializeField] private Vector2 heartAnchoredPosition = new Vector2(12f, -8f);
    [SerializeField] private Vector2 heartSize = new Vector2(260f, 100f);
    [SerializeField] private Vector2 heartCellSize = new Vector2(40f, 40f);
    [SerializeField] private Vector2 heartSpacing = new Vector2(10f, 0f);

    private GameObject heartContainerInstance;
    private HealthController heartHealthController;
    private int lastCurrentHp = int.MinValue;
    private int lastMaxHp = int.MinValue;

    private void Awake()
    {
        if (hpText != null)
        {
            hpText.enabled = useSimpleText;
            hpText.raycastTarget = false;
        }

        if (heartRoot == null)
            heartRoot = transform.parent as RectTransform;
        if (heartRoot == null)
            heartRoot = transform as RectTransform;

        if (hideLegacyPanelBackground && heartRoot != null)
        {
            var panelImage = heartRoot.GetComponent<Image>();
            HideBoxImage(panelImage);
        }
    }

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (!useSimpleText)
            EnsureHeartContainer();

        if (playerHealth != null)
        {
            if (playerHealth.onHpChanged != null)
                playerHealth.onHpChanged.AddListener(OnHpChanged);
            SyncHeartUi(playerHealth.currentHp, playerHealth.maxHp);
        }
    }

    private void Update()
    {
        if (playerHealth == null)
            return;

        if (playerHealth.currentHp != lastCurrentHp || playerHealth.maxHp != lastMaxHp)
            SyncHeartUi(playerHealth.currentHp, playerHealth.maxHp);
    }

    private void OnDestroy()
    {
        if (playerHealth != null && playerHealth.onHpChanged != null)
            playerHealth.onHpChanged.RemoveListener(OnHpChanged);
    }

    private void EnsureHeartContainer()
    {
        if (heartHealthController != null || heartRoot == null)
            return;

        heartHealthController = heartRoot.GetComponentInChildren<HealthController>(true);
        if (heartHealthController != null)
        {
            heartContainerInstance = heartHealthController.gameObject;
            heartContainerInstance.SetActive(true);
            ConfigureHeartContainerVisuals();
            ConfigureHeartContainerRect();
            return;
        }

        if (heartContainerPrefab == null)
        {
            Debug.LogWarning("[HpTextUI] HeartContainer prefab is not assigned.", this);
            return;
        }

        heartContainerInstance = Instantiate(heartContainerPrefab, heartRoot);
        heartContainerInstance.name = "HeartContainer";
        heartContainerInstance.SetActive(true);
        heartContainerInstance.transform.SetAsLastSibling();
        ConfigureHeartContainerVisuals();
        ConfigureHeartContainerRect();

        heartHealthController = heartContainerInstance.GetComponent<HealthController>();
    }

    private void OnHpChanged(int cur, int max)
    {
        SyncHeartUi(cur, max);
    }

    private void SyncHeartUi(int cur, int max)
    {
        lastCurrentHp = cur;
        lastMaxHp = max;

        if (hpText != null)
        {
            hpText.enabled = useSimpleText;
            if (useSimpleText)
                hpText.text = $"HP {cur}/{max}";
        }

        if (useSimpleText)
            return;

        EnsureHeartContainer();
        if (heartHealthController == null)
            return;

        if (heartHealthController.MaxHealth != max)
            heartHealthController.MaxHealth = max;

        if (!Mathf.Approximately(heartHealthController.Health, cur))
            heartHealthController.Health = cur;
    }

    private void ConfigureHeartContainerRect()
    {
        if (heartContainerInstance == null)
            return;

        var rect = heartContainerInstance.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = heartSize;
        rect.anchoredPosition = heartAnchoredPosition;
        rect.localScale = Vector3.one;
    }

    private void ConfigureHeartContainerVisuals()
    {
        if (heartContainerInstance == null)
            return;

        HideBoxImage(heartContainerInstance.GetComponent<Image>());

        GridLayoutGroup layout = heartContainerInstance.GetComponent<GridLayoutGroup>();
        if (layout != null)
        {
            layout.cellSize = heartCellSize;
            layout.spacing = heartSpacing;
        }
    }

    private static void HideBoxImage(Image image)
    {
        if (image == null)
            return;

        image.enabled = false;
        image.raycastTarget = false;
    }
}
