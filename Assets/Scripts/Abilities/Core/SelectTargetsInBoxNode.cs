using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
[AddTypeMenu("Цели/Цели в прямоугольнике")]
public class SelectTargetsInBoxNode : AbilityNode
{
    private static readonly List<Collider2D> OverlapBuffer = new List<Collider2D>(16);

    [BlackboardKeyOutput] public string outputTargetsKey = "area.targets";
    public Vector2 boxSize = new Vector2(2.5f, 1.5f);
    public float forwardOffset = 1.25f;
    public string layerName = "Enemy";
    [Tooltip("Не бить союзников: пропускать цели той же команды, что и кастер (Neutral проходит всегда)")]
    public bool excludeOwnTeam = true;

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.Owner == null)
        {
            return UniTask.FromResult(NodeResult.Failure);
        }

        Vector2 center = ctx.Owner.Center + new Vector2(ctx.Direction * forwardOffset, 0f);
        int mask = LayerMask.GetMask(string.IsNullOrWhiteSpace(layerName) ? "Enemy" : layerName);

        ContactFilter2D filter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers };
        filter.SetLayerMask(mask);
        int hitCount = Physics2D.OverlapBox(center, boxSize, 0f, filter, OverlapBuffer);

        List<IAbilityTarget> targets = new List<IAbilityTarget>(hitCount);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = OverlapBuffer[i];
            if (hit == null) continue;

            IAbilityTarget target = hit.GetComponentInParent<IAbilityTarget>();
            if (target == null || !target.IsAlive || targets.Contains(target))
            {
                continue;
            }

            if (excludeOwnTeam && target.Team != Team.Neutral && target.Team == ctx.Owner.Team)
            {
                continue;
            }

            targets.Add(target);
        }

        ctx.Blackboard.Set(outputTargetsKey, targets);
        return UniTask.FromResult(NodeResult.Success);
    }
}
