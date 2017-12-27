using System.Collections.Generic;

namespace Elecelf.Hibiki.Parser.SyntaxParser
{
    /// <summary>
    /// This interface is used to sign a class that can be used as a spearated front-end parser automata.
    /// For example, in a completed grammar each production is a IParseable.
    /// </summary>
    public interface IParseable
    {
        IState StartState { get; }
    }
}
