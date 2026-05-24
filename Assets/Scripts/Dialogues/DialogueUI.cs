using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;


public class DialogueUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private CanvasGroup root;
    [SerializeField] private TMP_Text lineText;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private Image speakerAvatar;
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private Button choiceButtonPrefab;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button closeButton;

    [SerializeField] private float charsPerSecond = 30f;
    [SerializeField] private CharacterDatabase characterDatabase;


    private readonly List<Button> choiceButtons = new List<Button>();
    private DialogueManager manager;

    private Tween _typingTween;

    private void Awake()
    {
        DOTween.Init();
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinuePressed);
            continueButton.onClick.AddListener(OnContinuePressed);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnClosePressed);
            closeButton.onClick.AddListener(OnClosePressed);
        }

        Hide();
    }

    public void SetManager(DialogueManager dialogueManager)
    {
        manager = dialogueManager;
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        ClearChoices();
        if (lineText != null)
        {
            lineText.text = string.Empty;
        }

        if (speakerText != null)
        {
            speakerText.text = string.Empty;
            speakerText.gameObject.SetActive(false);
        }

        SetVisible(false);
    }

    public void ShowText(string text)
    {
        _typingTween?.Kill();
        lineText.text = "";
        float duration = text.Length / charsPerSecond;

        _typingTween = DOVirtual.Float(0, text.Length, duration, x =>
        {
            int index = Mathf.Clamp((int)x, 0, text.Length);
            lineText.text = text.Substring(0, index);
        }).SetEase(Ease.Linear).SetUpdate(true);
    }

    public void Render(string line, string speaker, IReadOnlyList<string> choices)
    {
        if (lineText != null)
        {
            ShowText(line);
        }

        bool hasSpeaker = !string.IsNullOrWhiteSpace(speaker);
        if (speakerText != null)
        {
            speakerText.gameObject.SetActive(hasSpeaker);
            speakerText.text = hasSpeaker ? speaker : string.Empty;
        }

        if (speakerAvatar != null)
        {
            Sprite avatar = hasSpeaker && characterDatabase != null ? characterDatabase.GetAvatar(speaker) : null;
            speakerAvatar.sprite = avatar;
            speakerAvatar.gameObject.SetActive(avatar != null);
        }

        BuildChoices(choices);
    }

    private void BuildChoices(IReadOnlyList<string> choices)
    {
        ClearChoices();

        bool hasChoices = choices != null && choices.Count > 0;
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(!hasChoices);
        }

        if (!hasChoices)
        {
            SelectObject(continueButton != null ? continueButton.gameObject : null);
            return;
        }

        if (choicesContainer == null || choiceButtonPrefab == null)
        {
            Debug.LogError("DialogueUI: choicesContainer or choiceButtonPrefab is not set.");
            return;
        }

        for (int i = 0; i < choices.Count; i++)
        {
            int index = i;
            Button choiceButton = Instantiate(choiceButtonPrefab, choicesContainer);
            TMP_Text label = choiceButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = choices[i];
            }

            choiceButton.onClick.AddListener(() => manager?.ChooseChoice(index));
            choiceButtons.Add(choiceButton);
        }

        SelectObject(choiceButtons.Count > 0 ? choiceButtons[0].gameObject : null);
    }

    private void ClearChoices()
    {
        for (int i = 0; i < choiceButtons.Count; i++)
        {
            if (choiceButtons[i] != null)
            {
                Destroy(choiceButtons[i].gameObject);
            }
        }

        choiceButtons.Clear();
    }

    private void OnContinuePressed()
    {
        manager?.ContinueStory();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager == null)
        {
            return;
        }

        if (choiceButtons.Count > 0)
        {
            return;
        }

        if (eventData.pointerPress != null)
        {
            if (eventData.pointerPress == (continueButton != null ? continueButton.gameObject : null))
            {
                return;
            }

            if (eventData.pointerPress == (closeButton != null ? closeButton.gameObject : null))
            {
                return;
            }
        }

        manager.ContinueStory();
    }

    private void OnClosePressed()
    {
        manager?.CancelDialogue();
    }

    private void SelectObject(GameObject target)
    {
        if (EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        if (target != null)
        {
            EventSystem.current.SetSelectedGameObject(target);
        }
    }

    private void SetVisible(bool visible)
    {
        if (root == null)
        {
            gameObject.SetActive(visible);
            return;
        }

        root.alpha = visible ? 1f : 0f;
        root.interactable = visible;
        root.blocksRaycasts = visible;
    }
}
