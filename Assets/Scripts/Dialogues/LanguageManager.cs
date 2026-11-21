using UnityEngine;

public static class LanguageManager
{
    public enum Language { Ru, En }

    private static Language currentLanguage = Language.Ru;

    public static Language CurrentLanguage
    {
        get => currentLanguage;
        set
        {
            if (currentLanguage != value)
            {
                currentLanguage = value;
                OnLanguageChanged?.Invoke(value);
            }
        }
    }

    public static event System.Action<Language> OnLanguageChanged;

    public static string GetText(string textRu, string textEn)
        => currentLanguage == Language.Ru ? textRu : textEn;

    // Для быстрого переключения из инспектора через Context Menu
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        currentLanguage = Language.Ru;
    }
}