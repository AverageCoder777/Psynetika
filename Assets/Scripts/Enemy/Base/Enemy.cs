using System.Timers;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy : MonoBehaviour
{

    [SerializeField] private int enemyHealth = 100;
    [SerializeField] private float enemySpeed = 2f;
    [SerializeField] private int enemyDamage = 10;
    [SerializeField] private float enemyHitDuration = 2f;
    [SerializeField] private BoxCollider2D hitTrigger;
    [SerializeField] private BoxCollider2D followTrigger;
    private Animator animator;
    private bool isDead = false;
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
        enemySM = new EnemyStateMachine();
        idleState = new EnemyIdleState(this, enemySM);
        followState = new FollowState(this, enemySM);
        hitState = new EnemyHitState(this, enemySM);
        enemySM.Initialize(idleState);
    }
    void Update()
    {
        enemySM.CurrentEnemyState.HandleInput();
        enemySM.CurrentEnemyState.LogicUpdate();
    }
    void FixedUpdate()
    {
        enemySM.CurrentEnemyState.PhysicsUpdate();
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
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
        animator?.SetTrigger("Damage");
        if (enemyHealth <= 0)
        {
            Die();
        }
    }
    void Die()
    {
        if (isDead) return;
        isDead = true;
        animator?.SetTrigger("Die");
        if (hitTrigger != null) hitTrigger.enabled = false;
        if (followTrigger != null) followTrigger.enabled = false;
        this.enabled = false;
        Destroy(gameObject, 0.7f);
    }
    #region State Machine Variables
    public EnemyStateMachine enemySM;
    public EnemyIdleState idleState;
    public FollowState followState;
    public EnemyHitState hitState;
    #endregion
}
