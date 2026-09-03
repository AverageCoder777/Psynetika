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

    public SpellCastState(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
        : base(player, stateMachine, settings) { }

    public override void Enter()
    {
        base.Enter();
        elapsed = 0f;
        casted = false;
        jumpRequested = false;
        activeAbility = null;

        isSatanCaster = charManager.GetCurrentCharacterType() == PlayerCharacterType.Satan;
        castDirection = charManager.ActiveSR != null && charManager.ActiveSR.flipX ? -1f : 1f;
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

        castDuration = activeAbility.matchDurationToClip && activeAbility.animClip != null
            ? Mathf.Max(0.05f, activeAbility.animClip.length)
            : Mathf.Max(0.05f, activeAbility.castDuration);
        castMoment = castDuration * Mathf.Clamp01(activeAbility.castMomentNormalized);
        movement.Rb.linearVelocity = new Vector2(0f, movement.Rb.linearVelocity.y);

        PlayCastAnimation();
    }

    public override void HandleInput()
    {
        base.HandleInput();
        jumpRequested = movement.PlayerInput.actions["Jump"].WasPressedThisFrame();
    }

    public override void PhysicsUpdate()
    {
        movement.Rb.linearVelocity = new Vector2(0f, movement.Rb.linearVelocity.y);
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
            if (movement.Rb.linearVelocity.y < 0f)
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
        if (animator.runtimeAnimatorController is AnimatorOverrideController over)
        {
            if (HasPlaceholderInBaseController(over))
            {
                // Строковый индексатор ссылается на имя клипа БАЗОВОГО контроллера, поэтому
                // работает и после предыдущих подмен (поиск по over.animationClips — нет:
                // он возвращает уже подменённые клипы). null снимает подмену — способность
                // без своего клипа играет плейсхолдер, а не клип предыдущей способности.
                over[SpellPlaceholderClipName] = activeAbility.animClip;
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
        else if (!warnedAboutOverride)
        {
            Debug.LogWarning(
                "[SpellCastState] У персонажа не настроен AnimatorOverrideController — " +
                "анимация скилла будет играть только то, что зашито в SpellPlaceholder-стейт.");
            warnedAboutOverride = true;
        }

        animator.SetTrigger(SpellTriggerHash);
    }

    private static bool HasPlaceholderInBaseController(AnimatorOverrideController over)
    {
        AnimationClip[] clips = over.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].name == SpellPlaceholderClipName)
            {
                return true;
            }
        }
        return false;
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
