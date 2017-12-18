namespace Elecelf.Hibiki.Parser.SyntaxParser
{
    public interface ISyntaxElement
    {
        string ToString();

        (bool finished, bool success, ErrorInfo errorInfo) PassChar(char input, ParserSegment thread, ParserSessionContext sessionContext, ParserContext context);
    }
}
