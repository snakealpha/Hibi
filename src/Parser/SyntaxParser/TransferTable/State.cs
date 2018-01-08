using System;
using System.Collections.Generic;

namespace Elecelf.Hibiki.Parser.SyntaxParser.TransferTable
{
    public class RuntimeState : IState
    {
        public RuntimeState
        (
            OfflineState offlineState, 
            TransferTable transferTable
        )
        {
            OfflineState = offlineState;
            TransferTable = transferTable;
        }

        public OfflineState OfflineState { get; }

        public bool IsTerminal => OfflineState.IsTerminal;

        public TransferTable TransferTable { get; }
        
        public IEnumerable<ITransfer> Transfers => throw new NotImplementedException();

        public IEnumerable<ITransfer> PredictTransfers => Transfers;
    }

    public class OfflineState
    {
        public uint SymbolId { get; }

        private readonly List<OfflineTransfer> _transfers = new List<OfflineTransfer>();

        public OfflineState(bool isTerminal, string stateName, uint symbolId)
        {
            IsTerminal = isTerminal;
            StateName = stateName;
            SymbolId = symbolId;
        }

        public List<OfflineTransfer> Transfers => _transfers;

        public bool IsTerminal { get; }

        public string StateName { get; }

        public IState GenerateState(TransferTable table)
        {
            if (table.States.TryGetValue(SymbolId, out var existedState))
                return existedState;
            var state = new RuntimeState(this, table);
            table.States[SymbolId] = state;
            return state;
        }
    }
}