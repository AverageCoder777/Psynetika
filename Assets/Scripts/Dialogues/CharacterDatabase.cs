using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogues/Character Database", fileName = "CharacterDatabase")]
public class CharacterDatabase : ScriptableObject
{
    [SerializedDictionary("Speaker", "Avatar")]
    public SerializedDictionary<string, Sprite> avatars;

    public Sprite GetAvatar(string speaker)
    {
        if (string.IsNullOrEmpty(speaker) || avatars == null)
        {
            return null;
        }

        return avatars.TryGetValue(speaker, out Sprite sprite) ? sprite : null;
    }
}
