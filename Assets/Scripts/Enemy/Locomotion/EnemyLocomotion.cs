using System;
using UnityEngine;

/*
«Тело» врага: как он физически перемещается (ходьба, полёт, лазание). Выбирается в EnemyConfig.body.

Модуль — только настройки: EnemyConfig общий для всех экземпляров врага, поэтому рантайм-состояние
(форма паука, выбранное направление полёта и т.п.) живёт в EnemyLocomotionRuntime, который
CreateRuntime() создаёт на каждого врага отдельно.

Новое тело = наследник с [AddTypeMenu] + свой рантайм; тактики и состояния править не нужно.
*/
[Serializable]
public abstract class EnemyLocomotion
{
    public abstract EnemyLocomotionRuntime CreateRuntime(EnemyMovement movement);
}

// Рантайм тела одного врага. Вызывается только из EnemyMovement, на физическом шаге.
public abstract class EnemyLocomotionRuntime
{
    protected readonly EnemyMovement movement;

    protected Rigidbody2D Body => movement.Body;

    protected EnemyLocomotionRuntime(EnemyMovement movement)
    {
        this.movement = movement;
    }

    // Один раз при инициализации: настроить Rigidbody2D под этот вид движения.
    public virtual void Attach() { }

    /*
    Шаг к точке со скоростью speed (уже с учётом бафов и SpeedScale намерения).
    Возвращает false, если путь перекрыт (обрыв, стена) — тактика решает, что с этим делать.
    */
    public abstract bool MoveTowards(Vector2 destination, float speed, float deltaTime);

    // Погасить собственное движение. Гравитацию не трогает.
    public abstract void Stop();

    // Достигнута ли точка: наземному телу важен только X, летающему — обе оси.
    public abstract bool IsAt(Vector2 point);
}
