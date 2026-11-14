using UnityEngine;

public class HittingState : GroundedStates
{
    private float hitElapsed = 0f;
    private float hitDir = 1f;
    private float timeOfOneHit = 0f;
    private float hitDistance = 0f;
    private int damage;
    private bool hitCompleted = false;
    private bool meleeHitDone = false; // Чтобы бить в середине анимации, а не в конце
    private bool jumpRequested = false;
    private bool attackRequested = false;
    private readonly LayerMask enemyMask = LayerMask.GetMask("Enemy");
    private int comboCount = 0;
    private float lastHitTime = 0f;
    private bool playerIsSatan;
    private bool shooted = false;
    private EmpoweredAttackSession empoweredSwing;
    private bool empoweredSwingFired = false;
    private BoxCollider2D playerBoxCollider;

    // Нормализованный момент удара для ближнего боя (0.5 = середина взмаха, 1.0 = самый конец)
    private const float MeleeHitNormalizedTime = 0.5f;

    public HittingState(PlayerController player, StateMachine playerStateMachine, PlayerStaticSettings settings)
        : base(player, playerStateMachine, settings)
    {
        playerBoxCollider = player.GetComponent<BoxCollider2D>();
    }

    public override void Enter()
    {
        base.Enter();
        if (Time.time - lastHitTime > settings.combat.comboResetTime)
        {
            comboCount = 0;
        }

        playerIsSatan = charManager.GetCurrentCharacterType() == PlayerCharacterType.Satan;
        jumpRequested = false;
        attackRequested = false;
        movement.Rb.linearVelocity = new Vector2(0f, movement.Rb.linearVelocity.y);

        StartNewSwing(isFirstSwing: true);
    }

    public override void HandleInput()
    {
        base.HandleInput();
        jumpRequested |= movement.PlayerInput.actions["Jump"].WasPressedThisFrame();
        attackRequested |= movement.PlayerInput.actions["Attack"].WasPressedThisFrame();
    }

