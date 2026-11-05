using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Psynetika/Projectile Definition", fileName = "ProjectileDefinition")]
public class ProjectileDefinition : ScriptableObject
{
    public string id;
    public GameObject prefab;
    public float speed = 10f;
    public float lifetime = 5f;
    public Vector2 spawnOffset = new Vector2(0.65f, 0.22f);

    [Tooltip("Дистанция, пройдя которую снаряд исчезает (срабатывает onExpire). 0 — ограничен только lifetime")]
    [Min(0f)] public float maxDistance = 0f;

    [Header("Пробивание (волны)")]
    [Tooltip("Проходит сквозь цели: onHit срабатывает по каждой цели один раз, снаряд летит дальше. " +
             "Коллайдер префаба должен быть триггером, иначе цели будут физически толкать снаряд")]
    public bool pierceTargets = false;

    [Tooltip("Для пробивающего снаряда: исчезать (onExpire) при касании стен и прочих не-целей")]
    public bool stopOnObstacles = true;

    [Header("Бросок по дуге (гранаты)")]
    [Tooltip("Гравитация Rigidbody2D снаряда. 0 — летит по прямой, как пуля")]
    [Min(0f)] public float gravityScale = 0f;

    [Tooltip("Начальная вертикальная скорость; вместе с гравитацией даёт дугу броска")]
    public float launchUpSpeed = 0f;

    [SerializeReference, SubclassSelector]
    public List<AbilityNode> onSpawn = new();

    [SerializeReference, SubclassSelector]
    public List<AbilityNode> onHit = new();

    [SerializeReference, SubclassSelector]
    public List<AbilityNode> onExpire = new();
}
