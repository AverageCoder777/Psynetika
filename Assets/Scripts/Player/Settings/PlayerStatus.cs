using UnityEngine;

public class PlayerDynSettings : MonoBehaviour
{
    public int satanHP = 100;
    public int satanDamageBoost = 0;
    public int satanAttackSpeedBoost = 0;
    public int satanAttackRangeBoost = 0;
    public int dogHP = 100;
    public int dogDamageBoost = 0;
    public int dogAttackSpeedBoost = 0;
    public int dogAttackRangeBoost = 0;
    public float attackSpeedMultiplier = 1f;
    public float damageMultiplier = 1f;
    public float speedMultiplier = 1f;
    public event System.Action<int> OnHPChanged;
    public event System.Action OnSpeedBoostApplied;
    public event System.Action OnSpeedBoostEnded;
}
public enum AttackStatId
{
    CurrentDamage = 0,
    AttackSpeed = 1,
    AttackRange = 2
}
public enum StatMultId
{
    CurrentMaxHPMult = 0,
    CurrentDamageMult = 1,
    CurrentAttackSpeedMult = 2,
    CurrentMoveSpeedMult = 3,
    CurrentJumpForceMult = 4,
    CurrentHitDistanceMult = 5
}
