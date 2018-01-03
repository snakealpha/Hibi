using System.Collections.Generic;

namespace Elecelf.Hibiki.Parser.SyntaxParser
{
    public class SyntaxNode : IAstNode
    {
        public Token Token { get; private set; }
        public Symbol ProductionSymbol { set; get; }

        public IList<IAstNode> LeafNodes { get; } = new List<IAstNode>();

        public bool IsPending => false;

        public void SetToken(uint start, uint length)
        {
            Token = new Token()
            {
                LiteralStart = start,
                LiteralLength = length,
            };
        }
    }
}
