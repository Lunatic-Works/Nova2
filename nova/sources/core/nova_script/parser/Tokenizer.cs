using System;
using System.Text;

namespace Nova.Parser;

public class Tokenizer
{
    private readonly string _text;
    private int _column;
    private int _line;
    private int _index;
    private Token _next;

    public Token Peek => _next;

    public Tokenizer(string text)
    {
        _text = text;
        _line = 1;
        _column = 1;
        _index = 0;
        ParseNext();
    }

    public string SubString(int start, int length)
    {
        return _text.Substring(start, length);
    }

    private void AdvanceString(int length)
    {
        for (var i = 0; i < length; i++)
        {
            // Advance counters
            _index += 1;
            _column += 1;
            if (_index >= _text.Length)
                break;
            if (_text[_index] == '\n')
            {
                _column = 1;
                _line += 1;
            }
        }
    }

    private void AdvanceStringTill(char c)
    {
        while (_text[_index] != c)
        {
            // Advance counters
            _index += 1;
            _column += 1;
            if (_index >= _text.Length)
                break;
            if (_text[_index] == '\n')
            {
                _column = 1;
                _line += 1;
            }
        }
    }

    private char PeekChar(int offset = 0)
    {
        var idx = _index + offset;
        if (idx >= _text.Length)
        {
            return '\0';
        }

        return _text[idx];
    }

    public void SkipWhiteSpace()
    {
        while (_next.Type == TokenType.WhiteSpace)
        {
            ParseNext();
        }
    }

    public void AdvanceIdentifier()
    {
        while (_next.Type == TokenType.Character)
        {
            ParseNext();
        }
    }

    private void AdvanceQuotedSingleLine()
    {
        var quoteChar = _text[_next.Index];
        var escaped = false;
        var i = 0;
        for (; PeekChar(i) != '\0' && PeekChar(i) != '\n'; i++)
        {
            var c = PeekChar(i);
            if (!escaped && c == quoteChar)
            {
                break;
            }

            escaped = c == '\\';
        }

        if (PeekChar(i) == '\0')
        {
            throw new ParserException(_next, "Unpaired quote");
        }

        AdvanceString(i + 1);
    }

    private int TakeQuotedMultiline(int offset)
    {
        // Lua multiline string does not interpret escape sequence
        var len = 0;
        var lastIsRightSquareBracket = false;
        var lastIsLeftSquareBracket = false;
        for (; PeekChar(offset + len) != '\0'; len++)
        {
            var c = PeekChar(offset + len);
            if (lastIsLeftSquareBracket && c == '[')
            {
                var nested = TakeQuotedMultiline(offset + len + 1);
                len += nested;
                c = PeekChar(offset + len);
            }
            else if (lastIsRightSquareBracket && c == ']')
            {
                break;
            }

            lastIsLeftSquareBracket = c == '[';
            lastIsRightSquareBracket = c == ']';
        }

        if (!lastIsRightSquareBracket && PeekChar(offset + len) == ']')
        {
            throw new ParserException(_next, "Unpaired multiline string");
        }

        return len + 1;
    }

    private void AdvanceQuotedMultiline()
    {
        AdvanceString(TakeQuotedMultiline(0));
    }

    public void AdvanceQuoted(bool allowMultiline = true)
    {
        // Assert.IsTrue(_next.type == TokenType.Quote);
        var quoteChar = _text[_next.Index];

        if (_next.Length == 1 && quoteChar == '\'' || quoteChar == '\"')
        {
            AdvanceQuotedSingleLine();
        }
        else if (allowMultiline && _next.Length == 2 && quoteChar == '[' && _text[_next.Index + 1] == '[')
        {
            AdvanceQuotedMultiline();
        }
        else
        {
            throw new ParserException(_next, "Should not happen");
        }

        ParseNext();
    }

    private int IsBlockComment()
    {
        if (PeekChar() != '[')
        {
            return -1;
        }

        var i = 1;
        for (; PeekChar(i) == '='; i++) { }

        if (PeekChar(i) == '[')
        {
            return i - 1;
        }

        return -1;
    }

    private static string BlockCommentEndPattern(int num)
    {
        var sb = new StringBuilder();
        sb.Append(']');
        for (var i = 0; i < num; i++)
        {
            sb.Append('=');
        }

        sb.Append(']');
        return sb.ToString();
    }

    public void AdvanceComment()
    {
        // Assert.IsTrue(_next.type == TokenType.CommentStart);
        var blockCommentPattern = IsBlockComment();
        if (blockCommentPattern < 0)
        {
            AdvanceStringTill('\n');
            ParseNext();
            return;
        }

        var endPattern = BlockCommentEndPattern(blockCommentPattern);
        var endPatternIndex = _text.IndexOf(endPattern, _index, StringComparison.Ordinal);
        if (endPatternIndex == -1)
        {
            throw new ParserException(_next, "Unpaired block comment");
        }

        AdvanceString(endPatternIndex - _index + endPattern.Length);
        ParseNext();
    }

    private void PeekTokenType(out TokenType type, out int length, int offset = 0)
    {
        var c = PeekChar(offset);

        if (c == '\0')
        {
            type = TokenType.EndOfFile;
            length = 0;
            return;
        }

        // char.IsWhiteSpace('\n')
        if (c == '\n')
        {
            type = TokenType.NewLine;
            length = 1;
            return;
        }

        if (char.IsWhiteSpace(c))
        {
            type = TokenType.WhiteSpace;
            length = 1;
            return;
        }

        if (c == '@')
        {
            type = TokenType.At;
            length = 1;
            return;
        }

        if (c == ',')
        {
            type = TokenType.Comma;
            length = 1;
            return;
        }

        if (c == '=')
        {
            type = TokenType.Equal;
            length = 1;
            return;
        }

        if (c == '\'' || c == '"')
        {
            type = TokenType.Quote;
            length = 1;
            return;
        }

        // Lua multiline text
        var c2 = PeekChar(offset + 1);
        if (c == '[' && c2 == '[')
        {
            type = TokenType.Quote;
            length = 2;
            return;
        }

        if (c == '[')
        {
            type = TokenType.AttrStart;
            length = 1;
            return;
        }

        if (c == ']')
        {
            type = TokenType.AttrEnd;
            length = 1;
            return;
        }

        if (c == '<' && c2 == '|')
        {
            type = TokenType.BlockStart;
            length = 2;
            return;
        }

        if (c == '|' && c2 == '>')
        {
            type = TokenType.BlockEnd;
            length = 2;
            return;
        }

        if (c == '-' && c2 == '-')
        {
            type = TokenType.CommentStart;
            length = 2;
            return;
        }

        length = 1;
        type = TokenType.Character;
    }

    public void ParseNext()
    {
        var tokenStartIndex = _index;
        var tokenStartLine = _line;
        var tokenStartColumn = _column;
        PeekTokenType(out var tokenType, out var length);
        AdvanceString(length);

        _next = new Token
        {
            Index = tokenStartIndex,
            Length = length,
            Column = tokenStartColumn,
            Line = tokenStartLine,
            Type = tokenType
        };
    }
}
