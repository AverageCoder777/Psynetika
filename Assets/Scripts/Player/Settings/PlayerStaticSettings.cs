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
    [Header("═══ ROPE (SWING) ═══")] public RopeSettings rope;
    [Header("═══ COMBAT SYSTEM ═══")] public CombatSettings combat;
    [Header("═══ HEALTH ═══")] public HealthSettings health;
    [Header("═══ ENERGY (ULTIMATE) ═══")] public EnergySettings energy;
    [Header("═══ PHYSICS DETECTION ═══")] public PhysicsDetectionSettings detection;
    [Header("═══ SQUASH & STRETCH ═══")] public SquashStretchSettings squash;
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
    [Tooltip("Разгон при развороте (ввод против текущей скорости). Больше — резче смена направления.")]
    [Range(5f, 50f)] public float turnAccelerationRate = 30f;
}

[System.Serializable]
public class JumpPhysicsSettings
{
    [Range(0.1f, 10f)] public float airSpeedMultiplier = 0.75f;
    [Range(0f, 20f)] public float thrust = 12f;
    [Range(0f, 2f)] public float upGravityScale = 1.1f;
    [Range(0f, 3f)] public float downGravityScale = 2f;
    [Tooltip("Сколько всего прыжков до приземления, включая прыжок с земли (2 — двойной прыжок).")]
    [Range (1, 10)] public int maxAirJumpsCount = 2;

    [Header("Отзывчивость")]
    [Tooltip("Coyote time: сколько секунд после схода с края ещё можно прыгнуть как с земли.")]
    [Range(0f, 0.3f)] public float coyoteTime = 0.1f;
    [Tooltip("Буфер прыжка: нажатие незадолго до приземления выполнится сразу при касании земли.")]
    [Range(0f, 0.3f)] public float jumpBufferTime = 0.12f;
    [Tooltip("Переменная высота: при отпускании прыжка на подъёме вертикальная скорость умножается на это значение. 1 — выкл.")]
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;

    [Header("Траектория")]
    [Tooltip("У вершины прыжка (|скорость Y| меньше порога) при зажатом прыжке гравитация ослабляется — «зависание».")]
    [Range(0f, 5f)] public float apexHangSpeed = 1.5f;
    [Range(0.1f, 1f)] public float apexGravityMultiplier = 0.6f;
    [Tooltip("Предельная скорость падения.")]
    [Range(5f, 40f)] public float maxFallSpeed = 18f;
    [Tooltip("Торможение в воздухе без ввода. 0 — герой скользит по инерции как раньше.")]
    [Range(0f, 20f)] public float airFrictionRate = 4f;
}

[System.Serializable]
public class CrouchSettings
{
    [Range(0.3f, 1f)] public float crouchHeightMultiplier = 0.7f;
    [Range(0.3f, 0.9f)] public float crouchSpeedMultiplier = 0.5f;
    [Tooltip("Высота выстрела Сатаны в присяде: доля высоты пригнувшегося коллайдера от его низа. Низкий выстрел попадает по мелочи (нейрончикам), над которой проходят пули стоя.")]
    [Range(0.05f, 1f)] public float shotHeight = 0.3f;
}

[System.Serializable]
public class RollingSettings
{
    [Range(0f, 10f)] public float rollDistance = 4f;
    [Range(0.1f, 0.5f)] public float rollDuration = 0.25f;
    [Tooltip("Прыжок прерывает перекат и сохраняет его разгон.")]
    public bool jumpCancel = true;
    [Tooltip("Если направление зажато, после переката герой продолжает бежать, а не замирает на месте.")]
    public bool keepRunSpeedOnExit = true;
}

[System.Serializable]
public class WallSettings
{
    [Range(0.1f, 3f)] public float wallSlideSpeed = 1f;
    [Range(0f, 20f)] public float wallJumpForce = 10f;
    [Range(0.1f, 0.5f)] public float wallWaitTime = 0.2f;
    [Range(0f, 15f)] public float wallJumpSpeed = 5f;
    [Range(1f,10f)] public float wallVerticalMultiplier = 3f;
    [Tooltip("Сколько секунд после отскока от стены ввод не влияет на полёт. Потом возвращаются управление и двойной прыжок.")]
    [Range(0f, 0.5f)] public float wallJumpControlLock = 0.15f;
}

[System.Serializable]
public class LadderSettings
{
    [Range(0f, 10f)] public float climbSpeed = 5f;
    [Range(0.1f, 0.5f)] public float exitDelay = 0.25f;
}

[System.Serializable]
public class RopeSettings
{
    [Tooltip("Точка хвата относительно центра героя (пасть/лапы собаки).")]
    public Vector2 handOffset = new Vector2(0f, 0.6f);
    [Range(0.1f, 3f)] public float swingGravityScale = 1.5f;
    [Tooltip("Ускорение раскачки по касательной, пока зажато направление по ходу движения.")]
    [Range(0f, 30f)] public float swingAcceleration = 5f;
    [Range(0f, 2f)] public float swingDamping = 0.15f;
    [Range(10f, 90f)] public float maxSwingAngle = 80f;
    [Tooltip("Какая доля скорости влёта переходит в раскачку при хвате.")]
    [Range(0f, 1f)] public float catchMomentumKeep = 0.8f;
    [Range(0f, 10f)] public float climbSpeed = 3f;

