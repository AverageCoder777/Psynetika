using UnityEngine;

[CreateAssetMenu(menuName = "Dialogues/Linear Dialogue", fileName = "LinearDialogue")]
public class LinearDialogue : ScriptableObject, IDialogueSource
{
    [System.Serializable]
    public struct Line
    {
        [TextArea(1, 2)] public string speakerRu;
        [TextArea(1, 2)] public string speakerEn;
        [TextArea(2, 5)] public string textRu;
        [TextArea(2, 5)] public string textEn;
    }

    [SerializeField] private Line[] lines;

    public Line[] Lines => lines;

    public IDialogueRunner CreateRunner() => new LinearDialogueRunner(this);
}
