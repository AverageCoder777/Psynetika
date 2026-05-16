using UnityEngine;

public class HittingState : GroundedStates
{
    private static readonly int HittingHash = Animator.StringToHash("Hitting");
    private static readonly int ShootingHash = Animator.StringToHash("Shooting");
    private float hitElapsed = 0f;
    private float hitDir = 1f;
    private float hittingSpeed = 0f;
    private float hitDistance = 0f;
    private bool hitComplete = false;
    private bool jumpRequested = false;
    private LayerMask enemyMask = LayerMask.GetMask("Enemy");
    private int comboCount = 0;
    private float lastHitTime = 0f;
    private const float comboResetTime = 2f;
    private bool playerIsSatan;
    private bool shooted = false;

    public HittingState(Player player, StateMovMachine playerStateMachine)
        : base(player, playerStateMachine) { }

    public override void Enter()
    {
        base.Enter();
        if (Time.time - lastHitTime > comboResetTime)
        {
            comboCount = 0;
        }
        shooted = false;
        playerIsSatan = player.GetCurrentCharState() == player.SatanState;
        hittingSpeed = player.GetHittingSpeed();
        hitDistance = player.GetHitDistance();
        comboCount++;
        if (comboCount > 2) comboCount = 1;
        if (comboCount == 1)
        {
            if (playerIsSatan)
            {
                Animator.SetTrigger(ShootingHash);
            }
            else
            {
                Animator.SetTrigger(HittingHash);
            }
        }
        if (playerIsSatan)
        {
            Animator.SetBool("Shooting " + comboCount, true);
            Animator.SetTrigger("Shooting");
        }
        else
        {
            Animator.SetBool("Hitting " + comboCount, true);
        }
        lastHitTime = Time.time;
        player.LastState = this;
        jumpRequested = false;
        player.Rb.linearVelocity = new Vector2(0f, player.Rb.linearVelocity.y);
    }
    public override void HandleInput()
    {
        base.HandleInput();
        jumpRequested = player.PlayerInput.actions["Jump"].WasPressedThisFrame();
    }

    public override void LogicUpdate()
    {
        if (jumpRequested)
        {
            if (playerIsSatan)
            {
                
                Animator.SetBool("Shooting " + comboCount, false);
            }
            else
            {
                Animator.SetBool("Hitting " + comboCount, false);
            }
            hitComplete = true;
            if (player.DebugMessages)
                Debug.Log("Hitting interrupted by jump input -> switching to JumpingState");
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
        //base.PhysicsUpdate();
        player.Rb.linearVelocity = new Vector2(0f, player.Rb.linearVelocity.y);
        hitDir = player.ActiveSR.flipX ? -1f : 1f;
        hitElapsed += Time.deltaTime;
        BoxCollider2D box = player.GetComponent<BoxCollider2D>();
        Vector2 origin = (box != null) ? box.bounds.center : (Vector2)player.transform.position;
        Vector2 direction = Vector2.right * hitDir;
        if (!hitComplete)
        {
            if (playerIsSatan && !shooted && hitElapsed >= (hittingSpeed / 2))
            {
                Vector2 spawnPos = new(origin.x+(direction.x*0.65f), origin.y + (direction.y*0.22f));
                GameObject bulletObj = Object.Instantiate(
                    player.bulletPrefab,
                    spawnPos,
                    Quaternion.identity
                );
                Debug.DrawLine(spawnPos, spawnPos + Vector2.up * 0.1f, Color.blue, 0.1f);
                Bullet bullet = bulletObj.GetComponent<Bullet>();
                bullet.damage = player.GetHittingDamage();
                bullet.SetDirection(hitDir);
                shooted = true;
                if (player.DebugMessages)
                    Debug.Log("Shot a bullet in direction " + hitDir);
            }
            if (!playerIsSatan && hitElapsed >= hittingSpeed)
            {
                // Create a box area in front of the player
                Vector2 boxSize = new(hitDistance, 2f);
                Vector2 boxCenter = origin + direction * (hitDistance / 2f);
                
                Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, enemyMask);
                
                // Draw debug box
                Debug.DrawLine(boxCenter + new Vector2(-boxSize.x/2, -boxSize.y/2), 
                              boxCenter + new Vector2(boxSize.x/2, -boxSize.y/2), Color.green);
                Debug.DrawLine(boxCenter + new Vector2(boxSize.x/2, -boxSize.y/2), 
                              boxCenter + new Vector2(boxSize.x/2, boxSize.y/2), Color.green);
                Debug.DrawLine(boxCenter + new Vector2(boxSize.x/2, boxSize.y/2), 
                              boxCenter + new Vector2(-boxSize.x/2, boxSize.y/2), Color.green);
                Debug.DrawLine(boxCenter + new Vector2(-boxSize.x/2, boxSize.y/2), 
                              boxCenter + new Vector2(-boxSize.x/2, -boxSize.y/2), Color.green);
                
                if (hits.Length > 0)
                {
                    foreach (Collider2D collider in hits)
                    {
                        if (player.DebugMessages)
                            Debug.Log("Hit " + collider.name);
                        Enemy enemy = collider.GetComponent<Enemy>();
                        if (enemy != null)
                        {
                            enemy.TakeDamage(player.GetHittingDamage());
                            Debug.Log(
                                "Player hitted enemy with "
                                    + player.GetHittingDamage()
                                    + " damage points"
                            );
                        }
                        else
                        {
                            DamageDummy dummy = collider.GetComponent<DamageDummy>();
                            if (dummy != null)
                            {
                                dummy.TakeDamage(player.GetHittingDamage());
                                Debug.Log(
                                    "Player hitted dummy with "
                                        + player.GetHittingDamage()
                                        + " damage points"
                                );
                            }
                        }
                    }
                }
                else if (player.DebugMessages)
                {
                    Debug.Log("No enemies hit in area");
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
            Animator.SetBool("Shooting " + comboCount, false);
        }
        else
        {
            Animator.SetBool("Hitting " + comboCount, false);
        }
    }
}
