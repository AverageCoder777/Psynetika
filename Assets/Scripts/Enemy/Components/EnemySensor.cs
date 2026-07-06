using UnityEngine;

// Обнаружение игрока опросом триггеров (IsTouching) вместо OnTriggerEnter/Exit:
// корректно работает при спавне игрока внутри зоны и при переключении видимости.
public class EnemySensor : MonoBehaviour
{
    [SerializeField] private BoxCollider2D hitTrigger;
    [SerializeField] private BoxCollider2D followTrigger;

    private PlayerController player;
    private Collider2D playerCollider;
    private IAbilityTarget playerTarget;

    public bool PlayerInFollowRange { get; private set; }
    public bool PlayerInHitRange { get; private set; }
    public Transform PlayerTransform => player != null ? player.transform : null;
    public IAbilityTarget PlayerTarget => playerTarget;

    private void Start()
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
        bool visible = player != null && player.IsVisibleToEnemies && playerCollider != null;
        PlayerInFollowRange = visible && followTrigger != null && followTrigger.IsTouching(playerCollider);
        PlayerInHitRange = visible && hitTrigger != null && hitTrigger.IsTouching(playerCollider);
    }

    public void DisableSensing()
    {
        enabled = false;
        PlayerInFollowRange = false;
        PlayerInHitRange = false;
        if (hitTrigger != null) hitTrigger.enabled = false;
        if (followTrigger != null) followTrigger.enabled = false;
    }
}
