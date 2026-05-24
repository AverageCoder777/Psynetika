using System.Collections.Generic;
using Ink.Runtime;
using UnityEngine;

public class InkDialogueRunner : IDialogueRunner
{
    private readonly Story story;
    private string lastSpeaker = string.Empty;

    public InkDialogueRunner(TextAsset inkJsonAsset)
    {
        if (inkJsonAsset == null)
        {
            Debug.LogError("InkDialogueRunner: inkJsonAsset is null.");
            return;
        }

        story = new Story(inkJsonAsset.text);
    }

    public bool TryGetNext(out string line, out string speaker, out IReadOnlyList<string> choices)
    {
        line = string.Empty;
        speaker = string.Empty;
        choices = System.Array.Empty<string>();

        if (story == null)
        {
            return false;
        }

        while (story.canContinue)
        {
            string nextLine = story.Continue();
            nextLine = string.IsNullOrWhiteSpace(nextLine) ? string.Empty : nextLine.Trim();

            ParseTags(story.currentTags, out string parsedSpeaker);
            if (!string.IsNullOrEmpty(parsedSpeaker))
            {
                lastSpeaker = parsedSpeaker;
            }

            if (!string.IsNullOrEmpty(nextLine))
            {
                line = nextLine;
                speaker = lastSpeaker;
                choices = ExtractChoiceTexts();
                return true;
            }
        }

        if (story.currentChoices.Count > 0)
        {
            speaker = lastSpeaker;
            choices = ExtractChoiceTexts();
            return true;
        }

        return false;
    }

    public void Choose(int index)
    {
        if (story == null)
        {
            return;
        }

        if (index < 0 || index >= story.currentChoices.Count)
        {
            return;
        }

        story.ChooseChoiceIndex(index);
    }

    private List<string> ExtractChoiceTexts()
    {
        int count = story.currentChoices.Count;
        List<string> result = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            result.Add(story.currentChoices[i].text.Trim());
        }

        return result;
    }

    private static void ParseTags(IReadOnlyList<string> tags, out string speaker)
    {
        speaker = string.Empty;
        if (tags == null || tags.Count == 0)
        {
            return;
        }

        for (int i = 0; i < tags.Count; i++)
        {
            string tag = tags[i];
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            int separator = tag.IndexOf(':');
            if (separator <= 0 || separator >= tag.Length - 1)
            {
                continue;
            }

            string key = tag.Substring(0, separator).Trim().ToLowerInvariant();
            string value = tag.Substring(separator + 1).Trim();

            if (key == "speaker")
            {
                speaker = value;
            }
        }
    }
}
