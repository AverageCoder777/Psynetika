using UnityEngine;

public interface IAbilityCaster
{
    Transform Transform { get; }
    Vector2 Center { get; }
    float FacingDirection { get; }
    Team Team { get; }
    MonoBehaviour CoroutineHost { get; }
}
