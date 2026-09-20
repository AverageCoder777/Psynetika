using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D body;
    private EnemyAttack attack;
    private EnemyConfig config;
    private float baseSpeed;

    // +1 вправо, -1 влево. Спрайты врагов по умолчанию смотрят влево (flipX = движение вправо).
    public float FacingDirection { get; private set; } = -1f;

    // Точка появления: от неё строится маршрут патруля.
    public Vector2 SpawnPosition { get; private set; }

    public float Speed => baseSpeed * (attack != null ? attack.GetStatMult(StatMultId.CurrentMoveSpeedMult) : 1f);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        SpawnPosition = transform.position;
        body = FindBodyCollider();
    }

    public void Initialize(EnemyConfig cfg)
    {
        config = cfg;
        baseSpeed = cfg.moveSpeed;
        // Соседей ищем здесь: контроллер мог доставить их после нашего Awake.
        attack = GetComponent<EnemyAttack>();
        if (body == null) body = FindBodyCollider();
    }

    /*
    Вызывать из PhysicsUpdate: горизонтальный шаг к targetX, вертикаль остаётся физике.
    Возвращает false, если шаг не сделан из-за обрыва или стены впереди — состояние решает,
    что с этим делать (патруль разворачивается, преследование останавливается).
    */
    public bool MoveTowardsX(float targetX, float speedScale = 1f)
    {
        float dirX = targetX - rb.position.x;
        if (Mathf.Abs(dirX) <= 0.01f)
        {
            return true;
        }

        Face(dirX);
        float dir = dirX > 0f ? 1f : -1f;
        if (IsBlockedAhead(dir))
        {
            return false;
        }

        // Уснувшее тело игнорирует MovePosition и само от него не просыпается.
        if (rb.IsSleeping()) rb.WakeUp();

        float step = Speed * Mathf.Max(0f, speedScale) * Time.fixedDeltaTime;
        float newX = Mathf.MoveTowards(rb.position.x, targetX, step);
        rb.MovePosition(new Vector2(newX, rb.position.y));
        return true;
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

    public void StopHorizontal()
    {
        if (rb == null) return;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // Обрыв или стена по направлению dir. Выключено, пока в конфиге не заполнен ground.groundMask.
    public bool IsBlockedAhead(float dir)
    {
        if (config == null || config.ground == null) return false;
        if (!config.ground.stopAtLedges || config.ground.groundMask.value == 0) return false;

        Bounds bounds = body != null
            ? body.bounds
            : new Bounds(transform.position, new Vector3(0.5f, 1f, 0f));

        int mask = config.ground.groundMask.value;
        float edgeX = dir > 0f ? bounds.max.x : bounds.min.x;

        // Стена прямо по курсу.
        if (config.ground.wallProbeDistance > 0f)
        {
            Vector2 chest = new Vector2(edgeX, bounds.center.y);
            if (Physics2D.Raycast(chest, new Vector2(dir, 0f), config.ground.wallProbeDistance, mask))
            {
                return true;
            }
        }

        // Земля под следующим шагом: луч чуть за краем коллайдера.
        Vector2 probe = new Vector2(edgeX + dir * 0.05f, bounds.min.y + 0.05f);
        return !Physics2D.Raycast(probe, Vector2.down, config.ground.ledgeProbeDepth, mask);
    }

    private Collider2D FindBodyCollider()
    {
        foreach (Collider2D col in GetComponents<Collider2D>())
        {
            if (!col.isTrigger)
            {
                return col;
            }
        }
        return null;
    }
}
