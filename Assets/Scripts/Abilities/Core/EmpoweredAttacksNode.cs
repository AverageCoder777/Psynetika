using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/*
Временный режим: обычные атаки владельца становятся быстрыми усиленными взмахами, каждый тратит заряд
и запускает onSwing (например, расчёт урона + спавн волны). Нода ждёт конца режима — по таймеру
или когда кончатся заряды, — поэтому способность считается кастующейся, пока режим активен.
*/
[Serializable]
[AddTypeMenu("Режимы/Усиленные атаки")]
public class EmpoweredAttacksNode : AbilityNode
{
    [Tooltip("Сколько длится режим, сек")]
    [Min(0.1f)] public float duration = 10f;

    [Tooltip("Сколько усиленных взмахов доступно за режим")]
    [Min(1)] public int charges = 4;

    [Tooltip("Длительность усиленного взмаха относительно обычного: 0.5 — вдвое быстрее")]
    [Range(0.1f, 1f)] public float swingTimeMultiplier = 0.5f;

    [Tooltip("Момент взмаха (доля его длительности), в который срабатывает onSwing")]
    [Range(0f, 1f)] public float swingMomentNormalized = 0.5f;

    [SerializeReference, SubclassSelector]
    public List<AbilityNode> onSwing = new List<AbilityNode>();

    public override async UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.Owner is not IEmpoweredAttackOwner owner)
        {
            Debug.LogWarning("[EmpoweredAttacksNode] Owner does not implement IEmpoweredAttackOwner.");
            return NodeResult.Failure;
        }

        EmpoweredAttackSession session = new EmpoweredAttackSession(ctx, onSwing, charges, duration,
            swingTimeMultiplier, swingMomentNormalized);
        owner.BeginEmpoweredAttacks(session);
        ctx.RegisterCleanup(() =>
        {
            session.End();
            owner.EndEmpoweredAttacks(session);
        });

        await UniTask.WaitUntil(() => !session.IsActive, cancellationToken: ctx.Token);
        return NodeResult.Success;
    }
}
