using UnityEngine;
using UnityEngine.InputSystem;
using System;
using UnityEngine.UI;
using System.Collections;

public class PlayerScript : MonoBehaviour
{
    [SerializeField] UIScript ui;

    [Header("Jumping")]
    [Tooltip("Jumping height")]
    [Range(0, 100)]
    [SerializeField] float thrust = 300;
    [SerializeField] Image healthBar;
    [SerializeField] int maxHp = 100;
    [SerializeField] int hp = 100;
    bool grounded = true;
    private float jumpCooldown = 0.5f; // куллдаун между прыжками
    private float lastJumpTime = -999f;
    private bool firstJump = false; // флаг первого прыжка
    private bool canDoubleJump = false; // флаг возможности двойного прыжка

    [Header("Double Jumping")]
    [Tooltip("Jumping height for double jump")]
    [Range(0, 300)]
    [SerializeField] float doubleJumpThrust = 200;
    [SerializeField] float MaxHeight = 40f; // максимальная высота двойного прыжка
    private bool doubleJump = false; // флаг двойного прыжка

    [Header("Movement")]
    [Tooltip("Speed of Movement")]
    [Range(5, 15)]
    [SerializeField] float speed = 1;
    private Vector2 movementInput = Vector2.zero;
    private bool isMoving = false;

    // Приседание — фиксированное уменьшение хитбокса ровно в 2 раза
    const float CROUCH_HEIGHT_MULTIPLIER = 0.5f;
    bool isCrouching = false;
    bool crouchRequested = false;
    Vector2 originalCapsuleSize;
    Vector2 originalCapsuleOffset;

    // Ролл (кувырок)
    [Header("Roll")]
    [SerializeField] float rollDistance = 20f;     // настраиваемая длина кувырка (в юнитах)
    [SerializeField] float rollDuration = 0.25f;  // длительность кувырка (в секундах)
    bool isRolling = false;

    Rigidbody2D rb;
    Animator PlayerAnimator;
    SpriteRenderer SR;
    CapsuleCollider2D Col;
    BoxCollider2D Col1;

