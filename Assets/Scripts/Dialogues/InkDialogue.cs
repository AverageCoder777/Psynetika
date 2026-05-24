using UnityEngine;

[CreateAssetMenu(menuName = "Dialogues/Ink Dialogue", fileName = "InkDialogue")]
public class InkDialogue : ScriptableObject, IDialogueSource
{
    [SerializeField] private TextAsset inkJson;

    public TextAsset InkJson => inkJson;

    public IDialogueRunner CreateRunner() => new InkDialogueRunner(inkJson);
}
