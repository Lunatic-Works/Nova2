using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Nova.Parser;
using ParsedBlocks = IReadOnlyList<ParsedBlock>;
using ParsedChunks = IReadOnlyList<IReadOnlyList<ParsedBlock>>;

public enum BlockType
{
    EagerExecution,
    LazyExecution,
    Text,
    Separator
}

public readonly struct ParsedBlock()
{
    public int Line { get; init; }
    public BlockType Type { get; init; }
    public string Content { get; init; }
    public IReadOnlyDictionary<string, string> Attributes { get; init; }

    public ParsedBlock(int line, BlockType type, string content,
        IReadOnlyDictionary<string, string> attributes = null) : this()
    {
        Line = line;
        Type = type;
        Content = content?.Replace("\r", "") ?? "";
        Attributes = attributes ?? ImmutableDictionary<string, string>.Empty;
    }

    public IEnumerable<object> ToList()
    {
        IEnumerable<object> ret = [Type, Content];
        return ret.Concat(Attributes.OrderBy(x => x.Key).Cast<object>());
    }
}

public static class NovaParser
{
    private static ParsedBlock ParseCodeBlock(Tokenizer tokenizer, int line, BlockType type,
        IReadOnlyDictionary<string, string> attributes)
    {
        ParserException.ExpectToken(tokenizer.Peek, TokenType.BlockStart, "<|");
        var startToken = tokenizer.Peek;
        tokenizer.ParseNext();
        var matchFound = false;
        var startIndex = tokenizer.Peek.Index;
        var endIndex = startIndex;
        while (tokenizer.Peek.Type != TokenType.EndOfFile)
        {
            var token = tokenizer.Peek;
            if (token.Type == TokenType.CommentStart)
            {
                tokenizer.AdvanceComment();
                continue;
            }

            if (token.Type == TokenType.Quote)
            {
                tokenizer.AdvanceQuoted();
                continue;
            }

            tokenizer.ParseNext();

            if (token.Type == TokenType.BlockEnd)
            {
                matchFound = true;
                break;
            }

            endIndex = token.Index + token.Length;
        }

        var content = tokenizer.SubString(startIndex, endIndex - startIndex);

        if (!matchFound)
        {
            throw new ParserException(startToken, "Unpaired block start <|");
        }

        tokenizer.SkipWhiteSpace();

        ParserException.ExpectToken(tokenizer.Peek, TokenType.NewLine, TokenType.EndOfFile,
            "new line or end of file after |>");
        tokenizer.ParseNext();

        return new(line, type, content, attributes);
    }

    private static ParsedBlock ParseEagerExecutionBlock(Tokenizer tokenizer, int line)
    {
        ParserException.ExpectToken(tokenizer.Peek, TokenType.At, "@");
        tokenizer.ParseNext();
        var token = tokenizer.Peek;
        if (token.Type == TokenType.AttrStart)
        {
            return ParseCodeBlockWithAttributes(tokenizer, line, BlockType.EagerExecution);
        }

        if (token.Type == TokenType.BlockStart)
        {
            return ParseCodeBlock(tokenizer, line, BlockType.EagerExecution, null);
        }

        throw new ParserException(token, $"Except [ or <| after @, found {token.Type}");
    }

    private static string ExpectIdentifierOrString(Tokenizer tokenizer)
    {
        if (tokenizer.Peek.Type == TokenType.Character)
        {
            var startIndex = tokenizer.Peek.Index;
            tokenizer.AdvanceIdentifier();
            var endIndex = tokenizer.Peek.Index;
            return tokenizer.SubString(startIndex, endIndex - startIndex);
        }

        if (tokenizer.Peek.Type == TokenType.Quote)
        {
            var startIndex = tokenizer.Peek.Index;
            tokenizer.AdvanceQuoted(false);
            var endIndex = tokenizer.Peek.Index;
            return tokenizer.SubString(startIndex, endIndex - startIndex);
        }

        throw new ParserException(tokenizer.Peek, $"Expect identifier or string, found {tokenizer.Peek.Type}");
    }

    private static char EscapeChar(char c)
    {
        if (c == 'a') return '\a';
        if (c == 'b') return '\b';
        if (c == 'f') return '\f';
        if (c == 'n') return '\n';
        if (c == 'r') return '\r';
        if (c == 't') return '\t';
        if (c == 'v') return '\v';
        return c;
    }

    private static string EscapeString(string str)
    {
        var sb = new StringBuilder();
        var escaped = false;
        foreach (var c in str)
        {
            var nextEscaped = !escaped && c == '\\';
            if (escaped)
            {
                sb.Append(EscapeChar(c));
            }
            else if (c != '\\')
            {
                sb.Append(c);
            }

            escaped = nextEscaped;
        }

        return sb.ToString();
    }

    private static string UnQuote(string str)
    {
        if (str.Length == 0)
        {
            return str;
        }

        var first = str.First();
        if (str.Length >= 2 && (first == '\'' || first == '\"' && first == str.Last()))
        {
            return EscapeString(str.Substring(1, str.Length - 2));
        }

        return str;
    }

