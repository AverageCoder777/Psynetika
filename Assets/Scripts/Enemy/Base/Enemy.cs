using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Enemy : MonoBehaviour, IAbilityTarget
{
    private static readonly int DamageHash = Animator.StringToHash("Damage");
    private static readonly int DieHash = Animator.StringToHash("Die");
    [SerializeField] private int enemyHealth = 100;
    [SerializeField] private float enemySpeed = 2f;
    [SerializeField] private int enemyDamage = 10;
    [SerializeField] private float enemyHitDuration = 2f;
    [SerializeField] private BoxCollider2D hitTrigger;
    [SerializeField] private BoxCollider2D followTrigger;
    [SerializeField] private int coinsToDrop = 5;
    [SerializeField] private GameObject coinPrefab;
    private Animator animator;
    private bool isDead = false;
    private Player player;
    public Animator Animator => animator;
    public int EnemyHealth => enemyHealth;
    public float EnemySpeed => enemySpeed;
    public int EnemyDamage => enemyDamage;
    public float EnemyHitDuration => enemyHitDuration;
    public bool PlayerInHitRange { get; private set; } = false;
    public bool PlayerInFollowRange { get; private set; } = false;
    void Start()
    {
        animator = GetComponent<Animator>();
        player = GameObject.FindWithTag("Player")?.GetComponent<Player>();
        enemySM = new EnemyStateMachine();
        idleState = new EnemyIdleState(this, enemySM);
        followState = new FollowState(this, enemySM);
        hitState = new EnemyHitState(this, enemySM);
        enemySM.Initialize(idleState);
    }
    void Update()
    {
        if (player != null && !player.IsVisibleToEnemies)
        {
            PlayerInFollowRange = false;
            PlayerInHitRange = false;
        }
        enemySM.CurrentEnemyState.HandleInput();
        enemySM.CurrentEnemyState.LogicUpdate();
    }
    void FixedUpdate()
    {
        enemySM.CurrentEnemyState.PhysicsUpdate();
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || (player != null && !player.IsVisibleToEnemies)) return;
        if (followTrigger.IsTouching(other))
        {
            PlayerInFollowRange = true;
            Debug.Log("Player entered follow trigger");
        }
        if (hitTrigger.IsTouching(other))
        {
            PlayerInHitRange = true;
            Debug.Log("Player entered attack trigger");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!hitTrigger.IsTouching(other))
        {
            PlayerInHitRange = false;
            Debug.Log("Player exited attack trigger");
        }
        if (!followTrigger.IsTouching(other))
        {
            PlayerInFollowRange = false;
            Debug.Log("Player exited follow trigger");
        }

    }
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        enemyHealth -= damage;
        animator?.SetTrigger(DamageHash);
        if (enemyHealth <= 0)
        {
            Die();
        }
    }

    // IAbilityTarget
    Transform IAbilityTarget.Transform => transform;
    bool IAbilityTarget.IsAlive => !isDead;
    Team IAbilityTarget.Team => global::Team.Enemy;
    void IAbilityTarget.ReceiveDamage(DamageEvent ev) => TakeDamage(Mathf.RoundToInt(ev.Amount));
    void DropCoins()
    {
        for (int i = 0; i < coinsToDrop; i++)
        {
            GameObject coin = Instantiate(coinPrefab, transform.position, Quaternion.identity);
            coin.SetActive(true);
        }
    }
    void Die()
    {
        if (isDead) return;
        isDead = true;
        animator?.SetTrigger(DieHash);
        if (hitTrigger != null) hitTrigger.enabled = false;
        if (followTrigger != null) followTrigger.enabled = false;
        if (coinPrefab != null && coinsToDrop > 0)
        {
            DropCoins();
        }
        enabled = false;
        Destroy(gameObject, 0.7f);
    }
    #region State Machine Variables
    public EnemyStateMachine enemySM;
    public EnemyIdleState idleState;
    public FollowState followState;
    public EnemyHitState hitState;
    #endregion
}
