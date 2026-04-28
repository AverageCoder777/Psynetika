using UnityEngine;

public enum SpellOwner
{
    Any = 0,
    Satan = 1,
    Sobaka = 2
}

public enum SpellSlot
{
    Regular = 0,
    Ultimate = 1
}

public enum SpellType
{
    Projectile = 0,
    Stance = 1
}

[CreateAssetMenu(menuName = "Psynetika/Spell")]
public class SpellData : ScriptableObject
{
    public int Id = 0;
    public string SpellName;
    public string SpellDescription;
    public Sprite SpellIcon;
    public SpellOwner Owner = SpellOwner.Any;
    public SpellSlot Slot = SpellSlot.Regular;
    public SpellType Type = SpellType.Projectile;
    public float spellCooldown = 1f;
    [Min(0.05f)] public float castDuration = 0.35f;
    [Range(0f, 1f)] public float castMomentNormalized = 0.5f;

    public GameObject SpellPrefab;
}
