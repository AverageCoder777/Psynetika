using System.Collections;
using UnityEngine;

public class BerserkSpell : SpellBase
{
    public override void Cast(Vector2 spawnPos)
    {
        Debug.Log("[BerserkSpell] Cast requested.");
        if (caster == null)
        {
            Debug.LogWarning("[BerserkSpell] Cast aborted: caster is null.");
            Destroy(gameObject);
            return;
        }

        if (spellData is SpellSobakaData data)
        {
            Debug.Log($"[BerserkSpell] Cast started. Duration={data.SpellTime}s, maxStacks={data.maxStacks}.");
            caster.StartCoroutine(BerserkRoutine(data));
        }
        else
        {
            string actualType = spellData == null ? "null" : spellData.GetType().Name;
            Debug.LogWarning($"[BerserkSpell] Cast aborted: expected SpellSobakaData, got {actualType}.");
        }

        Destroy(gameObject);
    }

    private IEnumerator BerserkRoutine(SpellSobakaData data)
    {
        int stacks = 0;
        float elapsed = 0f;
        float nextDrainAt = data.hpDrainInterval;
        int criticalHp = Mathf.Max(1, Mathf.RoundToInt(caster.MaxHp * data.criticalHpThreshold));

        float baseAttackSpeed = caster.AttackSpeedMultiplier;
        float baseDamage = caster.DamageMultiplier;

        while (elapsed < data.SpellTime)
        {
            elapsed += Time.deltaTime;

            if (stacks < data.maxStacks && elapsed >= nextDrainAt)
            {
                int drainAmount = Mathf.RoundToInt(caster.MaxHp * data.hpDrainPercent);
                int drained = caster.TryDrainHP(drainAmount, criticalHp);

                if (drained > 0)
                {
                    stacks++;
                    caster.AttackSpeedMultiplier += data.boostPerStack;
                    caster.DamageMultiplier += data.boostPerStack;
                    Debug.Log($"[BerserkSpell] Stack +1 -> {stacks}/{data.maxStacks}. Drained {drained} HP.");
                }
                else
                {
                    Debug.Log("[BerserkSpell] HP drain skipped: reached critical HP threshold.");
                }

                nextDrainAt += data.hpDrainInterval;
            }

            yield return null;
        }

        caster.AttackSpeedMultiplier = baseAttackSpeed;
        caster.DamageMultiplier = baseDamage;
        Debug.Log("[BerserkSpell] Effect ended. Multipliers restored.");
    }
}
