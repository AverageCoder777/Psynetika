using System.Collections;
using UnityEngine;

public class SwitchState : GroundedStates
{
    private static readonly int IsSwitchingHash = Animator.StringToHash("isSwitching");

    public SwitchState(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
        : base(player, stateMachine, settings) { }

    public override void Enter()
    {
        base.Enter();
        player.StartCoroutine(SwitchCharacter());
        player.LastState = this;
    }

    private IEnumerator SwitchCharacter()
    {
        if (charManager.DogActive == false || charManager.SatanActive == false)
        {
            stateMachine.ChangeState(player.IdleState);
            yield break;
        }
        charManager.ActiveAnimator.SetTrigger(IsSwitchingHash);
        yield return new WaitForSeconds(settings.switching.switchDelay);

        if (charManager.GetCurrentCharacterType() == PlayerCharacterType.Satan)

        {
            charManager.Satan.SetActive(false);
            charManager.ActiveCharacter = charManager.Dog;
            charManager.Dog.SetActive(true);
            if (player.debugMessages) Debug.Log("Switched to Sobaka");
            charManager.SetCurrentCharacterType(PlayerCharacterType.Dog);
        }
        else
        {

            charManager.Dog.SetActive(false);
            charManager.ActiveCharacter = charManager.Satan;
            charManager.Satan.SetActive(true);
            if (player.debugMessages) Debug.Log("Switched to Satan");
            charManager.SetCurrentCharacterType(PlayerCharacterType.Satan);
        }


        charManager.ActiveAnimator.SetBool(IsSwitchingHash, false);
        CacheActiveVisuals();
        stateMachine.ChangeState(player.IdleState);
    }

    void CacheActiveVisuals()
    {
        charManager.ActiveAnimator = charManager.ActiveCharacter.GetComponent<Animator>();
        charManager.ActiveSR = charManager.ActiveCharacter.GetComponent<SpriteRenderer>();
    }
}
