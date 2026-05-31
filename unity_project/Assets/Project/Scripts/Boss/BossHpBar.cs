using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BossHealth.onHpChanged 이벤트를 구독해 HP 바 Image의 fillAmount를 갱신한다.
/// </summary>
public class BossHpBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private Text bossNameLabel;
    [SerializeField] private string bossName = "Delulu, the Dream-Eater";

    private static Sprite fallbackFillSprite;
    private static Font uiFont;

    private void Awake()
    {
        ResolveFillImage();
        ConfigureFillImage();
        EnsureBossNameLabel();
    }

    private void Start()
    {
        if (bossHealth == null)
            bossHealth = FindFirstObjectByType<BossHealth>();

        if (bossHealth != null)
        {
            bossHealth.onHpChanged.AddListener(OnHpChanged);
            OnHpChanged(bossHealth.currentHp, bossHealth.maxHp);
        }

        EnsureBossNameLabel();
    }

    private void OnDestroy()
    {
        if (bossHealth != null && bossHealth.onHpChanged != null)
            bossHealth.onHpChanged.RemoveListener(OnHpChanged);
    }

    private void OnHpChanged(int cur, int max)
    {
        if (fillImage == null)
            return;

        ConfigureFillImage();

        float ratio = Mathf.Clamp01((float)cur / Mathf.Max(1, max));
        fillImage.fillAmount = ratio;
    }

    private void ResolveFillImage()
    {
        if (fillImage == null)
            fillImage = GetComponent<Image>();

        if (fillImage == null)
            fillImage = GetComponentInChildren<Image>(true);
    }

    private void ConfigureFillImage()
    {
        if (fillImage == null)
            return;

        EnsureFillSprite();

        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.preserveAspect = false;
        fillImage.raycastTarget = false;
    }

    private void EnsureBossNameLabel()
    {
        if (fillImage == null)
            return;

        Transform panel = fillImage.transform.parent != null ? fillImage.transform.parent.parent : null;
        if (panel == null)
            panel = fillImage.transform.parent;
        if (panel == null)
            return;

        if (bossNameLabel == null)
        {
            Transform existing = panel.Find("BossNameText");
            if (existing != null)
                bossNameLabel = existing.GetComponent<Text>();
        }

        if (bossNameLabel == null)
        {
            GameObject labelGo = new GameObject("BossNameText", typeof(RectTransform));
            labelGo.transform.SetParent(panel, false);
            bossNameLabel = labelGo.AddComponent<Text>();
        }

        RectTransform rect = bossNameLabel.rectTransform;
        rect.anchorMin = new Vector2(0f, 0.58f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        bossNameLabel.text = bossName;
        bossNameLabel.font = GetUiFont();
        bossNameLabel.alignment = TextAnchor.MiddleCenter;
        bossNameLabel.fontSize = 22;
        bossNameLabel.color = Color.white;
        bossNameLabel.raycastTarget = false;
        bossNameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        bossNameLabel.verticalOverflow = VerticalWrapMode.Truncate;
    }

    private void EnsureFillSprite()
    {
        if (fillImage == null || fillImage.sprite != null)
            return;

        if (fallbackFillSprite == null)
        {
            fallbackFillSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );

            fallbackFillSprite.name = "BossHpBarFallbackFill";
        }

        fillImage.sprite = fallbackFillSprite;
        fillImage.preserveAspect = false;
    }

    private static Font GetUiFont()
    {
        if (uiFont != null)
            return uiFont;

        uiFont = Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "Arial", "Arial Unicode MS" },
            22
        );

        if (uiFont == null)
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return uiFont;
    }

    /// <summary>
    /// 3페이즈 진입 시 이름표를 빨간색으로 변경합니다.
    /// ElevatorBossController가 Phase3 진입 시점에 호출합니다.
    /// </summary>
    public void SetPhase3NameColor()
    {
        EnsureBossNameLabel();
        if (bossNameLabel != null)
            bossNameLabel.color = new Color(0.85f, 0.15f, 0.15f);
    }
}