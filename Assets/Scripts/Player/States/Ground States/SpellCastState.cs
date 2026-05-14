using UnityEngine;

public class SpellCastState : GroundedStates
{
    private const string SpellPlaceholderClipName = "SpellPlaceholder";
    private static readonly int SpellTriggerHash = Animator.StringToHash("Spell");

    private float elapsed = 0f;
    private float castMoment = 0f;
    private float castDuration = 0f;
    private float castDirection = 1f;
    private bool casted = false;
    private bool isSatanCaster = false;
    private bool jumpRequested = false;
    private SpellSlot activeSlot;
    private SpellData activeSpell;

    public SpellCastState(Player player, StateMovMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        base.Enter();
        elapsed = 0f;
        casted = false;
        jumpRequested = false;
        activeSpell = null;

        isSatanCaster = player.GetCurrentCharState() == player.SatanState;
        castDirection = player.ActiveSR != null && player.ActiveSR.flipX ? -1f : 1f;
        activeSlot = player.PendingSpellSlot;

        if (player.SpellController == null)
        {
            stateMachine.ChangeState(player.IdleState);
            return;
        }

        if (!player.SpellController.TryGetReadySpell(isSatanCaster, activeSlot, out activeSpell))
        {
            stateMachine.ChangeState(player.IdleState);
            return;
        }

        castDuration = Mathf.Max(0.05f, activeSpell.castDuration);
        castMoment = castDuration * Mathf.Clamp01(activeSpell.castMomentNormalized);
        player.Rb.linearVelocity = new Vector2(0f, player.Rb.linearVelocity.y);

        PlayCastAnimation();
    }

    public override void HandleInput()
    {
        base.HandleInput();
        jumpRequested = player.PlayerInput.actions["Jump"].WasPressedThisFrame();
    }

    public override void PhysicsUpdate()
    {
        player.Rb.linearVelocity = new Vector2(0f, player.Rb.linearVelocity.y);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (jumpRequested)
        {
            stateMachine.ChangeState(player.JumpingState);
            return;
        }

        elapsed += Time.deltaTime;
        if (!casted && elapsed >= castMoment)
        {
            CastSpell();
            casted = true;
        }

        if (elapsed >= castDuration)
        {
            if (player.Rb.linearVelocity.y < 0f)
            {
                stateMachine.ChangeState(player.AirState);
                return;
            }

            stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void Exit()
    {
        base.Exit();
    }

    private void PlayCastAnimation()
    {
        if (activeSpell.animClip == null) return;

        Animator animator = player.ActiveAnimator;
        if (animator == null) return;

        if (animator.runtimeAnimatorController is AnimatorOverrideController over)
        {
            over[SpellPlaceholderClipName] = activeSpell.animClip;
            animator.SetTrigger(SpellTriggerHash);
        }
        else
        {
            Debug.LogWarning(
                "[SpellCastState] У персонажа не настроен AnimatorOverrideController — анимация спелла не сыграет.");
        }
    }

    private void CastSpell()
    {
        if (activeSpell == null) return;

        BoxCollider2D box = player.GetComponent<BoxCollider2D>();
        Vector2 origin = box != null ? box.bounds.center : (Vector2)player.transform.position;

        SpellCastContext ctx = new SpellCastContext
        GameObject spellObj = Object.Instantiate(activeSpell.SpellPrefab, spawnPos, Quaternion.identity);

        SpellBase spellBase = spellObj.GetComponent<SpellBase>();
        if (spellBase != null)
        {
            Caster = player,
            CasterCenter = origin,
            Direction = castDirection,
            Data = activeSpell
        };
        activeSpell.Cast(ctx);

        player.SpellController.StartCooldown(isSatanCaster, activeSlot, activeSpell);
    }
}
