using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class DialogueTriggerZone : MonoBehaviour
{
    [Tooltip("LinearDialogue or InkDialogue asset (any ScriptableObject implementing IDialogueSource).")]
    [SerializeField] private ScriptableObject dialogueSource;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Triggers once and then disables itself permanently.")]
    [SerializeField] private bool triggerOnce;
    [Tooltip("If not triggerOnce, allow firing again when the player re-enters the zone.")]
    [SerializeField] private bool retriggerOnReenter = true;
    [SerializeField] private UnityEvent onDialogueStarted;
    [SerializeField] private UnityEvent onDialogueFinished;

    private bool hasPlayed;
    private bool playerInside;
    private bool waitingForDialogueEnd;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || !other.CompareTag(playerTag))
        {
            return;
        }

        if (playerInside)
        {
            return;
        }

        playerInside = true;
        TryStart();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null || !other.CompareTag(playerTag))
        {
            return;
        }

        playerInside = false;
    }

    private void TryStart()
    {
        if (triggerOnce && hasPlayed)
        {
            return;
        }

        if (hasPlayed && !retriggerOnReenter)
        {
            return;
        }

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning($"DialogueTriggerZone '{name}': DialogueManager not found in scene.");
            return;
        }

        if (DialogueManager.Instance.IsDialogueActive)
        {
            return;
        }

        IDialogueSource source = dialogueSource as IDialogueSource;
        if (source == null)
        {
            Debug.LogWarning($"DialogueTriggerZone '{name}': dialogueSource is not assigned or does not implement IDialogueSource.");
            return;
        }

        if (playerInput == null)
        {
            playerInput = FindObjectOfType<PlayerInput>();
            if (playerInput == null)
            {
                Debug.LogWarning($"DialogueTriggerZone '{name}': PlayerInput not found.");
                return;
            }
        }

        if (!DialogueManager.Instance.StartDialogue(source, playerInput))
        {
            return;
        }

        hasPlayed = true;
        waitingForDialogueEnd = true;
        DialogueManager.Instance.DialogueEnded += HandleDialogueEnded;
        onDialogueStarted?.Invoke();
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

        if (triggerOnce)
        {
            enabled = false;
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnValidate()
    {
        if (dialogueSource != null && dialogueSource is not IDialogueSource)
        {
            Debug.LogWarning($"DialogueTriggerZone '{name}': assigned dialogueSource '{dialogueSource.name}' does not implement IDialogueSource.", this);
        }
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
