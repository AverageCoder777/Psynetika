using UnityEngine;

public abstract class SpellBase : MonoBehaviour
{
    [SerializeField] private SpellOwner spellOwner = SpellOwner.Any;
    protected Player caster;
    protected SpellData spellData;
    protected float direction = 1f;

    public SpellOwner Owner => spellOwner;

    public virtual void Initialize(Player owner, SpellData data, float castDirection)
    {
        caster = owner;
        spellData = data;
        direction = Mathf.Sign(castDirection);
    }

    public abstract void Cast(Vector2 spawnPosition);
}
