using System.Collections;
using UnityEngine;

public class SwitchState : States
{
    public SwitchState(Player player, StateMachine stateMachine)
        : base(player, stateMachine) { }

    private UIScript uiScript;

    public override void Enter()
    {
        base.Enter();
        if (player.DebugMessages)
            Debug.Log("Entered Switch State");
        if (uiScript == null)
        {
            uiScript = GameObject.FindAnyObjectByType<UIScript>();
            if (uiScript == null && player.DebugMessages)
            {
                Debug.LogError("UIScript не найден в сцене!");
            }
        }
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
            if (player.DebugMessages)
                Debug.Log("Switched to Sobaka");
            uiScript.UpdateText("Sobaka");
        }
        else if (player.Satan != null)
        {
            player.Sobaka.SetActive(false);
            player.ActiveCharacter = player.Satan;
            player.Satan.SetActive(true);
            uiScript.UpdateText("Satan");
            if (player.DebugMessages)
                Debug.Log("Switched to Satan");
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
