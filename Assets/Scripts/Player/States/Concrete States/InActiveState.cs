using UnityEngine;

public class InActiveState : States
{
    public InActiveState(Player player, StateMachine stateMachine)
        : base(player, stateMachine){}

    public override void Enter()
    {
        player.healthPlayerManager.ChangeActive(false);

    }
}
