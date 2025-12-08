using UnityEngine;

public class HittingState : States
{
    private float hitElapsed = 0f;
    private float hitDir = 1f;
    private float hittingSpeed = 0f;
    private float hitDistance = 0f; 
    private bool hitComplete = false;
    private LayerMask enemyMask = LayerMask.GetMask("Enemy");

    public HittingState(Player player, StateMachine playerStateMachine) : base(player, playerStateMachine)
    {
    }
    public override void Enter()
    {
        base.Enter();
        hittingSpeed = player.GetHittingSpeed();
        hitDir = player.ActiveSR != null && player.ActiveSR.flipX ? -1f : 1f;
        hitDistance = player.GetHitDistance();
        animator.SetTrigger("Hitting");
        Debug.Log("Entered Hitting State");
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (hitComplete)
        {
            stateMachine.ChangeState(player.IdleState);
        }
    }
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        hitElapsed += Time.deltaTime;
        BoxCollider2D box = player.GetComponent<BoxCollider2D>();
        Vector2 origin = (box != null) ? box.bounds.center : (Vector2)player.transform.position;
        Vector2 direction = Vector2.right * hitDir;
        if (!hitComplete)
        {
            Debug.DrawRay(origin, direction * hitDistance, Color.red);
        }
        if (!hitComplete && hitElapsed >= hittingSpeed)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, hitDistance, enemyMask);
            if (hit.collider != null)
            {
                Debug.DrawLine(origin, hit.point, Color.green);
                Debug.Log("Hit " + hit.collider.name);
                Enemy enemy = hit.collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(player.GetHittingDamage());
                }
            }
            else
            {
                Debug.DrawLine(origin, origin + direction * hitDistance, new Color(1f, 0.5f, 0f));
            }
            hitComplete = true;
        }
    }
    public override void Exit()
    {
        base.Exit();
        hitElapsed = 0f;
        hitComplete = false;
        animator.ResetTrigger("Hitting");
        Debug.Log("Exited Hitting State");
    }
}