using UnityEngine;

[CreateAssetMenu(menuName = "Psynetika/Status Effect Config", fileName = "StatusEffectConfig")]
public class StatusEffectConfig : ScriptableObject
{
    [Header("VFX")]
    public GameObject burnVfxPrefab;
    public GameObject glitchVfxPrefab;
    public GameObject explosionVfxPrefab;

    [Header("Burn")]
    [Min(0.1f)] public float burnDuration = 6f;
    [Min(0.05f)] public float burnTickInterval = 1f;
    [Min(1)] public int burnTickDamage = 5;

    [Header("Glitch")]
    [Min(0.1f)] public float glitchDuration = 6f;
    [Range(0f, 1f)] public float glitchSlowPerStack = 0.05f;
    [Min(1)] public int glitchMaxStacks = 3;

    [Header("Reaction")]
    [Min(0f)] public float explosionDamageMultiplier = 0.5f;
    [Min(0f)] public float explosionVfxLifetime = 2f;
}
