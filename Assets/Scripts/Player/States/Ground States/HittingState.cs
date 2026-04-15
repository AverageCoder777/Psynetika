using UnityEngine;

public class HittingState : GroundedStates
{
    private float hitElapsed = 0f;
    private float hitDir = 1f;
    private float hittingSpeed = 0f;
    private float hitDistance = 0f;
    private bool hitComplete = false;
    private LayerMask enemyMask = LayerMask.GetMask("Enemy");
    private int comboCount = 0;
    private float lastHitTime = 0f;
    private const float comboResetTime = 2f;
    private bool playerIsSatan;
    private bool shooted = false;

    public HittingState(Player player, StateMachine playerStateMachine)
        : base(player, playerStateMachine) { }

    public override void Enter()
    {
        base.Enter();
        if (Time.time - lastHitTime > comboResetTime)
        {
            comboCount = 0;
        }
        shooted = false;
        playerIsSatan = player.CharacterIsSatan();
        hittingSpeed = player.GetHittingSpeed();
        hitDir = player.ActiveSR != null && player.ActiveSR.flipX ? -1f : 1f;
        hitDistance = player.GetHitDistance();
        comboCount++;
        if (comboCount > 2) comboCount = 1;
        if (comboCount == 1)
        {
            if (playerIsSatan)
            {
                animator.SetTrigger("Shooting");
            }
            else
            {
                animator.SetTrigger("Hitting");
            }
        }
        if (playerIsSatan)
        {
            animator.SetBool("Shooting " + comboCount, true);
        }
        else
        {
            animator.SetBool("Hitting " + comboCount, true);
        }
        lastHitTime = Time.time;
#if UNITY_EDITOR
        if (player.DebugMessages)
            Debug.Log("Entered Hitting State");
#endif
    }

    public override void LogicUpdate()
    {
        bool jumpRequested = false;
        var jumpAction = player.PlayerInput.actions["Jump"];
        if (jumpAction.WasPressedThisFrame())
            jumpRequested = true;
        if (jumpRequested)
        {
            if (playerIsSatan)
            {
                animator.SetBool("Shooting " + comboCount, false);
            }
            else
            {
                animator.SetBool("Hitting " + comboCount, false);
            }
            hitComplete = true;
#if UNITY_EDITOR
            if (player.DebugMessages)
                Debug.Log("Hitting interrupted by jump input -> switching to JumpingState");
#endif
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
            if (playerIsSatan && !shooted && hitElapsed >= (hittingSpeed / 2))
            {
                Vector2 spawnPos = origin + direction * 0.56f;
                GameObject bulletObj = Object.Instantiate(
                    player.bulletPrefab,
                    spawnPos,
                    Quaternion.identity
                );
#if UNITY_EDITOR
                Debug.DrawLine(spawnPos, spawnPos + Vector2.up * 0.1f, Color.blue, 0.1f);
#endif
                Bullet bullet = bulletObj.GetComponent<Bullet>();
                bullet.damage = player.GetHittingDamage();
                bullet.SetDirection(hitDir);
                shooted = true;
#if UNITY_EDITOR
                if (player.DebugMessages)
                    Debug.Log("Shot a bullet in direction " + hitDir);
#endif
            }
            if (!playerIsSatan && hitElapsed >= hittingSpeed)
            {
                RaycastHit2D hit = Physics2D.Raycast(origin, direction, hitDistance, enemyMask);
                if (hit.collider != null)
                {
#if UNITY_EDITOR
                    Debug.DrawLine(origin, hit.point, Color.green);
                    if (player.DebugMessages)
                        Debug.Log("Hit " + hit.collider.name);
#endif
                    Enemy enemy = hit.collider.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(player.GetHittingDamage());
#if UNITY_EDITOR
                        Debug.Log(
                            "Player hitted enemy with "
                                + player.GetHittingDamage()
                                + " damage points"
#endif
                        );
                    }
                }
                else
                {
                    Debug.DrawLine(origin, origin + direction * hitDistance, Color.red);
                }
            }
            if (hitElapsed >= hittingSpeed)
            {
                hitComplete = true;
            }
        }
    }
    public override void Exit()
    {
        base.Exit();
        hitElapsed = 0f;
        hitComplete = false;
        shooted = false;
        if (playerIsSatan)
        {
            animator.SetBool("Shooting " + comboCount, false);
        }
        else
        {
            animator.SetBool("Hitting " + comboCount, false);
        }
#if UNITY_EDITOR
        if (player.DebugMessages)
            Debug.Log("Exited Hitting State");
#endif
    }
}
