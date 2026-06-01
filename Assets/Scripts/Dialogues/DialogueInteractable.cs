using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DialogueInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("LinearDialogue or InkDialogue asset (any ScriptableObject implementing IDialogueSource).")]
    [SerializeField] private ScriptableObject dialogueSource;
    [SerializeField] private UnityEngine.InputSystem.PlayerInput playerInput;
    [SerializeField] private bool oneShot;
    [SerializeField] private UnityEvent onDialogueStarted;
    [SerializeField] private UnityEvent onDialogueFinished;

    private bool hasPlayed;
    private bool waitingForDialogueEnd;

    public bool TryPlay()
    {
        Interact();
        return waitingForDialogueEnd;
    }

    public void SetDialogueSource(ScriptableObject source)
    {
        if (source != null && source is not IDialogueSource)
        {
            Debug.LogWarning($"DialogueInteractable '{name}': '{source.name}' не реализует IDialogueSource.", this);
            return;
        }
        dialogueSource = source;
    }

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

        IDialogueSource source = dialogueSource as IDialogueSource;
        if (source == null)
        {
            Debug.LogWarning($"DialogueInteractable '{name}': dialogueSource is not assigned or does not implement IDialogueSource.");
            return;
        }

        if (playerInput == null)
        {
            playerInput = FindObjectOfType<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput == null)
            {
                Debug.LogWarning($"DialogueInteractable '{name}': PlayerInput not found.");
                return;
            }
        }

        bool started = DialogueManager.Instance.StartDialogue(source, playerInput);
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

    private void OnValidate()
    {
        if (dialogueSource != null && dialogueSource is not IDialogueSource)
        {
            Debug.LogWarning($"DialogueInteractable '{name}': assigned dialogueSource '{dialogueSource.name}' does not implement IDialogueSource.", this);
        }
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
