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

    private void Update()
    {
        if (player == null) return;

        bool isSatan = player.PlayerCharManager.GetCurrentCharacterType() == PlayerCharacterType.Satan;
        SpellController sc = player.SpellController;

        UpdateSlot(regularSlotRoot,  regularIcon,  regularCooldownFill,  sc, isSatan, SpellSlot.Regular);
        UpdateSlot(ultimateSlotRoot, ultimateIcon, ultimateCooldownFill, sc, isSatan, SpellSlot.Ultimate);
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
