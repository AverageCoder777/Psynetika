using System;
using UnityEngine;

/*
Одна атака врага, описанная данными: замах -> момент удара (Execute) -> отход.

Новый вид атаки = новый наследник с [AddTypeMenu] и без единой правки контроллера/состояний:
класс сразу появится в выпадающем списке EnemyConfig.attacks (SubclassSelector).
Выбором подходящей атаки занимается EnemyAttack.PickAttack(), тайминги проигрывает EnemyAttackState.
*/
[Serializable]
public abstract class EnemyAttackModule
{
    [Tooltip("Подпись для читаемости списка в инспекторе, на логику не влияет")]
    public string label;

    [Tooltip("Параметр аниматора: как trigger в начале замаха и как bool на время замаха. Пусто = без анимации")]
    public string animatorParameter = "Hit";

    [Tooltip("Замах до момента удара, сек. Делится на скорость атаки врага")]
    [Min(0.05f)] public float windup = 1f;

    [Tooltip("Отход после удара, сек. Делится на скорость атаки врага")]
    [Min(0f)] public float recovery = 0f;

    [Tooltip("Персональный кулдаун этой атаки, сек. 0 = только общий attackCooldown из конфига")]
    [Min(0f)] public float cooldown = 0f;

    [Tooltip("Минимальная дистанция по X до игрока")]
    [Min(0f)] public float minRange = 0f;

    [Tooltip("Максимальная дистанция по X до игрока. 0 = без ограничения, границей служит зона атаки сенсора")]
    [Min(0f)] public float maxRange = 0f;

    [Tooltip("Повернуться к игроку в начале замаха")]
    public bool faceTargetOnStart = true;

    // Дополнительное условие применимости (готовность способности, наличие префаба и т.п.).
    // Дистанция и кулдауны проверяются до этого вызова в EnemyAttack.PickAttack().
    public virtual bool CanUse(EnemyController controller) => true;

    // Момент удара: нанести урон, выстрелить, скастовать.
    public abstract void Execute(EnemyController controller);

    protected static IAbilityTarget ResolveTarget(EnemyController controller)
    {
        return controller != null && controller.Sensor != null ? controller.Sensor.PlayerTarget : null;
    }
}
