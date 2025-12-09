using UnityEngine;

public class Enemy : MonoBehaviour
{

    [SerializeField] private int enemyHealth = 100;
    [SerializeField] private float enemySpeed = 2f;
    [SerializeField] private int enemyDamage = 10;
    [SerializeField] private float enemyHitDuration = 2f;
    private Animator animator;
    public Animator Animator => animator;
    public int EnemyHealth => enemyHealth;
    public float EnemySpeed => enemySpeed;
    public int EnemyDamage => enemyDamage;
    public float EnemyHitDuration => enemyHitDuration;
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
        
    }
    public void TakeDamage(int damage)
    {
        enemyHealth -= damage;
        if (enemyHealth <= 0)
        {
            Die();
        }
    }
    void Die()
    {
        animator.SetTrigger("Die");
        Destroy(gameObject);
    }
    #region State Machine Variables
    public EnemyStateMachine enemySM;
    public EnemyIdleState idleState;
    public FollowState followState;
    public EnemyHitState hitState;
    #endregion
}
