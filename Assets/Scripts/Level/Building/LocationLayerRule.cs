using System;
using UnityEngine;

/*
Правило: какие слои Aseprite обрабатывать каким действием.

Маска — простой wildcard (* и ?), регистр не важен: «col_*» поймает и col_ground, и Col_Ground.
Порядок правил в конфиге значим — берётся первое совпавшее.
*/
[Serializable]
public class LocationLayerRule
{
    [Tooltip("Маска имени слоя: col_*, plat_*, *_bg. Регистр не важен")]
    public string namePattern = "col_*";

    [SerializeReference, SubclassSelector]
    public LocationLayerAction action;

    public bool Matches(string layerName)
    {
        return !string.IsNullOrWhiteSpace(namePattern) && IsMatch(namePattern, layerName);
    }

    // Wildcard без регулярных выражений: '*' — любая последовательность, '?' — один символ.
    public static bool IsMatch(string pattern, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        int patternIndex = 0;
        int valueIndex = 0;
        int starIndex = -1;
        int starValueIndex = 0;

        while (valueIndex < value.Length)
        {
            bool sameChar = patternIndex < pattern.Length
                && (pattern[patternIndex] == '?' || CharsEqual(pattern[patternIndex], value[valueIndex]));

            if (sameChar)
            {
                patternIndex++;
                valueIndex++;
            }
            else if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                starIndex = patternIndex;
                starValueIndex = valueIndex;
                patternIndex++;
            }
            else if (starIndex >= 0)
            {
                // Откат к последней звёздочке: она съедает ещё один символ значения.
                patternIndex = starIndex + 1;
                starValueIndex++;
                valueIndex = starValueIndex;
            }
            else
            {
                return false;
            }
        }

        while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
        {
            patternIndex++;
        }

        return patternIndex == pattern.Length;
    }

    private static bool CharsEqual(char a, char b)
    {
        return char.ToLowerInvariant(a) == char.ToLowerInvariant(b);
    }
}
