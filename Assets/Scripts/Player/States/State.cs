using UnityEngine;
public abstract class State
{
    protected PlayerController player;
    protected PlayerMoving movement;
    protected PlayerAttack attack;
    protected PlayerCharacterManager charManager;
    protected Animator animator;
    protected StateMachine stateMachine;
    protected PlayerStaticSettings settings;
    public State(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
    {
        this.player = player;
        movement = player.Movement;
        attack = player.Attack;
        charManager = player.PlayerCharManager;
        this.stateMachine = stateMachine;
        this.settings = settings;
    }
    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void LogicUpdate()
    {
    }
    public virtual void HandleInput()
    {
    }
    public virtual void PhysicsUpdate()
    {
    }
    protected string DetectFloor()
    {
        Vector2 floorDetectionDirection = Vector2.down;
        Vector2 platformDetectionDirection = Vector2.down;
        Vector2 raycastOrigin = (Vector2)player.transform.position - Vector2.up * 0.5f;

        RaycastHit2D hitFloor = Physics2D.Raycast(
            raycastOrigin,
            floorDetectionDirection,
            settings.detection.floorDetectionDistance,
            LayerMask.GetMask("Floor")
        );
        RaycastHit2D hitPlatform = Physics2D.Raycast(
            raycastOrigin,
            platformDetectionDirection,
            settings.detection.floorDetectionDistance,
            LayerMask.GetMask("Platform")
        );
        #if UNITY_EDITOR
        if (player.debugMessages)
        {
            Debug.DrawRay(raycastOrigin, floorDetectionDirection * settings.detection.floorDetectionDistance,
                hitFloor.collider != null ? Color.blue : Color.yellow);
            Debug.DrawRay(raycastOrigin, platformDetectionDirection * settings.detection.floorDetectionDistance,
                hitPlatform.collider != null ? Color.blue : Color.yellow);
        }
        #endif
        if (hitFloor.collider != null)
        {
            return "Floor";
        }
        if (hitPlatform.collider != null)
        {
            return "Platform";
        }
        return "None";
    }
    protected Collider2D DetectWall()
    {
        return DetectWallHit().collider;
    }

    protected RaycastHit2D DetectWallHit()
    {
        Vector2 wallDetectionDirection = charManager.ActiveSR.flipX ? Vector2.left : Vector2.right;
        Vector2 raycastOrigin = (Vector2)player.transform.position + wallDetectionDirection / 4f;

        RaycastHit2D hit = Physics2D.Raycast(
            raycastOrigin,
            wallDetectionDirection,
            settings.detection.wallDetectionDistance,
            LayerMask.GetMask("Walls")
        );
        #if UNITY_EDITOR
        if (player.debugMessages)
        {
            Debug.DrawRay(raycastOrigin, wallDetectionDirection * settings.detection.wallDetectionDistance,
                hit.collider != null ? Color.green : Color.red);
        }
        #endif
        return hit;
    }
}
