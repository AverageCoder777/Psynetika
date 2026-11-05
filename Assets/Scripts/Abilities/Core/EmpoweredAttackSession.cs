using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/*
Активный режим усиленных атак, выданный способностью. Каждый взмах владельца тратит заряд и запускает
ноды onSwing в контексте исходного каста: Blackboard и токен отмены общие, направление — текущего взмаха.
Режим кончается по таймеру, по исчерпании зарядов или при отмене каста.
*/
public class EmpoweredAttackSession
{
    private readonly AbilityContext castContext;
    private readonly List<AbilityNode> onSwing;
    private bool ended;

    public int ChargesLeft { get; private set; }
    public float EndsAt { get; }
    public float SwingTimeMultiplier { get; }
    public float SwingMomentNormalized { get; }

    public bool IsActive => !ended && ChargesLeft > 0 && Time.time < EndsAt
                            && !castContext.Token.IsCancellationRequested;

    public EmpoweredAttackSession(AbilityContext castContext, List<AbilityNode> onSwing, int charges, float duration,
        float swingTimeMultiplier, float swingMomentNormalized)
    {
        this.castContext = castContext;
        this.onSwing = onSwing;
        ChargesLeft = Mathf.Max(1, charges);
        EndsAt = Time.time + Mathf.Max(0.1f, duration);
        SwingTimeMultiplier = Mathf.Clamp(swingTimeMultiplier, 0.1f, 1f);
        SwingMomentNormalized = Mathf.Clamp01(swingMomentNormalized);
    }

    public void End()
    {
        ended = true;
    }

    // Тратит заряд и запускает onSwing. false — режим уже кончился, взмах обычный.
    public bool TrySwing(float direction)
    {
        if (!IsActive)
        {
            return false;
        }

        ChargesLeft--;
        float swingDirection = Mathf.Approximately(direction, 0f) ? 1f : Mathf.Sign(direction);
        AbilityContext ctx = new AbilityContext
        {
            Owner = castContext.Owner,
            AimPosition = castContext.Owner.Center + Vector2.right * swingDirection,
            Direction = swingDirection,
            Definition = castContext.Definition,
            Instance = castContext.Instance,
            Services = castContext.Services,
            Token = castContext.Token,
            Blackboard = castContext.Blackboard
        };
        RunSwing(ctx).Forget();
        return true;
    }

    private async UniTaskVoid RunSwing(AbilityContext ctx)
    {
        try
        {
            await AbilityNodeList.Run(onSwing, ctx);
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            ctx.RunCleanups();
        }
    }
}