    public void OnMove(InputAction.CallbackContext context)
    {
        // Игнорируем движение во время приседания или ролла
        if (isCrouching || isRolling)
        {
            movementInput = Vector2.zero;
            isMoving = false;
            return;
        }

        movementInput = context.ReadValue<Vector2>();
        // Двигаемся только если есть входное значение по горизонтали
        if (context.performed)
        {
            isMoving = Math.Abs(movementInput.x) > 0.01f;
        }
        else if (context.canceled)
        {
            isMoving = false;
            movementInput = Vector2.zero;
        }
    }
    public void OnDoubleJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (canDoubleJump)
            {
                if (transform.position.y < MaxHeight)
                {
                    doubleJump = true;
                }
            }
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        // Если прыгнули, ставим триггер jumped в true
        if (context.performed)
            firstJump = true;
    }

    // Обработчик приседания (привязать кнопку C в Input System)
    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            crouchRequested = true;
            if (grounded) StartCrouch(); // можно начать присед только на земле
        }
        else if (context.canceled)
        {
            crouchRequested = false;
            StopCrouch();
        }
    }

    // Обработчик ролла
    public void OnRoll(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            TryStartRoll();
        }
    }

    void TryStartRoll()
    {
        if (!grounded) return;         // только на земле
        if (isCrouching) return;       // нельзя в приседе
        if (isRolling) return;         // нельзя пока уже роллимся
        // запускаем ролл в направлении взгляда
        float dir = SR != null && SR.flipX ? -1f : 1f;
        StartCoroutine(RollCoroutine(dir));
    }
    IEnumerator RollCoroutine(float dir)
    {
        isRolling = true;
        isMoving = false;
        // прерываем присед, если он был (на всякий случай)
        if (isCrouching) StopCrouch();

        float rollSpeed = (rollDuration > 0f) ? rollDistance / rollDuration : 0f;
        float elapsed = 0f;

        PlayerAnimator.SetTrigger("Rolling");

        while (elapsed < rollDuration)
        {
            // задаём горизонтальную скорость ролла
            rb.linearVelocity = new Vector2(dir * rollSpeed, rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // остановка после ролла
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        isRolling = false;
        isMoving = true; //КОСТЯ ДУРАК!!!!!!!!
    }

    void StartCrouch()
    {
        // защита: не начинать приседание если не на земле или уже приседаем
        if (isCrouching || !grounded) return;
        isCrouching = true;
        // уменьшить капсулу по высоте ровно в 2 раза и сместить центр, чтобы нижняя точка осталась на месте
        Vector2 newSize = new Vector2(originalCapsuleSize.x, originalCapsuleSize.y * CROUCH_HEIGHT_MULTIPLIER);
        float delta = originalCapsuleSize.y - newSize.y;
        Col.size = newSize;
        Col.offset = new Vector2(originalCapsuleOffset.x, originalCapsuleOffset.y - delta / 2f);

        // анимация и стоп движения
        PlayerAnimator.SetBool("Crouching", true);
        movementInput = Vector2.zero;
        isMoving = false;
        // при приседе горизонтальная скорость гасится
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    void StopCrouch()
    {
        if (!isCrouching) return;
        // восстановить размеры капсулы
        Col.size = originalCapsuleSize;
        Col.offset = originalCapsuleOffset;
        isCrouching = false;
        PlayerAnimator.SetBool("Crouching", false);
    }

    void Jump()
    {
        if (!grounded) return;
        if (isCrouching) return; // нельзя прыгать из приседа
        if (isRolling) return;   // нельзя прыгать из ролла
        rb.AddForce(transform.up * thrust * 10);
        grounded = false;
        canDoubleJump = true;
        PlayerAnimator.SetTrigger("Jumping");
        lastJumpTime = Time.time;
    }
    void DoubleJump()
    {
        if (canDoubleJump)
        {
            rb.AddForce(transform.up * doubleJumpThrust, ForceMode2D.Impulse);
            PlayerAnimator.SetTrigger("DoubleJumping");
            canDoubleJump = false;
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        PlayerAnimator = GetComponent<Animator>();
        SR = GetComponent<SpriteRenderer>();
        Col = GetComponent<CapsuleCollider2D>();
        Col1 = GetComponent<BoxCollider2D>();

        // сохранить оригинальные параметры капсулы для восстановления
        if (Col != null)
        {
            originalCapsuleSize = Col.size;
            originalCapsuleOffset = Col.offset;
        }

        if (healthBar != null)
        {
            healthBar.fillAmount = (float)hp / maxHp;
        }
    }

    void Move()
    {
        // Если приседаем или роллимся — движение запрещено
        if (isCrouching || isRolling)
        {
            PlayerAnimator.SetBool("Moving", false);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // Двигаемся только если есть входное значение по горизонтали
        rb.linearVelocity = new Vector2(movementInput.x * speed, rb.linearVelocity.y);
        if (grounded && Math.Abs(movementInput.x) > 0.01f)
        {
            PlayerAnimator.SetBool("Moving", true);
        }
        else
        {
            PlayerAnimator.SetBool("Moving", false);
        }

        if (movementInput.x > 0.01f)
        {
            SR.flipX = false;
            //Col.offset = new Vector2(0.195f, Col.offset.y);
        }
        if (movementInput.x < -0.01f)
        {
            SR.flipX = true;
            //Col.offset = new Vector2(-0.1195f, Col.offset.y);
        }
    }

    void FixedUpdate()
    {
        PlayerAnimator.SetFloat("Velocity", rb.linearVelocity.y);
        PlayerAnimator.SetBool("Grounded", grounded);
        // Прыжок только во время нажатия
        if (firstJump)
        {
            Jump();
            // Вырубаем триггер, чтобы не было повторных автоматических прыжков
            firstJump = false;
        }
        if (doubleJump)
        {
            DoubleJump();
            doubleJump = false;
        }

        // Двигаемся только если есть входное значение по горизонтали
        if (isMoving)
        {
            Move();
        }
        else
        {
            // Если входа нет и мы на земле — останавливаем персонажа намертво
            if (grounded && !isRolling)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            // Убедимся, что анимация движения выключена
            PlayerAnimator.SetBool("Moving", false);
        }
    }

    void OnTriggerStay2D(Collider2D col)
    {
        // Проверяем, касается ли триггерный BoxCollider земли
        if (col.CompareTag("Floor"))
        {
            bool wasGrounded = grounded;
            grounded = true;
            firstJump = false;
            canDoubleJump = false;
            if (!wasGrounded && crouchRequested)
            {
                StartCrouch();
            }
        }
    }
    void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Floor"))
        {
            grounded = false;
        }
    }

    public void DecreasingHealth(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TakeDamage(10);
        }
    }

    public void HealHealth(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Heal(10);
        }
    }
    public void TakeDamage(int damage)
    {
        hp -= 10;
        if (hp <= 0)
        {
            hp = 0; 
        }
        if (healthBar != null)
        {
            healthBar.fillAmount = (float)hp / maxHp;
        }
        GameOverActive();
    }
    public void Heal(int damage)
    {
        hp += 10;
        if (hp >= 100)
        {
            hp = 100; 
        }
        if (healthBar != null)
        {
            healthBar.fillAmount = (float)hp / maxHp;
        }
    }

    public void ResetHealth()
    {
        hp = maxHp;
        if (healthBar != null)
        {
            healthBar.fillAmount = (float)hp / maxHp;
        }
    }

    public void GameOverActive()
    {
        if (hp <= 0)
        {
            ui.GameOver();
        }
    }
}

