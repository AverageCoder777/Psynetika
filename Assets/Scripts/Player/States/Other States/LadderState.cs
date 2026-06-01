using UnityEngine;

public class LadderState : State
{
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int ClimbingHash = Animator.StringToHash("Climbing");
    private readonly float climbSpeed = 5f;
    private Ladder currentLadder;
    private float enterTime = 0f;
    private readonly float exitDelay = 0.25f; // Задержка перед возможностью выхода

    public LadderState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public void SetLadder(Ladder ladder)
    {
        currentLadder = ladder;
    }

    public override void Enter()
    {
        base.Enter();
    
        Movement.Rb.gravityScale = 0f;
        Movement.Rb.linearVelocity = Vector2.zero;
        
        Animator.SetBool(ClimbingHash, true);
        Animator.SetBool(GroundedHash, false);
        
        enterTime = Time.time;
        
        player.LastState = this;
    }

    public override void HandleInput()
    {
        base.HandleInput();
        Movement.MovementInput = Movement.PlayerInput.actions["Move"].ReadValue<Vector2>();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (Time.time - enterTime > exitDelay && Movement.PlayerInput.actions["Interact"].WasPressedThisFrame())
        {
            stateMachine.ChangeState(player.FlyingState);
            return;
        }
        
        if (Movement.MovementInput.x != 0)
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
        
        if (Movement.MovementInput.y > 0)
        {
            verticalMovement = climbSpeed;
        }
        else if (Movement.MovementInput.y < 0)
        {
            verticalMovement = -climbSpeed;
        }
        Movement.Rb.linearVelocity = new Vector2(0f, verticalMovement);
    }

    public override void Exit()
    {
        base.Exit();

        Movement.Rb.gravityScale = Movement.DownGravityScale;
        Animator.SetBool(ClimbingHash, false);
        Animator.SetBool(GroundedHash, true);
    }
}
