using UnityEngine;

public class SpellController : MonoBehaviour
{
    [Header("Satan")]
    [SerializeField] private SpellData satanRegular;
    [SerializeField] private SpellData satanUltimate;

    [Header("Sobaka")]
    [SerializeField] private SpellData sobakaRegular;
    [SerializeField] private SpellData sobakaUltimate;

    private float satanRegularNextCastAt;
    private float satanUltimateNextCastAt;
    private float sobakaRegularNextCastAt;
    private float sobakaUltimateNextCastAt;

    public bool TryGetReadySpell(bool isSatan, SpellSlot slot, out SpellData spell)
    {
        spell = GetSlotData(isSatan, slot);
        if (spell == null) return false;
        return Time.time >= GetNextCastAt(isSatan, slot);
    }

    public void StartCooldown(bool isSatan, SpellSlot slot, SpellData spell)
    {
        if (spell == null) return;
        SetNextCastAt(isSatan, slot, Time.time + Mathf.Max(0f, spell.spellCooldown));
    }

    public float GetCooldownProgress(bool isSatan, SpellSlot slot)
    {
        SpellData spell = GetSlotData(isSatan, slot);
        if (spell == null) return 1f;
        float remaining = GetNextCastAt(isSatan, slot) - Time.time;
        if (remaining <= 0f) return 1f;
        return 1f - remaining / spell.spellCooldown;
    }

    public void SetSpell(SpellData spell)
    {
        bool isSatan = spell.Owner == SpellOwner.Satan;
        bool isSobaka = spell.Owner == SpellOwner.Sobaka;
        if (!isSatan && !isSobaka)
        {
            Debug.LogWarning("Spell Owner not a player");
            return;
        }
        if (spell.Slot == SpellSlot.Regular)
        {
            if (isSatan) satanRegular = spell; else sobakaRegular = spell;
        }
        else
        {
            if (isSatan) satanUltimate = spell; else sobakaUltimate = spell;
        }
    }

    private SpellData GetSlotData(bool isSatan, SpellSlot slot)
    {
        if (isSatan) return slot == SpellSlot.Regular ? satanRegular : satanUltimate;
        return slot == SpellSlot.Regular ? sobakaRegular : sobakaUltimate;
    }

    private float GetNextCastAt(bool isSatan, SpellSlot slot)
    {
        if (isSatan) return slot == SpellSlot.Regular ? satanRegularNextCastAt : satanUltimateNextCastAt;
        return slot == SpellSlot.Regular ? sobakaRegularNextCastAt : sobakaUltimateNextCastAt;
    }

    private void SetNextCastAt(bool isSatan, SpellSlot slot, float value)
    {
        if (isSatan)
        {
            if (slot == SpellSlot.Regular) satanRegularNextCastAt = value;
            else satanUltimateNextCastAt = value;
        }
        else
        {
            if (slot == SpellSlot.Regular) sobakaRegularNextCastAt = value;
            else sobakaUltimateNextCastAt = value;
        }
    }
}
