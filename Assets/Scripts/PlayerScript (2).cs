using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;

public class PlayerScript : MonoBehaviour
{
    [Header("Jumping")]
    [Tooltip("Jumping height")]
    [Range(0, 100)]
    [SerializeField] float thrust = 300;
    bool grounded = true;
    bool jumped;
    private float jumpCooldown = 0.5f; // куллдаун между прыжками
    private float lastJumpTime = -999f;

    [Header("Movement")]
    [Tooltip("Speed of Movement")]
    [Range(5, 15)]
    [SerializeField] float speed = 1;
    private Vector2 movementInput = Vector2.zero;

    [Header("Attacking")]
    //[SerializeField] GameObject attackHitBox;
    [SerializeField] public int attackPower = 1;
    bool attack_active;
    bool attack_pressed = false;
    [SerializeField] public int maxHP = 5;
    int currentHP;
    //[SerializeField] private Image hpBar;
    //[SerializeField] private Image xpBar;
    int level = 1;
    int currentxp = 1;
    int maxXP = 100;
    [Header("GameOver Menu")]
    //[SerializeField] GameObject gameOverMenu;


    Rigidbody2D rb;
    Animator PlayerAnimator;
    SpriteRenderer SR;
    CapsuleCollider2D Col;
    BoxCollider2D Col1;
    bool attackGP;

    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        jumped = context.action.triggered;
    }
    public void OnAttack(InputAction.CallbackContext context)
    {
        attackGP = context.action.triggered;
    }


    void Jump()
    {
        if (jumped && grounded && Time.time - lastJumpTime >= jumpCooldown)
        {
            rb.AddForce(transform.up * thrust * 10);
            grounded = false;
            PlayerAnimator.SetTrigger("Jumping");
            lastJumpTime = Time.time;
        }
    }
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        PlayerAnimator = GetComponent<Animator>();
        SR = GetComponent<SpriteRenderer>();
        Col = GetComponent<CapsuleCollider2D>();
        Col1 = GetComponent<BoxCollider2D>();
        currentHP = maxHP;
        currentHP = PlayerPrefs.GetInt("PlayerHP", maxHP);
        currentxp = PlayerPrefs.GetInt("PlayerXP", 1);
        level = PlayerPrefs.GetInt("PlayerLevel", 1);
        maxXP = PlayerPrefs.GetInt("PlayerMaxXP", 100);
        maxHP = PlayerPrefs.GetInt("PlayerMaxHP", maxHP);

        /*/UpdateProgressBar();/*/
    }

    void Move()
    {
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
            Col.offset = new Vector2(0.195f, Col.offset.y);
        }
        if (movementInput.x < -0.01f)
        {
            SR.flipX = true;
            Col.offset = new Vector2(-0.1195f, Col.offset.y);
        }
    }
    void Attack()
    {
        if (attackGP && !attack_pressed) // Проверяем, что кнопка нажата и еще не обработана
        {
            attack_pressed = true;
            attack_active = true;
            PlayerAnimator.SetTrigger("Attack1");
        }
        else if (!attackGP) // Сбрасываем флаг, если кнопка отпущена
        {
            attack_pressed = false;
        }
    }

    void FixedUpdate()
    {
        PlayerAnimator.SetFloat("Velocity", rb.linearVelocity.y);
        PlayerAnimator.SetBool("Grounded", grounded);
        Jump();
        Move();
        Attack();

        // Хитбокс активен только во время анимации атаки
        /*/if (PlayerAnimator.GetCurrentAnimatorStateInfo(0).IsName("AttackAnim"))
        {
            attack_active = true;
            attackHitBox.SetActive(true);
        }
        else
        {
            attack_active = false;
            attackHitBox.SetActive(false);
        }/*/
    }

    void OnTriggerStay2D(Collider2D col)
    {
        // Проверяем, касается ли триггерный BoxCollider земли
        if (col.CompareTag("Floor"))
        {
            grounded = true;
        }
    }
    /*/public void TakeDamage(int damage)
    {
        currentHP -= damage;
        PlayerAnimator.SetTrigger("HitCharacter");
        UpdateProgressBar();
        if (currentHP <= 0)
        {
            GameOver();
        }
        PlayerPrefs.SetInt("PlayerHP", currentHP);
        PlayerPrefs.Save();
    }/*/
    /*/void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("EnemyAttack"))
        {
            EnemyScript enemy = other.GetComponentInParent<EnemyScript>();
            if (enemy != null)
            {
                TakeDamage(enemy.EnemyAttackPower);
            }
        }
    }/*/
    void OnTriggerExit2D(Collider2D col)
    {
        // Если триггерный BoxCollider перестает касаться земли, сбрасываем grounded
        if (col.CompareTag("Floor"))
        {
            grounded = false;
        }
    }

    /*/void UpdateProgressBar()
    {
        float hpPercentage = (float)currentHP / maxHP;
        hpBar.fillAmount = hpPercentage;
        float xpPercentage = (float)currentxp / maxXP;
        xpBar.fillAmount = xpPercentage;

    }
    public void Experience()
    {
        currentxp += 50;

        if (currentxp >= maxXP)
        {
            currentxp -= maxXP;
            level += 1;
            attackPower += 1;
            currentHP = maxHP;
        }
        UpdateProgressBar();
        PlayerPrefs.SetInt("PlayerHP", currentHP);
        PlayerPrefs.SetInt("PlayerXP", currentxp);
        PlayerPrefs.SetInt("PlayerLevel", level);
        PlayerPrefs.SetInt("PlayerMaxXP", maxXP);
        PlayerPrefs.SetInt("PlayerMaxHP", maxHP);
        PlayerPrefs.Save();
    }/*/
    void OnApplicationQuit()
    {
        // Сохраняем значения при выходе из игры
        PlayerPrefs.SetInt("PlayerHP", currentHP);
        PlayerPrefs.SetInt("PlayerXP", currentxp);
        PlayerPrefs.SetInt("PlayerLevel", level);
        PlayerPrefs.SetInt("PlayerMaxXP", maxXP);
        PlayerPrefs.SetInt("PlayerMaxHP", maxHP);
        PlayerPrefs.Save();
    }

    /*/void GameOver()
    {
        gameOverMenu.SetActive(true);
        Time.timeScale = 0f;
    }
    public void Heal()
    {
        currentHP = maxHP;
        UpdateProgressBar();
        PlayerPrefs.SetInt("PlayerHP", currentHP);
        PlayerPrefs.Save();
    }/*/
}