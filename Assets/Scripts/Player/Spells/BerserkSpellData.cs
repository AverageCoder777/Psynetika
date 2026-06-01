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
        if (ctx.attackCaster == null) return;
        ctx.attackCaster.StartCoroutine(Routine(ctx.attackCaster));
    }

    private IEnumerator Routine(PlayerAttack caster)
    {
        int stacks = 0;
        float elapsed = 0f;
        float nextDrainAt = hpDrainInterval;
        var playerHealth = caster.GetComponent<PlayerHealth>();
        int criticalHp = Mathf.Max(1, Mathf.RoundToInt(playerHealth.GetCurrentMaxHP() * criticalHpThreshold));

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
                    int drainAmount = Mathf.RoundToInt(playerHealth.GetCurrentMaxHP() * hpDrainPercent);
                    int drained = 0;
                    if (playerHealth != null)
                    {
                        PlayerController player = caster.GetComponent<PlayerController>();
                        var type = player.GetCurrentCharacterType();
                        int hp = playerHealth.GetCurrentHPOfCharacter(type);
                        if (hp > criticalHp)
                        {
                            int actual = Mathf.Min(drainAmount, hp - criticalHp);
                            playerHealth.TakeDamage(actual);
                            drained = actual;
                        }
                    }

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
