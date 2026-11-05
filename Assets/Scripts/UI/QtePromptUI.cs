using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
Подсказка QTE на экране: кнопка, которую надо жать, со счётчиком оставшихся нажатий
(каждое нажатие сокращает захват, кнопка вздрагивает) и полоса времени до самоубийства.

Собирает себя сама при первом показе (свой Canvas поверх всего), поэтому в сцену ничего
добавлять не нужно. Хочется своего оформления — положить на сцену объект с этим компонентом
и назначить ссылки: тогда используется он, а не собранный кодом.
*/
public class QtePromptUI : MonoBehaviour
{
    private static QtePromptUI instance;

    [SerializeField] private RectTransform root;
    [SerializeField] private RectTransform keyBox;
    [SerializeField] private TextMeshProUGUI keyLabel;
    [SerializeField] private TextMeshProUGUI pressesLabel;
    [SerializeField] private RectTransform timerFill;
    [SerializeField] private Color keyColor = new(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color timerColor = new(1f, 0.3f, 0.25f, 0.95f);
    [Tooltip("Насколько кнопка вздрагивает от нажатия")]
    [SerializeField] private float pressPunch = 0.18f;

    private QteSequence sequence;
    private int shownPresses;
    private float punch;

    public static QtePromptUI Instance
    {
        get
        {
            if (instance == null) instance = FindAnyObjectByType<QtePromptUI>(FindObjectsInactive.Include);
            if (instance == null) instance = CreateDefault();
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        if (root != null) root.gameObject.SetActive(false);
    }

    public void Show(QteSequence qte)
    {
        sequence = qte;
        if (root == null || qte == null) return;

        if (keyLabel != null) keyLabel.text = QteSequence.KeyLabel(qte.TargetKey);
        shownPresses = qte.PressesLeft;
        punch = 0f;
        root.gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        sequence = null;
        if (root != null) root.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (sequence != null) Refresh();
    }

    private void Refresh()
    {
        if (sequence.PressesLeft < shownPresses) punch = 1f;
        shownPresses = sequence.PressesLeft;
        if (pressesLabel != null) pressesLabel.text = "×" + sequence.PressesLeft;

        if (keyBox != null)
        {
            punch = Mathf.MoveTowards(punch, 0f, Time.unscaledDeltaTime * 8f);
            keyBox.localScale = Vector3.one * (1f + pressPunch * punch);
        }

        if (timerFill != null)
        {
            float t = sequence.TimeLimit > 0f ? Mathf.Clamp01(sequence.TimeLeft / sequence.TimeLimit) : 0f;
            timerFill.anchorMax = new Vector2(t, 1f);
        }
    }

    #region Сборка по умолчанию
    private static QtePromptUI CreateDefault()
    {
        GameObject go = new("QTE Prompt");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Компонент добавляем выключенным, чтобы Awake увидел уже назначенные ссылки.
        go.SetActive(false);
        QtePromptUI ui = go.AddComponent<QtePromptUI>();

        RectTransform root = new GameObject("Root", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(go.transform, false);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.68f);
        root.sizeDelta = new Vector2(420f, 170f);

        Image key = CreateImage("Key", root, ui.keyColor);
        RectTransform box = key.rectTransform;
        box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.6f);
        box.sizeDelta = new Vector2(120f, 120f);

        TextMeshProUGUI keyLabel = CreateText(box, "", 60f);
        keyLabel.color = Color.black;

        // Счётчик нажатий справа от кнопки.
        TextMeshProUGUI presses = CreateText(box, "", 44f);
        presses.rectTransform.anchorMin = new Vector2(1f, 0f);
        presses.rectTransform.anchorMax = new Vector2(1f, 1f);
        presses.rectTransform.pivot = new Vector2(0f, 0.5f);
        presses.rectTransform.sizeDelta = new Vector2(110f, 0f);
        presses.rectTransform.anchoredPosition = new Vector2(16f, 0f);
        presses.alignment = TextAlignmentOptions.MidlineLeft;
        presses.color = Color.white;

        ui.root = root;
        ui.keyBox = box;
        ui.keyLabel = keyLabel;
        ui.pressesLabel = presses;
        ui.timerFill = CreateBar("Timer", root, 0f, 0.1f, ui.timerColor);
        go.SetActive(true);
        return ui;
    }

    // Полоса на 70% ширины подсказки между долями высоты yMin..yMax; возвращает заливку.
    private static RectTransform CreateBar(string name, RectTransform parent, float yMin, float yMax, Color color)
    {
        RectTransform back = CreateImage(name, parent, new Color(0f, 0f, 0f, 0.6f)).rectTransform;
        back.anchorMin = new Vector2(0.15f, yMin);
        back.anchorMax = new Vector2(0.85f, yMax);
        back.offsetMin = back.offsetMax = Vector2.zero;

        RectTransform fill = CreateImage("Fill", back, color).rectTransform;
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        return fill;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI CreateText(RectTransform parent, string text, float fontSize)
    {
        TextMeshProUGUI label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(parent, false);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }
    #endregion
}
