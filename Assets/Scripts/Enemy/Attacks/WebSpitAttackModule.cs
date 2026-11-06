using System;
using UnityEngine;

/*
Плевок паутиной (Мама паук): паутина летит по прямой в игрока. Попала — игрок опутан и проходит
QTE, как при захвате нейрончика (GrabbedState). По умолчанию провал QTE не убивает,
а только наносит урон (qte.killOnFail = false), — паутина держит игрока под ядом и следующими плевками.

Работает с любой дистанции: нужен только замеченный игрок. Логика снаряда — SpiderWebProjectile.
*/
[Serializable]
[AddTypeMenu("Дальний бой/Плевок паутиной (QTE)")]
public class WebSpitAttackModule : EnemyAttackModule
{
    [Tooltip("Префаб с компонентом SpiderWebProjectile")]
    public SpiderWebProjectile projectilePrefab;

    [Tooltip("Скорость полёта паутины, ед/сек")]
    [Min(0.1f)] public float speed = 11f;

    [Tooltip("Сколько секунд паутина летит, прежде чем исчезнуть")]
    [Min(0.1f)] public float lifetime = 4f;

    [Tooltip("Смещение точки плевка от центра тела к игроку, ед")]
    [Min(0f)] public float muzzleDistance = 0.6f;

    [Tooltip("Перекат проскакивает сквозь паутину")]
    public bool rollDodges = true;

    [Header("QTE")]
    public QteSettings qte = new()
    {
        pressesToEscape = 6,
        timeLimit = 2.5f,
        killOnFail = false,
        failDamage = 10
    };

    public override bool CanUse(EnemyController controller) =>
        projectilePrefab != null && SpitAim.HasTarget(controller);

    public override void Execute(EnemyController controller)
    {
        if (!SpitAim.TryGetTargetCenter(controller, out Vector2 target)) return;

        Vector2 origin = SpitAim.BodyCenter(controller);
        Vector2 direction = target - origin;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : new Vector2(controller.Movement.FacingDirection, 0f);

        SpiderWebProjectile web = UnityEngine.Object.Instantiate(projectilePrefab, origin + direction * muzzleDistance, Quaternion.identity);
        web.Launch(direction * speed, lifetime, qte, rollDodges);
    }
}

// Общая наводка плевков: центр тела стреляющего и центр тела игрока.
internal static class SpitAim
{
    public static bool HasTarget(EnemyController controller)
    {
        IAbilityTarget target = controller.Sensor != null ? controller.Sensor.PlayerTarget : null;
        return target != null && target.IsAlive && controller.Sensor.PlayerInFollowRange;
    }

    public static bool TryGetTargetCenter(EnemyController controller, out Vector2 center)
    {
        center = default;
        Transform player = controller.Sensor != null ? controller.Sensor.PlayerTransform : null;
        if (player == null) return false;

        center = player.TryGetComponent(out Collider2D body) ? (Vector2)body.bounds.center : (Vector2)player.position;
        return true;
    }

    public static Vector2 BodyCenter(EnemyController controller)
    {
        Collider2D body = controller.Movement != null ? controller.Movement.BodyCollider : null;
        return body != null ? (Vector2)body.bounds.center : (Vector2)controller.transform.position;
    }
}