    public override void LogicUpdate()
    {
        //base.LogicUpdate();

        if (jumpRequested)
        {
            charManager.ActiveAnimator.SetBool("Attack " + comboCount, false);
            hitCompleted = true;
            stateMachine.ChangeState(player.JumpingState);
            return;
        }

        SyncAttackClipSpeed();

        // Считаем таймер в LogicUpdate синхронно с рендером и Animator
        if (!hitCompleted)
        {
            hitElapsed += Time.deltaTime;
            ProcessAttackEvents();

            if (hitElapsed >= timeOfOneHit)
            {
                hitCompleted = true;
                charManager.ActiveAnimator.SetBool("Attack " + comboCount, false);
            }
        }

        if (hitCompleted)
        {
            if (attackRequested)
            {
                StartNewSwing(isFirstSwing: false);
                return;
            }

            stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        movement.Rb.linearVelocity = new Vector2(0f, movement.Rb.linearVelocity.y);
    }

    public override void Exit()
    {
        base.Exit();
        hitElapsed = 0f;
        hitCompleted = false;
        shooted = false;
        meleeHitDone = false;
        attackRequested = false;
        empoweredSwing = null;
        charManager.ActiveAnimator.SetBool("Attack " + comboCount, false);
    }

    private void StartNewSwing(bool isFirstSwing)
    {
        hitElapsed = 0f;
        hitCompleted = false;
        shooted = false;
        meleeHitDone = false;
        attackRequested = false;

        if (!isFirstSwing)
        {
            charManager.ActiveAnimator.SetBool("Attack " + comboCount, false);
        }

        // Если в игре есть Attack 3 — замени 2 на 3
        comboCount++;
        if (comboCount > 2) comboCount = 1;

        PrepareSwing();
        hitDistance = attack.GetCurrentAttackStat<float>(AttackStatId.AttackRange);
        damage = attack.GetCurrentAttackStat<int>(AttackStatId.CurrentDamage);

        Animator anim = charManager.ActiveAnimator;
        if (isFirstSwing)
        {
            anim.SetBool("Attack 1", false);
            anim.SetBool("Attack 2", false);
            anim.SetBool("Attack 3", false);
        }

        anim.SetBool("Attack " + comboCount, true);
        anim.Update(0f);
        SyncAttackClipSpeed();

        lastHitTime = Time.time;
    }

    private void ProcessAttackEvents()
    {
        hitDir = charManager.ActiveSR.flipX ? -1f : 1f;
        Vector2 origin = (playerBoxCollider != null) ? playerBoxCollider.bounds.center : (Vector2)player.transform.position;
        Vector2 direction = Vector2.right * hitDir;

        // 1. Дальний бой (Сатана)
        if (playerIsSatan && !shooted && hitElapsed >= (timeOfOneHit * 0.5f))
        {
            Vector2 spawnPos = new(
                origin.x + (direction.x * settings.combat.bulletSpawnOffsetX),
                origin.y + (direction.y * settings.combat.bulletSpawnOffsetY)
            );
            attack.FireBullet(spawnPos, hitDir);
            shooted = true;
        }

        // 2. Усиленная атака (Ину)
        if (empoweredSwing != null && !empoweredSwingFired
            && hitElapsed >= timeOfOneHit * empoweredSwing.SwingMomentNormalized)
        {
            empoweredSwingFired = true;
            empoweredSwing.TrySwing(hitDir);
        }

        // 3. Обычный удар в ближнем бою
        if (!playerIsSatan && empoweredSwing == null && !meleeHitDone
            && hitElapsed >= timeOfOneHit * MeleeHitNormalizedTime)
        {
            meleeHitDone = true;
            PerformMeleeHit(origin, direction);
        }
    }

    private void PerformMeleeHit(Vector2 origin, Vector2 direction)
    {
        Vector2 boxSize = new(hitDistance, hitDistance);
        Vector2 boxCenter = origin + direction * (hitDistance / 2f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, enemyMask);
        foreach (Collider2D collider in hits)
        {
            if (!DamageHelper.TryDamage(collider, damage, DamageType.Physical, attack))
            {
                if (collider.TryGetComponent(out DamageDummy dummy))
                {
                    dummy.TakeDamage(damage);
                }
            }
        }
    }

    private void PrepareSwing()
    {
        // ВНИМАНИЕ: Если AttackSpeed — это удары в секунду, замени на: 1f / attack.GetCurrentAttackStat<float>(...)
        timeOfOneHit = attack.GetCurrentAttackStat<float>(AttackStatId.AttackSpeed);
        empoweredSwing = playerIsSatan ? null : attack.EmpoweredAttack;
        empoweredSwingFired = false;

        if (empoweredSwing != null)
        {
            timeOfOneHit *= empoweredSwing.SwingTimeMultiplier;
        }
    }

    private void SyncAttackClipSpeed()
    {
        if (hitCompleted || timeOfOneHit <= 0f) return;

        Animator animator = charManager.ActiveAnimator;

        // Если идет переход (Transition), берем данные СЛЕДУЮЩЕГО стейта (самой атаки), иначе текущего
        bool inTransition = animator.IsInTransition(0);
        AnimatorStateInfo stateInfo = inTransition
            ? animator.GetNextAnimatorStateInfo(0)
            : animator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName("Attack 1") && !stateInfo.IsName("Attack 2") && !stateInfo.IsName("Attack 3"))
        {
            return;
        }

        AnimatorClipInfo[] clips = inTransition
            ? animator.GetNextAnimatorClipInfo(0)
            : animator.GetCurrentAnimatorClipInfo(0);

        if (clips == null || clips.Length == 0 || clips[0].clip == null) return;

        float clipLength = clips[0].clip.length;
        if (clipLength <= 0f) return;
        animator.SetFloat("Attack Speed", clipLength / timeOfOneHit);
    }
}
