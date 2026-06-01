using UnityEngine;
using UnityEngine.InputSystem;
//Компоненты юнити
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
//Скрипт
[RequireComponent(typeof(PlayerController))]
public class PlayerMoving : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float dogSpeed = 8f; // базовая скорость собаки
    [SerializeField] private float satanSpeed = 6f; // базовая скорость сатаны
    [SerializeField] private float accelerationRate = 15f; //ускорение, с которым игрок начинает движение
    [SerializeField] private float frictionRate = 20f; // трение, которое замедляет персонажа, если игрок перестал идти
    [SerializeField] private Vector2 movementInput = Vector2.zero;

    [Header("Прыжок")]
    [SerializeField] private float thrust = 12f; // ускорение, придаваемое персонажу при прыжке
    [SerializeField] private float doubleJumpThrust = 6f; // ускорение двойного прыжка
    [SerializeField] private float upGravityScale = 1.1f; // множитель гравитации в первой половине параболы прыжка
    [SerializeField] private float downGravityScale = 2f; // множитель гравитации во второй половине параболы прыжка

    [Header("Цепление за стену")]
    [SerializeField] private float wallSlideSpeed = 1f; //скорость соскальзывания по стене
    [SerializeField] private float wallJumpForce = 10f; //сила прыжка от стены
    [SerializeField] private float wallDetectionDistance = 0.5f; //Расстояние, на котором стена обнаруживается
    [SerializeField] private float wallWaitTime = 0.2f; //Время, которое игрок должен провести на стене, чтобы можно было отпрыгнуть от неё

    [Header("Приседание")]
    [SerializeField] private float crouchMult = 0.7f; //Модификатор, который умножает высоту коллайдера при приседании

    [Header("Рывок/кувырок")]
    [SerializeField] private float rollDistance = 4f; //расстояние рывка/кувырка
    [SerializeField] private float rollDuration = 0.25f; //время рывка/кувырка
    [Header("Прыжок через платформу вниз")]
    [Tooltip("Имя слоя для платформ")]
    [SerializeField] private string platformLayerName = "Platform";
    [Tooltip("Время, в течение которого игрок не будет сталкиваться с платформами при провале")]
    [SerializeField] private float dropThroughDuration = 0.5f;
    public string PlatformLayerName => platformLayerName;
    public float DropThroughDuration => dropThroughDuration;
    private PlayerInput playerInput;
    private PlayerController player;
    private Rigidbody2D rb;
    public PlayerInput PlayerInput => playerInput;
    public Vector2 MovementInput
    {
        get => movementInput;
        set => movementInput = value;
    }
    public float AccelerationRate => accelerationRate;
    public float FrictionRate => frictionRate;
    public float Thrust => thrust;
    public float DoubleJumpThrust => doubleJumpThrust;
    public float UpGravityScale => upGravityScale;
    public float DownGravityScale => downGravityScale;
    public float WallSlideSpeed => wallSlideSpeed;
    public float WallJumpForce => wallJumpForce;
    public float WallDetectionDistance => wallDetectionDistance;
    public float WallWaitTime => wallWaitTime;
    public float RollDistance => rollDistance;
    public float RollDuration => rollDuration;
    public float CrouchMult => crouchMult;
    private float speedMultiplier = 1f;
    public float SpeedMultiplier
    {
        get => speedMultiplier;
        set => speedMultiplier = value;
    }
    public Rigidbody2D Rb => rb;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        player = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
    }

    public float GetCharSpeed()
    {
        float base_ = player.GetCurrentCharacterType() == PlayerCharacterType.Satan ? satanSpeed : dogSpeed;
        return base_ * speedMultiplier;
    }
}

