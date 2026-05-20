using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Psynetika/Projectile Definition", fileName = "ProjectileDefinition")]
public class ProjectileDefinition : ScriptableObject
{
    public string id;
    public GameObject prefab;
    public float speed = 10f;
    public float lifetime = 5f;
    public Vector2 spawnOffset = new(0.65f, 0.22f);

    [SerializeReference, SubclassSelector]
    public List<AbilityNode> onSpawn = new();

    [SerializeReference, SubclassSelector]
    public List<AbilityNode> onHit = new();

    [SerializeReference, SubclassSelector]
    public List<AbilityNode> onExpire = new();
}
