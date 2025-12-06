using UnityEngine;
using System.Collections;

public class PlayerRollingState : PlayerState
{
    private Coroutine rollCoroutine;
    private bool rollEnd;

    public PlayerRollingState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        float dir = player.GetActiveSpriteRenderer().flipX ? -1f : 1f;
        rollCoroutine = player.StartCoroutine(RollCoroutine(dir));
        animator.SetTrigger("Rolling");
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (rollEnd && player.Rb.linearVelocity.y == 0)
        {
            player.StopCoroutine(rollCoroutine);
            stateMachine.ChangeState(player.IdleState);
        }
        else if (rollEnd && player.Rb.linearVelocity.y < 0)
        {
            player.StopCoroutine(rollCoroutine);
            stateMachine.ChangeState(player.FallingState);
        }
    }
    public override void Exit()
    {
        animator.ResetTrigger("Rolling");
    }

    private IEnumerator RollCoroutine(float dir)
    {
        float rollSpeed = (player.RollDuration > 0f) ? player.RollDistance / player.RollDuration : 0f;
        float elapsed = 0f;

        while (elapsed < player.RollDuration)
        {
            player.Rb.linearVelocity = new Vector2(dir * rollSpeed, player.Rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }
        player.Rb.linearVelocity = new Vector2(0f, player.Rb.linearVelocity.y);
        rollEnd = true;
    }
}