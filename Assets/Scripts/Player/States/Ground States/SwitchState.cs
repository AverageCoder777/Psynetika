using System.Collections;
using UnityEngine;

public class SwitchState : GroundedStates
{
    public SwitchState(Player player, StateMovMachine stateMachine)
        : base(player, stateMachine) { }

    private UIScript uiScript;

    public override void Enter()
    {
        base.Enter();
        if (player.DebugMessages)
            Debug.Log("Entered Switch State");
        if (uiScript == null)
        {
            uiScript = Object.FindAnyObjectByType<UIScript>();
            if (uiScript == null && player.DebugMessages)
            {
                Debug.LogError("UIScript не найден в сцене!");
            }
        }
        player.StartCoroutine(SwitchCharacter());
        player.LastState = this;
    }

    private IEnumerator SwitchCharacter()
    {
        player.ActiveAnimator.SetTrigger("isSwitching");
        yield return new WaitForSeconds(player.SwitchDelay);

        if (player.GetCurrentCharState() == player.SatanState)
        {
            player.Satan.SetActive(false);
            player.ActiveCharacter = player.Sobaka;
            player.Sobaka.SetActive(true);
            if (player.DebugMessages)
                Debug.Log("Switched to Sobaka");
            player.CharacterSM.ChangeState(player.SobakaState);
            uiScript.UpdateText("Sobaka");
        }
        else
        {
            player.Sobaka.SetActive(false);
            player.ActiveCharacter = player.Satan;
            player.Satan.SetActive(true);
            uiScript.UpdateText("Satan");
            if (player.DebugMessages)
                Debug.Log("Switched to Satan");
            player.CharacterSM.ChangeState(player.SatanState);
        }
        player.ActiveAnimator.SetBool("isSwitching", false);
        CacheActiveVisuals();

        if (player.DebugMessages)
            Debug.Log("Switched character");
        stateMachine.ChangeState(player.IdleState);
    }

    void CacheActiveVisuals()
    {
        player.ActiveAnimator = player.ActiveCharacter.GetComponent<Animator>();
        player.ActiveSR = player.ActiveCharacter.GetComponent<SpriteRenderer>();
    }
}
