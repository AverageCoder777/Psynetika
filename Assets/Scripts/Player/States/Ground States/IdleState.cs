using Unity.VisualScripting;
using UnityEngine;

public class IdleState : GroundedStates
{
    private static readonly int MovingHash = Animator.StringToHash("Moving");
    private bool jump;
    private bool crouch;
    private bool roll;
    private bool @switch;
    private bool hit;
    private bool spell;
    private bool spellUltimate;
    public IdleState(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
        : base(player, stateMachine, settings) { }
    
    public override void Enter()
    {
        base.Enter();
        animator.SetBool(MovingHash, false);
        grounded = true;
        movement.Rb.linearVelocity = new Vector2(0, movement.Rb.linearVelocity.y);
    }
    public override void HandleInput()
    {
        base.HandleInput();
        crouch = movement.PlayerInput.actions["Crouch"].IsPressed();
        jump = movement.PlayerInput.actions["Jump"].WasPressedThisFrame();
        roll = movement.PlayerInput.actions["Roll"].WasPressedThisFrame();
        @switch = movement.PlayerInput.actions["Switch"].WasPressedThisFrame();
        hit = movement.PlayerInput.actions["Attack"].WasPressedThisFrame();
        spell = movement.PlayerInput.actions["Spell"].WasPressedThisFrame();
        spellUltimate = movement.PlayerInput.actions["SpellUltimate"].WasPressedThisFrame();
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (crouch)
        {
            stateMachine.ChangeState(player.CrouchingState);
        }
        else if (jump)
        {
            grounded = false;
            stateMachine.ChangeState(player.JumpingState);
        }
        else if (roll)
        {
            stateMachine.ChangeState(player.RollingState);
        }
        else if (Mathf.Abs(movement.Rb.linearVelocityY) > 0.1f && !DetectPlatform())
        {
            grounded = false;
            stateMachine.ChangeState(player.FlyingState);
        }
        else if (@switch && charManager.DogActive != false && charManager.SatanActive != false)
        {
            stateMachine.ChangeState(player.SwitchState);
        }
        else if (spell)
        {
            player.PendingSpellSlot = SpellSlot.Regular;
            stateMachine.ChangeState(player.SpellCastState);
            return;
        }
        else if (spellUltimate)
        {
            player.PendingSpellSlot = SpellSlot.Ultimate;
            stateMachine.ChangeState(player.SpellCastState);
            return;
        }
        else if (hit)
        {
            stateMachine.ChangeState(player.HittingState);
        }
    }
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
    public override void Exit()
    {
        base.Exit();
    }

    protected bool DetectPlatform()
    {
        // Проверяем столкновение вниз (направление гравитации)
        Vector2 platformDetectionDirection = Vector2.down;

        // Смещаем raycast origin вниз от центра игрока, чтобы избежать его собственного коллайдера
        Vector2 raycastOrigin = (Vector2)player.transform.position - Vector2.up * 0.5f;

        // Проверяем столкновение только вниз
        RaycastHit2D hit = Physics2D.Raycast(
            raycastOrigin,
            platformDetectionDirection,
            settings.detection.platformDetectionDistance,
            LayerMask.GetMask("Platform")
        );
        if (player.debugMessages)
        {
            Debug.DrawRay(raycastOrigin, platformDetectionDirection * settings.detection.platformDetectionDistance,
                hit.collider != null ? Color.blue : Color.yellow);
        }

        return hit.collider != null;
    }
}
