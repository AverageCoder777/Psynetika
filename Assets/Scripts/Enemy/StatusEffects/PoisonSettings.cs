using System;
using UnityEngine;

// Параметры отравления. Задаются источником (взрыв тела паука, способность…), а не носителем:
// разные источники травят по-разному, поэтому в StatusEffectConfig их нет.
[Serializable]
public class PoisonSettings
{
    [Tooltip("Длительность отравления, сек")]
    [Min(0.1f)] public float duration = 2f;

    [Tooltip("Интервал между тиками урона, сек")]
    [Min(0.05f)] public float tickInterval = 0.5f;

    [Tooltip("Урон за тик (сырой, мимо статус-пайплайна)")]
    [Min(1)] public int tickDamage = 2;

    [Tooltip("VFX на отравленном на время эффекта. Пусто = без VFX")]
    public GameObject vfxPrefab;
}
