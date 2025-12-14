using UnityEngine;

public class HittingState : GroundedState
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
        animator.SetTrigger(player.CharacterIsSatan() ? "Shooting" : "Hitting");
        if (player.DebugMessages) Debug.Log("Entered Hitting State");
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
        bool jumpRequested = false;
        var jumpAction = player.PlayerInput.actions["Jump"];
        if (jumpAction.WasPressedThisFrame()) jumpRequested = true;
        if (jumpRequested)
        {
            animator.ResetTrigger("Hitting");
            hitComplete = true;
            if (player.DebugMessages) Debug.Log("Hitting interrupted by jump input -> switching to JumpingState");
            stateMachine.ChangeState(player.JumpingState);
            return;
        }
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
            if (player.CharacterIsSatan())
            {
                Vector2 spawnPos = origin + direction * 0.5f;
                GameObject bulletObj = Object.Instantiate(player.bulletPrefab, spawnPos, Quaternion.identity);
                Bullet bullet = bulletObj.GetComponent<Bullet>();
                if (bullet != null)
                {
                    bullet.damage = player.GetHittingDamage();
                    bullet.SetDirection(hitDir);
                }
                if (player.DebugMessages) Debug.Log("Shot a bullet in direction " + hitDir);
            }
            else
            {
                RaycastHit2D hit = Physics2D.Raycast(origin, direction, hitDistance, enemyMask);
                if (hit.collider != null)
                {
                    Debug.DrawLine(origin, hit.point, Color.green);
                    if (player.DebugMessages) Debug.Log("Hit " + hit.collider.name);
                    Enemy enemy = hit.collider.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(player.GetHittingDamage());
                        Debug.Log("Player hitted enemy with " + player.GetHittingDamage() + " damage points");
                    }
                }
                else
                {
                    Debug.DrawLine(origin, origin + direction * hitDistance, new Color(1f, 0.5f, 0f));
                }
            }
            hitComplete = true;
        }
    }
    public override void Exit()
    {
        base.Exit();
        hitElapsed = 0f;
        hitComplete = false;
        animator.ResetTrigger(player.CharacterIsSatan() ? "Shooting" : "Hitting");
        if (player.DebugMessages) Debug.Log("Exited Hitting State");
    }
}