using UnityEngine;

public class SpellController : MonoBehaviour
{
    [Header("Primary Spells")]
    [SerializeField] private SpellData satanPrimarySpell;
    [SerializeField] private SpellData sobakaPrimarySpell;

    private float satanNextCastAt = 0f;
    private float sobakaNextCastAt = 0f;

    public bool TryGetReadySpell(bool isSatan, out SpellData spell)
    {
        spell = isSatan ? satanPrimarySpell : sobakaPrimarySpell;
        if (spell == null)
        {
            return false;
        }

        if (!IsOwnerAllowed(spell, isSatan))
        {
            return false;
        }

        float nextCastAt = isSatan ? satanNextCastAt : sobakaNextCastAt;
        return Time.time >= nextCastAt;
    }

    public void StartCooldown(bool isSatan, SpellData spell)
    {
        if (spell == null)
        {
            return;
        }

        float nextCastAt = Time.time + Mathf.Max(0f, spell.spellCooldown);
        if (isSatan)
        {
            satanNextCastAt = nextCastAt;
        }
        else
        {
            sobakaNextCastAt = nextCastAt;
        }
    }

    private static bool IsOwnerAllowed(SpellData spell, bool isSatan)
    {
        if (spell.Owner == SpellOwner.Any)
        {
            return true;
        }

        if (isSatan)
        {
            return spell.Owner == SpellOwner.Satan;
        }

        return spell.Owner == SpellOwner.Sobaka;
    }

    public void SetSpell(SpellData spell)
    {
        if (spell.Owner == SpellOwner.Satan)
        {
            satanPrimarySpell = spell;
            return;
        }
        else if (spell.Owner == SpellOwner.Sobaka)
            { 
                sobakaPrimarySpell = spell;
                return;
            }
        Debug.LogWarning("Spell Owner not a player");
        
    }
}
