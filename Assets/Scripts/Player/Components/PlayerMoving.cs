using UnityEngine;
using UnityEngine.InputSystem;
//Компоненты юнити
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]

//Скрипт
[RequireComponent(typeof(PlayerCharacterManager))]
public class PlayerMoving : MonoBehaviour, IMovable, IJumpable
{
    private PlayerStaticSettings settings;
    private PlayerDynSettings status;
    public float Speed => GetCurrentSpeed();
    public Vector2 MovementInput {get => movementInput ; set => movementInput = value;} 
    private PlayerInput playerInput;
    private PlayerCharacterManager charManager;
    private Vector2 movementInput;
    private Rigidbody2D rb;
    public PlayerInput PlayerInput => playerInput;
    public Rigidbody2D Rb => rb;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        charManager = GetComponent<PlayerCharacterManager>();
        status = charManager.GetComponent<PlayerDynSettings>();
        rb = GetComponent<Rigidbody2D>();
        settings = Resources.Load<PlayerStaticSettings>("PlayerDefaultSettings");
    }

    public float GetCurrentSpeed()
    {
        return charManager.GetCurrentCharacterType() == PlayerCharacterType.Satan ?
        settings.move.satanSpeed * status.speedMultiplier :
        settings.move.dogSpeed * status.speedMultiplier;
    }
    public float GetJumpForce()
    {
        return settings.jump.thrust;
    }
}

