using UnityEngine;

[CreateAssetMenu(menuName = "Psynetika/Ability Spell")]
public class AbilitySpellData : SpellData
{
    public AbilityDefinition ability;

    public override void Cast(SpellCastContext ctx)
    {
        if (ability == null)
        {
            Debug.LogWarning($"[{name}] AbilityDefinition is not assigned.");
            return;
        }

        if (ctx.attackCaster == null)
        {
            return;
        }

        AbilityRunner runner = ctx.attackCaster.GetComponent<AbilityRunner>();
        if (runner == null)
        {
            runner = ctx.attackCaster.gameObject.AddComponent<AbilityRunner>();
        }

        if (runner.Caster == null)
        {
            runner.Initialize(ctx.attackCaster, new AbilityServices());
        }

        Vector2 aimPosition = ctx.CasterCenter + Vector2.right * ctx.Direction;
        runner.TryCast(ability, null, aimPosition);
    }
}
