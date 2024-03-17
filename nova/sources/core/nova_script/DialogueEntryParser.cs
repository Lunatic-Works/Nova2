using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Godot;
using Nova.Parser;

namespace Nova;
using ParsedBlocks = IReadOnlyList<ParsedBlock>;
using ParsedChunks = IReadOnlyList<IReadOnlyList<ParsedBlock>>;

public static class DialogueEntryParser
{
    private static readonly Regex MarkdownCodePattern =
        new(@"`([^`]*)`", RegexOptions.Compiled);

    private static readonly Regex MarkdownLinkPattern =
        new(@"\[([^\]]*)\]\(([^\)]*)\)", RegexOptions.Compiled);

    private const string StageKey = "stage";

    private static string GetStageName(DialogueActionStage stage)
    {
        return stage switch
        {
            DialogueActionStage.BeforeCheckpoint => "before_checkpoint",
            DialogueActionStage.Default => "",
            DialogueActionStage.AfterDialogue => "after_dialogue",
            _ => throw new ArgumentOutOfRangeException($"Invalid DialogueActionStage {stage}."),
        };
    }

    private static string GetCode(ParsedBlocks chunk, DialogueActionStage stage)
    {
        var sb = new StringBuilder();
        var stageName = GetStageName(stage);
        foreach (var block in chunk)
        {
            if (block.Type == BlockType.LazyExecution)
            {
                var stageValue = block.Attributes.GetValueOrDefault(StageKey, "");
                if (stageValue != stageName)
                {
                    continue;
                }

                sb.Append(block.Content).Append('\n');
            }
        }
        return sb.ToString().Trim();
    }

    private static ulong GetChunkHash(ParsedBlocks chunk)
    {
        return Utils.HashList(chunk.SelectMany(block => block.ToList()));
    }

    private static DialogueEntry ParseDialogueEntry(ParsedBlocks chunk, Dictionary<string, string> hiddenNames)
    {
        var text = NovaParser.GetText(chunk);
        // Markdown syntaxes used in tutorials
        // They are not in the NovaScript spec. If they interfere with your scenarios or you have performance concern,
        // you can comment out them
        text = MarkdownCodePattern.Replace(text, @"<style=Code>$1</style>");
        text = MarkdownLinkPattern.Replace(text, @"<link=""$2""><style=Link>$1</style></link>");

        NovaParser.ParseNameDialogue(text, out var displayName, out var characterName,
            out var dialogue, hiddenNames);

        var actions = new Dictionary<DialogueActionStage, RefCounted>();
        foreach (var stage in Enum.GetValues<DialogueActionStage>())
        {
            var code = GetCode(chunk, stage);
            // TODO: add any preprocessor here

            // add action in default stage
            if (string.IsNullOrEmpty(code))
            {
                if (stage != DialogueActionStage.Default)
                {
                    continue;
                }
                actions.Add(stage, GDRuntime.BaseLazyBlock);
            }
            else
            {
                var action = GDRuntime.CompileLazyBlock(code);
                actions.Add(stage, action);
            }
        }

        return new DialogueEntry(characterName, displayName, dialogue, actions, GetChunkHash(chunk));
    }

    public static IReadOnlyList<DialogueEntry> ParseDialogueEntries(ParsedChunks chunks)
    {
        var hiddenNames = new Dictionary<string, string>();
        return chunks.Select(chunk => ParseDialogueEntry(chunk, hiddenNames)).ToList();
    }

    private static LocalizedDialogueEntry ParseLocalizedDialogueEntry(ParsedBlocks chunk)
    {
        var text = NovaParser.GetText(chunk);
        NovaParser.ParseNameDialogue(text, out var displayName, out var hiddenName, out var dialogue);
        if (!string.IsNullOrEmpty(hiddenName))
        {
            throw new ParserException(
                "Cannot set internal character name in non-default locale.\n" +
                $"hiddenName: {hiddenName}, displayName: {displayName}, dialogue: {dialogue}");
        }

        return new LocalizedDialogueEntry { DisplayName = displayName, Dialogue = dialogue };
    }

    public static IReadOnlyList<LocalizedDialogueEntry> ParseLocalizedDialogueEntries(ParsedChunks chunks)
    {
        return chunks.Select(ParseLocalizedDialogueEntry).ToList();
    }
}
