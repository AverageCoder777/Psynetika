using UnityEngine;

public class PlayerJumpingState : PlayerState
{
    private bool canDoubleJump;
    private bool doubleJumpInput;
    public PlayerJumpingState(Player player, PlayerStateMachine playerStateMachine)
        : base(player, playerStateMachine)
    {
    }

    public override void Enter()
    {
        Jump();
        canDoubleJump = true;
        animator.SetTrigger("Jumping");
        Debug.Log("Entered Jumping State");
    }
    private void Jump()
    {
        player.Rb.AddForce(player.transform.up * player.Thrust, ForceMode2D.Impulse);
    }
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
    public override void HandleInput()
    {
        doubleJumpInput = player.playerInput.actions["Jump"].WasPressedThisFrame();
        player.MovementInput = player.playerInput.actions["Move"].ReadValue<Vector2>();   
    }
    public override void LogicUpdate()
    {
        if (doubleJumpInput && canDoubleJump && player.transform.position.y < player.MaxDoubleJumpHeight)
        {
            Jump();
            animator.SetTrigger("DoubleJumping");
            canDoubleJump = false;
        }
        if (player.Rb.linearVelocity.y < 0)
        {
            stateMachine.ChangeState(player.FallingState);
        }
        if (player.Rb.linearVelocity.y == 0)
        {
            stateMachine.ChangeState(player.IdleState);
        }

    }
    public override void Exit()
    {
        base.Exit();
        animator.ResetTrigger("Jumping");
        animator.ResetTrigger("DoubleJumping");
        Debug.Log("Exited Jumping State");
    }
}
