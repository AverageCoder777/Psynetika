using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AbilityRunner : MonoBehaviour
{
    private readonly Dictionary<AbilityDefinition, AbilityInstance> instances = new();
    private readonly List<CancellationTokenSource> activeCasts = new();
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

        CancellationTokenSource castCts =
            CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        activeCasts.Add(castCts);

        AbilityContext ctx = new()
        {
            Owner = caster,
            ResolvedTarget = target,
            AimPosition = aimPosition,
            Direction = caster.FacingDirection,
            Definition = def,
            Instance = inst,
            Services = services,
            Token = castCts.Token
        };

        ExecuteAsync(def, inst, ctx, castCts).Forget();
        return true;
    }

    // Прерывает все активные касты (смерть, стан). Cleanup-действия каждого каста
    // отработают в его finally, так что временные бафы откатятся.
    public void CancelAll()
    {
        if (activeCasts.Count == 0) return;

        CancellationTokenSource[] toCancel = activeCasts.ToArray();
        for (int i = 0; i < toCancel.Length; i++)
        {
            try
            {
                toCancel[i].Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Каст успел завершиться между снимком списка и отменой.
            }
        }
    }

    private async UniTaskVoid ExecuteAsync(AbilityDefinition def, AbilityInstance inst, AbilityContext ctx,
        CancellationTokenSource castCts)
    {
        inst.IsExecuting = true;
        int succeeded = 0;
        NodeResult lastResult = NodeResult.Success;
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

                    lastResult = await node.Execute(ctx);
                    if (lastResult != NodeResult.Success)
                    {
                        break;
                    }

                    succeeded++;
                }
            }

            // Каст не состоялся вовсе (первая же нода провалилась) — возвращаем кулдаун.
            if (lastResult == NodeResult.Failure && succeeded == 0)
            {
                inst.CooldownEndsAt = 0f;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            ctx.RunCleanups();
            inst.IsExecuting = false;
            activeCasts.Remove(castCts);
            castCts.Dispose();
        }
    }
}
