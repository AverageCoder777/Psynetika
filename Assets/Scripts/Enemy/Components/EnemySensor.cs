using UnityEngine;

/*
Обнаружение игрока. Два режима:
  Triggers — опрос триггер-коллайдеров (IsTouching) вместо OnTriggerEnter/Exit: корректно работает
             при спавне игрока внутри зоны и при переключении видимости. Точная форма зон, но требует
             ручной обвязки двух дочерних коллайдеров на префабе.
  Radius   — зоны берутся из EnemyConfig.perception. Ничего настраивать на префабе не нужно.
Режим Auto выбирает Triggers, если коллайдеры назначены, иначе Radius.

Если на враге есть EnemyAggroZone, зона агро берётся из неё (прямоугольник в сцене),
а режим выше определяет только зону удара.
*/
public class EnemySensor : MonoBehaviour
{
    [SerializeField] private BoxCollider2D hitTrigger;
    [SerializeField] private BoxCollider2D followTrigger;

    private static readonly EnemyPerceptionSettings DefaultPerception = new();

    private EnemyPerceptionSettings perception = DefaultPerception;
    private PlayerController player;
    private Collider2D playerCollider;
    private IAbilityTarget playerTarget;
    private EnemyAggroZone aggroZone;
    private float lastSeenInZoneTime = float.NegativeInfinity;

    public bool PlayerInFollowRange { get; private set; }
    public bool PlayerInHitRange { get; private set; }
    public Transform PlayerTransform => player != null ? player.transform : null;
    public IAbilityTarget PlayerTarget => playerTarget;

    // Дистанция по X до игрока; используется модулями атак для проверки minRange/maxRange.
    public float HorizontalDistanceToPlayer =>
        player != null ? Mathf.Abs(player.transform.position.x - transform.position.x) : float.PositiveInfinity;

    public void Initialize(EnemyConfig config)
    {
        if (config != null && config.perception != null)
        {
            perception = config.perception;
        }
        aggroZone = GetComponent<EnemyAggroZone>();
    }

    private void Start()
    {
        TryFindPlayer();
    }

    private void TryFindPlayer()
    {
        GameObject playerGo = GameObject.FindWithTag("Player");
        if (playerGo != null)
        {
            player = playerGo.GetComponent<PlayerController>();
            playerCollider = playerGo.GetComponent<Collider2D>();
            playerTarget = playerGo.GetComponent<PlayerHealth>();
        }
    }

    private void Update()
    {
        // Игрок мог быть неактивен на момент Start (интро, поздний спавн) — доискиваем.
        if (player == null) TryFindPlayer();

        bool visible = player != null && player.IsVisibleToEnemies;
        if (!visible)
        {
            PlayerInFollowRange = false;
            PlayerInHitRange = false;
            return;
        }

        if (UseTriggers)
        {
            UpdateByTriggers();
        }
        else
        {
            UpdateByRadius();
        }

        if (aggroZone != null)
        {
            UpdateByAggroZone();
        }
    }

    private bool UseTriggers => perception.mode switch
    {
        EnemyPerceptionSettings.DetectionMode.Triggers => true,
        EnemyPerceptionSettings.DetectionMode.Radius => false,
        _ => hitTrigger != null || followTrigger != null
    };

    private void UpdateByTriggers()
    {
        PlayerInFollowRange = playerCollider != null && followTrigger != null && followTrigger.IsTouching(playerCollider);
        PlayerInHitRange = playerCollider != null && hitTrigger != null && hitTrigger.IsTouching(playerCollider);
    }

    private void UpdateByRadius()
    {
        Vector2 delta = (Vector2)player.transform.position - (Vector2)transform.position;

        if (perception.verticalTolerance > 0f && Mathf.Abs(delta.y) > perception.verticalTolerance)
        {
            PlayerInFollowRange = false;
            PlayerInHitRange = false;
            return;
        }

        float distance = delta.magnitude;

        // Гистерезис: замечаем на followRange, теряем только за followRange + loseTargetPadding,
        // иначе враг дёргается между преследованием и покоем на самой границе зоны.
        float loseRange = perception.followRange + perception.loseTargetPadding;
        PlayerInFollowRange = PlayerInFollowRange ? distance <= loseRange : distance <= perception.followRange;
        PlayerInHitRange = distance <= perception.attackRange;
    }

    // Игрок в зоне — замечен. Вышел — ещё loseDelay секунд считается замеченным: враг ждёт у границы.
    private void UpdateByAggroZone()
    {
        if (aggroZone.Contains(player.transform.position))
        {
            lastSeenInZoneTime = Time.time;
            PlayerInFollowRange = true;
            return;
        }

        PlayerInFollowRange = PlayerInFollowRange && Time.time - lastSeenInZoneTime < aggroZone.loseDelay;
    }

    public void DisableSensing()
    {
        enabled = false;
        PlayerInFollowRange = false;
        PlayerInHitRange = false;
        if (hitTrigger != null) hitTrigger.enabled = false;
        if (followTrigger != null) followTrigger.enabled = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Вне плей-мода Initialize ещё не вызывался — берём настройки прямо из конфига врага.
        EnemyPerceptionSettings settings = perception;
        if (!Application.isPlaying
            && TryGetComponent(out EnemyController controller)
            && controller.Config != null
            && controller.Config.perception != null)
        {
            settings = controller.Config.perception;
        }

        if (settings.mode == EnemyPerceptionSettings.DetectionMode.Triggers) return;
        bool hasZone = TryGetComponent(out EnemyAggroZone _);
        if (settings.mode == EnemyPerceptionSettings.DetectionMode.Auto && (hitTrigger != null || followTrigger != null)) return;

        // С зоной агро радиус агро не используется — рисует её сама EnemyAggroZone.
        if (!hasZone)
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, settings.followRange);
        }
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, settings.attackRange);
    }
#endif
}
