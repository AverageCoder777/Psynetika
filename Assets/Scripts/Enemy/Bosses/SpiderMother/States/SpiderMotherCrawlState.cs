using System.Collections.Generic;
using UnityEngine;

/*
Мама паук переползает (роль Relocate): идёт по маршруту арены точка за точкой и по прибытии
переходит в заданный режим. На вертикальных участках прижимается к стене, на горизонтальных — стоит на земле.
Не атакует и уязвима — удобный момент, чтобы её подстрелить.
*/
public class SpiderMotherCrawlState : EnemyStates
{
    private readonly SpiderMotherController boss;
    private readonly List<SpiderRoutePoint> route = new();

    private SpiderSpot destination;
    private EnemyStateId next;
    private int index;

    public SpiderMotherCrawlState(SpiderMotherController controller, EnemyStateMachine stateMachine)
        : base(controller, stateMachine)
    {
        boss = controller;
    }

    // Ползти к точке to и по прибытии перейти в режим then.
    public bool Begin(SpiderSpot to, EnemyStateId then)
    {
        destination = to;
        next = then;
        boss.Arena.BuildRoute(boss.Spot, to, route);
        return ChangeState(EnemyStateId.Relocate);
    }

    public override void Enter()
    {
        index = 0;
        StartSegment();
    }

    public override void LogicUpdate()
    {
        if (index >= route.Count)
        {
            boss.Spot = destination;
            ChangeState(next);
            return;
        }
        UpdateLocomotionFlags();
    }

    public override void PhysicsUpdate()
    {
        if (index >= route.Count)
        {
            Movement.Stop();
            return;
        }

        SpiderRoutePoint point = route[index];
        if (Movement.IsAt(point.Position))
        {
            boss.SnapTo(point.Position);
            Movement.Stop();
            index++;
            StartSegment();
            return;
        }

        Movement.Execute(MoveIntent.To(point.Position, boss.Boss.wallPhase.crawlSpeedScale));
    }

    public override void Exit()
    {
        ClearLocomotionFlags();
        Movement.Stop();
    }

    // Поза на участок: вверх-вниз — по стене, вбок — по земле.
    private void StartSegment()
    {
        if (index >= route.Count) return;

        SpiderRoutePoint point = route[index];
        Vector2 delta = point.Position - Movement.Body.position;
        boss.SetPose(Mathf.Abs(delta.y) > Mathf.Abs(delta.x) ? point.Side : 0);
    }
}
