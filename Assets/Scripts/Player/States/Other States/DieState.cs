using UnityEngine;
using System.Collections;

public class DyingState : State
{
    public DyingState(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
         : base(player, stateMachine, settings){ }
    
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int IsSwitchingHash = Animator.StringToHash("isSwitching");
    private float deathTimer = 0f;
    private PlayerCharacterType _type;
    public override void Enter()
    {
        animator.SetTrigger(DieHash);
        player.DisableEnemyVisibility();
        deathTimer = 0f;
        _type = charManager.GetCurrentCharacterType();
    }
    public override void LogicUpdate()
    {
        deathTimer += Time.deltaTime;
        if (deathTimer >= settings.health.resurrectionDelay)
        {
            if (_type == PlayerCharacterType.Dog)
            {
                charManager.DogActive = false;
            }
            else if (_type == PlayerCharacterType.Satan)
            {
                charManager.SatanActive = false;
            }
            player.StartCoroutine(SwitchCharacter());
            stateMachine.ChangeState(player.IdleState);
        }
    }
    public override void PhysicsUpdate()
    {
        movement.Rb.linearVelocity = Vector2.zero;
    }
    public override void Exit()
    {
        deathTimer = 0f;
        player.EnableEnemyVisibility();
    }
    
    private IEnumerator SwitchCharacter()
    {
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
