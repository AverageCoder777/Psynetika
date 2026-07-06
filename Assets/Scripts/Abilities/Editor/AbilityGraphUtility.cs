using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Редакторские знания о графе способности: имена/категории нод, краткие summary
// для заголовков, обход графа рефлексией, сбор ключей Blackboard и валидация.
public static class AbilityGraphUtility
{
    // ---------- Имена и меню ----------

    public static string GetDisplayName(Type nodeType)
    {
        AddTypeMenuAttribute menu = nodeType.GetCustomAttribute<AddTypeMenuAttribute>();
        string fromMenu = menu?.GetTypeNameWithoutPath();
        return !string.IsNullOrEmpty(fromMenu) ? fromMenu : ObjectNames.NicifyVariableName(nodeType.Name);
    }

    public static string GetMenuPath(Type nodeType)
    {
        AddTypeMenuAttribute menu = nodeType.GetCustomAttribute<AddTypeMenuAttribute>();
        return !string.IsNullOrWhiteSpace(menu?.MenuName) ? menu.MenuName : "Прочее/" + ObjectNames.NicifyVariableName(nodeType.Name);
    }

    public static List<Type> GetNodeTypes()
    {
        List<Type> types = new();
        foreach (Type type in TypeCache.GetTypesDerivedFrom<AbilityNode>())
        {
            if (type.IsAbstract || type.IsGenericType) continue;
            if (type.GetCustomAttribute<HideInTypeMenuAttribute>() != null) continue;
            types.Add(type);
        }
        types.Sort((a, b) => string.Compare(GetMenuPath(a), GetMenuPath(b), StringComparison.Ordinal));
        return types;
    }

    // ---------- Summary для заголовка ноды ----------

    public static string GetSummary(AbilityNode node)
    {
        switch (node)
        {
            case null: return "пустая нода!";
            case DamageNode n: return $"{n.amount} урона ({n.type})";
            case ApplyDamageToTargetsNode n: return $"'{n.inputDamageKey}' по целям '{n.inputTargetsKey}' ({n.type})";
            case ComputeOwnerHitDamageNode n: return $"базовый ×{n.damageMultiplier} → '{n.outputDamageKey}'";
            case DrainOwnerHpNode n: return $"-{Mathf.RoundToInt(n.drainPercent * 100)}% max HP, стоп на {Mathf.RoundToInt(n.criticalHpPercent * 100)}%";
            case SelectTargetsInBoxNode n: return $"{n.boxSize.x}×{n.boxSize.y}, слой '{n.layerName}' → '{n.outputTargetsKey}'";
            case SpawnProjectileNode n: return n.projectile != null ? n.projectile.name : "снаряд не задан!";
            case SpawnVfxNode n: return n.vfxPrefab != null
                ? $"{n.vfxPrefab.name}{(n.destroyOnCastEnd ? " (до конца каста)" : $" ({n.lifetime} с)")}"
                : "префаб не задан!";
            case ApplyStatBoostNode n: return $"{n.stat} {(n.amount >= 0 ? "+" : "")}{n.amount}{(n.revertOnEnd ? "" : " (навсегда)")}";
            case IncrementCounterNode n: return $"'{n.key}' до {n.max}";
            case DelayNode n: return $"{n.seconds} с";
            case TimedTickNode n: return $"{n.duration} с, тик {n.tickInterval} с, нод: {CountOf(n.onTick)}";
            case SequenceNode n: return $"нод: {CountOf(n.children)}";
            case BranchNode n: return $"then: {CountOf(n.onSuccess)}, else: {CountOf(n.onFailure)}";
            case ParallelNode n: return $"веток: {CountOf(n.children)}";
            default: return string.Empty;
        }
    }

    private static int CountOf(List<AbilityNode> list) => list?.Count ?? 0;

    // ---------- Обход графа ----------

    // Обходит все ноды, достижимые из ассета (включая вложенные списки и графы
    // ссылающихся ProjectileDefinition — они шарят Blackboard каста).
    public static void VisitNodes(UnityEngine.Object rootAsset, Action<AbilityNode> visitor)
    {
        HashSet<object> visited = new();

        void VisitNode(AbilityNode node)
        {
            if (node == null || !visited.Add(node)) return;
            visitor(node);

            foreach (FieldInfo field in node.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                object value = field.GetValue(node);
                switch (value)
                {
                    case AbilityNode child:
                        VisitNode(child);
                        break;
                    case IEnumerable enumerable when typeof(IEnumerable<AbilityNode>).IsAssignableFrom(field.FieldType):
                        foreach (object item in enumerable) VisitNode(item as AbilityNode);
                        break;
                    case ProjectileDefinition projectile:
                        VisitProjectile(projectile);
                        break;
                }
            }
        }

        void VisitList(List<AbilityNode> nodes)
        {
            if (nodes == null) return;
            foreach (AbilityNode node in nodes) VisitNode(node);
        }

        void VisitProjectile(ProjectileDefinition projectile)
        {
            if (projectile == null || !visited.Add(projectile)) return;
            VisitList(projectile.onSpawn);
            VisitList(projectile.onHit);
            VisitList(projectile.onExpire);
        }

        switch (rootAsset)
        {
            case AbilityDefinition ability:
                VisitList(ability.root);
                break;
            case ProjectileDefinition projectile:
                VisitProjectile(projectile);
                break;
        }
    }

