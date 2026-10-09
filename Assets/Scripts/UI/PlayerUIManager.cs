using UnityEngine;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    [Header("Обычная способность")]
    [Tooltip("Опционально: корневой объект слота. Если задан, скрывается целиком при отсутствии способности.")]
    [SerializeField] private GameObject regularSlotRoot;
    [SerializeField] private Image regularIcon;
    [SerializeField] private Image regularCooldownFill;

    [Header("Ультимативная способность")]
    [Tooltip("Опционально: корневой объект слота. Если задан, скрывается целиком при отсутствии способности.")]
    [SerializeField] private GameObject ultimateSlotRoot;
    [SerializeField] private Image ultimateIcon;
    [SerializeField] private Image ultimateCooldownFill;

    [Header("Энергия ульты (как в Genshin: иконка заполняется снизу вверх)")]
    [Tooltip("Опционально: Image заливки поверх иконки ульты. Пусто — создаётся автоматически копией иконки. " +
             "Тип Filled / Vertical / Bottom выставляется кодом; без своего спрайта берётся иконка способности.")]
    [SerializeField] private Image ultimateEnergyFill;
    [Tooltip("Опционально: включается, когда шкала полная и ульту можно жать.")]
    [SerializeField] private GameObject ultimateReadyIndicator;
    [Tooltip("Цвет иконки, пока шкала не полная (незалитая часть).")]
    [SerializeField] private Color chargingIconColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [Tooltip("Цвет залитой части у Сатаны (белый — родные цвета иконки).")]
    [SerializeField] private Color satanEnergyColor = Color.white;
    [Tooltip("Цвет залитой части у Пса (белый — родные цвета иконки).")]
    [SerializeField] private Color dogEnergyColor = Color.white;
    [Tooltip("Скорость визуального долива шкалы, доля шкалы в секунду. Расход показывается мгновенно.")]
    [SerializeField, Min(0.1f)] private float energyFillSpeed = 1.5f;
    [Tooltip("Пульсация иконки, когда ульта готова (0 — без пульсации).")]
    [SerializeField, Range(0f, 0.3f)] private float readyPulseAmplitude = 0.06f;
    [SerializeField, Min(0f)] private float readyPulseSpeed = 4f;

    private float shownEnergy;
    private bool hasShownHero;
    private bool shownHeroIsSatan;
    private bool energyFillUsesAbilityIcon;
    private Vector3 ultimateIconBaseScale = Vector3.one;

    private void Awake()
    {
        if (ultimateIcon != null)
        {
            ultimateIconBaseScale = ultimateIcon.rectTransform.localScale;
            if (ultimateEnergyFill == null)
                ultimateEnergyFill = CreateEnergyFill(ultimateIcon);
        }

        if (ultimateEnergyFill != null)
        {
            energyFillUsesAbilityIcon = ultimateEnergyFill.sprite == null;
            ultimateEnergyFill.type = Image.Type.Filled;
            ultimateEnergyFill.fillMethod = Image.FillMethod.Vertical;
            ultimateEnergyFill.fillOrigin = (int)Image.OriginVertical.Bottom;
            ultimateEnergyFill.raycastTarget = false;
        }
    }

    // Слой заливки — растянутый на всю иконку дочерний Image. Первый ребёнок, чтобы
    // оверлей кулдауна (если он лежит внутри иконки) оставался сверху.
    private static Image CreateEnergyFill(Image icon)
    {
        GameObject go = new GameObject("UltimateEnergyFill", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(icon.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.SetAsFirstSibling();

        Image fill = go.GetComponent<Image>();
        fill.preserveAspect = icon.preserveAspect;
        return fill;
    }

    private void Update()
    {
        if (player == null) return;

        bool isSatan = player.PlayerCharManager.GetCurrentCharacterType() == PlayerCharacterType.Satan;
        SpellController sc = player.SpellController;

        UpdateSlot(regularSlotRoot,  regularIcon,  regularCooldownFill,  sc, isSatan, SpellSlot.Regular);
        UpdateSlot(ultimateSlotRoot, ultimateIcon, ultimateCooldownFill, sc, isSatan, SpellSlot.Ultimate);
        UpdateEnergy(sc, isSatan);
    }

    private void UpdateEnergy(SpellController sc, bool isSatan)
    {
        AbilityDefinition ultimate = sc.GetAbilityData(isSatan, SpellSlot.Ultimate);
        float progress = sc.GetUltimateEnergyProgress(isSatan);
        bool ready = ultimate != null && progress >= 1f;

        // Набор энергии доливается плавно; трата и смена героя — мгновенно.
        if (!hasShownHero || shownHeroIsSatan != isSatan || progress < shownEnergy)
            shownEnergy = progress;
        else
            shownEnergy = Mathf.MoveTowards(shownEnergy, progress, energyFillSpeed * Time.deltaTime);
        hasShownHero = true;
        shownHeroIsSatan = isSatan;

        if (ultimateEnergyFill != null)
        {
            if (energyFillUsesAbilityIcon && ultimate != null && ultimate.icon != null)
                ultimateEnergyFill.sprite = ultimate.icon;

            ultimateEnergyFill.fillAmount = shownEnergy;
            ultimateEnergyFill.color = isSatan ? satanEnergyColor : dogEnergyColor;
            // На полной шкале иконка сама горит в полную яркость — заливка не нужна.
            ultimateEnergyFill.enabled = ultimate != null && !ready;
        }

        if (ultimateIcon != null)
        {
            ultimateIcon.color = ready ? Color.white : chargingIconColor;

            float pulse = ready && readyPulseAmplitude > 0f
                ? 1f + readyPulseAmplitude * (0.5f + 0.5f * Mathf.Sin(Time.time * readyPulseSpeed))
                : 1f;
            ultimateIcon.rectTransform.localScale = ultimateIconBaseScale * pulse;
        }

        if (ultimateReadyIndicator != null && ultimateReadyIndicator.activeSelf != ready)
            ultimateReadyIndicator.SetActive(ready);
    }

    private static void UpdateSlot(GameObject slotRoot, Image icon, Image fill, SpellController sc, bool isSatan, SpellSlot slot)
    {
        AbilityDefinition ability = sc.GetAbilityData(isSatan, slot);
        bool hasAbility = ability != null;

        SetSlotVisible(slotRoot, icon, fill, hasAbility);

        if (!hasAbility)
            return;

        if (icon != null && ability.icon != null)
            icon.sprite = ability.icon;

        if (fill != null)
            fill.fillAmount = 1f - sc.GetCooldownProgress(isSatan, slot);
    }

    private static void SetSlotVisible(GameObject slotRoot, Image icon, Image fill, bool visible)
    {
        if (slotRoot != null)
        {
            if (slotRoot.activeSelf != visible)
                slotRoot.SetActive(visible);
            return;
        }

        if (icon != null && icon.gameObject.activeSelf != visible)
            icon.gameObject.SetActive(visible);

        if (fill != null && fill.gameObject.activeSelf != visible)
            fill.gameObject.SetActive(visible);
    }
}
