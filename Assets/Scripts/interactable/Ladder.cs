using UnityEngine;

public class Ladder : MonoBehaviour, IInteractable
{
    private Player player;

    private void Start()
    {
        player = FindFirstObjectByType<Player>();
        if (player == null)
        {
            Debug.LogError("Игрок не найден на сцене!");
        }
    }

    public void Interact()
    {
        if (player == null)
        {
            Debug.LogWarning("Игрок не найден для скрипта лестницы");
            return;
        }
        else
        {
            var ladderState = player.LadderState;
            if (ladderState != null)
            {
                ladderState.SetLadder(this);
                player.PlayerSM.ChangeState(ladderState);
            }
        }
    }
}
