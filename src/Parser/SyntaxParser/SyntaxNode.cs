using System.Collections.Generic;

namespace Elecelf.Hibiki.Parser.SyntaxParser
{
    public class SyntaxNode : IAstNode
    {
        public Token Token { set; get; }
        public Symbol ProductionSymbol { set; get; }

        public IEnumerable<IAstNode> LeafNodes { get; } = new List<IAstNode>();

        public bool IsPending => false;
    }
}
