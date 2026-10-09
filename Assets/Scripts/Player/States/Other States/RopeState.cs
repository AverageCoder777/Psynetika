using UnityEngine;

/*
Вис на верёвке (только собака).

Герой — грузик маятника: угол, угловая скорость и длина подвеса считаются здесь, а Rigidbody2D
остаётся динамическим и каждый шаг получает скорость «доехать до расчётной точки». Поэтому стены
и пол по-прежнему его останавливают: если тело не доехало, маятник берёт фактическое положение и гасит скорость.

Управление:
  A/D        — раскачка (толкает только по ходу движения, поэтому зажатое направление само раскачивает);
  W/S        — подтянуться/спуститься (момент импульса сохраняется: подтянулся внизу — раскачка быстрее);
  Space      — прыжок с верёвки, скорость раскачки переходит в прыжок;
  F, или S ещё раз на самом конце верёвки — отпустить.
*/
public class RopeState : State
{
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int SwingingHash = Animator.StringToHash("Swinging");

    private Rope rope;
    private Rope pendingRope;
    private Rope lastReleasedRope;
    private float lastReleaseTime = float.NegativeInfinity;

    private float angle;            // от вертикали вниз, «+» — правее крепления, радианы
    private float angularVelocity;
    private float distance;         // от крепления до руки
    private Vector2 expectedOffset; // где рука должна оказаться после шага физики (относительно крепления)

    private Vector2 moveInput;
    private bool jumpInput;
    private bool releaseInput;
    private bool downPressed;

