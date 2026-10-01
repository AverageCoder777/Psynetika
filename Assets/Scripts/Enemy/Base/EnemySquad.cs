using System.Collections.Generic;
using UnityEngine;

/*
Очередь атак команды врагов (EnemyConfig.squad): в каждый момент очередь держит один враг —
он идёт на сближение и бьёт, остальные ждут. Очередь берётся тактикой перед сближением
и состоянием атаки на время удара, а отпускается в их Exit().

Держатель — EnemyAttack: уничтоженный враг для Unity сравнивается с null, поэтому
очередь не зависнет на погибшем, даже если Exit() не отработал.
*/
public sealed class EnemySquad
{
    private static readonly Dictionary<string, EnemySquad> squads = new();

    private EnemyAttack holder;
    private EnemyAttack lastHolder;
    private float heldSince;
    private float freeAt;

    // Реестр статический: при отключённом Domain Reload его нужно чистить вручную между запусками.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => squads.Clear();

    public static EnemySquad Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (!squads.TryGetValue(id, out EnemySquad squad))
        {
            squad = new EnemySquad();
            squads[id] = squad;
        }
        return squad;
    }

    // Может ли who начать атаку прямо сейчас. Ничего не меняет.
    public bool CanAcquire(EnemyAttack who, float maxTurnTime)
    {
        if (who == null) return false;
        if (holder == who) return true;
        if (holder != null && Time.time - heldSince < maxTurnTime) return false;

        // Пауза между атаками — для остальных: тот, кто только что отпустил очередь
        // при переходе «сближение → удар», забирает её обратно сразу.
        return who == lastHolder || Time.time >= freeAt;
    }

    public bool TryAcquire(EnemyAttack who, float maxTurnTime)
    {
        if (!CanAcquire(who, maxTurnTime)) return false;

        if (holder != who)
        {
            holder = who;
            heldSince = Time.time;
        }
        return true;
    }

    public void Release(EnemyAttack who, float gap)
    {
        if (who == null || holder != who) return;

        holder = null;
        lastHolder = who;
        freeAt = Time.time + Mathf.Max(0f, gap);
    }
}
