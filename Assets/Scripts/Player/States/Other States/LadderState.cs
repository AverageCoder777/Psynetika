using UnityEngine;

public class LadderState : State
{
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int ClimbingHash = Animator.StringToHash("Climbing");
    private readonly float climbSpeed = 5f;
    private Ladder currentLadder;
    private float enterTime = 0f;
    private readonly float exitDelay = 0.25f; // Задержка перед возможностью выхода

    public LadderState(Player player, StateMovMachine stateMachine) : base(player, stateMachine)
    {
    }

    public void SetLadder(Ladder ladder)
    {
        currentLadder = ladder;
    }

    public override void Enter()
    {
        base.Enter();
    
        player.Rb.gravityScale = 0f;
        player.Rb.linearVelocity = Vector2.zero;
        
        Animator.SetBool(ClimbingHash, true);
        Animator.SetBool(GroundedHash, false);
        
        enterTime = Time.time;
        
        if (player.DebugMessages)
            Debug.Log("Entered Ladder State");
        
        player.LastState = this;
    }

    public override void HandleInput()
    {
        base.HandleInput();
        player.MovementInput = player.PlayerInput.actions["Move"].ReadValue<Vector2>();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (Time.time - enterTime > exitDelay && player.PlayerInput.actions["Interact"].WasPressedThisFrame())
        {
            stateMachine.ChangeState(player.AirState);
            return;
        }
        
        if (player.MovementInput.x != 0)
        {
            stateMachine.ChangeState(player.AirState);
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
        
        if (player.MovementInput.y > 0)
        {
            verticalMovement = climbSpeed;
        }
        else if (player.MovementInput.y < 0)
        {
            verticalMovement = -climbSpeed;
        }
        player.Rb.linearVelocity = new Vector2(0f, verticalMovement);
    }

    public override void Exit()
    {
        base.Exit();

        player.Rb.gravityScale = player.DownGravityScale;
        Animator.SetBool(ClimbingHash, false);
        Animator.SetBool(GroundedHash, true);
        
        if (player.DebugMessages)
            Debug.Log("Exited Ladder State");
    }
}