    [Header("Прыжок с верёвки")]
    [Tooltip("Множитель скорости раскачки при прыжке.")]
    [Range(0.5f, 2f)] public float releaseVelocityMultiplier = 1.15f;
    [Tooltip("Доля обычного прыжка (jump.thrust), добавляемая вверх.")]
    [Range(0f, 1.5f)] public float jumpOffThrustMultiplier = 0.6f;
    [Tooltip("Минимальная горизонтальная скорость в сторону зажатого направления.")]
    [Range(0f, 10f)] public float minJumpOffSpeedX = 4f;
    [Tooltip("Сколько секунд нельзя снова схватить ту же верёвку после отпускания.")]
    [Range(0f, 1f)] public float regrabCooldown = 0.35f;

    [Tooltip("Расхождение (юниты) между расчётной и фактической позицией, после которого раскачка считается упёршейся.")]
    [Range(0.05f, 1f)] public float blockedTolerance = 0.15f;
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
    [Range(0.1f, 1f)] public float bulletSpawnOffsetX = 0.65f;
    [Range(0.1f, 1f)] public float bulletSpawnOffsetY = 0.22f;
}

[System.Serializable]
public class HealthSettings
{
    [Range(1, 500)] public int dogMaxHP = 100;
    [Range(1, 500)] public int satanMaxHP = 100;
    [Range(1f, 10f)] public float resurrectionDelay = 5.5f;
}

[System.Serializable]
public class EnergySettings
{
    // Сатана (Лилит) по умолчанию ещё и копит энергию со временем — как Эдгар в Brawl Stars.
    public HeroEnergySettings satan = new HeroEnergySettings { passiveRegenPerSecond = 2f };
    public HeroEnergySettings dog = new HeroEnergySettings();
}

[System.Serializable]
public class HeroEnergySettings
{
    [Tooltip("Размер шкалы. Ульта доступна только при полной шкале и тратит её целиком.")]
    [Min(1f)] public float maxEnergy = 100f;
    [Range(0f, 1f)] public float startEnergyNormalized = 0f;

    [Header("От урона")]
    [Tooltip("Энергия за 1 единицу реально снятого HP (оверкилл не считается).")]
    [Min(0f)] public float energyPerDamage = 1f;
    [Min(0f)] public float basicAttackDamageMultiplier = 1f;
    [Tooltip("Урон обычной способности (E). Плоский бонус за каст — нода «Энергия/Получить энергию ульты» в графе способности.")]
    [Min(0f)] public float regularAbilityDamageMultiplier = 1f;
    [Tooltip("Урон самой ульты. 0 — ульта не заряжает сама себя.")]
    [Min(0f)] public float ultimateDamageMultiplier = 0f;

    [Header("Пассивно")]
    [Min(0f)] public float passiveRegenPerSecond = 0f;
    [Tooltip("Копить пассивную энергию, пока герой не активен (переключён).")]
    public bool regenWhileInactive = false;
    public bool resetOnDeath = false;
}

[System.Serializable]
public class PhysicsDetectionSettings
{
    [Range(0.05f, 0.2f)] public float headCheckDistanceBuffer = 0.1f;
    [Range(0.01f, 1f)] public float wallDetectionDistance = 0.5f;
    [Range(0.2f, 1f)] public float dropThroughDuration = 0.5f;
    [Range(-0.2f, 0f)] public float jumpWallVelocityThreshold = -0.1f; //Детект перехода с прыжка в падение и детект соскальзывания по стене
    [Range(0.001f,0.4f)] public float platformDropThreshold = 0.1f; //Детект падения через платформу
    [Range(0.001f,0.1f)] public float movementInputThreshold = 0.001f;
    [Range (0.01f,2f)] public float floorDetectionDistance = 0.8f;
    [Range(0.1f, 1f)] public float platformDetectionDistance = 1f;
}

// Визуальная упругость героя. Масштабирует только дочерний визуал (Satan/Dog), коллайдер не меняется.
[System.Serializable]
public class SquashStretchSettings
{
    public bool enabled = true;
    [Tooltip("Растяжение по вертикали в момент прыжка (0.2 — на 20% выше).")]
    [Range(0f, 0.5f)] public float jumpStretch = 0.2f;
    [Tooltip("Максимальное сплющивание при приземлении.")]
    [Range(0f, 0.5f)] public float maxLandSquash = 0.25f;
    [Tooltip("Скорость падения, при которой сплющивание максимально.")]
    [Range(1f, 30f)] public float landSquashFullSpeed = 14f;
    [Tooltip("Растяжение в полёте на единицу вертикальной скорости.")]
    [Range(0f, 0.05f)] public float airStretchPerSpeed = 0.008f;
    [Range(0f, 0.3f)] public float maxAirStretch = 0.12f;
    [Tooltip("Жёсткость пружины возврата формы. Больше — быстрее и мельче колебания.")]
    [Range(50f, 1000f)] public float stiffness = 380f;
    [Tooltip("Демпфирование пружины. Меньше — дольше «желейно» покачивается.")]
    [Range(1f, 60f)] public float damping = 18f;
}