    // ---------- Ключи Blackboard ----------

    public static void CollectKeys(UnityEngine.Object rootAsset, HashSet<string> outputs, HashSet<string> inputs)
    {
        VisitNodes(rootAsset, node =>
        {
            foreach (FieldInfo field in node.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.FieldType != typeof(string)) continue;

                string value = field.GetValue(node) as string;
                if (string.IsNullOrWhiteSpace(value)) continue;

                if (field.GetCustomAttribute<BlackboardKeyOutputAttribute>() != null) outputs?.Add(value);
                if (field.GetCustomAttribute<BlackboardKeyInputAttribute>() != null) inputs?.Add(value);
            }
        });
    }

    public static List<string> CollectAllKeys(UnityEngine.Object rootAsset)
    {
        HashSet<string> keys = new();
        CollectKeys(rootAsset, keys, keys);
        List<string> sorted = new(keys);
        sorted.Sort(StringComparer.Ordinal);
        return sorted;
    }

    // ---------- Валидация ----------

    public static List<string> Validate(UnityEngine.Object rootAsset)
    {
        List<string> problems = new();

        if (rootAsset is AbilityDefinition ability)
        {
            if (ability.root == null || ability.root.Count == 0)
            {
                problems.Add("Список нод пуст — способность ничего не делает.");
            }

            if (ability.animClip != null && !ability.matchDurationToClip &&
                Mathf.Abs(ability.animClip.length - ability.castDuration) > 0.05f)
            {
                problems.Add($"Клип '{ability.animClip.name}' длится {ability.animClip.length:0.00} с, а castDuration = {ability.castDuration:0.00} с — " +
                             "анимация обрежется или зависнет. Подгони castDuration или включи matchDurationToClip.");
            }
        }

        if (rootAsset is ProjectileDefinition projectileDef)
        {
            if (projectileDef.prefab == null)
            {
                problems.Add("Не задан префаб снаряда.");
            }
            else
            {
                if (projectileDef.prefab.GetComponent<Rigidbody2D>() == null)
                    problems.Add($"У префаба '{projectileDef.prefab.name}' нет Rigidbody2D — снаряд не полетит.");
                if (projectileDef.prefab.GetComponentInChildren<Collider2D>() == null)
                    problems.Add($"У префаба '{projectileDef.prefab.name}' нет Collider2D — снаряд ни во что не попадёт.");
            }

            if ((projectileDef.onHit == null || projectileDef.onHit.Count == 0) &&
                (projectileDef.onExpire == null || projectileDef.onExpire.Count == 0))
            {
                problems.Add("onHit и onExpire пусты — снаряд ничего не делает при попадании.");
            }
        }

        HashSet<string> outputs = new();
        HashSet<string> inputs = new();
        CollectKeys(rootAsset, outputs, inputs);

        VisitNodes(rootAsset, node =>
        {
            switch (node)
            {
                case SpawnProjectileNode n when n.projectile == null:
                    problems.Add("Спавн снаряда: не назначен ProjectileDefinition.");
                    break;
                case SpawnVfxNode n when n.vfxPrefab == null:
                    problems.Add("VFX на кастере: не назначен префаб эффекта.");
                    break;
                case SpawnProjectileNode n when n.projectile.prefab == null:
                    problems.Add($"Спавн снаряда: у '{n.projectile.name}' не задан префаб.");
                    break;
                case SelectTargetsInBoxNode n when !string.IsNullOrWhiteSpace(n.layerName) && LayerMask.NameToLayer(n.layerName) < 0:
                    problems.Add($"Цели в прямоугольнике: слой '{n.layerName}' не существует в проекте.");
                    break;
                case TimedTickNode n when n.onTick == null || n.onTick.Count == 0:
                    problems.Add("Тики по таймеру: список onTick пуст — эффект будет просто ждать.");
                    break;
            }
        });

        foreach (string input in inputs)
        {
            if (!outputs.Contains(input))
            {
                problems.Add($"Ключ '{input}' читается, но ни одна нода его не записывает — проверь порядок нод или опечатку.");
            }
        }

        return problems;
    }
}
