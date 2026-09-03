using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettings", menuName = "Psynetika/Player Settings")]
public class PlayerStaticSettings : ScriptableObject
{
    [Header("═══ CHARACTER SWITCHING ═══")] public SwitchingSettings switching;
    [Header("═══ MOVEMENT ═══")] public MovementSettings move;
    [Header("═══ JUMP & AIR PHYSICS ═══")] public JumpPhysicsSettings jump;
    [Header("═══ CROUCH SYSTEM ═══")] public CrouchSettings crouch;
    [Header("═══ ROLLING ═══")] public RollingSettings rolling;
    [Header("═══ WALL MECHANICS ═══")] public WallSettings wall;
    [Header("═══ LADDER MECHANICS ═══")] public LadderSettings ladder;
    [Header("═══ PLATFORM INTERACTIONS ═══")] public PlatformSettings platform;
    [Header("═══ COMBAT SYSTEM ═══")] public CombatSettings combat;
    [Header("═══ HEALTH ═══")] public HealthSettings health;
    [Header("═══ PHYSICS DETECTION ═══")] public PhysicsDetectionSettings detection;
}

[System.Serializable]
public class SwitchingSettings
{
    [Range(0.1f, 2f)] public float switchDelay = 0.5f;
}

[System.Serializable]
public class MovementSettings
{
    [Range(1f, 15f)] public float dogSpeed = 8f;
    [Range(1f, 15f)] public float satanSpeed = 6f;
    [Range(5f, 30f)] public float accelerationRate = 15f;
    [Range(5f, 30f)] public float frictionRate = 20f;

}

[System.Serializable]
public class JumpPhysicsSettings
{
    [Range(5f, 20f)] public float thrust = 12f;
    [Range(2f, 10f)] public float doubleJumpThrust = 6f;
    [Range(0.5f, 2f)] public float upGravityScale = 1.1f;
    [Range(1.5f, 3f)] public float downGravityScale = 2f;
    [Range(10f, 100f)] public float maxDoubleJumpHeight = 40f;
    [Range(-0.01f, 0f)] public float jumpVelocityThreshold = -0.001f;
}

[System.Serializable]
public class CrouchSettings
{
    [Range(0.3f, 1f)] public float crouchHeightMultiplier = 0.7f;
    [Range(0.3f, 0.9f)] public float crouchSpeedMultiplier = 0.5f;
    [Range(0.05f, 0.2f)] public float headCheckDistanceBuffer = 0.1f;
    [Range(1f, 3f)] public float capsuleHeightDivider = 1.5f;
}

[System.Serializable]
public class RollingSettings
{
    [Range(2f, 10f)] public float rollDistance = 4f;
    [Range(0.1f, 0.5f)] public float rollDuration = 0.25f;
}

[System.Serializable]
public class WallSettings
{
    [Range(0.1f, 3f)] public float wallSlideSpeed = 1f;

    [Range(5f, 20f)] public float wallJumpForce = 10f;

    [Range(0.2f, 1f)] public float wallDetectionDistance = 0.5f;

    [Range(0.1f, 0.5f)] public float wallWaitTime = 0.2f;

    [Range(5f, 15f)] public float wallJumpSpeed = 5f;
}

[System.Serializable]
public class LadderSettings
{
    [Range(2f, 10f)] public float climbSpeed = 5f;

    [Range(0.1f, 0.5f)] public float exitDelay = 0.25f;
}

[System.Serializable]
public class PlatformSettings
{
    [Range(0.2f, 1f)] public float dropThroughDuration = 0.5f;

    [Range(0.5f, 2f)] public float platformDetectionDistance = 1f;
}

[System.Serializable]
public class CombatSettings
{
    [Header("Dog Combat")]
    [Range(0.5f, 3f)] public float dogBaseHitTime = 1f;

    [Range(0.5f, 3f)] public float dogBaseHitDistance = 1f;

    [Range(1, 50)] public int dogBaseDamage = 10;

    [Header("Satan Combat")]
    [Range(0.5f, 3f)] public float satanBaseHitTime = 2f;

    [Range(0.5f, 3f)] public float satanBaseHitDistance = 2f;

    [Range(1, 50)] public int satanBaseDamage = 22;

    [Header("Combo System")]
    [Range(1f, 5f)] public float comboResetTime = 2f;

    [Header("Hit Detection")]
    [Range(0.5f, 4f)] public float hitDetectionBoxHeight = 2f;
    [Range(0.1f, 1f)] public float bulletSpawnOffsetX = 0.65f;
    [Range(0.1f, 1f)] public float bulletSpawnOffsetY = 0.22f;
}

[System.Serializable]
public class HealthSettings
{
    [Range(10, 500)] public int dogMaxHP = 100;
    [Range(10, 500)] public int satanMaxHP = 100;
    [Range(2f, 10f)] public float resurrectionDelay = 5.5f;
}

[System.Serializable]
public class PhysicsDetectionSettings
{
    [Range(0.1f, 2f)] public float floorDetectionDistance = 0.8f;
    [Range(0.1f, 1f)] public float platformDetectionDistance = 1f;
    [Range(0.01f, 0.5f)] public float raycastOffset = 0.25f;
    [Range(0.01f, 1f)] public float raycastOffsetVertical = 0.5f;
    [Range(0.001f, 0.1f)] public float velocityThreshold = 0.001f;
    [Range(0.01f, 0.5f)] public float smallVelocityThreshold = 0.1f;
}
