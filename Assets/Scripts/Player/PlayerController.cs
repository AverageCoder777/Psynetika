using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Characters (дочерние объекты с Animator + SpriteRenderer)")]
    public GameObject characterA;
    public GameObject characterB;
    private GameObject activeCharacter;
    private Animator activeAnimator;
    private SpriteRenderer activeSR;

    [Header("Movement")]
    [SerializeField] float speed = 5f;
    private Vector2 movementInput = Vector2.zero;
    private bool isMoving = false;

    [Header("Jumping")]
    [SerializeField] float thrust = 12f;
    [SerializeField] float doubleJumpThrust = 8f;
    [SerializeField] float maxDoubleJumpHeight = 40f;
    bool grounded = true;
    bool firstJump = false;
    bool canDoubleJump = false;
    bool doubleJump = false;

    [Header("Crouch")]
    const float CROUCH_HEIGHT_MULTIPLIER = 0.5f;
    bool isCrouching = false;
    bool crouchRequested = false;
    Vector2 originalCapsuleSize;
    Vector2 originalCapsuleOffset;

    [Header("Roll")]
    [SerializeField] float rollDistance = 4f;
    [SerializeField] float rollDuration = 0.25f;
    bool isRolling = false;

    [Header("Health / UI")]
    [SerializeField] UIScript ui;
    [SerializeField] UnityEngine.UI.Image healthBar;
    [SerializeField] int maxHp = 100;
    [SerializeField] int hp = 100;

    private bool isSwitching = false;
    public float switchDelay = 0.5f;

    [Header("Platform drop-through")]
    [Tooltip("Имя слоя для платформ (если используешь слои)")]
    [SerializeField] private string platformLayerName = "Platform";
    [Tooltip("Время, в течение которого игрок не будет сталкиваться с платформами при провале")]
    [SerializeField] private float dropThroughDuration = 0.5f;

    Rigidbody2D rb;
    CapsuleCollider2D capsule;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsule = GetComponent<CapsuleCollider2D>();

        if (capsule != null)
        {
            originalCapsuleSize = capsule.size;
            originalCapsuleOffset = capsule.offset;
        }

        if (characterA == null && transform.childCount > 0) characterA = transform.GetChild(0).gameObject;
        if (characterB == null && transform.childCount > 1) characterB = transform.GetChild(1).gameObject;

        activeCharacter = characterA;
        if (characterA != null) characterA.SetActive(true);
        if (characterB != null) characterB.SetActive(false);

        CacheActiveVisuals();
        UpdateHealthUI();
    }

    void CacheActiveVisuals()
    {
        if (activeCharacter != null)
        {
            activeAnimator = activeCharacter.GetComponent<Animator>();
            activeSR = activeCharacter.GetComponent<SpriteRenderer>();
        }
        else
        {
            activeAnimator = null;
            activeSR = null;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (isRolling)
        {
            movementInput = Vector2.zero;
            isMoving = false;
            return;
        }

        movementInput = context.ReadValue<Vector2>();
        if (context.performed) isMoving = Mathf.Abs(movementInput.x) > 0.01f;
        if (context.canceled) { isMoving = false; movementInput = Vector2.zero; }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if ((crouchRequested || isCrouching) && grounded)
            {
                StartCoroutine(DropThroughPlatform());
                Debug.Log("Dropping through platform");
                return;
            }
            firstJump = true;
        }
    }

    public void OnDoubleJump(InputAction.CallbackContext context)
    {
        if (context.performed && canDoubleJump && transform.position.y < maxDoubleJumpHeight)
            doubleJump = true;
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            crouchRequested = true;
            if (grounded) StartCrouch();
        }
        else if (context.canceled)
        {
            crouchRequested = false;
            StopCrouch();
        }
    }

    public void OnRoll(InputAction.CallbackContext context)
    {
        if (context.started) TryStartRoll();
    }

    public void OnSwitchCharacter(InputAction.CallbackContext context)
    {
        if (context.performed && !isSwitching)
            StartCoroutine(SwitchCharacter());
    }

    IEnumerator SwitchCharacter()
    {
        isSwitching = true;

        if (activeAnimator != null) activeAnimator.SetBool("isSwitching", true);

        yield return new WaitForSeconds(switchDelay);

        if (activeCharacter == characterA && characterB != null)
        {
            if (characterA != null) characterA.SetActive(false);
            characterB.SetActive(true);
            activeCharacter = characterB;
        }
        else if (characterA != null)
        {
            if (characterB != null) characterB.SetActive(false);
            characterA.SetActive(true);
            activeCharacter = characterA;
        }

        CacheActiveVisuals();

        if (activeAnimator != null) activeAnimator.SetBool("isSwitching", false);

        isSwitching = false;

        Debug.Log("Switched character");
    }

    void TryStartRoll()
    {
        if (!grounded || isCrouching || isRolling) return;
        float dir = (activeSR != null && activeSR.flipX) ? -1f : 1f;
        StartCoroutine(RollCoroutine(dir));
    }

    IEnumerator RollCoroutine(float dir)
    {
        isRolling = true;
        isMoving = false;
        if (isCrouching) StopCrouch();

        float rollSpeed = (rollDuration > 0f) ? rollDistance / rollDuration : 0f;
        float elapsed = 0f;
        if (activeAnimator != null) activeAnimator.SetTrigger("Rolling");

        while (elapsed < rollDuration)
        {
            rb.linearVelocity = new Vector2(dir * rollSpeed, rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        isRolling = false;
        isMoving = true;
    }

    void StartCrouch()
    {
        if (isCrouching || !grounded) return;
        isCrouching = true;
        Vector2 newSize = new Vector2(originalCapsuleSize.x, originalCapsuleSize.y * CROUCH_HEIGHT_MULTIPLIER);
        float delta = originalCapsuleSize.y - newSize.y;
        capsule.size = newSize;
        capsule.offset = new Vector2(originalCapsuleOffset.x, originalCapsuleOffset.y - delta / 2f);

        activeAnimator.SetBool("Crouching", true);
    }

    void StopCrouch()
    {
        if (!isCrouching) return;
        capsule.size = originalCapsuleSize;
        capsule.offset = originalCapsuleOffset;
        isCrouching = false;
        if (activeAnimator != null) activeAnimator.SetBool("Crouching", false);
    }

    void Jump()
    {
        if (!grounded || isCrouching || isRolling) return;
        rb.AddForce(transform.up * thrust, ForceMode2D.Impulse);
        grounded = false;
        canDoubleJump = true;
        if (activeAnimator != null) activeAnimator.SetTrigger("Jumping");
    }

    void DoubleJump()
    {
        if (!canDoubleJump) return;
        rb.AddForce(transform.up * doubleJumpThrust, ForceMode2D.Impulse);
        if (activeAnimator != null) activeAnimator.SetTrigger("DoubleJumping");
        canDoubleJump = false;
    }

    void FixedUpdate()
    {
        activeAnimator.SetFloat("Velocity", rb.linearVelocity.y);
        activeAnimator.SetBool("Grounded", grounded);

        if (firstJump)
        {
            Jump();
            firstJump = false;
        }
        if (doubleJump)
        {
            DoubleJump();
            doubleJump = false;
        }

        if (isMoving)
        {
            Move();
        }
        else
        {
            if (grounded && !isRolling) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            activeAnimator.SetBool("Moving", false);
        }
    }

    void Move()
    {
        if (isRolling)
        {
            if (activeAnimator != null) activeAnimator.SetBool("Moving", false);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(movementInput.x * speed, rb.linearVelocity.y);
        if (grounded && Mathf.Abs(movementInput.x) > 0.01f)
            activeAnimator.SetBool("Moving", true);
        else
            activeAnimator.SetBool("Moving", false);

        if (movementInput.x > 0.01f && activeSR != null) activeSR.flipX = false;
        if (movementInput.x < -0.01f && activeSR != null) activeSR.flipX = true;
    }

    void OnTriggerStay2D(Collider2D ground)
    {
        if (ground.CompareTag("Floor") || ground.CompareTag("Platform"))
        {
            grounded = true;
            firstJump = false;
            canDoubleJump = false;
        }
    }
    void OnTriggerExit2D(Collider2D ground)
    {
        if (rb.linearVelocity.y!=0)
        {
            grounded = false;
        }
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;
        if (hp < 0) hp = 0;
        Debug.Log("Player took " + damage + " damage. Current HP: " + hp);
        UpdateHealthUI();
        GameOverActive();
    }

    public void Heal(int amount)
    {
        hp += amount;
        if (hp > maxHp) hp = maxHp;
        UpdateHealthUI();
    }

    void UpdateHealthUI()
    {
        if (healthBar != null) healthBar.fillAmount = (float)hp / maxHp;
    }

    public void ResetHealth()
    {
        hp = maxHp;
        UpdateHealthUI();
    }

    void GameOverActive()
    {
        if (hp <= 0 && ui != null) ui.GameOver();
    }

    IEnumerator DropThroughPlatform()
    {
        int platformLayer = LayerMask.NameToLayer(platformLayerName);
        if (platformLayer == -1) yield break;

        int playerLayer = gameObject.layer;
        Physics2D.IgnoreLayerCollision(playerLayer, platformLayer, true);
        yield return new WaitForSeconds(dropThroughDuration);
        Physics2D.IgnoreLayerCollision(playerLayer, platformLayer, false);
    }
}
