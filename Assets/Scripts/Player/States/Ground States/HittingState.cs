using UnityEngine;

// Удар собаки / выстрел Сатаны. Взмах делится на замах (до hitMomentNormalized) и восстановление.
// Во время атаки можно медленно идти (attackMoveSpeedMultiplier); прыжок и перекат прерывают атаку
// в любой момент, ввод направления — на восстановлении. Анимация ускоряется под длительность взмаха.
public class HittingState : GroundedStates
{
    private float hitElapsed = 0f;
    private float hitDir = 1f;
    private float timeOfOneHit = 0f;
    private float hitDistance = 0f;
    private int damage;
    private bool hitCompleted = false;
    private bool jumpRequested = false;
    private bool rollRequested = false;
    private bool attackRequested = false;
    private LayerMask enemyMask = LayerMask.GetMask("Enemy");
    private int comboCount = 0;
    private float lastHitTime = 0f;
    private bool playerIsSatan;
    private bool shooted = false;
    private bool meleeDone = false;
    private EmpoweredAttackSession empoweredSwing;
    private bool empoweredSwingFired = false;

    // У усиленного взмаха Ину свой момент срабатывания — восстановление начинается только после него.
    private float HitMoment => timeOfOneHit * (empoweredSwing != null
        ? empoweredSwing.SwingMomentNormalized
        : settings.combat.hitMomentNormalized);
    private bool InRecovery => hitElapsed >= HitMoment;

    public HittingState(PlayerController player, StateMachine playerStateMachine, PlayerStaticSettings settings)
        : base(player, playerStateMachine, settings) { }

    public override void Enter()
    {
        base.Enter();
        attack.ConsumeBufferedAttack();
        if (Time.time - lastHitTime > settings.combat.comboResetTime)
        {
            comboCount = 0;
        }
        hitElapsed = 0f;
        hitCompleted = false;
        shooted = false;
        meleeDone = false;
        playerIsSatan = charManager.GetCurrentCharacterType() == PlayerCharacterType.Satan;
        PrepareSwing();
        hitDistance = attack.GetCurrentAttackStat<float>(AttackStatId.AttackRange);
        damage = attack.GetCurrentAttackStat<int>(AttackStatId.CurrentDamage);
        comboCount++;
        if (comboCount > 2) comboCount = 1;

        charManager.ActiveAnimator.SetTrigger(playerIsSatan ? "Shooting" : "Hitting");

        // Выключить все флаги комбо перед включением нового
        string prefix = ComboPrefix;
        charManager.ActiveAnimator.SetBool(prefix + "1", false);
        charManager.ActiveAnimator.SetBool(prefix + "2", false);
        charManager.ActiveAnimator.SetBool(prefix + "3", false);
        charManager.ActiveAnimator.SetBool(prefix + comboCount, true);

        lastHitTime = Time.time;
        jumpRequested = false;
        rollRequested = false;
        attackRequested = false;
    }

    private string ComboPrefix => playerIsSatan ? "Shooting " : "Hitting ";

    public override void HandleInput()
    {
        base.HandleInput();
        jumpRequested = movement.PlayerInput.actions["Jump"].WasPressedThisFrame();
        rollRequested = movement.PlayerInput.actions["Roll"].WasPressedThisFrame();
        attackRequested = attackRequested || movement.PlayerInput.actions["Attack"].WasPressedThisFrame();
    }

