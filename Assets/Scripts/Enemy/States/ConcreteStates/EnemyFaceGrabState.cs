using UnityEngine;
using UnityEngine.Rendering;

/*
Враг держится за лицо игрока (роль Grab), пока тот проходит QTE (GrabbedState игрока).

Тело на это время вне физики: позиция каждый кадр ставится на лицо героя, спрайт рисуется поверх него.
Исход приходит от игрока через IPlayerGrabber.OnGrabEnded:
  Escaped           — отлетает и уходит в замешательство (Stagger);
  Failed/Interrupted — спрыгивает и продолжает бой.
Если режим кончился со стороны врага (смерть от Burn и т.п.), Exit сам отпускает игрока.
*/
public class EnemyFaceGrabState : EnemyStates, IPlayerGrabber
{
    private readonly FaceGrabAttackModule settings;
    private readonly int latchedHash;
    private readonly SpriteRenderer sprite;

    private Transform cachedPlayerTransform;
    private PlayerController player;
    private Collider2D playerCollider;
    private bool latched;
    private int savedSortingLayer;
    private int savedSortingOrder;

    public EnemyFaceGrabState(EnemyController controller, EnemyStateMachine stateMachine, FaceGrabAttackModule settings)
        : base(controller, stateMachine)
    {
        this.settings = settings;
        latchedHash = string.IsNullOrEmpty(settings.latchedAnimatorParameter)
            ? 0
            : Animator.StringToHash(settings.latchedAnimatorParameter);
        sprite = controller.GetComponent<SpriteRenderer>();
    }

    public bool CanLatch()
    {
        if (!ResolvePlayer()) return false;

        State current = player.PlayerSM?.CurrentPlayerState;
        return current != null && current != player.GrabbedState && current != player.DyingState;
    }

    // Момент удара: хватаем игрока и, если вышло, переходим в режим «на лице».
    public bool TryLatch()
    {
        if (!ResolvePlayer() || !player.TryGrab(this, settings.qte)) return false;
        return ChangeState(EnemyStateId.Grab);
    }

    public override void Enter()
    {
        latched = true;
        ClearLocomotionFlags();
        Movement.SetPhysicsSuspended(true);
        if (latchedHash != 0) SetFlag(latchedHash, true);

        // Рисуемся поверх героя. Спрайты героев сидят в SortingGroup на корне игрока — сравниваться надо с ней.
        SpriteRenderer playerSprite = player.PlayerCharManager.ActiveSR;
        SortingGroup playerGroup = player.GetComponent<SortingGroup>();
        if (sprite != null && (playerGroup != null || playerSprite != null))
        {
            savedSortingLayer = sprite.sortingLayerID;
            savedSortingOrder = sprite.sortingOrder;
            sprite.sortingLayerID = playerGroup != null ? playerGroup.sortingLayerID : playerSprite.sortingLayerID;
            sprite.sortingOrder = (playerGroup != null ? playerGroup.sortingOrder : playerSprite.sortingOrder) + 1;
        }
        SnapToFace();
    }

    public override void LogicUpdate()
    {
        if (!latched) return;

        // Игрока уничтожили посреди захвата (смена сцены и т.п.) — просто спрыгиваем.
        if (player == null)
        {
            latched = false;
            Detach(settings.jumpOffVelocity, 1f);
            ChangeState(LostTargetStateId);
            return;
        }

        SnapToFace();
    }

    void IPlayerGrabber.OnGrabEnded(GrabOutcome outcome)
    {
        if (!latched) return;
        latched = false;

        float facing = PlayerFacing();
        if (outcome == GrabOutcome.Escaped)
        {
            Detach(settings.throwOffVelocity, facing);
            if (controller.GetState(EnemyStateId.Stagger) is EnemyStunnedState stunned)
            {
                stunned.Begin(settings.stunDuration, settings.stunnedAnimatorParameter);
                return;
            }
        }
        else
        {
            Detach(settings.jumpOffVelocity, facing);
        }
        ChangeState(EnemyStateId.Engage);
    }

    public override void Exit()
    {
        // Ушли из режима не по сигналу игрока (враг погиб) — игрок должен освободиться сам.
        if (latched)
        {
            latched = false;
            if (player != null) player.ReleaseGrab(this);
        }

        Movement.SetPhysicsSuspended(false);
        if (latchedHash != 0) SetFlag(latchedHash, false);
        if (sprite != null)
        {
            sprite.sortingLayerID = savedSortingLayer;
            sprite.sortingOrder = savedSortingOrder;
        }
    }

    private void SnapToFace()
    {
        float facing = PlayerFacing();
        Vector2 top = playerCollider != null
            ? new Vector2(playerCollider.bounds.center.x, playerCollider.bounds.max.y)
            : (Vector2)player.transform.position;

        controller.transform.position = new Vector3(
            top.x + facing * settings.faceOffset.x,
            top.y + settings.faceOffset.y,
            controller.transform.position.z);
        // Мордой к лицу героя.
        Movement.Face(-facing);
    }

    private void Detach(Vector2 velocity, float directionX)
    {
        Movement.SetPhysicsSuspended(false);
        Movement.ApplyKnockback(new Vector2(velocity.x * directionX, velocity.y), 0.3f);
    }

    private float PlayerFacing()
    {
        SpriteRenderer playerSprite = player != null ? player.PlayerCharManager.ActiveSR : null;
        return playerSprite != null && playerSprite.flipX ? -1f : 1f;
    }

    // Компоненты игрока ищем один раз на объект, а не каждый кадр: CanLatch опрашивается при выборе атаки.
    private bool ResolvePlayer()
    {
        Transform target = Sensor.PlayerTransform;
        if (target == null) return false;

        if (target != cachedPlayerTransform)
        {
            cachedPlayerTransform = target;
            player = target.GetComponent<PlayerController>();
            playerCollider = target.GetComponent<Collider2D>();
        }
        return player != null;
    }
}
