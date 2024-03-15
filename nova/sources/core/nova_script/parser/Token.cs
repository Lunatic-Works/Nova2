namespace Nova.Parser
{
    public enum TokenType
    {
        BlockStart,
        BlockEnd,
        AttrStart,
        AttrEnd,
        At,
        Equal,
        Comma,
        NewLine,
        Quote,
        Character,
        WhiteSpace,
        CommentStart,
        EndOfFile
    }

    public readonly struct Token
    {
        public int Index { get; init; }
        public int Length { get; init; }
        public int Line { get; init; }
        public int Column { get; init; }
        public TokenType Type { get; init; }
    }
}
