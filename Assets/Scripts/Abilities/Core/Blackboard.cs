using System.Collections.Generic;
using UnityEngine;

// Пер-кастовое хранилище данных между нодами. Ключи задаются в инспекторе нод,
// доступ — только через типизированные методы: конверсия int<->float централизована здесь,
// чтобы ноды не разбирали object вручную.
public class Blackboard
{
    private readonly Dictionary<string, object> values = new();

    public void Set<T>(string key, T value)
    {
        values[key] = value;
    }

    public bool TryGet<T>(string key, out T value)
    {
        if (values.TryGetValue(key, out object raw))
        {
            if (raw is T typed)
            {
                value = typed;
                return true;
            }

            if (typeof(T) == typeof(float) && raw is int intValue)
            {
                value = (T)(object)(float)intValue;
                return true;
            }

            if (typeof(T) == typeof(int) && raw is float floatValue)
            {
                value = (T)(object)Mathf.RoundToInt(floatValue);
                return true;
            }
        }

        value = default;
        return false;
    }

    public T Get<T>(string key, T fallback = default)
    {
        return TryGet(key, out T value) ? value : fallback;
    }

    public bool Has(string key) => values.ContainsKey(key);

    public void Remove(string key) => values.Remove(key);
}
