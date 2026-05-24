using System.Collections.Generic;

public interface IDialogueRunner
{
    bool TryGetNext(out string line, out string speaker, out IReadOnlyList<string> choices);
    void Choose(int index);
}