    public override void LogicUpdate()
    {
        if (jumpRequested)
        {
            if (player.debugMessages)
                Debug.Log("Hitting interrupted by jump input -> switching to JumpingState");
            stateMachine.ChangeState(player.JumpingState);
            return;
        }

        if (rollRequested && settings.combat.rollCancelsAttack)
        {
            // Перекат в сторону зажатого направления — уход из-под удара назад тоже работает.
            if (Mathf.Abs(movementInput.x) > 0.01f)
                charManager.ActiveSR.flipX = movementInput.x < 0f;
            stateMachine.ChangeState(player.RollingState);
            return;
        }

        // Атакуя на ходу, можно сойти с края — тогда падаем, а не «висим» в ударе.
        if (movement.Rb.linearVelocity.y < settings.detection.jumpWallVelocityThreshold && DetectFloor() == "None")
        {
            stateMachine.ChangeState(player.FlyingState);
            return;
        }

        if (InRecovery && settings.combat.moveCancelsRecovery && !attackRequested
            && Mathf.Abs(movementInput.x) > 0.01f)
        {
            stateMachine.ChangeState(player.IdleState);
            return;
        }

        if (hitCompleted)
        {
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
        UpdateMovement();

        hitDir = charManager.ActiveSR.flipX ? -1f : 1f;
        hitElapsed += Time.fixedDeltaTime;
        BoxCollider2D box = player.GetComponent<BoxCollider2D>();
        Vector2 origin = (box != null) ? box.bounds.center : (Vector2)player.transform.position;
        Vector2 direction = Vector2.right * hitDir;
        if (hitCompleted)
        {
            return;
        }

        if (playerIsSatan && !shooted && hitElapsed >= HitMoment)
        {
            Vector2 spawnPos = new(origin.x + (direction.x * settings.combat.bulletSpawnOffsetX), origin.y + (direction.y * settings.combat.bulletSpawnOffsetY));
            Debug.DrawLine(spawnPos, spawnPos + Vector2.up * 0.1f, Color.blue, 0.1f);
            attack.FireBullet(spawnPos, hitDir);
            shooted = true;
            if (player.debugMessages)
                Debug.Log("Shot a bullet in direction " + hitDir);
        }
        if (empoweredSwing != null && !empoweredSwingFired && hitElapsed >= HitMoment)
        {
            empoweredSwingFired = true;
            empoweredSwing.TrySwing(hitDir);
        }
        if (!playerIsSatan && empoweredSwing == null && !meleeDone && hitElapsed >= HitMoment)
        {
            meleeDone = true;
            DoMeleeHit(origin, direction);
        }
        if (hitElapsed >= timeOfOneHit)
        {
            hitCompleted = true;
        }
    }

    // Ходьба во время атаки: медленнее обычного. Направление взгляда фиксировано на замахе
    // (удар не «перевернётся»), на восстановлении можно развернуться к следующему удару комбо.
    private void UpdateMovement()
    {
        float targetX = movementInput.x * movement.GetCurrentSpeed() * settings.combat.attackMoveSpeedMultiplier;
        float currentX = movement.Rb.linearVelocity.x;
        float rate = Mathf.Abs(movementInput.x) > 0.01f ? settings.move.accelerationRate : settings.move.frictionRate;
        float newX = Mathf.Lerp(currentX, targetX, rate * Time.fixedDeltaTime);
        movement.Rb.linearVelocity = new Vector2(newX, movement.Rb.linearVelocity.y);

        if (InRecovery)
        {
            if (movementInput.x > 0.01f)
                charManager.ActiveSR.flipX = false;
            else if (movementInput.x < -0.01f)
                charManager.ActiveSR.flipX = true;
        }
    }

    private void DoMeleeHit(Vector2 origin, Vector2 direction)
    {
        // Зона удара — квадрат перед героем
        Vector2 boxSize = new(hitDistance, hitDistance);
        Vector2 boxCenter = origin + direction * (hitDistance / 2f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, enemyMask);

        Debug.DrawLine(boxCenter + new Vector2(-boxSize.x/2, -boxSize.y/2),
                      boxCenter + new Vector2(boxSize.x/2, -boxSize.y/2), Color.green);
        Debug.DrawLine(boxCenter + new Vector2(boxSize.x/2, -boxSize.y/2),
                      boxCenter + new Vector2(boxSize.x/2, boxSize.y/2), Color.green);
        Debug.DrawLine(boxCenter + new Vector2(boxSize.x/2, boxSize.y/2),
                      boxCenter + new Vector2(-boxSize.x/2, boxSize.y/2), Color.green);
        Debug.DrawLine(boxCenter + new Vector2(-boxSize.x/2, boxSize.y/2),
                      boxCenter + new Vector2(-boxSize.x/2, -boxSize.y/2), Color.green);

        if (hits.Length == 0)
        {
            if (player.debugMessages)
                Debug.Log("No enemies hit in area");
            return;
        }

        foreach (Collider2D collider in hits)
        {
            if (player.debugMessages)
                Debug.Log("Hit " + collider.name);
            if (DamageHelper.TryDamage(collider, attack.GetCurrentAttackStat<int>(AttackStatId.CurrentDamage), DamageType.Physical, attack))
            {
                if (player.debugMessages)
                    Debug.Log("Player hit " + collider.name + " with " + attack.GetCurrentAttackStat<int>(AttackStatId.CurrentDamage) + " damage points");
            }
            else
            {
                DamageDummy dummy = collider.GetComponent<DamageDummy>();
                if (dummy != null)
                {
                    dummy.TakeDamage(damage);
                    if (player.debugMessages)
                        Debug.Log("Player hitted dummy with " + damage + " damage points");
                }
            }
        }
    }

    // Ину в режиме усиленных атак (её E): взмах быстрее, и вместо удара в упор он запускает
    // ноды способности (воздушную волну). Решается на каждый взмах — режим мог кончиться посреди комбо.
    private void PrepareSwing()
    {
        timeOfOneHit = attack.GetCurrentAttackStat<float>(AttackStatId.AttackSpeed);
        empoweredSwing = playerIsSatan ? null : attack.EmpoweredAttack;
        empoweredSwingFired = false;
        if (empoweredSwing != null)
        {
            timeOfOneHit *= empoweredSwing.SwingTimeMultiplier;
        }
        // Анимацию взмаха ускоряем вместе с ним, иначе она обрежется на середине.
        float clipLength = playerIsSatan ? settings.combat.satanSwingClipLength : settings.combat.dogSwingClipLength;
        charManager.ActiveAnimator.speed = clipLength / Mathf.Max(0.05f, timeOfOneHit);
    }

    private void RestartAttack()
    {
        hitElapsed = 0f;
        hitCompleted = false;
        shooted = false;
        meleeDone = false;
        attackRequested = false;
        attack.ConsumeBufferedAttack();
        PrepareSwing();

        string prefix = ComboPrefix;
        charManager.ActiveAnimator.SetBool(prefix + comboCount, false);

        comboCount++;
        if (comboCount > 2) comboCount = 1;

        charManager.ActiveAnimator.SetBool(prefix + comboCount, true);
        lastHitTime = Time.time;
    }

    private void ResetAnimator()
    {
        charManager.ActiveAnimator.SetBool(ComboPrefix + comboCount, false);
    }

    public override void Exit()
    {
        base.Exit();
        hitElapsed = 0f;
        hitCompleted = false;
        shooted = false;
        meleeDone = false;
        attackRequested = false;
        empoweredSwing = null;
        charManager.ActiveAnimator.speed = 1f;
        ResetAnimator();
    }
}
