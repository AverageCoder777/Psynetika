using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private bool pauseGameplay;
    [Tooltip("UI elements (HUD, mini-map, etc.) that should be hidden while a dialogue is active.")]
    [SerializeField] private GameObject[] gameplayUI;

    private IDialogueRunner currentRunner;
    private IReadOnlyList<string> currentChoices = Array.Empty<string>();
    private UnityEngine.InputSystem.PlayerInput activePlayerInput;
    private InputAction submitAction;
    private InputAction cancelAction;
    private string previousActionMap = "Player";
    private float cachedTimeScale = 1f;

    public bool IsDialogueActive => currentRunner != null;
    public event Action DialogueEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool StartDialogue(IDialogueSource source, UnityEngine.InputSystem.PlayerInput playerInput)
    {
        if (source == null)
        {
            return false;
        }

        return StartDialogueInternal(source.CreateRunner(), playerInput);
    }

    public bool StartDialogue(TextAsset inkJsonAsset, UnityEngine.InputSystem.PlayerInput playerInput)
    {
        if (inkJsonAsset == null)
        {
            return false;
        }

        return StartDialogueInternal(new InkDialogueRunner(inkJsonAsset), playerInput);
    }

    private bool StartDialogueInternal(IDialogueRunner runner, UnityEngine.InputSystem.PlayerInput playerInput)
    {
        if (runner == null || IsDialogueActive)
        {
            return false;
        }

        if (dialogueUI == null)
        {
            Debug.LogError("DialogueManager: DialogueUI reference is missing.");
            return false;
        }

        currentRunner = runner;
        activePlayerInput = playerInput;

        if (activePlayerInput != null)
        {
            previousActionMap = activePlayerInput.currentActionMap != null
                ? activePlayerInput.currentActionMap.name
                : "Player";
            activePlayerInput.SwitchCurrentActionMap("UI");
            CacheUiActions(activePlayerInput);
            SubscribeUiActions();
        }

        if (pauseGameplay)
        {
            cachedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        SetGameplayUIVisible(false);

        dialogueUI.SetManager(this);
        dialogueUI.Show();
        ContinueStory();
        return true;
    }

    private void SetGameplayUIVisible(bool visible)
    {
        if (gameplayUI == null)
        {
            return;
        }

        for (int i = 0; i < gameplayUI.Length; i++)
        {
            if (gameplayUI[i] != null)
            {
                gameplayUI[i].SetActive(visible);
            }
        }
    }

    public void ContinueStory()
    {
        if (!IsDialogueActive)
        {
            return;
        }

        if (!currentRunner.TryGetNext(out string line, out string speaker, out IReadOnlyList<string> choices))
        {
            EndDialogue();
            return;
        }

        currentChoices = choices ?? Array.Empty<string>();
        dialogueUI.Render(line, speaker, currentChoices);
    }

    public void ChooseChoice(int index)
    {
        if (!IsDialogueActive)
        {
            return;
        }

        if (index < 0 || index >= currentChoices.Count)
        {
            return;
        }

        currentRunner.Choose(index);
        ContinueStory();
    }

    public void CancelDialogue()
    {
        if (!IsDialogueActive)
        {
            return;
        }

        EndDialogue();
    }

    private void EndDialogue()
    {
        currentRunner = null;
        currentChoices = Array.Empty<string>();

        UnsubscribeUiActions();
        if (activePlayerInput != null)
        {
            activePlayerInput.SwitchCurrentActionMap(previousActionMap);
        }

        if (pauseGameplay)
        {
            Time.timeScale = cachedTimeScale;
        }

        if (dialogueUI != null)
        {
            dialogueUI.Hide();
        }

        SetGameplayUIVisible(true);

        activePlayerInput = null;
        submitAction = null;
        cancelAction = null;
        DialogueEnded?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void CacheUiActions(UnityEngine.InputSystem.PlayerInput playerInput)
    {
        submitAction = playerInput.actions["Submit"];
        cancelAction = playerInput.actions["Cancel"];
    }

    private void SubscribeUiActions()
    {
        if (submitAction != null)
        {
            submitAction.performed += OnSubmitPerformed;
        }

        if (cancelAction != null)
        {
            cancelAction.performed += OnCancelPerformed;
        }
    }

    private void UnsubscribeUiActions()
    {
        if (submitAction != null)
        {
            submitAction.performed -= OnSubmitPerformed;
        }

        if (cancelAction != null)
        {
            cancelAction.performed -= OnCancelPerformed;
        }
    }

    private void OnSubmitPerformed(InputAction.CallbackContext context)
    {
        if (!IsDialogueActive || currentChoices.Count > 0)
        {
            return;
        }

        ContinueStory();
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        CancelDialogue();
    }
}
