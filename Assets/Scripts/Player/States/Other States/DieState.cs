using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class DyingState : State
{
    public DyingState(Player player, StateMovMachine stateMachine) : base(player, stateMachine)
    {
    }
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int IsSwitchingHash = Animator.StringToHash("isSwitching");
    private UIScript uiScript;
    private float deathTimer = 0f;
    private const float resurrectionDelay = 5.5f;
    public override void Enter()
    {
        if (uiScript == null)
        {
            uiScript = Object.FindAnyObjectByType<UIScript>();
            if (uiScript == null && player.DebugMessages)
            {
                Debug.LogError("UIScript не найден в сцене!");
            }
        }
        Animator.SetTrigger(DieHash);
        player.DisableEnemyVisibility();
        deathTimer = 0f;
    }
    public override void HandleInput()
    {
    }
    public override void LogicUpdate()
    {
        deathTimer += Time.deltaTime;
        if (deathTimer >= resurrectionDelay)
        {
            if (player.GetCurrentCharState() == player.DogState)
            {
                player.DogActive = false;
            }
            else if (player.GetCurrentCharState() == player.SatanState)
            {
                player.SatanActive = false;
            }
            if (!player.DogActive && !player.SatanActive)
            {
                uiScript.GameOver();
                return;
            }
            else
            {
                player.StartCoroutine(SwitchCharacter());
                stateMachine.ChangeState(player.IdleState);
            }
        }
    }
    public override void PhysicsUpdate()
    {
        player.Rb.linearVelocity = Vector2.zero;
    }
    public override void Exit()
    {
        deathTimer = 0f;
        player.EnableEnemyVisibility();
    }
    private IEnumerator SwitchCharacter()
    {
        player.ActiveAnimator.SetTrigger(IsSwitchingHash);
        yield return new WaitForSeconds(player.SwitchDelay);

        if (player.GetCurrentCharState() == player.SatanState)
        {
            player.Satan.SetActive(false);
            player.ActiveCharacter = player.Dog;
            player.Dog.SetActive(true);
            if (player.DebugMessages)
                Debug.Log("Switched to Sobaka");
            player.CharacterSM.ChangeState(player.DogState);
        }
        else
        {
            player.Dog.SetActive(false);
            player.ActiveCharacter = player.Satan;
            player.Satan.SetActive(true);
            if (player.DebugMessages)
                Debug.Log("Switched to Satan");
            player.CharacterSM.ChangeState(player.SatanState);
        }
        player.ActiveAnimator.SetBool(IsSwitchingHash, false);
        CacheActiveVisuals();
        player.UpdateHealthUI();
        stateMachine.ChangeState(player.IdleState);
    }
    void CacheActiveVisuals()
    {
        player.ActiveAnimator = player.ActiveCharacter.GetComponent<Animator>();
        player.ActiveSR = player.ActiveCharacter.GetComponent<SpriteRenderer>();
    }
}
