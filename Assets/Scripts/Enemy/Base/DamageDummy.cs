using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DamageDummy : MonoBehaviour, IAbilityTarget
{
    [SerializeField, Min(0.25f)] private float averageWindowSeconds = 5f;
    [SerializeField] private bool logDamageStats = true;

    private readonly Queue<DamageSample> windowSamples = new Queue<DamageSample>();
    private float windowDamage = 0f;

    public void TakeDamage(int damage)
    {
        int safeDamage = Mathf.Max(0, damage);
        if (safeDamage <= 0)
        {
            return;
        }

        float now = Time.time;
        windowSamples.Enqueue(new DamageSample(now, safeDamage));
        windowDamage += safeDamage;

        float safeWindow = Mathf.Max(averageWindowSeconds, 0.25f);
        while (windowSamples.Count > 0 && now - windowSamples.Peek().Time > safeWindow)
        {
            DamageSample oldSample = windowSamples.Dequeue();
            windowDamage -= oldSample.Damage;
        }

        if (!logDamageStats)
        {
            return;
        }

        float averageHit = windowSamples.Count > 0 ? windowDamage / windowSamples.Count : 0f;
        float dps = windowDamage / safeWindow;
        Debug.Log($"[DamageDummy] Hit: {safeDamage} | AvgDamage ({safeWindow:0.0}s): {averageHit:0.0} | DPS: {dps:0.0}");
    }

    private readonly struct DamageSample
    {
        public DamageSample(float time, int damage)
        {
            Time = time;
            Damage = damage;
        }

        public float Time { get; }
        public int Damage { get; }
    }

    // IAbilityTarget
    Transform IAbilityTarget.Transform => transform;
    bool IAbilityTarget.IsAlive => true;
    Team IAbilityTarget.Team => global::Team.Neutral;
    void IAbilityTarget.ReceiveDamage(DamageEvent ev) => TakeDamage(Mathf.RoundToInt(ev.Amount));
}
