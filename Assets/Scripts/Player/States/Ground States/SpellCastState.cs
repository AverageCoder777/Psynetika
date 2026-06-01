using UnityEngine;

public class SpellCastState : GroundedStates
{
    private const string SpellPlaceholderClipName = "SpellPlaceholder";
    private static readonly int SpellTriggerHash = Animator.StringToHash("Spell");
    private static bool warnedAboutOverride = false;
    private static bool warnedAboutPlaceholder = false;

    private float elapsed = 0f;
    private float castMoment = 0f;
    private float castDuration = 0f;
    private float castDirection = 1f;
    private bool casted = false;
    private bool isSatanCaster = false;
    private bool jumpRequested = false;
    private SpellSlot activeSlot;
    private AbilityDefinition activeAbility;

    public SpellCastState(PlayerController player, StateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        base.Enter();
        elapsed = 0f;
        casted = false;
        jumpRequested = false;
        activeAbility = null;

        isSatanCaster = player.GetCurrentCharacterType() == PlayerCharacterType.Satan;
        castDirection = CharManager.ActiveSR != null && CharManager.ActiveSR.flipX ? -1f : 1f;
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
        Movement.Rb.linearVelocity = new Vector2(0f, Movement.Rb.linearVelocity.y);

        PlayCastAnimation();
    }

    public override void HandleInput()
    {
        base.HandleInput();
        jumpRequested = Movement.PlayerInput.actions["Jump"].WasPressedThisFrame();
    }

    public override void PhysicsUpdate()
    {
        Movement.Rb.linearVelocity = new Vector2(0f, Movement.Rb.linearVelocity.y);
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
            if (Movement.Rb.linearVelocity.y < 0f)
            {
                stateMachine.ChangeState(player.FlyingState);
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
        Animator animator = CharManager.ActiveAnimator;
        if (animator == null) return;

        if (animator.runtimeAnimatorController is AnimatorOverrideController over)
        {
            if (activeAbility.animClip != null)
            {
                AnimationClip key = FindPlaceholderKey(over);
                if (key != null)
                {
                    over[key] = activeAbility.animClip;
                }
                else if (!warnedAboutPlaceholder)
                {
                    Debug.LogWarning(
                        $"[SpellCastState] В базовом контроллере '{over.runtimeAnimatorController.name}' " +
                        $"нет AnimationClip с именем '{SpellPlaceholderClipName}'. " +
                        "Переименуй .anim-файл, который стоит Motion'ом в стейте SpellPlaceholder, " +
                        $"чтобы его asset-имя было '{SpellPlaceholderClipName}'.");
                    warnedAboutPlaceholder = true;
                }
            }
        }
        else if (!warnedAboutOverride)
        {
            Debug.LogWarning(
                "[SpellCastState] У персонажа не настроен AnimatorOverrideController — " +
                "анимация скилла будет играть только то, что зашито в SpellPlaceholder-стейт.");
            warnedAboutOverride = true;
        }

        animator.SetTrigger(SpellTriggerHash);
    }

    private static AnimationClip FindPlaceholderKey(AnimatorOverrideController over)
    {
        AnimationClip[] clips = over.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].name == SpellPlaceholderClipName)
            {
                return clips[i];
            }
        }
        return null;
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
