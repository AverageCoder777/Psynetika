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
                RaycastHit2D hit = Physics2D.Raycast(origin, direction, hitDistance, enemyMask);
                if (hit.collider != null)
                {
                    Debug.DrawLine(origin, hit.point, Color.green);
                    if (player.DebugMessages)
                        Debug.Log("Hit " + hit.collider.name);
                    Enemy enemy = hit.collider.GetComponent<Enemy>();
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
                        DamageDummy dummy = hit.collider.GetComponent<DamageDummy>();
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
            Animator.SetBool("Shooting " + comboCount, false);
        }
        else
        {
            Animator.SetBool("Hitting " + comboCount, false);
        }
    }
}
