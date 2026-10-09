using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// Рантайм зоны от SpawnAbilityZoneNode: каждые tickInterval секунд лечит союзников кастера
// (IAbilityHealable) и наносит урон остальным через полный пайплайн ReceiveDamage —
// так урон типа Glitch сам накладывает/стакает Glitch-статус и даёт реакцию с Burn.
public class AbilityZone : MonoBehaviour
{
    private static readonly List<Collider2D> OverlapBuffer = new List<Collider2D>(32);

    private readonly HashSet<IAbilityTarget> processedThisTick = new HashSet<IAbilityTarget>();
    private SpawnAbilityZoneNode settings;
    private IAbilityCaster owner;
    private Team ownerTeam;
    private AbilityDefinition sourceAbility;

    public void Initialize(SpawnAbilityZoneNode zoneSettings, IAbilityCaster caster, AbilityDefinition source)
    {
        settings = zoneSettings;
        owner = caster;
        // Команду запоминаем сразу: кастер может умереть или исчезнуть, пока зона ещё действует.
        ownerTeam = caster != null ? caster.Team : Team.Neutral;
        sourceAbility = source;
        Run(destroyCancellationToken).Forget();
    }

    private async UniTaskVoid Run(CancellationToken token)
    {
        float interval = Mathf.Max(0.1f, settings.tickInterval);
        float startTime = Time.time;
        float endTime = startTime + Mathf.Max(0.1f, settings.duration);

        try
        {
            if (settings.tickOnSpawn)
            {
                Tick();
            }

            // Тики привязаны к абсолютному времени старта, как в TimedTickNode, — без накопления дрейфа.
            for (int tick = 1; ; tick++)
            {
                float nextTickAt = startTime + tick * interval;
                if (nextTickAt > endTime + 0.001f)
                {
                    break;
                }

                float wait = nextTickAt - Time.time;
                if (wait > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(wait), cancellationToken: token);
                }
                Tick();
            }

            float remaining = endTime - Time.time;
            if (remaining > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(remaining), cancellationToken: token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Destroy(gameObject);
    }

    private void Tick()
    {
        ContactFilter2D filter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers };
        filter.SetLayerMask(settings.targetLayers);
        int hitCount = Physics2D.OverlapCircle(transform.position, settings.radius, filter, OverlapBuffer);

        // Уничтоженный кастер не должен получать OnDamageDealt — урон тогда «ничей».
        IAbilityCaster attacker = owner is UnityEngine.Object ownerObject && ownerObject == null ? null : owner;

        processedThisTick.Clear();
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = OverlapBuffer[i];
            if (hit == null) continue;

            // У цели бывает несколько коллайдеров — обрабатываем её один раз за тик.
            IAbilityTarget target = hit.GetComponentInParent<IAbilityTarget>();
            if (target == null || !target.IsAlive || !processedThisTick.Add(target))
            {
                continue;
            }

            if (ownerTeam != Team.Neutral && target.Team == ownerTeam)
            {
                if (settings.allyHealPerTick > 0f && target is IAbilityHealable healable)
                {
                    healable.ReceiveHeal(settings.allyHealPerTick);
                }
                continue;
            }

            if (settings.enemyDamagePerTick > 0f)
            {
                target.ReceiveDamage(new DamageEvent
                {
                    Attacker = attacker,
                    Target = target,
                    Amount = settings.enemyDamagePerTick,
                    Type = settings.damageType,
                    SourceAbility = sourceAbility
                });
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (settings == null) return;
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, settings.radius);
    }
}
