using System;
using UnityEngine;
using Random = UnityEngine.Random;

/*
Плевок ядом (Мама паук): капля летит дугой туда, где стоит игрок, и растекается ядовитой лужей.
Лужа лежит puddleLifetimeMin..Max секунд и накладывает кратковременное отравление на того,
кто в ней стоит. Капля, задевшая игрока в полёте, растекается прямо под ним.

Точка приземления — поверхность под игроком (пол или платформа из groundMask), поэтому лужи
остаются и на платформах, с которых по паучихе стреляют. Логика — PoisonGlobProjectile и PoisonPuddle.
*/
[Serializable]
[AddTypeMenu("Дальний бой/Плевок ядом (лужа)")]
public class PoisonSpitAttackModule : EnemyAttackModule
{
    [Tooltip("Префаб с компонентом PoisonGlobProjectile")]
    public PoisonGlobProjectile globPrefab;

    [Tooltip("Префаб с компонентом PoisonPuddle")]
    public PoisonPuddle puddlePrefab;

    [Tooltip("Поверхности, на которых растекается лужа: пол и платформы")]
    public LayerMask groundMask;

    [Tooltip("Скорость капли по прямой, ед/сек: из неё считается время полёта")]
    [Min(0.1f)] public float speed = 10f;

    [Tooltip("Время полёта не меньше и не больше этих значений, сек")]
    [Min(0.05f)] public float minFlightTime = 0.7f;
    [Min(0.05f)] public float maxFlightTime = 1.6f;

    [Tooltip("Высота дуги над прямой линией полёта, ед")]
    [Min(0f)] public float arcHeight = 3f;

    [Tooltip("Разброс точки приземления по X вокруг игрока, ед")]
    [Min(0f)] public float landingSpread = 0.5f;

    [Header("Лужа")]
    [Tooltip("Сколько лежит лужа: от min до max, сек")]
    [Min(0.1f)] public float puddleLifetimeMin = 5f;
    [Min(0.1f)] public float puddleLifetimeMax = 7f;

    [Tooltip("Отравление от лужи: кратковременное")]
    public PoisonSettings poison = new()
    {
        duration = 1.5f,
        tickInterval = 0.5f,
        tickDamage = 3
    };

    public override bool CanUse(EnemyController controller) =>
        globPrefab != null && puddlePrefab != null && SpitAim.HasTarget(controller);

    public override void Execute(EnemyController controller)
    {
        if (!SpitAim.TryGetTargetCenter(controller, out Vector2 target)) return;

        target.x += Random.Range(-landingSpread, landingSpread);
        RaycastHit2D ground = Physics2D.Raycast(target, Vector2.down, 40f, groundMask);
        if (ground.collider == null) return;

        Vector2 origin = SpitAim.BodyCenter(controller);
        Vector2 landing = ground.point;
        float flightTime = Mathf.Clamp(Vector2.Distance(origin, landing) / speed,
            Mathf.Min(minFlightTime, maxFlightTime), Mathf.Max(minFlightTime, maxFlightTime));

        PoisonPuddle puddle = puddlePrefab;
        PoisonSettings puddlePoison = poison;
        float lifetime = Random.Range(Mathf.Min(puddleLifetimeMin, puddleLifetimeMax), Mathf.Max(puddleLifetimeMin, puddleLifetimeMax));

        PoisonGlobProjectile glob = UnityEngine.Object.Instantiate(globPrefab, origin, Quaternion.identity);
        glob.Launch(landing, flightTime, arcHeight, groundMask,
            point => PoisonPuddle.Spawn(puddle, point, lifetime, puddlePoison));
    }
}
