using UnityEngine;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    [SerializeField] private Player player;

    [Header("Обычная способность")]
    [SerializeField] private Image regularIcon;
    [SerializeField] private Image regularCooldownFill;

    [Header("Ультимативная способность")]
    [SerializeField] private Image ultimateIcon;
    [SerializeField] private Image ultimateCooldownFill;

    private void Update()
    {
        if (player == null) return;

        bool isSatan = player.GetCurrentCharState() == player.SatanState;
        SpellController sc = player.SpellController;

        UpdateSlot(regularIcon,  regularCooldownFill,  sc, isSatan, SpellSlot.Regular);
        UpdateSlot(ultimateIcon, ultimateCooldownFill, sc, isSatan, SpellSlot.Ultimate);
    }

    private static void UpdateSlot(Image icon, Image fill, SpellController sc, bool isSatan, SpellSlot slot)
    {
        SpellData spell = sc.GetSpellData(isSatan, slot);

        if (icon != null && spell != null && spell.SpellIcon != null)
            icon.sprite = spell.SpellIcon;

        if (fill != null)
            fill.fillAmount = 1f - sc.GetCooldownProgress(isSatan, slot);
    }
}
