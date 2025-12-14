using UnityEngine;
using System.Collections;

public class SwitchState : States
{
    public SwitchState(Player player, StateMachine stateMachine) : base(player, stateMachine)
    {
    }
    public override void Enter()
    {
        base.Enter();
        if (player.DebugMessages) Debug.Log("Entered Switch State");
        player.StartCoroutine(SwitchCharacter());
    }
    private IEnumerator SwitchCharacter()
    {
        player.ActiveAnimator.SetTrigger("isSwitching");
        yield return new WaitForSeconds(player.SwitchDelay);
        
        if (player.ActiveCharacter == player.Satan && player.Sobaka != null)
        {
            player.Satan.SetActive(false);
            player.ActiveCharacter = player.Sobaka;
            player.Sobaka.SetActive(true);
            if (player.DebugMessages) Debug.Log("Switched to Character B");
        }
        else if (player.Satan != null)
        {
            player.Sobaka.SetActive(false);
            player.ActiveCharacter = player.Satan;
            player.Satan.SetActive(true);
            if (player.DebugMessages) Debug.Log("Switched to Character A");
        }
        player.ActiveAnimator.SetBool("isSwitching", false);
        CacheActiveVisuals();

        if (player.DebugMessages) Debug.Log("Switched character");
        stateMachine.ChangeState(player.IdleState);
    }
    void CacheActiveVisuals()
    {
        player.ActiveAnimator = player.ActiveCharacter.GetComponent<Animator>();
        player.ActiveSR = player.ActiveCharacter.GetComponent<SpriteRenderer>();
    }
}
