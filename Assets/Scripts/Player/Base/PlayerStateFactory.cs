public class PlayerStateFactory
{
    public void InitializeAllStates(PlayerController controller, StateMachine stateMachine, PlayerStaticSettings settings)
    {
        controller.IdleState = new IdleState(controller, stateMachine, settings);
        controller.JumpingState = new JumpingState(controller, stateMachine, settings);
        controller.CrouchingState = new CrouchingState(controller, stateMachine, settings);
        controller.RollingState = new RollingState(controller, stateMachine, settings);
        controller.FlyingState = new FlyingState(controller, stateMachine, settings);
        controller.HittingState = new HittingState(controller, stateMachine, settings);
        controller.SpellCastState = new SpellCastState(controller, stateMachine, settings);
        controller.SwitchState = new SwitchState(controller, stateMachine, settings);
        controller.WallState = new WallState(controller, stateMachine, settings);
        controller.LadderState = new LadderState(controller, stateMachine, settings);
        controller.DyingState = new DyingState(controller, stateMachine, settings);
    }
}