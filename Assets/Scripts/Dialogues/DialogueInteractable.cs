using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DialogueInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private TextAsset inkJsonAsset;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private bool oneShot;
    [SerializeField] private UnityEvent onDialogueStarted;
    [SerializeField] private UnityEvent onDialogueFinished;

    private bool hasPlayed;
    private bool waitingForDialogueEnd;

    public void Interact()
    {
        if (oneShot && hasPlayed)
        {
            return;
        }

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning($"DialogueInteractable '{name}': DialogueManager not found in scene.");
            return;
        }

        if (DialogueManager.Instance.IsDialogueActive)
        {
            return;
        }

        if (inkJsonAsset == null)
        {
            Debug.LogWarning($"DialogueInteractable '{name}': inkJsonAsset is not assigned.");
            return;
        }

        if (playerInput == null)
        {
            playerInput = FindObjectOfType<PlayerInput>();
            if (playerInput == null)
            {
                Debug.LogWarning($"DialogueInteractable '{name}': PlayerInput not found.");
                return;
            }
        }

        bool started = DialogueManager.Instance.StartDialogue(inkJsonAsset, playerInput);
        if (!started)
        {
            return;
        }

        hasPlayed = true;
        waitingForDialogueEnd = true;
        DialogueManager.Instance.DialogueEnded += HandleDialogueEnded;
        onDialogueStarted?.Invoke();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void HandleDialogueEnded()
    {
        if (!waitingForDialogueEnd)
        {
            return;
        }

        waitingForDialogueEnd = false;
        onDialogueFinished?.Invoke();
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (!DialogueManager.HasInstance)
        {
            return;
        }

        DialogueManager.Instance.DialogueEnded -= HandleDialogueEnded;
    }
}
