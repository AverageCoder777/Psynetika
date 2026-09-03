using UnityEngine;

public class LadderState : State
{
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int ClimbingHash = Animator.StringToHash("Climbing");
    private Ladder currentLadder;
    private float enterTime = 0f;

    public LadderState(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
         : base(player, stateMachine, settings) { }

    public void SetLadder(Ladder ladder)
    {
        currentLadder = ladder;
    }

    public override void Enter()
    {
        base.Enter();
    
        movement.Rb.gravityScale = 0f;
        movement.Rb.linearVelocity = Vector2.zero;
        
        animator.SetBool(ClimbingHash, true);
        animator.SetBool(GroundedHash, false);
        
        enterTime = Time.time;
        player.LastState = this;
    }

    public override void HandleInput()
    {
        base.HandleInput();
        movement.MovementInput = movement.PlayerInput.actions["Move"].ReadValue<Vector2>();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (Time.time - enterTime > settings.ladder.exitDelay && movement.PlayerInput.actions["Interact"].WasPressedThisFrame())
        {
            stateMachine.ChangeState(player.FlyingState);
            return;
        }
        
        if (movement.MovementInput.x != 0)
        {
            stateMachine.ChangeState(player.FlyingState);
            return;
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        if (currentLadder != null)
        {
            Vector3 playerPos = player.transform.position;
            playerPos.x = currentLadder.transform.position.x;
            player.transform.position = playerPos;
        }

        float verticalMovement = 0f;
        
        if (movement.MovementInput.y > 0)
        {
            verticalMovement = settings.ladder.climbSpeed;
        }
        else if (movement.MovementInput.y < 0)
        {
            verticalMovement = -settings.ladder.climbSpeed;
        }
        movement.Rb.linearVelocity = new Vector2(0f, verticalMovement);
    }

    public override void Exit()
    {
        base.Exit();

        movement.Rb.gravityScale = settings.jump.downGravityScale;
        animator.SetBool(ClimbingHash, false);
        animator.SetBool(GroundedHash, true);
    }
}
