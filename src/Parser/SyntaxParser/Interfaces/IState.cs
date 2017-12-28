using System.Collections.Generic;

namespace Elecelf.Hibiki.Parser.SyntaxParser
{
    /// <summary>
    /// A state of a front-end parser automata.
    /// </summary>
    public interface IState
    {
        IEnumerable<ITransfer> Transfers { get; }

        bool IsTerminal { get; }

        IEnumerable<ITransfer> PredictTransfers { get; }
    }
}