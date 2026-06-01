using UnityEngine;

public class HittingState : GroundedStates
{
    private float hitElapsed = 0f;
    private float hitDir = 1f;
    private float timeOfOneHit = 0f;
    private float hitDistance = 0f;
    private bool hitCompleted = false;
    private bool jumpRequested = false;
    private bool attackRequested = false;
    private LayerMask enemyMask = LayerMask.GetMask("Enemy");
    private int comboCount = 0;
    private float lastHitTime = 0f;
    private const float comboResetTime = 2f;
    private bool playerIsSatan;
    private bool shooted = false;

    public HittingState(PlayerController player, StateMachine playerStateMachine)
        : base(player, playerStateMachine) { }

    public override void Enter()
    {
        base.Enter();
        if (Time.time - lastHitTime > comboResetTime)
        {
            comboCount = 0;
        }
        shooted = false;
        playerIsSatan = player.GetCurrentCharacterType() == PlayerCharacterType.Satan;
        timeOfOneHit = Attack.GetHitTime();
        hitDistance = Attack.GetHitDistance();
        comboCount++;
        if (comboCount > 2) comboCount = 1;

        if (playerIsSatan)
        {
            Animator.SetTrigger("Shooting");
        }
        else
        {
            Animator.SetTrigger("Hitting");
        }

        // Выключить все флаги комбо перед включением нового
        if (playerIsSatan)
        {
            Animator.SetBool("Shooting 1", false);
            Animator.SetBool("Shooting 2", false);
            Animator.SetBool("Shooting 3", false);
        }
        else
        {
            Animator.SetBool("Hitting 1", false);
            Animator.SetBool("Hitting 2", false);
            Animator.SetBool("Hitting 3", false);
        }

        // Установить флаг для текущего комбо
        if (playerIsSatan)
        {
            Animator.SetBool("Shooting " + comboCount, true);
        }
        else
        {
            Animator.SetBool("Hitting " + comboCount, true);
        }
        lastHitTime = Time.time;
        player.LastState = this;
        jumpRequested = false;
        attackRequested = false;
        Movement.Rb.linearVelocity = new Vector2(0f, Movement.Rb.linearVelocity.y);
    }
    public override void HandleInput()
    {
        base.HandleInput();
        jumpRequested = Movement.PlayerInput.actions["Jump"].WasPressedThisFrame();
        attackRequested = attackRequested || Movement.PlayerInput.actions["Attack"].WasPressedThisFrame();
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
            hitCompleted = true;
            if (player.debugMessages)
                Debug.Log("Hitting interrupted by jump input -> switching to JumpingState");
            stateMachine.ChangeState(player.JumpingState);
            return;
        }
        if (hitCompleted)
        {
            Debug.Log("Sostoyanie attackRequested: " + attackRequested);
            if (attackRequested)
            {
                RestartAttack();
            }
            else
            {
                stateMachine.ChangeState(player.IdleState);
            }
        }
    }

    public override void PhysicsUpdate()
    {
        Movement.Rb.linearVelocity = new Vector2(0f, Movement.Rb.linearVelocity.y);
        hitDir = CharManager.ActiveSR.flipX ? -1f : 1f;
        hitElapsed += Time.deltaTime;
        BoxCollider2D box = player.GetComponent<BoxCollider2D>();
        Vector2 origin = (box != null) ? box.bounds.center : (Vector2)player.transform.position;
        Vector2 direction = Vector2.right * hitDir;
        if (!hitCompleted)
        {
            if (playerIsSatan && !shooted && hitElapsed >= (timeOfOneHit / 2))
            {
                Vector2 spawnPos = new(origin.x + (direction.x * 0.65f), origin.y + (direction.y * 0.22f));
                GameObject bulletObj = Object.Instantiate(
                    Attack.bulletPrefab,
                    spawnPos,
                    Quaternion.identity
                );
                Debug.DrawLine(spawnPos, spawnPos + Vector2.up * 0.1f, Color.blue, 0.1f);
                Bullet bullet = bulletObj.GetComponent<Bullet>();
                bullet.damage = Attack.GetHitDamage();
                bullet.SetDirection(hitDir);
                shooted = true;
                if (player.debugMessages)
                    Debug.Log("Shot a bullet in direction " + hitDir);
            }
            if (!playerIsSatan && hitElapsed >= timeOfOneHit)
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
                        if (player.debugMessages)
                            Debug.Log("Hit " + collider.name);
                        Enemy enemy = collider.GetComponent<Enemy>();
                        if (enemy != null)
                        {
                            enemy.TakeDamage(Attack.GetHitDamage());
                            Debug.Log(
                                "Player hitted enemy with "
                                    + Attack.GetHitDamage()
                                    + " damage points"
                            );
                        }
                        else
                        {
                            DamageDummy dummy = collider.GetComponent<DamageDummy>();
                            if (dummy != null)
                            {
                                dummy.TakeDamage(Attack.GetHitDamage());
                                Debug.Log(
                                    "Player hitted dummy with "
                                        + Attack.GetHitDamage()
                                        + " damage points"
                                );
                            }
                        }
                    }
                }
                else if (player.debugMessages)
                {
                    Debug.Log("No enemies hit in area");
                }
            }
            if (hitElapsed >= timeOfOneHit)
            {
                hitCompleted = true;
            }
        }
    }

    private void RestartAttack()
    {
        hitElapsed = 0f;
        hitCompleted = false;
        shooted = false;
        attackRequested = false;

        // Выключить текущий флаг комбо
        if (playerIsSatan)
        {
            Animator.SetBool("Shooting " + comboCount, false);
        }
        else
        {
            Animator.SetBool("Hitting " + comboCount, false);
        }

        // Перейти на следующий комбо
        comboCount++;
        if (comboCount > 2) comboCount = 1;

        // Включить новый флаг комбо
        if (playerIsSatan)
        {
            Animator.SetBool("Shooting " + comboCount, true);
        }
        else
        {
            Animator.SetBool("Hitting " + comboCount, true);
        }
    }
    private void ResetAnimator()
    {
        if (playerIsSatan)
        {
            Animator.SetBool("Shooting " + comboCount, false);
        }
        else
        {
            Animator.SetBool("Hitting " + comboCount, false);
        }
    }

    public override void Exit()
    {
        base.Exit();
        hitElapsed = 0f;
        hitCompleted = false;
        shooted = false;
        attackRequested = false;
        ResetAnimator();
    }
}
