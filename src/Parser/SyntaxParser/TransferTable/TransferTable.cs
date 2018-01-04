using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Elecelf.Hibiki.Parser.SyntaxParser.TransferTable
{
    public class TransferTable : IParseable
    {
        public IState StartState { get; private set; }

        public IDictionary<Symbol, IState> States { get; } = new ConcurrentDictionary<Symbol, IState>();

        public IDictionary<Symbol, ITransfer> Transfers { get; } = new ConcurrentDictionary<Symbol, ITransfer>();
    }
}