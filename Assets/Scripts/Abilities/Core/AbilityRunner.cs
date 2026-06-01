using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AbilityRunner : MonoBehaviour
{
    private readonly Dictionary<AbilityDefinition, AbilityInstance> instances = new();
    private AbilityServices services;
    private IAbilityCaster caster;

    public AbilityServices Services => services;
    public IAbilityCaster Caster => caster;

    public void Initialize(IAbilityCaster owner, AbilityServices abilityServices)
    {
        caster = owner;
        services = abilityServices ?? new AbilityServices();
    }

    public AbilityInstance GetInstance(AbilityDefinition def)
    {
        if (def == null) return null;
        if (!instances.TryGetValue(def, out AbilityInstance inst))
        {
            inst = new AbilityInstance();
            instances[def] = inst;
        }
        return inst;
    }

    public bool IsReady(AbilityDefinition def)
    {
        if (def == null) return false;
        AbilityInstance inst = GetInstance(def);
        if (inst.IsExecuting) return false;
        return Time.time >= inst.CooldownEndsAt;
    }

    public float GetCooldownProgress(AbilityDefinition def)
    {
        if (def == null) return 1f;
        AbilityInstance inst = GetInstance(def);
        float remaining = inst.CooldownEndsAt - Time.time;
        if (remaining <= 0f) return 1f;
        if (def.cooldown <= 0f) return 1f;
        return 1f - remaining / def.cooldown;
    }

    public bool TryCast(AbilityDefinition def, IAbilityTarget target, Vector2 aimPosition)
    {
        if (def == null) return false;
        if (caster == null) return false;
        if (!IsReady(def)) return false;

        AbilityInstance inst = GetInstance(def);
        inst.CooldownEndsAt = Time.time + Mathf.Max(0f, def.cooldown);

        AbilityContext ctx = new()
        {
            Owner = caster,
            ResolvedTarget = target,
            AimPosition = aimPosition,
            Direction = caster.FacingDirection,
            Definition = def,
            Instance = inst,
            Services = services,
            Token = this.GetCancellationTokenOnDestroy()
        };

        ExecuteAsync(def, inst, ctx).Forget();
        return true;
    }

    private async UniTaskVoid ExecuteAsync(AbilityDefinition def, AbilityInstance inst, AbilityContext ctx)
    {
        inst.IsExecuting = true;
        try
        {
            if (def.root != null)
            {
                foreach (AbilityNode node in def.root)
                {
                    if (node == null)
                    {
                        continue;
                    }

                    NodeResult result = await node.Execute(ctx);
                    if (result != NodeResult.Success)
                    {
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            inst.IsExecuting = false;
        }
    }
}
