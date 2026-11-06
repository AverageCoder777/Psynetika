using System;
using UnityEngine;

/*
Конфиг мини-босса «Мама паук». Наследник EnemyConfig: здоровье, атаки (плевки паутиной и ядом),
обнаружение, статусы, лут и смерть настраиваются как у любого врага, а здесь — фазы боя.
Геометрия боя (стены, точки на стенах, место на земле) живёт не в конфиге, а в сцене — SpiderMotherArena.

Цикл боя:
  стена — сидит на точке стены и плюётся (attacksPerPerch атак), потом переползает на другую
          точку; сменив perchesBeforeDescent точек, спускается на землю;
  земля — закрывается щитом-паутиной и рожает микро-паучков; щит разбит — стан,
          не разбит за timeout — снимает щит и уползает обратно на стену;
  стан  — уязвима и ничего не делает stun.duration секунд, затем снова на стену.
*/
[CreateAssetMenu(menuName = "Psynetika/Bosses/Spider Mother Config", fileName = "SpiderMotherConfig")]
public class SpiderMotherConfig : EnemyConfig
{
    [Header("Босс")]
    [Tooltip("Имя над полоской здоровья")]
    public string bossTitle = "Мама паук";

    public SpiderMotherWallPhase wallPhase = new();
    public SpiderMotherGroundPhase groundPhase = new();
    public SpiderMotherStunPhase stun = new();
}

[Serializable]
public class SpiderMotherWallPhase
{
    [Tooltip("Сколько атак сделать с одной точки стены: от min до max")]
    [Min(1)] public int attacksPerPerchMin = 2;
    [Min(1)] public int attacksPerPerchMax = 3;

    [Tooltip("Сколько точек стены сменить перед спуском на землю: от min до max")]
    [Min(1)] public int perchesBeforeDescentMin = 2;
    [Min(1)] public int perchesBeforeDescentMax = 3;

    [Tooltip("Пауза после прибытия на точку до первой атаки, сек")]
    [Min(0f)] public float settleTime = 0.6f;

    [Tooltip("Пауза между атаками на одной точке: от min до max, сек")]
    [Min(0f)] public float attackPauseMin = 0.5f;
    [Min(0f)] public float attackPauseMax = 1.1f;

    [Tooltip("Шанс переползти на другую стену (по земле), а не на другую точку той же стены")]
    [Range(0f, 1f)] public float otherWallChance = 0.35f;

    [Tooltip("Скорость лазания: доля от moveSpeed")]
    [Min(0.05f)] public float crawlSpeedScale = 1f;

    public int RollAttacks() => RollRange(attacksPerPerchMin, attacksPerPerchMax);
    public int RollPerches() => RollRange(perchesBeforeDescentMin, perchesBeforeDescentMax);
    public float RollPause() => UnityEngine.Random.Range(Mathf.Min(attackPauseMin, attackPauseMax), Mathf.Max(attackPauseMin, attackPauseMax));

    internal static int RollRange(int min, int max) =>
        UnityEngine.Random.Range(Mathf.Min(min, max), Mathf.Max(min, max) + 1);
}

[Serializable]
public class SpiderMotherGroundPhase
{
    [Tooltip("Префаб микро-паучка — обычный враг с EnemyController")]
    public GameObject minionPrefab;

    [Tooltip("Сколько паучков родить за спуск: от min до max")]
    [Min(0)] public int minionsMin = 3;
    [Min(0)] public int minionsMax = 5;

    [Tooltip("Пауза перед первым паучком и между паучками, сек")]
    [Min(0.05f)] public float spawnInterval = 0.45f;

    [Tooltip("Живых паучков в арене не больше этого: лишние не рождаются")]
    [Min(1)] public int maxAliveMinions = 8;

    [Tooltip("Разброс места рождения вокруг паучихи по X и Y, ед")]
    public Vector2 spawnSpread = new(2f, 0.3f);

    [Tooltip("Прочность щита-паутины")]
    [Min(1)] public int shieldHp = 60;

    [Tooltip("Щит не разбили за это время — паучиха снимает его и уползает на стену, сек")]
    [Min(1f)] public float timeout = 15f;

    [Tooltip("Bool аниматора, пока щит поднят. Пусто = без анимации")]
    public string shieldedAnimatorParameter = "Shielded";

    public int RollMinions() => SpiderMotherWallPhase.RollRange(minionsMin, minionsMax);
}

[Serializable]
public class SpiderMotherStunPhase
{
    [Tooltip("Стан после разбитого щита, сек")]
    [Range(0.5f, 5f)] public float duration = 4f;

    [Tooltip("Bool аниматора на время стана. Пусто = без анимации")]
    public string animatorParameter = "Stunned";
}
