using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
Полоска здоровья босса вверху экрана. Собирается кодом (Show), отдельный префаб UI не нужен.
Под основной полоской — тонкая полоска щита, видна, пока щит поднят.
*/
public class BossHealthBarUI : MonoBehaviour
{
    private static readonly Color BackColor = new(0f, 0f, 0f, 0.65f);
    private static readonly Color HealthColor = new(0.78f, 0.12f, 0.2f, 1f);
    private static readonly Color ShieldColor = new(0.92f, 0.92f, 1f, 0.95f);

    private EnemyHealth health;
    private SpiderWebShield shield;
    private RectTransform healthFill;
    private RectTransform shieldFill;
    private GameObject shieldBar;

    public static BossHealthBarUI Show(string title, EnemyHealth health, SpiderWebShield shield = null)
    {
        GameObject go = new("Boss Health Bar", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        BossHealthBarUI ui = go.AddComponent<BossHealthBarUI>();
        ui.Build(go.transform, title);
        ui.Bind(health, shield);
        return ui;
    }

    public void Hide()
    {
        Destroy(gameObject);
    }

    private void Build(Transform root, string title)
    {
        RectTransform panel = CreateRect("Panel", root);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -40f);
        panel.sizeDelta = new Vector2(900f, 80f);

        TextMeshProUGUI label = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        label.rectTransform.SetParent(panel, false);
        label.rectTransform.anchorMin = new Vector2(0f, 1f);
        label.rectTransform.anchorMax = new Vector2(1f, 1f);
        label.rectTransform.pivot = new Vector2(0.5f, 1f);
        label.rectTransform.sizeDelta = new Vector2(0f, 36f);
        label.text = title;
        label.fontSize = 30f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;

        healthFill = CreateBar("Health", panel, -40f, 22f, HealthColor, out _);
        shieldFill = CreateBar("Shield", panel, -66f, 8f, ShieldColor, out shieldBar);
    }

    private void Bind(EnemyHealth boss, SpiderWebShield bossShield)
    {
        health = boss;
        shield = bossShield;
        if (health != null)
        {
            health.HpChanged += OnHealthChanged;
            OnHealthChanged(health.CurrentHp, health.MaxHp);
        }
        if (shield != null)
        {
            shield.HpChanged += OnShieldChanged;
            OnShieldChanged(shield.IsUp ? shield.CurrentHp : 0, shield.MaxHp);
        }
        else
        {
            shieldBar.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (health != null) health.HpChanged -= OnHealthChanged;
        if (shield != null) shield.HpChanged -= OnShieldChanged;
    }

    private void OnHealthChanged(int current, int max) => SetFill(healthFill, current, max);

    private void OnShieldChanged(int current, int max)
    {
        shieldBar.SetActive(current > 0);
        SetFill(shieldFill, current, max);
    }

    private static void SetFill(RectTransform fill, int current, int max)
    {
        if (fill == null) return;
        fill.anchorMax = new Vector2(max > 0 ? Mathf.Clamp01((float)current / max) : 0f, 1f);
    }

    private static RectTransform CreateBar(string name, RectTransform parent, float y, float height, Color color, out GameObject bar)
    {
        RectTransform back = CreateRect(name, parent);
        back.anchorMin = new Vector2(0f, 1f);
        back.anchorMax = new Vector2(1f, 1f);
        back.pivot = new Vector2(0.5f, 1f);
        back.anchoredPosition = new Vector2(0f, y);
        back.sizeDelta = new Vector2(0f, height);
        back.gameObject.AddComponent<Image>().color = BackColor;

        // Заполнение — растяжение по якорям: у Image без спрайта fillAmount не работает.
        RectTransform fill = CreateRect("Fill", back);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = color;

        bar = back.gameObject;
        return fill;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }
}
