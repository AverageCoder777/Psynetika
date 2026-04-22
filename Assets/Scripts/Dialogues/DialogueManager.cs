using System;
using System.Collections.Generic;
using Ink.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private bool pauseGameplay;

    private Story currentStory;
    private PlayerInput activePlayerInput;
    private InputAction submitAction;
    private InputAction cancelAction;
    private string previousActionMap = "Player";
    private float cachedTimeScale = 1f;

    public bool IsDialogueActive => currentStory != null;
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

    public bool StartDialogue(TextAsset inkJsonAsset, PlayerInput playerInput)
    {
        if (inkJsonAsset == null || IsDialogueActive)
        {
            return false;
        }

        currentStory = new Story(inkJsonAsset.text);
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

        if (dialogueUI == null)
        {
            Debug.LogError("DialogueManager: DialogueUI reference is missing.");
            EndDialogue();
            return false;
        }

        dialogueUI.SetManager(this);
        dialogueUI.Show();
        ContinueStory();
        return true;
    }

    public void ContinueStory()
    {
        if (!IsDialogueActive)
        {
            return;
        }

        if (!TryReadNextLine(out string line, out string speaker))
        {
            EndDialogue();
            return;
        }

        List<string> choices = ExtractChoiceTexts();
        dialogueUI.Render(line, speaker, choices);
    }

    public void ChooseChoice(int index)
    {
        if (!IsDialogueActive)
        {
            return;
        }

        if (index < 0 || index >= currentStory.currentChoices.Count)
        {
            return;
        }

        currentStory.ChooseChoiceIndex(index);
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

    private bool TryReadNextLine(out string line, out string speaker)
    {
        line = string.Empty;
        speaker = string.Empty;

        while (currentStory.canContinue)
        {
            string nextLine = currentStory.Continue();
            nextLine = string.IsNullOrWhiteSpace(nextLine) ? string.Empty : nextLine.Trim();

            ParseTags(currentStory.currentTags, out string parsedSpeaker);
            if (!string.IsNullOrEmpty(parsedSpeaker))
            {
                speaker = parsedSpeaker;
            }

            if (!string.IsNullOrEmpty(nextLine))
            {
                line = nextLine;
                return true;
            }
        }

        if (currentStory.currentChoices.Count > 0)
        {
            return true;
        }

        return false;
    }

    private static void ParseTags(IReadOnlyList<string> tags, out string speaker)
    {
        speaker = string.Empty;
        if (tags == null || tags.Count == 0)
        {
            return;
        }

        for (int i = 0; i < tags.Count; i++)
        {
            string tag = tags[i];
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            int separator = tag.IndexOf(':');
            if (separator <= 0 || separator >= tag.Length - 1)
            {
                continue;
            }

            string key = tag.Substring(0, separator).Trim().ToLowerInvariant();
            string value = tag.Substring(separator + 1).Trim();

            if (key == "speaker")
            {
                speaker = value;
            }
        }
    }

    private List<string> ExtractChoiceTexts()
    {
        int count = currentStory.currentChoices.Count;
        List<string> result = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            result.Add(currentStory.currentChoices[i].text.Trim());
        }

        return result;
    }

    private void EndDialogue()
    {
        currentStory = null;

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

    private void CacheUiActions(PlayerInput playerInput)
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
        if (!IsDialogueActive || currentStory.currentChoices.Count > 0)
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
