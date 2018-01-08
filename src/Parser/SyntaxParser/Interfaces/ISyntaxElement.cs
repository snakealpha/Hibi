namespace Elecelf.Hibiki.Parser.SyntaxParser
{
    /// <summary>
    /// A syntax element is a basic component of a automata, defined how a string can be parsed to a little piece of ast.
    /// </summary>
    public interface ISyntaxElement
    {
        string ToString();

        (bool finished, bool success, ErrorInfo errorInfo) PassChar(char input, int offset, ParserContext context);

        ISyntaxElement GetThisElement(string literal, ParserContext context);
    }
}
