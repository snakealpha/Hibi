using System.Collections.Generic;
using Elecelf.Hibiki.Parser.GrammarGraph;

namespace Elecelf.Hibiki.Parser.SyntaxParser.TransferTable
{
    public class RuntimeTransfer:ITransfer
    {
        public RuntimeTransfer(OfflineTransfer offlineTransfer, ISyntaxElement syntaxElement, TransferTable transferTable)
        {
            OfflineTransfer = offlineTransfer;
            SyntaxElement = syntaxElement;
            TransferTable = transferTable;
        }

        public OfflineTransfer OfflineTransfer { get; }

        public ISyntaxElement SyntaxElement { get; }

        public TransferTable TransferTable { get; }

        public IState TransfedState => TransferTable.OfflineStates[OfflineTransfer.TransferedState].GenerateState(TransferTable);
    }

    public class OfflineTransfer
    {
        public enum SyntaxElementType
        {
            String,
            Escape,
            Symbol,
        }

        public static Dictionary<SyntaxElementType, TransferCondition> ProtoConditions =
            new Dictionary<SyntaxElementType, TransferCondition>
            {
                {SyntaxElementType.Symbol, new SymbolTransferCondition(new Symbol())},
                {SyntaxElementType.String, new StringTransferCondition("")},
                {SyntaxElementType.Escape, new EscapeTransferCondition("")},
            };

        public OfflineTransfer(uint transferedState, SyntaxElementType elementType, string elementLiteral)
        {
            TransferedState = transferedState;
            ElementType = elementType;
            ElementLiteral = elementLiteral;
        }

        public uint TransferedState { get; }

        public SyntaxElementType ElementType { get; }

        public string ElementLiteral { get; }

        public ITransfer GenerateTransfer(TransferTable table, ParserContext context)
        {
            return new RuntimeTransfer(
                this,
                ProtoConditions[ElementType].GetThisElement(ElementLiteral, context),
                table);
        }
    }
}
