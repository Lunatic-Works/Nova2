using System;
using System.Runtime.Serialization;

namespace Nova.Parser
{
    public class ParserException : Exception
    {
        public ParserException() { }
        public ParserException(string message) : base(message) { }
        public ParserException(string message, Exception inner) : base(message, inner) { }
        public ParserException(Token token, string msg) : this(ErrorMessage(token, msg)) { }

        public static string ErrorMessage(Token token, string msg)
        {
            return ErrorMessage(token.Line, token.Column, msg);
        }

        public static string ErrorMessage(int line, int column, string msg)
        {
            return $"Line {line}, Column {column}: {msg}";
        }

        public static void ExpectToken(Token token, TokenType type, string display)
        {
            if (token.Type != type)
            {
                throw new ParserException(token, $"Expect {display}");
            }
        }

        // TODO: params?
        public static void ExpectToken(Token token, TokenType typeA, TokenType typeB, string display)
        {
            if (token.Type != typeA && token.Type != typeB)
            {
                throw new ParserException(token, $"Expect {display}");
            }
        }
    }
}
