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
    public IdleState(Player player, StateMovMachine stateMachine)
        : base(player, stateMachine)
    {
    }
    public override void Enter()
    {
        base.Enter();
        Animator.SetBool(MovingHash, false);
        grounded = true;
        player.Rb.linearVelocity = new Vector2(0, player.Rb.linearVelocity.y);
        if (player.DebugMessages) Debug.Log("Entered Idle State");

    }
    public override void HandleInput()
    {
        base.HandleInput();
        crouch = player.PlayerInput.actions["Crouch"].IsPressed();
        jump = player.PlayerInput.actions["Jump"].WasPressedThisFrame();
        roll = player.PlayerInput.actions["Roll"].WasPressedThisFrame();
        @switch = player.PlayerInput.actions["Switch"].WasPressedThisFrame();
        hit = player.PlayerInput.actions["Attack"].WasPressedThisFrame();
        spell = player.PlayerInput.actions["Spell"].WasPressedThisFrame();
        spellUltimate = player.PlayerInput.actions["SpellUltimate"].WasPressedThisFrame();
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
        if (player.Rb.linearVelocity.y < 0 && !DetectPlatform())
        {
            grounded = false;
            stateMachine.ChangeState(player.AirState);
        }
        if (roll)
        {
            stateMachine.ChangeState(player.RollingState);
        }
        if (@switch)
        {
            stateMachine.ChangeState(player.SwitchState);
        }
        if (spell)
        {
            player.PendingSpellSlot = SpellSlot.Regular;
            stateMachine.ChangeState(player.SpellCastState);
            return;
        }
        if (spellUltimate)
        {
            player.PendingSpellSlot = SpellSlot.Ultimate;
            stateMachine.ChangeState(player.SpellCastState);
            return;
        }
        if (hit)
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
        if (player.DebugMessages) Debug.Log("Exited Idle State");
    }

    protected bool DetectPlatform()
    {
        // Проверяем столкновение вниз (направление гравитации)
        Vector2 platformDetectionDirection = Vector2.down;
        
        // Смещаем raycast origin вниз от центра игрока, чтобы избежать его собственного коллайдера
        Vector2 raycastOrigin = (Vector2)player.transform.position - Vector2.up * 0.5f;
        
        float detectionDistance = 1f;
        
        // Проверяем столкновение только вниз
        RaycastHit2D hit = Physics2D.Raycast(
            raycastOrigin,
            platformDetectionDirection,
            detectionDistance,
            LayerMask.GetMask("Platform")
        );

        // Отрисовка raycast для отладки (синий - столкновение, желтый - нет)
        Debug.DrawRay(raycastOrigin, platformDetectionDirection * detectionDistance, 
            hit.collider != null ? Color.blue : Color.yellow);

        return hit.collider != null;
    }
}
