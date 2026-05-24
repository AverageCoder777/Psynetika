using UnityEngine;

[CreateAssetMenu(menuName = "Dialogues/Linear Dialogue", fileName = "LinearDialogue")]
public class LinearDialogue : ScriptableObject, IDialogueSource
{
    [System.Serializable]
    public struct Line
    {
        public string speaker;
        [TextArea(2, 5)] public string text;
    }

    [SerializeField] private Line[] lines;

    public Line[] Lines => lines;

    public IDialogueRunner CreateRunner() => new LinearDialogueRunner(this);
}
