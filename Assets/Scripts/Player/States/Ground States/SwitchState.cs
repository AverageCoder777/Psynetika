using System.Collections;
using UnityEngine;

public class SwitchState : GroundedStates
{
    private static readonly int IsSwitchingHash = Animator.StringToHash("isSwitching");

    public SwitchState(PlayerController player, StateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        base.Enter();
        player.StartCoroutine(SwitchCharacter());
        player.LastState = this;
    }

    private IEnumerator SwitchCharacter()
    {
        if (CharManager.DogActive == false || CharManager.SatanActive == false)
        {
            stateMachine.ChangeState(player.IdleState);
            yield break;
        }
        CharManager.ActiveAnimator.SetTrigger(IsSwitchingHash);
        yield return new WaitForSeconds(player.SwitchDelay);

        if (player.GetCurrentCharacterType() == PlayerCharacterType.Satan)

        {
            CharManager.Satan.SetActive(false);
            CharManager.ActiveCharacter = CharManager.Dog;
            CharManager.Dog.SetActive(true);
            if (player.debugMessages) Debug.Log("Switched to Sobaka");
            player.SetCurrentCharacterType(PlayerCharacterType.Dog);
        }
        else
        {

            CharManager.Dog.SetActive(false);
            CharManager.ActiveCharacter = CharManager.Satan;
            CharManager.Satan.SetActive(true);
            if (player.debugMessages) Debug.Log("Switched to Satan");
            player.SetCurrentCharacterType(PlayerCharacterType.Satan);
        }


        CharManager.ActiveAnimator.SetBool(IsSwitchingHash, false);
        CacheActiveVisuals();
        stateMachine.ChangeState(player.IdleState);
    }

    void CacheActiveVisuals()
    {
        CharManager.ActiveAnimator = CharManager.ActiveCharacter.GetComponent<Animator>();
        CharManager.ActiveSR = CharManager.ActiveCharacter.GetComponent<SpriteRenderer>();
    }
}
