using UnityEngine;

public class JumpingState : States
{
    private bool canDoubleJump;
    private bool doubleJumpInput;
    public JumpingState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
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
        base.HandleInput();
        doubleJumpInput = player.playerInput.actions["Jump"].WasPressedThisFrame(); 
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
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
