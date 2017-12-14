namespace Elecelf.Hibiki.Parser.SyntaxParser
{
    public interface ITransfer
    {
        ISyntaxElement SyntaxElement { get; }
        IState TransfedState { get; }
    }
}