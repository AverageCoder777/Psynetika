using UnityEngine;
using UnityEngine.InputSystem;

[AddComponentMenu("Dialogues/Language Switcher")]
public class LanguageSwitcher : MonoBehaviour
{
    [SerializeField] private LanguageManager.Language currentLanguage = LanguageManager.Language.Ru;

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            LanguageManager.CurrentLanguage = currentLanguage;
        }
    }

    private void Awake()
    {
        LanguageManager.CurrentLanguage = currentLanguage;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            ToggleLanguage();
        }
    }

    [ContextMenu("Toggle Language (F1)")]
    public void ToggleLanguage()
    {
        currentLanguage = currentLanguage == LanguageManager.Language.Ru
            ? LanguageManager.Language.En
            : LanguageManager.Language.Ru;
        LanguageManager.CurrentLanguage = currentLanguage;
        Debug.Log($"Language switched to: {currentLanguage}");
    }

    [ContextMenu("Set Russian")]
    public void SetRussian() => SetLanguage(LanguageManager.Language.Ru);

    [ContextMenu("Set English")]
    public void SetEnglish() => SetLanguage(LanguageManager.Language.En);

    private void SetLanguage(LanguageManager.Language lang)
    {
        currentLanguage = lang;
        LanguageManager.CurrentLanguage = lang;
    }
}