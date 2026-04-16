using UnityEngine;

[CreateAssetMenu(menuName = "Psynetika/Config")]
public class PsynetikaConfig : ScriptableObject
{
    [SerializeField] public string version = "0.0.1";

    [Header("Player")]
    [Header("Movement")]
    [SerializeField] public float Speed = 5f;
    [SerializeField] public float AccelerationRate = 15f;
    [SerializeField] public float FrictionRate = 20f;

    [Header("Jump")]
    [SerializeField] public float Thrust = 7f;
    [SerializeField] public float DoubleJumpThrust = 4f;
    [SerializeField] public float UpGravityScale = 1.1f;
    [SerializeField] public float DownGravityScale = 2f;

    [Header("Wall")]
    [SerializeField] public float WallSlideSpeed = 1f;
    [SerializeField] public float WallJumpForce = 10f;
    [SerializeField] public float WallDetectionDistance = 0.5f;
    [SerializeField] public float WallWaitTime = 0.1f;

    [Header("Crouch")]
    [SerializeField] public float CROUCH_HEIGHT_MULTIPLIER = 0.5f;

    [Header("Roll")]
    [SerializeField] public float RollDistance = 4f;
    [SerializeField] public float RollDuration = 0.25f;

    [Header("Health")]
    [SerializeField] public float MaxHP = 100f;

    [Header("Character Switch Delay")]
    [SerializeField] public float SwitchDelay = 0.5f;

    [Header("Drop Through Platform")]
    [Tooltip("Layer name for one-way platforms")]
    [SerializeField] public string PlatformLayerName = "Platform";
    [Tooltip("How long player ignores platform collision while dropping down")]
    [SerializeField] public float DropThroughDuration = 0.5f;

    [Header("Sobaka Attack")]
    [Tooltip("Attack speed affects attack interval")]
    [SerializeField] public float HittingSpeedSobaka = 1f;
    [SerializeField] public float HitDistanceSobaka = 1f;
    [SerializeField] public int HittingDamageSobaka = 10;

    [Header("Satana Attack")]
    [SerializeField] public GameObject BulletPrefab;
    [SerializeField] public int HittingDamageSatana = 22;
    [Tooltip("Attack speed affects shot interval")]
    [SerializeField] public float HittingSpeedSatana = 2f;
    [SerializeField] public float HitDistanceSatana = 2f;
    [SerializeField] public bool DebugMessages = false;

    [Header("Enemy")]
    [SerializeField] public int enemyHealth = 100;
    [SerializeField] public float enemySpeed = 2f;
    [SerializeField] public int enemyDamage = 10;
    [SerializeField] public float enemyHitDuration = 2f;
}
