using UnityEngine;

/*
Исполняет намерения движения (MoveIntent) через «тело» из EnemyConfig.body.
Сам знает только общее для всех тел: направление взгляда, точку спавна, скорость с бафами,
границы территории (поводок, EnemyAggroZone) и блокировку управления при отбрасывании.

Движение идёт через скорость Rigidbody2D, а не MovePosition: иначе отбрасывание и рывки
перезаписывались бы каждым физическим шагом.
*/
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private EnemyAttack attack;
    private EnemyHomeSettings home;
    private EnemyAggroZone aggroZone;
    private EnemyLocomotionRuntime locomotion;
    private float baseSpeed;
    private float controlLockedUntil;

    public Rigidbody2D Body => rb;

    // Физическое тело врага (не триггер) — на префабе бывают ещё триггеры зон сенсора.
    public Collider2D BodyCollider { get; private set; }

    // +1 вправо, -1 влево. Спрайты врагов по умолчанию смотрят влево (flipX = движение вправо).
    public float FacingDirection { get; private set; } = -1f;

    // Точка появления: от неё строятся маршрут патруля, поводок и возвращение домой.
    public Vector2 SpawnPosition { get; private set; }

    public float Speed => baseSpeed * (attack != null ? attack.GetStatMult(StatMultId.CurrentMoveSpeedMult) : 1f);

    // Результат последнего шага: враг реально двигается (для аниматора).
    public bool IsMoving { get; private set; }

    // Последний шаг упёрся в препятствие или границу поводка — тактика решает, что делать.
    public bool LastMoveBlocked { get; private set; }

    public bool IsControlLocked => Time.time < controlLockedUntil;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        SpawnPosition = transform.position;
        BodyCollider = FindBodyCollider();
    }

    public void Initialize(EnemyConfig cfg)
    {
        baseSpeed = cfg.moveSpeed;
        home = cfg.home;
        // Соседей ищем здесь: контроллер мог доставить их после нашего Awake.
        attack = GetComponent<EnemyAttack>();
        aggroZone = GetComponent<EnemyAggroZone>();
        if (BodyCollider == null) BodyCollider = FindBodyCollider();
        if (!cfg.blocksPlayer) IgnorePlayerContacts();

        locomotion = cfg.ResolveBody().CreateRuntime(this);
        locomotion.Attach();
    }

    // Исключаем слой Player только у физических коллайдеров врага, а не матрицей слоёв:
    // матрица отключила бы и триггеры сенсора, по которым враг замечает и бьёт игрока.
    private void IgnorePlayerContacts()
    {
        int playerMask = LayerMask.GetMask("Player");
        if (playerMask == 0) return;

        foreach (Collider2D col in GetComponents<Collider2D>())
        {
            if (!col.isTrigger)
            {
                col.excludeLayers = col.excludeLayers.value | playerMask;
            }
        }
    }

    // Вызывать из PhysicsUpdate состояния.
    public void Execute(in MoveIntent intent)
    {
        LastMoveBlocked = false;
        IsMoving = false;
        if (locomotion == null || IsControlLocked) return;

        if (!intent.HasDestination || intent.SpeedScale <= 0f)
        {
            locomotion.Stop();
            return;
        }

        Vector2 destination = ClampToTerritory(intent.Destination, out bool clamped);
        if (locomotion.IsAt(destination))
        {
            locomotion.Stop();
            // Дошёл до границы территории, а цель за ней: для тактики это такое же препятствие, как стена.
            LastMoveBlocked = clamped;
            return;
        }

        bool moved = locomotion.MoveTowards(destination, Speed * intent.SpeedScale, Time.fixedDeltaTime);
        LastMoveBlocked = !moved;
        IsMoving = moved;
    }

    public bool IsAt(Vector2 point) => locomotion != null && locomotion.IsAt(point);

    public void Face(float dirX)
    {
        if (Mathf.Abs(dirX) <= 0.01f) return;

        FacingDirection = dirX > 0f ? 1f : -1f;
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = dirX > 0f;
        }
    }

    public void Stop()
    {
        IsMoving = false;
        if (locomotion != null)
        {
            locomotion.Stop();
        }
        else if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    // Отбрасывание: задаёт скорость и на lockTime отключает управление, чтобы полёт доигрался.
    public void ApplyKnockback(Vector2 velocity, float lockTime)
    {
        if (rb == null) return;
        rb.linearVelocity = velocity;
        controlLockedUntil = Mathf.Max(controlLockedUntil, Time.time + Mathf.Max(0f, lockTime));
        IsMoving = false;
    }

    // Враг сам не уходит от точки спавна дальше leashRadius и не выходит из EnemyAggroZone;
    // отбрасыванием его вынести можно, тогда следующие намерения вернут его обратно.
    private Vector2 ClampToTerritory(Vector2 destination, out bool clamped)
    {
        clamped = false;

        if (home != null && home.leashRadius > 0f)
        {
            Vector2 offset = destination - SpawnPosition;
            if (offset.sqrMagnitude > home.leashRadius * home.leashRadius)
            {
                destination = SpawnPosition + offset.normalized * home.leashRadius;
                clamped = true;
            }
        }

        if (aggroZone != null && !aggroZone.Contains(destination))
        {
            destination = aggroZone.Clamp(destination);
            clamped = true;
        }

        return destination;
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
