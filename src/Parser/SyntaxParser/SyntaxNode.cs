using System.Collections.Generic;

namespace Elecelf.Hibiki.Parser.SyntaxParser
{
    public class SyntaxNode
    {
        public Token Token;
        public Symbol Production;
        public IEnumerable<SyntaxNode> LeaveNodes;
    }
}
