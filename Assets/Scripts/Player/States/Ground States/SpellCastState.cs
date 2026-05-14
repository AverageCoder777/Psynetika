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
    private AbilityDefinition activeAbility;

    public SpellCastState(Player player, StateMovMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        base.Enter();
        elapsed = 0f;
        casted = false;
        jumpRequested = false;
        activeAbility = null;

        isSatanCaster = player.GetCurrentCharState() == player.SatanState;
        castDirection = player.ActiveSR != null && player.ActiveSR.flipX ? -1f : 1f;
        activeSlot = player.PendingSpellSlot;

        if (player.SpellController == null)
        {
            stateMachine.ChangeState(player.IdleState);
            return;
        }

        if (!player.SpellController.TryGetReadyAbility(isSatanCaster, activeSlot, out activeAbility))
        {
            stateMachine.ChangeState(player.IdleState);
            return;
        }

        castDuration = Mathf.Max(0.05f, activeAbility.castDuration);
        castMoment = castDuration * Mathf.Clamp01(activeAbility.castMomentNormalized);
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
        if (activeAbility.animClip == null) return;

        Animator animator = player.ActiveAnimator;
        if (animator == null) return;

        if (animator.runtimeAnimatorController is AnimatorOverrideController over)
        {
            over[SpellPlaceholderClipName] = activeAbility.animClip;
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
        if (activeAbility == null) return;

        BoxCollider2D box = player.GetComponent<BoxCollider2D>();
        Vector2 origin = box != null ? box.bounds.center : (Vector2)player.transform.position;
        Vector2 aimPosition = origin + Vector2.right * castDirection;
        player.SpellController.TryCast(isSatanCaster, activeSlot, null, aimPosition);
    }
}
