using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Nova.Parser;

using ParsedBlocks = IReadOnlyList<ParsedBlock>;
using ParsedChunks = IReadOnlyList<IReadOnlyList<ParsedBlock>>;

public readonly struct ParsedDialogueEntry
{
    public int Line { get; init; }
    public string CharacterName { get; init; }
    public string DisplayName { get; init; }
    public string Dialogue { get; init; }
    public ParsedBlocks CodeBlocks { get; init; }
}

public readonly struct ParsedNode
{
    public string Name { get; init; }
    public IReadOnlyList<ParsedDialogueEntry> DialogueEntries { get; init; }
    public ParsedBlock HeadEagerBlock { get; init; }
    public ParsedBlock TailEagerBlock { get; init; }
}

public static class NodeParser
{
    private static readonly Regex NameDialoguePattern =
        new(@"^(?<name>[^/：:]*)(//(?<hidden>[^：:]*))?(：：|::)(?<dialogue>(.|\n)*)",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    private static readonly Regex NodeLabelPattern =
        new(@"label\s*\(?'(?<name>[^']*)'", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    /// <remarks>
    /// There can be multiple text blocks in a chunk. They are concatenated into one, separated by newlines
    /// </remarks>
    public static string GetText(ParsedBlocks chunk)
    {
        var sb = new StringBuilder();
        var first = true;
        foreach (var block in chunk)
        {
            if (block.Type == BlockType.Text)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append('\n');
                }

                sb.Append(block.Content);
            }
        }

        return sb.ToString();
    }

    // Update hiddenNames in place
    public static void ParseNameDialogue(string text, out string displayName, out string hiddenName,
        out string dialogue, Dictionary<string, string> hiddenNames)
    {
        // Coarse test for performance
        if (text.IndexOf("：：", StringComparison.Ordinal) < 0 && text.IndexOf("::", StringComparison.Ordinal) < 0)
        {
            displayName = "";
            hiddenName = "";
            dialogue = text;
            return;
        }

        var m = NameDialoguePattern.Match(text);
        if (m.Success)
        {
            displayName = m.Groups["name"].Value;
            hiddenName = m.Groups["hidden"].Value;
            dialogue = m.Groups["dialogue"].Value;
        }
        else
        {
            displayName = "";
            hiddenName = "";
            dialogue = text;
        }

        if (hiddenNames == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(hiddenName))
        {
            if (!string.IsNullOrEmpty(displayName))
            {
                if (hiddenNames.TryGetValue(displayName, out var name))
                {
                    hiddenName = name;
                }
                else
                {
                    hiddenName = displayName;
                }
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(displayName))
            {
                hiddenNames[displayName] = hiddenName;
            }
        }
    }

    private static string TryGetNodeName(string code)
    {
        var m = NodeLabelPattern.Match(code);
        return m.Success ? m.Groups["name"].Value : null;
    }

    private static List<ParsedNode> SplitChunksToNodes(ParsedChunks chunks)
    {
        var nodes = new List<ParsedNode>();
        string nodeName = null;
        var dialogueEntries = new List<ParsedDialogueEntry>();
        ParsedBlock headEagerBlock = null;
        var hiddenNames = new Dictionary<string, string>();

        void FlushNode(ParsedBlock tailEagerBlock)
        {
            nodes.Add(new ParsedNode
            {
                Name = nodeName,
                DialogueEntries = dialogueEntries,
                HeadEagerBlock = headEagerBlock,
                TailEagerBlock = tailEagerBlock
            });
            nodeName = null;
            dialogueEntries = [];
            headEagerBlock = null;
            hiddenNames.Clear();
        }

        foreach (var chunk in chunks)
        {
            var firstBlock = chunk[0];
            if (firstBlock.Type == BlockType.EagerExecution)
            {
                var newNodeName = TryGetNodeName(firstBlock.Content);
                if (string.IsNullOrEmpty(newNodeName))
                {
                    // firstBlock is tail eager block
                    if (string.IsNullOrEmpty(nodeName))
                    {
                        throw new ParserException(
                            $"Nova: Unmatched tail eager block at line {firstBlock.Line}:\n{firstBlock.Content}");
                    }
                    else
                    {
                        FlushNode(firstBlock);
                    }
                }
                else
                {
                    // firstBlock is head eager block
                    if (!string.IsNullOrEmpty(nodeName))
                    {
                        FlushNode(firstBlock);
                    }

                    nodeName = newNodeName;
                    headEagerBlock = firstBlock;
                }
            }
            else
            {
                var text = GetText(chunk);
                ParseNameDialogue(text, out var displayName, out var characterName, out var dialogue, hiddenNames);
                var codeBlocks = chunk.Where(block => block.Type == BlockType.LazyExecution).ToList();
                dialogueEntries.Add(new ParsedDialogueEntry
                {
                    Line = chunk[0].Line,
                    CharacterName = characterName,
                    DisplayName = displayName,
                    Dialogue = dialogue,
                    CodeBlocks = codeBlocks
                });
            }
        }

        return nodes;
    }

    public static IReadOnlyList<ParsedNode> ParseNodes(string text)
    {
        return SplitChunksToNodes(Parser.ParseChunks(text));
    }
}
