using UnityEngine;
using System.Collections;

public class DyingState : State
{
    public DyingState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
    {
    }
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int IsSwitchingHash = Animator.StringToHash("isSwitching");
    private float deathTimer = 0f;
    private const float resurrectionDelay = 5.5f;
    private PlayerCharacterType _type;
    public override void Enter()
    {
        Animator.SetTrigger(DieHash);
        player.DisableEnemyVisibility();
        deathTimer = 0f;
        _type = player.GetCurrentCharacterType();
    }
    public override void LogicUpdate()
    {
        deathTimer += Time.deltaTime;
        if (deathTimer >= resurrectionDelay)
        {
            if (_type == PlayerCharacterType.Dog)
            {
                CharManager.DogActive = false;
            }
            else if (_type == PlayerCharacterType.Satan)
            {
                CharManager.SatanActive = false;
            }
            player.StartCoroutine(SwitchCharacter());
            stateMachine.ChangeState(player.IdleState);
        }
    }
    public override void PhysicsUpdate()
    {
        Movement.Rb.linearVelocity = Vector2.zero;
    }
    public override void Exit()
    {
        deathTimer = 0f;
        player.EnableEnemyVisibility();
    }
    
    private IEnumerator SwitchCharacter()
    {
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
