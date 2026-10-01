using System;
using UnityEngine;

/*
Тактика: куда враг хочет двигаться в текущем режиме. В EnemyConfig их две:
  idle   — пока игрок не замечен (стоять, патрулировать, блуждать),
  engage — в бою между атаками (преследовать, держать дистанцию, кружить).

Тактика выдаёт MoveIntent и не знает, ходит враг, летает или лазает — это решает тело
(EnemyLocomotion). Поэтому одна тактика работает на любом теле.

Как и тело, модуль — только настройки (конфиг общий), состояние каждого врага — в EnemyTacticRuntime.
*/
[Serializable]
public abstract class EnemyTactic
{
    public abstract EnemyTacticRuntime CreateRuntime(EnemyController owner);
}

public abstract class EnemyTacticRuntime
{
    protected readonly EnemyController owner;

    protected EnemyMovement Movement => owner.Movement;
    protected EnemySensor Sensor => owner.Sensor;
    protected EnemyAttack Attack => owner.Attack;
    protected Vector2 Position => Movement.Body.position;

    protected EnemyTacticRuntime(EnemyController owner)
    {
        this.owner = owner;
    }

    public virtual void Enter() { }

    // Вызывается из PhysicsUpdate состояния: намерение сразу исполняется тем же физическим шагом,
    // поэтому тактика видит результат прошлого шага (Movement.LastMoveBlocked) без рассинхрона.
    public abstract MoveIntent Tick(float deltaTime);

    public virtual void Exit() { }
}
