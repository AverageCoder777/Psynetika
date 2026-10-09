using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

// Оставляет в точке каста (у снаряда — в точке взрыва) область, которая по тикам
// лечит союзников кастера и бьёт остальных. Нода не ждёт окончания зоны: снаряд,
// породивший её, сразу возвращается в пул, а зона живёт своим AbilityZone.
[Serializable]
[AddTypeMenu("Зоны/Зона: лечение союзников + урон врагам")]
public class SpawnAbilityZoneNode : AbilityNode
{
    [Tooltip("Визуал области (опционально). Без префаба зона создаётся пустым объектом")]
    public GameObject zonePrefab;

    [Tooltip("Масштабировать префаб под радиус: визуал должен быть кругом диаметром 1 юнит")]
    public bool scaleVisualToRadius = true;

    public Vector2 offset = Vector2.zero;

    [Min(0.1f)] public float radius = 2.5f;
    [Min(0.1f)] public float duration = 5f;
    [Min(0.1f)] public float tickInterval = 1f;

    [Tooltip("Первый тик сразу при появлении зоны")]
    public bool tickOnSpawn = true;

    [Header("Союзники")]
    [Min(0f)] public float allyHealPerTick = 5f;

    [Header("Враги")]
    [Min(0f)] public float enemyDamagePerTick = 4f;
    public DamageType damageType = DamageType.Glitch;

    [Tooltip("Слои, на которых ищутся цели зоны")]
    public LayerMask targetLayers = ~0;

    public override UniTask<NodeResult> Execute(AbilityContext ctx)
    {
        if (ctx?.Owner == null)
        {
            return UniTask.FromResult(NodeResult.Failure);
        }

        Vector2 position = ctx.AimPosition + offset;
        GameObject zoneObject = zonePrefab != null
            ? UnityEngine.Object.Instantiate(zonePrefab, position, Quaternion.identity)
            : new GameObject("AbilityZone");
        zoneObject.transform.position = position;

        if (scaleVisualToRadius && zonePrefab != null)
        {
            zoneObject.transform.localScale = zonePrefab.transform.localScale * (radius * 2f);
        }

        if (!zoneObject.TryGetComponent(out AbilityZone zone))
        {
            zone = zoneObject.AddComponent<AbilityZone>();
        }
        zone.Initialize(this, ctx.Owner, ctx.Definition);
        return UniTask.FromResult(NodeResult.Success);
    }
}
