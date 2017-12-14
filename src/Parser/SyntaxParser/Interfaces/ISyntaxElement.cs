namespace Elecelf.Hibiki.Parser.SyntaxParser
{
    public interface ISyntaxElement
    {
        string ToString();

        (bool finished, bool success) PassChar(char input, ParserSessionContext sessionContext, ParserContext context);
    }
}
