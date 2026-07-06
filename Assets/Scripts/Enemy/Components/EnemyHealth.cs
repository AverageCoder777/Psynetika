using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IAbilityTarget, IDirectDamageReceiver
{
    public event Action<int, int> HpChanged;
    public event Action<DamageEvent> Damaged;
    public event Action Died;

    private int maxHp = 1;
    private int currentHp = 1;
    private StatusEffectHandler statusHandler;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsAlive => currentHp > 0;

    private void Awake()
    {
        statusHandler = GetComponent<StatusEffectHandler>();
    }

    public void Initialize(EnemyConfig config)
    {
        maxHp = Mathf.Max(1, config.maxHp);
        currentHp = maxHp;
    }

    Transform IAbilityTarget.Transform => transform;
    bool IAbilityTarget.IsAlive => IsAlive;
    Team IAbilityTarget.Team => Team.Enemy;

    // Полный пайплайн урона: реакции/резисты статусов -> урон -> наложение статуса.
    void IAbilityTarget.ReceiveDamage(DamageEvent ev)
    {
        if (!IsAlive) return;

        if (statusHandler != null)
        {
            ev.Amount = statusHandler.ProcessIncomingDamage(ev);
        }
        ApplyDamage(ev);
        if (IsAlive && statusHandler != null)
        {
            statusHandler.MaybeApplyStatusFromDamage(ev);
        }
    }

    // Сырой урон без статус-пайплайна. Тики Burn обязаны идти сюда,
    // иначе каждый тик заново накладывал бы Burn через ReceiveDamage.
    public void ApplyDamage(DamageEvent ev)
    {
        if (!IsAlive) return;

        int amount = Mathf.Max(0, Mathf.RoundToInt(ev.Amount));
        if (amount <= 0) return;

        currentHp = Mathf.Max(0, currentHp - amount);
        Damaged?.Invoke(ev);
        HpChanged?.Invoke(currentHp, maxHp);
        if (currentHp <= 0)
        {
            Died?.Invoke();
        }
    }
}
