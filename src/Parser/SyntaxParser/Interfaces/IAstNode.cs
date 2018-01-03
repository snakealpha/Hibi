using System.Collections.Generic;

namespace Elecelf.Hibiki.Parser.SyntaxParser
{
    /// <summary>
    /// If a class implement this interface, it can be a root or leaf node of a ast.
    /// </summary>
    public interface IAstNode
    {
        /// <summary>
        /// Is this node pending?
        /// At the end step of a parse workflow, the whole ast cannot include a pending node.
        /// </summary>
        bool IsPending { get; }

        IList<IAstNode> LeafNodes { get; }

        Token Token { get; }

        Symbol ProductionSymbol { get; }

        void SetToken(uint start, uint length);
    }
}