using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Psynetika/Berserk Spell")]
public class BerserkSpellData : SpellData
{
    [Header("Berserk")]
    public float duration = 15f;
    public float hpDrainInterval = 1.5f;
    [Range(0f, 1f)] public float hpDrainPercent = 0.1f;
    [Range(0f, 1f)] public float criticalHpThreshold = 0.1f;
    [Range(0f, 1f)] public float boostPerStack = 0.1f;
    public int maxStacks = 5;
    public GameObject vfxPrefab;

    public override void Cast(SpellCastContext ctx)
    {
        if (ctx.Caster == null) return;
        ctx.Caster.StartCoroutine(Routine(ctx.Caster));
    }

    private IEnumerator Routine(Player caster)
    {
        int stacks = 0;
        float elapsed = 0f;
        float nextDrainAt = hpDrainInterval;
        int criticalHp = Mathf.Max(1, Mathf.RoundToInt(caster.MaxHp * criticalHpThreshold));

        float baseAttackSpeed = caster.AttackSpeedMultiplier;
        float baseDamage = caster.DamageMultiplier;

        GameObject activeVfx = null;
        if (vfxPrefab != null)
        {
            activeVfx = Instantiate(vfxPrefab, caster.transform);
            activeVfx.transform.localPosition = Vector3.zero;
        }

        try
        {
            while (elapsed < duration)
            {
                if (caster == null) yield break;

                elapsed += Time.deltaTime;

                if (stacks < maxStacks && elapsed >= nextDrainAt)
                {
                    int drainAmount = Mathf.RoundToInt(caster.MaxHp * hpDrainPercent);
                    int drained = caster.TryDrainHP(drainAmount, criticalHp);

                    if (drained > 0)
                    {
                        stacks++;
                        caster.AttackSpeedMultiplier += boostPerStack;
                        caster.DamageMultiplier += boostPerStack;
                    }

                    nextDrainAt += hpDrainInterval;
                }

                yield return null;
            }
        }
        finally
        {
            if (caster != null)
            {
                caster.AttackSpeedMultiplier = baseAttackSpeed;
                caster.DamageMultiplier = baseDamage;
            }

            if (activeVfx != null)
            {
                Destroy(activeVfx);
            }
        }
    }
}
