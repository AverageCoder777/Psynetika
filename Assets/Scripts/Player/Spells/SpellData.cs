using UnityEngine;

public enum SpellOwner
{
    Any = 0,
    Satan = 1,
    Sobaka = 2
}

[CreateAssetMenu(menuName = "Psynetika/Spell")]
public class SpellData : ScriptableObject
{
    public string SpellName;
    public string SpellDescription;
    public Sprite SpellIcon;
    public SpellOwner Owner = SpellOwner.Any;
    public float spellCooldown = 1f;
    public GameObject SpellPrefab;
    public float spellRange;
    public float spellSpeed;
    public float spellDamage;

    [Min(0.05f)] public float castDuration = 0.35f;
    [Range(0f, 1f)] public float castMomentNormalized = 0.5f;
    public float spawnForwardOffset = 0.56f;
    public string castTrigger = "Shooting";
    public string castBool = "Shooting 1";
}
