using UnityEngine;

public abstract class GroundedStates : State
{
    protected bool grounded = true;
    public GroundedStates(Player player, StateMachine stateMachine) : base(player, stateMachine)
    {
    }
    public override void Enter()
    {
        player.MovementInput = Vector2.zero;
        animator.SetBool("Grounded", true);
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (player.MovementInput.x != 0)
        {
            animator.SetBool("Moving", true);
        }
        else
        {
            animator.SetBool("Moving", false);
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
        // Целевое ускорение выходит из ввода, умноженного на скорость
        float targetVelocityX = player.MovementInput.x * player.Speed;
        float currentVelocityX = player.Rb.linearVelocity.x;
        
        // Выбираем знак (+/-) для ускорения
        float accelerationToUse = player.MovementInput.x != 0 ? player.AccelerationRate : player.FrictionRate;
        
        // Плавно интерполируем текущую скорость к целевой для эффекта инерции
        float newVelocityX = Mathf.Lerp(currentVelocityX, targetVelocityX, accelerationToUse * Time.fixedDeltaTime);
        
        // Применяем новое ускорение, но оставляем горизонтальное неизменным
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
        animator.SetBool("Moving", false);
        animator.SetBool("Grounded", false);
    }
}