    public RopeState(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
        : base(player, stateMachine, settings) { }

    private Vector2 HandPosition => movement.Rb.position + settings.rope.handOffset;

    /// <summary>
    /// Ищет верёвку у руки героя и запоминает её для Enter. Вызывают состояния, из которых можно схватиться.
    /// </summary>
    public bool TryFindRope()
    {
        if (charManager.GetCurrentCharacterType() != PlayerCharacterType.Dog)
        {
            return false;
        }

        // Зажатое «вниз» — игрок хочет пролететь мимо верёвки.
        if (movement.PlayerInput.actions["Move"].ReadValue<Vector2>().y < -0.5f)
        {
            return false;
        }

        Rope ignored = Time.time - lastReleaseTime < settings.rope.regrabCooldown ? lastReleasedRope : null;
        return Rope.TryFindGrabbable(HandPosition, ignored, out pendingRope);
    }

    public override void Enter()
    {
        base.Enter();
        rope = pendingRope;
        pendingRope = null;

        Vector2 offset = HandPosition - rope.Anchor;
        angle = Mathf.Atan2(offset.x, -offset.y);
        distance = Mathf.Clamp(offset.magnitude, rope.MinHoldDistance, rope.Length);
        expectedOffset = offset;

        // Скорость влёта переходит в раскачку: берём её касательную к дуге составляющую.
        float tangentialSpeed = Vector2.Dot(movement.Rb.linearVelocity, Tangent(angle));
        angularVelocity = tangentialSpeed * settings.rope.catchMomentumKeep / distance;

        movement.Rb.gravityScale = 0f;
        rope.Attach(player.transform, settings.rope.handOffset, distance);
        player.ResetAirJumps();
        player.LastState = this;

        charManager.ActiveAnimator.SetBool(GroundedHash, false);
        SetSwinging(true);
        jumpInput = releaseInput = downPressed = false;
    }

    public override void HandleInput()
    {
        base.HandleInput();
        moveInput = movement.PlayerInput.actions["Move"].ReadValue<Vector2>();
        jumpInput = movement.PlayerInput.actions["Jump"].WasPressedThisFrame();
        releaseInput = movement.PlayerInput.actions["Interact"].WasPressedThisFrame();
        downPressed = movement.PlayerInput.actions["Crouch"].WasPressedThisFrame();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (rope == null || !rope.isActiveAndEnabled)
        {
            stateMachine.ChangeState(player.FlyingState);
            return;
        }

        if (jumpInput)
        {
            JumpOff();
            stateMachine.ChangeState(player.FlyingState);
            return;
        }

        // Скорость Rigidbody уже равна скорости раскачки — просто отпускаем.
        bool atRopeEnd = distance >= rope.Length - 0.01f;
        if (releaseInput || (downPressed && atRopeEnd))
        {
            stateMachine.ChangeState(player.FlyingState);
            return;
        }

        if (moveInput.x > 0.01f)
            charManager.ActiveSR.flipX = false;
        else if (moveInput.x < -0.01f)
            charManager.ActiveSR.flipX = true;
        else if (Mathf.Abs(angularVelocity) > 0.1f)
            charManager.ActiveSR.flipX = angularVelocity < 0f;
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        if (rope == null)
        {
            return;
        }

        RopeSettings s = settings.rope;
        float dt = Time.fixedDeltaTime;

        // 1. Упёрлись в стену/пол — продолжаем из фактического положения, скорость гасим.
        Vector2 offset = HandPosition - rope.Anchor;
        if ((offset - expectedOffset).sqrMagnitude > s.blockedTolerance * s.blockedTolerance)
        {
            angle = Mathf.Atan2(offset.x, -offset.y);
            distance = Mathf.Clamp(offset.magnitude, rope.MinHoldDistance, rope.Length);
            angularVelocity = 0f;
        }

        // 2. Лазание по верёвке с сохранением момента импульса (ω·r² = const).
        float climb = Mathf.Abs(moveInput.y) > 0.5f ? Mathf.Sign(moveInput.y) : 0f;
        if (climb != 0f)
        {
            float newDistance = Mathf.Clamp(distance - climb * s.climbSpeed * dt, rope.MinHoldDistance, rope.Length);
            angularVelocity *= (distance * distance) / (newDistance * newDistance);
            distance = newDistance;
        }

        // 3. Маятник: гравитация + раскачка. Толкаем только по ходу движения (или из покоя),
        //    иначе зажатое направление просто держало бы героя в наклоне. Как только верхняя точка
        //    размаха дошла до maxSwingAngle, толкать перестаём — упор в лимит остаётся страховкой.
        float gravity = -Physics2D.gravity.y * s.swingGravityScale;
        float maxAngle = s.maxSwingAngle * Mathf.Deg2Rad;
        float tangentialAcceleration = -gravity * Mathf.Sin(angle);
        float input = Mathf.Abs(moveInput.x) > 0.01f ? Mathf.Sign(moveInput.x) : 0f;
        bool pushesAlongMotion = Mathf.Abs(angularVelocity) < 0.05f || input == Mathf.Sign(angularVelocity);
        if (input != 0f && pushesAlongMotion && PeakAngle(gravity) < maxAngle)
        {
            tangentialAcceleration += input * s.swingAcceleration * Mathf.Cos(angle);
        }

        angularVelocity += tangentialAcceleration / distance * dt;
        angularVelocity /= 1f + s.swingDamping * dt;
        angle += angularVelocity * dt;

        if (Mathf.Abs(angle) > maxAngle)
        {
            angle = Mathf.Sign(angle) * maxAngle;
            angularVelocity = 0f;
        }

        // 4. Ведём тело в расчётную точку скоростью — коллизии при этом работают как обычно.
        expectedOffset = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)) * distance;
        Vector2 targetBody = rope.Anchor + expectedOffset - s.handOffset;
        movement.Rb.linearVelocity = (targetBody - movement.Rb.position) / dt;
        rope.SetHoldDistance(distance);
    }

    public override void Exit()
    {
        base.Exit();
        if (rope != null)
        {
            rope.Detach();
            lastReleasedRope = rope;
            lastReleaseTime = Time.time;
            rope = null;
        }

        movement.Rb.gravityScale = movement.Rb.linearVelocity.y >= 0f
            ? settings.jump.upGravityScale
            : settings.jump.downGravityScale;
        SetSwinging(false);
    }

    private void JumpOff()
    {
        RopeSettings s = settings.rope;
        Vector2 velocity = Tangent(angle) * (angularVelocity * distance * s.releaseVelocityMultiplier);

        // Вниз прыжок не тянет: вертикаль раскачки учитываем, только если она помогает.
        velocity.y = Mathf.Max(velocity.y, 0f) + settings.jump.thrust / movement.Rb.mass * s.jumpOffThrustMultiplier;

        // Зажатое направление гарантирует хотя бы минимальный отскок в его сторону — даже из виса без раскачки.
        float input = Mathf.Abs(moveInput.x) > 0.01f ? Mathf.Sign(moveInput.x) : 0f;
        if (input != 0f && input * velocity.x < s.minJumpOffSpeedX)
        {
            velocity.x = input * s.minJumpOffSpeedX;
        }

        movement.Rb.linearVelocity = velocity;
        player.TryConsumeAirJump();
    }

    private static Vector2 Tangent(float angleRad) => new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

    // Угол верхней точки размаха при текущей энергии (без учёта затухания).
    private float PeakAngle(float gravity)
    {
        float speed = angularVelocity * distance;
        float cosPeak = Mathf.Cos(angle) - speed * speed / (2f * gravity * distance);
        return Mathf.Acos(Mathf.Clamp(cosPeak, -1f, 1f));
    }

    // У аниматора собаки параметра Swinging может ещё не быть — не спамим предупреждениями.
    private void SetSwinging(bool value)
    {
        Animator animator = charManager.ActiveAnimator;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == SwingingHash)
            {
                animator.SetBool(SwingingHash, value);
                return;
            }
        }
    }
}
