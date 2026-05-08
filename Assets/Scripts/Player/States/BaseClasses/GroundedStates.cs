using UnityEngine;

public abstract class GroundedStates : State
{
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int MovingHash = Animator.StringToHash("Moving");
    protected bool grounded = true;
    public GroundedStates(Player player, StateMovMachine stateMachine) : base(player, stateMachine)
    {
    }
    public override void Enter()
    {
        Animator.SetBool(GroundedHash, true);
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (player.MovementInput.x != 0)
        {
            Animator.SetBool(MovingHash, true);
        }
        else
        {
            Animator.SetBool(MovingHash, false);
        }
    }
    public override void HandleInput()
    {
        base.HandleInput();
        player.MovementInput = player.PlayerInput.actions["Move"].ReadValue<Vector2>();
    }
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        float targetVelocityX = player.MovementInput.x * player.GetCharSpeed();
        float currentVelocityX = player.Rb.linearVelocity.x;
        float accelerationToUse = player.MovementInput.x != 0 ? player.AccelerationRate : player.FrictionRate;
        float newVelocityX = Mathf.Lerp(currentVelocityX, targetVelocityX, accelerationToUse * Time.fixedDeltaTime);
        
        player.Rb.linearVelocity = new Vector2(newVelocityX, player.Rb.linearVelocity.y);
        
        // Крутим спрайт в зависимости от направления
        if (player.MovementInput.x > 0.01f)
            player.ActiveSR.flipX = false;
        else if (player.MovementInput.x < -0.01f)
            player.ActiveSR.flipX = true;
    }
    public override void Exit()
    {
        base.Exit();
        Animator.SetBool(MovingHash, false);
        Animator.SetBool(GroundedHash, false);
    }
}
