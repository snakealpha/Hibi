using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Elecelf.Hibiki.Parser.SyntaxParser.TransferTable
{
    public class TransferTable : IParseable
    {
        private uint _startStateId;
        public IState StartState { get; private set; }

        public IDictionary<uint, IState> States { get; } = new ConcurrentDictionary<uint, IState>();

        public IDictionary<uint, OfflineState> OfflineStates { get; } = new ConcurrentDictionary<uint, OfflineState>();
    }
}