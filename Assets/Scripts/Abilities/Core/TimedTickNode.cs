using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
[AddTypeMenu("Тайминг/Тики по таймеру")]
public class TimedTickNode : AbilityNode
{
    [Min(0.1f)] public float duration = 15f;
    [Min(0.1f)] public float tickInterval = 1.5f;

    [SerializeReference, SubclassSelector]
    public List<AbilityNode> onTick = new List<AbilityNode>();

    public override async UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        float interval = Mathf.Max(0.1f, tickInterval);
        float startTime = Time.time;
        float endTime = startTime + Mathf.Max(0.1f, duration);

        // Тики привязаны к абсолютному времени старта, чтобы задержки выполнения нод не накапливали дрейф.
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
                await UniTask.Delay(TimeSpan.FromSeconds(wait), cancellationToken: ctx.Token);
            }

            NodeResult result = await AbilityNodeList.Run(onTick, ctx);
            if (result == NodeResult.Cancelled)
            {
                return result;
            }
            // Failure тика (нечего дрейнить, счётчик стаков на максимуме) пропускает
            // остаток тика, но сам эффект продолжает тикать до конца duration.
        }

        return NodeResult.Success;
    }
}
