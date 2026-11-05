using System;
using UnityEngine;

/*
Захват лица с QTE (нейрончик). Игрок в зоне удара — враг прыгает на лицо, от этого не уклониться.
Игрок заблокирован и проходит QTE: одна случайная кнопка, каждое нажатие сокращает захват.
  успел   — враг сброшен с лица и на stunDuration впадает в замешательство (не атакует, можно добить);
  не успел — персонаж совершает самоубийство, враг спрыгивает и продолжает бой.

Режимы «на лице» (Grab) и «замешательство» (Stagger) модуль регистрирует сам — отдельный
контроллер-наследник не нужен. Логика режима — EnemyFaceGrabState, со стороны игрока — GrabbedState.
*/
[Serializable]
[AddTypeMenu("Захват/Прыжок на лицо (QTE)")]
public class FaceGrabAttackModule : EnemyAttackModule
{
    [Header("QTE")]
    public QteSettings qte = new();

    [Header("На лице")]
    [Tooltip("Где держится враг: смещение от верхней точки коллайдера игрока. X зеркалится по взгляду игрока")]
    public Vector2 faceOffset = new(0.25f, -0.35f);

    [Tooltip("Bool аниматора, пока враг на лице. Пусто = без анимации")]
    public string latchedAnimatorParameter = "Grabbing";

    [Header("Сброс с лица (QTE пройдено)")]
    [Tooltip("Скорость отлёта: X — в сторону взгляда игрока, Y — вверх")]
    public Vector2 throwOffVelocity = new(4f, 3f);

    [Tooltip("Замешательство после сброса, сек: не атакует и не двигается")]
    [Min(0f)] public float stunDuration = 2.5f;

    [Tooltip("Bool аниматора на время замешательства. Пусто = без анимации")]
    public string stunnedAnimatorParameter = "Stunned";

    [Header("Провал QTE")]
    [Tooltip("Скорость, с которой враг спрыгивает с погибшего героя: X — в сторону взгляда героя, Y — вверх")]
    public Vector2 jumpOffVelocity = new(2f, 4f);

    // Схваченного или мёртвого игрока не хватаем повторно — второй нейрончик ждёт своей очереди.
    public override bool CanUse(EnemyController controller) =>
        controller.GetState(EnemyStateId.Grab) is EnemyFaceGrabState state && state.CanLatch();

    public override void Execute(EnemyController controller)
    {
        if (controller.GetState(EnemyStateId.Grab) is EnemyFaceGrabState state)
        {
            state.TryLatch();
        }
    }

    public override void RegisterStates(EnemyController controller)
    {
        controller.RegisterState(EnemyStateId.Grab, new EnemyFaceGrabState(controller, controller.StateMachine, this));
        if (!controller.HasState(EnemyStateId.Stagger))
        {
            controller.RegisterState(EnemyStateId.Stagger, new EnemyStunnedState(controller, controller.StateMachine));
        }
    }
}
