using System;
using UnityEngine;

// Блоки настроек EnemyConfig. Вынесены в отдельные [Serializable]-классы,
// чтобы конфиг читался группами, а новые параметры добавлялись в один блок.

// Как враг замечает игрока.
[Serializable]
public class EnemyPerceptionSettings
{
    public enum DetectionMode
    {
        Auto = 0,
        Triggers = 1,
        Radius = 2
    }

    [Tooltip("Auto: если на EnemySensor назначены триггер-коллайдеры — используются они, иначе радиусы ниже")]
    public DetectionMode mode = DetectionMode.Auto;

    [Tooltip("Радиус агро: с этой дистанции враг начинает преследовать")]
    [Min(0f)] public float followRange = 8f;

    [Tooltip("Радиус атаки: с этой дистанции враг переходит в атаку")]
    [Min(0f)] public float attackRange = 1.5f;

    [Tooltip("Вертикальный допуск: игрок выше/ниже этой дельты не считается замеченным. 0 = без ограничения")]
    [Min(0f)] public float verticalTolerance = 3f;

    [Tooltip("Насколько дальше радиуса агро враг тянет цель, прежде чем потерять её (гистерезис против дрожания на границе)")]
    [Min(0f)] public float loseTargetPadding = 1.5f;
}

// Патруль, пока игрок не замечен.
[Serializable]
public class EnemyPatrolSettings
{
    [Tooltip("Выключено = враг стоит на месте, пока не увидит игрока")]
    public bool enabled = false;

    [Tooltip("Половина маршрута: враг ходит от точки спавна на ±distance по X")]
    [Min(0.1f)] public float distance = 3f;

    [Tooltip("Пауза на краю маршрута, сек")]
    [Min(0f)] public float waitTime = 1f;

    [Tooltip("Доля от moveSpeed на патруле")]
    [Range(0.1f, 2f)] public float speedScale = 0.5f;
}

// Проверки земли под ногами: общие для патруля и преследования.
[Serializable]
public class EnemyGroundSettings
{
    [Tooltip("Не шагать за край платформы и не упираться в стену. Работает только при заполненном Ground Mask")]
    public bool stopAtLedges = false;

    [Tooltip("Слои земли/стен. Пусто = проверки выключены")]
    public LayerMask groundMask;

    [Tooltip("На какую глубину искать землю перед собой")]
    [Min(0.05f)] public float ledgeProbeDepth = 1f;

    [Tooltip("На каком расстоянии перед собой считать стену препятствием")]
    [Min(0f)] public float wallProbeDistance = 0.1f;
}
