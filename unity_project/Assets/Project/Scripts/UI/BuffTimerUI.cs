using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays active buff icons with countdown timers in the top-right corner.
/// Attach to a Canvas. Automatically finds the player's BuffController.
/// </summary>
public class BuffTimerUI : MonoBehaviour
{
    [Header("Colors")]
    public Color attackBuffColor = new Color(1f, 0.35f, 0.2f, 1f);
    public Color speedBuffColor = new Color(0.2f, 0.7f, 1f, 1f);
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);

    [Header("Layout")]
    public float iconSize = 36f;
    public float barWidth = 100f;
    public float barHeight = 14f;
    public float spacing = 6f;
    public float marginRight = 12f;
    public float marginTop = 12f;

    private BuffController buff;

    // Attack buff UI
    private GameObject attackBuffPanel;
    private Image attackBarFill;
    private Text attackTimerText;
    private Text attackLabel;
    private float attackBuffTotal;

    // Speed buff UI
    private GameObject speedBuffPanel;
    private Image speedBarFill;
    private Text speedTimerText;
    private Text speedLabel;
    private float speedBuffTotal;

    private void Start()
    {
        CachePlayer();
        BuildUI();
    }

    private void Update()
    {
        if (buff == null)
        {
            CachePlayer();
            if (buff == null) return;
        }

        UpdateBuffDisplay(
            buff.AttackBuffActive, buff.AttackBuffRemaining,
            attackBuffPanel, attackBarFill, attackTimerText,
            ref attackBuffTotal);

        UpdateBuffDisplay(
            buff.SpeedBuffActive, buff.SpeedBuffRemaining,
            speedBuffPanel, speedBarFill, speedTimerText,
            ref speedBuffTotal);
    }

    private void CachePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            buff = player.GetComponent<BuffController>();
        }
    }

    private void UpdateBuffDisplay(
        bool active, float remaining,
        GameObject panel, Image barFill, Text timerText,
        ref float totalDuration)
    {
        if (panel == null) return;

        if (active)
        {
            if (!panel.activeSelf) panel.SetActive(true);

            if (remaining > totalDuration - 0.05f)
            {
                totalDuration = remaining;
            }

            float ratio = totalDuration > 0f ? remaining / totalDuration : 0f;
            barFill.fillAmount = ratio;
            timerText.text = remaining.ToString("F1") + "s";
        }
        else
        {
            if (panel.activeSelf) panel.SetActive(false);
            totalDuration = 0f;
        }
    }

    private void BuildUI()
    {
        // Create a container anchored to top-right
        GameObject container = CreateUIObject("BuffTimerContainer", transform);
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(1f, 1f);
        containerRect.anchorMax = new Vector2(1f, 1f);
        containerRect.pivot = new Vector2(1f, 1f);
        containerRect.anchoredPosition = new Vector2(-marginRight, -marginTop);
        containerRect.sizeDelta = new Vector2(barWidth + iconSize + spacing + 20f, (barHeight + iconSize + spacing) * 2 + spacing);

        // Vertical layout
        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = spacing;
        vlg.childAlignment = TextAnchor.UpperRight;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Attack buff panel
        attackBuffPanel = CreateBuffPanel(container.transform, "ATK x2", attackBuffColor,
            out attackBarFill, out attackTimerText, out attackLabel);
        attackBuffPanel.SetActive(false);

        // Speed buff panel
        speedBuffPanel = CreateBuffPanel(container.transform, "SPD x2", speedBuffColor,
            out speedBarFill, out speedTimerText, out speedLabel);
        speedBuffPanel.SetActive(false);
    }

    private GameObject CreateBuffPanel(Transform parent, string labelText, Color color,
        out Image barFill, out Text timerText, out Text label)
    {
        float panelHeight = barHeight + 20f;
        float panelWidth = barWidth + 60f;

        // Panel background
        GameObject panel = CreateUIObject("BuffPanel", parent);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = backgroundColor;

        // Horizontal layout
        HorizontalLayoutGroup hlg = panel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4f;
        hlg.padding = new RectOffset(6, 6, 3, 3);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Label
        GameObject labelObj = CreateUIObject("Label", panel.transform);
        label = labelObj.AddComponent<Text>();
        label.text = labelText;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 12;
        label.fontStyle = FontStyle.Bold;
        label.color = color;
        label.alignment = TextAnchor.MiddleLeft;
        LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
        labelLE.preferredWidth = 46f;
        labelLE.minWidth = 46f;

        // Bar background
        GameObject barBg = CreateUIObject("BarBg", panel.transform);
        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        LayoutElement barBgLE = barBg.AddComponent<LayoutElement>();
        barBgLE.preferredWidth = barWidth;
        barBgLE.minWidth = barWidth;

        // Bar fill (child of bar background)
        GameObject fillObj = CreateUIObject("Fill", barBg.transform);
        barFill = fillObj.AddComponent<Image>();
        barFill.color = color;
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillOrigin = 0;
        barFill.fillAmount = 1f;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // Timer text
        GameObject timerObj = CreateUIObject("Timer", panel.transform);
        timerText = timerObj.AddComponent<Text>();
        timerText.text = "0.0s";
        timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timerText.fontSize = 11;
        timerText.color = Color.white;
        timerText.alignment = TextAnchor.MiddleRight;
        LayoutElement timerLE = timerObj.AddComponent<LayoutElement>();
        timerLE.preferredWidth = 36f;
        timerLE.minWidth = 36f;

        return panel;
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }
}
