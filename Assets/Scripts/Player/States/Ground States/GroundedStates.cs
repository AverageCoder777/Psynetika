using UnityEngine;

public abstract class GroundedStates : State
{
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int MovingHash = Animator.StringToHash("Moving");
    protected Vector2 movementInput;
    protected bool grounded = true;
    protected bool hasMoveInput;
    public GroundedStates(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
        : base(player, stateMachine, settings) { }
    public override void Enter()
    {
        charManager.ActiveAnimator.SetBool(GroundedHash, true);
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
        hasMoveInput = Mathf.Abs(movementInput.x) > 0.01f;
        charManager.ActiveAnimator.SetBool(MovingHash, hasMoveInput);
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
        if (Mathf.Abs(movementInput.x) < 0.01f && Mathf.Abs(newVelocityX) < 0.05f)
        {
            newVelocityX = 0f;
        }
        movement.Rb.linearVelocity = new Vector2(newVelocityX, movement.Rb.linearVelocity.y);

        if (movementInput.x > 0.01f)
            charManager.ActiveSR.flipX = false;
        else if (movementInput.x < -0.01f)
            charManager.ActiveSR.flipX = true;
    }
    public override void Exit()
    {
        base.Exit();
        charManager.ActiveAnimator.SetBool(MovingHash, false);
        charManager.ActiveAnimator.SetBool(GroundedHash, false);
    }
}
