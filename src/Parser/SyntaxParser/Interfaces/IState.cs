using System.Collections.Generic;

namespace Elecelf.Hibiki.Parser.SyntaxParser
{
    public interface IState
    {
        IEnumerable<ITransfer> Transfers { get; }
    }
}