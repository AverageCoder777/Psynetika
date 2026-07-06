using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// Контекст одного каста: создаётся раннером на каждый запуск способности,
// живёт до конца цепочки (включая снаряды, которые шарят его Blackboard).
public class AbilityContext
{
    public IAbilityCaster Owner;
    public IAbilityTarget ResolvedTarget;
    public Vector2 AimPosition;
    public float Direction;
    public AbilityDefinition Definition;
    public AbilityInstance Instance;
    public AbilityServices Services;
    public CancellationToken Token;
    public Blackboard Blackboard = new Blackboard();

    private List<Action> cleanups;

    // Гарантированный откат временных эффектов (бафов, VFX): действия выполняются
    // в finally раннера — в том числе при отмене каста и исключениях.
    public void RegisterCleanup(Action cleanup)
    {
        if (cleanup == null) return;
        cleanups ??= new List<Action>();
        cleanups.Add(cleanup);
    }

    // Выполняет откаты в обратном порядке; ошибки одного отката не блокируют остальные.
    public void RunCleanups()
    {
        if (cleanups == null) return;

        for (int i = cleanups.Count - 1; i >= 0; i--)
        {
            try
            {
                cleanups[i]();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        cleanups.Clear();
    }
}
