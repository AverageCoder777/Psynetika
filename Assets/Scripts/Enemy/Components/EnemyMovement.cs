using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private EnemyAttack attack;
    private float baseSpeed;

    // +1 вправо, -1 влево. Спрайты врагов по умолчанию смотрят влево (flipX = движение вправо).
    public float FacingDirection { get; private set; } = -1f;
    public float Speed => baseSpeed * (attack != null ? attack.GetStat(StatId.MoveSpeed) : 1f);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        attack = GetComponent<EnemyAttack>();
    }

    public void Initialize(EnemyConfig config)
    {
        baseSpeed = config.moveSpeed;
    }

    // Вызывать из PhysicsUpdate: горизонтальный шаг к targetX, вертикаль остаётся физике.
    public void MoveTowardsX(float targetX)
    {
        // Уснувшее тело игнорирует MovePosition и само от него не просыпается.
        if (rb.IsSleeping()) rb.WakeUp();

        float newX = Mathf.MoveTowards(rb.position.x, targetX, Speed * Time.fixedDeltaTime);
        float dirX = targetX - rb.position.x;
        rb.MovePosition(new Vector2(newX, rb.position.y));
        Face(dirX);
    }

    public void Face(float dirX)
    {
        if (Mathf.Abs(dirX) <= 0.01f) return;

        FacingDirection = dirX > 0f ? 1f : -1f;
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = dirX > 0f;
        }
    }
}
