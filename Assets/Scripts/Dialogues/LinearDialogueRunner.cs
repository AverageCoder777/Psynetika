using System.Collections.Generic;

public class LinearDialogueRunner : IDialogueRunner
{
    private static readonly IReadOnlyList<string> EmptyChoices = new string[0];

    private readonly LinearDialogue dialogue;
    private int index;

    public LinearDialogueRunner(LinearDialogue dialogue)
    {
        this.dialogue = dialogue;
        index = 0;
    }

    public bool TryGetNext(out string line, out string speaker, out IReadOnlyList<string> choices)
    {
        choices = EmptyChoices;
        line = string.Empty;
        speaker = string.Empty;

        if (dialogue == null || dialogue.Lines == null)
        {
            return false;
        }

        while (index < dialogue.Lines.Length)
        {
            LinearDialogue.Line current = dialogue.Lines[index++];
            if (string.IsNullOrWhiteSpace(current.text))
            {
                continue;
            }

            line = current.text;
            speaker = current.speaker ?? string.Empty;
            return true;
        }

        return false;
    }

    public void Choose(int index)
    {
    }
}
