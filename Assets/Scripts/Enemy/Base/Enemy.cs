using UnityEngine;

public class Enemy : MonoBehaviour
{

    [SerializeField] private float enemyHealth = 100f;
    [SerializeField] private float enemySpeed = 2f;
    [SerializeField] private float enemyDamage = 10f;
    private Animator animator;
    public Animator Animator => animator;
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
    public void TakeDamage(float damage)
    {
        enemyHealth -= damage;
        if (enemyHealth <= 0)
        {
            Die();
        }
        else
        {
            enemySM.ChangeState(hitState);
        }
    }
    void Die()
    {
        animator.SetTrigger("Die");
        Destroy(gameObject);
    }
    #region State Machine Variables
    private EnemyStateMachine enemySM;
    private EnemyIdleState idleState;
    private FollowState followState;
    private EnemyHitState hitState;
    #endregion
}
