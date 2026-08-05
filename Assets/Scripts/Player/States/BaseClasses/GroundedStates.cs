using UnityEngine;

public abstract class GroundedStates : State
{
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int MovingHash = Animator.StringToHash("Moving");
    protected Vector2 movementInput;
    protected bool grounded = true;
    public GroundedStates(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings) 
        : base(player, stateMachine, settings) { }
    public override void Enter()
    {
        animator.SetBool(GroundedHash, true);
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (movementInput.x != 0)
        {
            animator.SetBool(MovingHash, true);
        }
        else
        {
            animator.SetBool(MovingHash, false);
        }
    }
    public override void HandleInput()
    {
        base.HandleInput();
        movementInput = movement.PlayerInput.actions["Move"].ReadValue<Vector2>();
    }
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        float targetVelocityX = movementInput.x * movement.GetCurrentSpeed();
        float currentVelocityX = movement.Rb.linearVelocity.x;
        float accelerationToUse = movementInput.x != 0 ? settings.move.accelerationRate : settings.move.frictionRate;
        float newVelocityX = Mathf.Lerp(currentVelocityX, targetVelocityX, accelerationToUse * Time.fixedDeltaTime);
        
        movement.Rb.linearVelocity = new Vector2(newVelocityX, movement.Rb.linearVelocity.y);
        
        // Крутим спрайт в зависимости от направления
        if (movementInput.x > 0.01f)
            charManager.ActiveSR.flipX = false;
        else if (movementInput.x < -0.01f)
            charManager.ActiveSR.flipX = true;
    }
    public override void Exit()
    {
        base.Exit();
        animator.SetBool(MovingHash, false);
        animator.SetBool(GroundedHash, false);
    }
}
