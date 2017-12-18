using System;
using System.Collections;
using System.Collections.Generic;
using Elecelf.Hibiki.Parser.GrammarGraph;

namespace Elecelf.Hibiki.Parser
{
    public static class LegitimacyCheck
    {
        /// <summary>
        /// Hibiki-Parser is a LL(1) parser. Left-recursion must be avoid. This method is used to check if a automata is a left-recursion one.
        /// </summary>
        /// <param name="context">Context of automata.</param>
        /// <param name="automata">Automata you wanna check.</param>
        /// <returns>Is automata a left-recursion automata.</returns>
        public static bool LeftRecursion(ParserContext context, GrammarAutomata automata)
        {
            throw new LeftRecursionExceprion("Check Left Recursion here. Since Hibiki-Parser is a LL(1) parser, left recursion is not allowed. should be transfer to other formal.");
        }
    }

    public class ParseErrorException : Exception
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();
        public sealed override IDictionary Data => _data;

        public ParseErrorException(string info) : base(info)
        {

        }

        public ParseErrorException(string info, string dataName, object dataValue) : base(info)
        {
            Data[dataName] = dataValue;
        }
    }

    /// <summary>
    /// This type of exception is used to sign a grammar error.
    /// </summary>
    public class GrammarLegitimacyException : Exception
    {
        public GrammarLegitimacyException(string message) : base(message)
        {
            
        }
    }

    /// <summary>
    /// Hibiki's front-end parser is a table driven LL(1) parser.
    /// In a LL parser, left-recursion is not allowed.
    /// </summary>
    public class LeftRecursionExceprion : GrammarLegitimacyException
    {
        public LeftRecursionExceprion(string message) : base(message)
        {
            
        }
    }

    /// <summary>
    /// This type is used to sign some logic error inside the parser.
    /// </summary>
    public class ParserInnerException : Exception
    {
        public ParserInnerException(string message) : base(message)
        {
            
        }
    }
}