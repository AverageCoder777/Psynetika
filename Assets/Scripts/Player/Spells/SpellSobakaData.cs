using UnityEngine;

[CreateAssetMenu(menuName = "Psynetika/SpellSobaka")]
public class SpellSobakaData : SpellData
{
    [Header("Berserk")]
    public float SpellTime = 15f;
    public float hpDrainInterval = 1.5f;
    [Range(0f, 1f)] public float hpDrainPercent = 0.1f;
    [Range(0f, 1f)] public float criticalHpThreshold = 0.1f;
    [Range(0f, 1f)] public float boostPerStack = 0.1f;
    public int maxStacks = 5;
}