    private static ParsedBlock ParseCodeBlockWithAttributes(Tokenizer tokenizer, int line, BlockType type)
    {
        ParserException.ExpectToken(tokenizer.Peek, TokenType.AttrStart, "[");
        tokenizer.ParseNext();
        var attributes = new Dictionary<string, string>();

        while (tokenizer.Peek.Type != TokenType.EndOfFile)
        {
            tokenizer.SkipWhiteSpace();
            if (tokenizer.Peek.Type == TokenType.AttrEnd)
            {
                tokenizer.ParseNext();
                break;
            }

            var key = ExpectIdentifierOrString(tokenizer);
            string value = null;

            tokenizer.SkipWhiteSpace();
            if (tokenizer.Peek.Type == TokenType.Equal)
            {
                tokenizer.ParseNext();
                tokenizer.SkipWhiteSpace();
                value = ExpectIdentifierOrString(tokenizer);
            }

            tokenizer.SkipWhiteSpace();
            if (tokenizer.Peek.Type == TokenType.Comma || tokenizer.Peek.Type == TokenType.AttrEnd)
            {
                tokenizer.ParseNext();
            }
            else
            {
                throw new ParserException(tokenizer.Peek, "Expect , or ]");
            }

            attributes.Add(UnQuote(key.Trim()), value == null ? null : UnQuote(value.Trim()));

            if (tokenizer.Peek.Type == TokenType.AttrEnd)
            {
                break;
            }
        }

        return ParseCodeBlock(tokenizer, line, type, attributes);
    }

    private static ParsedBlock ParseTextBlock(Tokenizer tokenizer, int line, int startIndex)
    {
        while (tokenizer.Peek.Type != TokenType.EndOfFile && tokenizer.Peek.Type != TokenType.NewLine)
        {
            tokenizer.ParseNext();
        }

        var endIndex = tokenizer.Peek.Index;
        var content = tokenizer.SubString(startIndex, endIndex - startIndex);

        // eat up the last newline
        tokenizer.ParseNext();

        return new(line, BlockType.Text, content);
    }

    private static ParsedBlock ParseBlock(Tokenizer tokenizer)
    {
        var startIndex = tokenizer.Peek.Index;
        tokenizer.SkipWhiteSpace();
        var endIndex = tokenizer.Peek.Index;
        var line = tokenizer.Peek.Line;
        var tokenType = tokenizer.Peek.Type;

        if (tokenType == TokenType.NewLine || tokenType == TokenType.EndOfFile)
        {
            var content = tokenizer.SubString(startIndex, endIndex - startIndex);
            tokenizer.ParseNext();
            return new(line, BlockType.Separator, content);
        }

        if (tokenType == TokenType.At)
        {
            return ParseEagerExecutionBlock(tokenizer, line);
        }

        if (tokenType == TokenType.AttrStart)
        {
            return ParseCodeBlockWithAttributes(tokenizer, line, BlockType.LazyExecution);
        }

        if (tokenType == TokenType.BlockStart)
        {
            return ParseCodeBlock(tokenizer, line, BlockType.LazyExecution, null);
        }

        return ParseTextBlock(tokenizer, line, startIndex);
    }

    private static List<ParsedBlock> MergeConsecutiveSeparators(List<ParsedBlock> oldBlocks)
    {
        var blocks = new List<ParsedBlock> { new(0, BlockType.Separator, null, null) };

        foreach (var block in oldBlocks)
        {
            if (block.Type != BlockType.Separator || blocks.Last().Type != BlockType.Separator)
            {
                blocks.Add(block);
            }
        }

        blocks.RemoveAt(0);
        if (blocks.Count > 0 && blocks.Last().Type == BlockType.Separator)
        {
            blocks.RemoveAt(blocks.Count - 1);
        }

        return blocks;
    }

    public static ParsedBlocks ParseBlocks(string text)
    {
        var tokenizer = new Tokenizer(text);
        var blocks = new List<ParsedBlock>();
        while (tokenizer.Peek.Type != TokenType.EndOfFile)
        {
            blocks.Add(ParseBlock(tokenizer));
        }

        return MergeConsecutiveSeparators(blocks);
    }

    /// <summary>
    /// Split blocks at separators and eager execution blocks. Each chunk contain at least one block.
    /// </summary>
    private static List<ParsedBlocks> SplitBlocksToChunks(ParsedBlocks blocks)
    {
        var chunks = new List<ParsedBlocks>();
        var chunk = new List<ParsedBlock>();

        void FlushChunk()
        {
            if (chunk.Count > 0)
            {
                chunks.Add(chunk);
                chunk = [];
            }
        }

        foreach (var block in blocks)
        {
            if (block.Type == BlockType.Separator)
            {
                FlushChunk();
            }
            else if (block.Type == BlockType.EagerExecution)
            {
                FlushChunk();
                chunk.Add(block);
                FlushChunk();
            }
            else
            {
                chunk.Add(block);
            }
        }

        FlushChunk();
        return chunks;
    }

    public static ParsedChunks ParseChunks(string text)
    {
        return SplitBlocksToChunks(ParseBlocks(text));
    }

    private static readonly Regex NameDialoguePattern =
        new(@"^(?<name>[^/：:]*)(//(?<hidden>[^：:]*))?(：：|::)(?<dialogue>(.|\n)*)",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture);

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
        out string dialogue, Dictionary<string, string> hiddenNames = null)
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
}
