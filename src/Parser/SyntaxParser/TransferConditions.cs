using System;
using Elecelf.Hibiki.Parser.SyntaxParser;

namespace Elecelf.Hibiki.Parser.GrammarGraph
{
    public abstract class TransferCondition : ISyntaxElement
    {
        public abstract (bool finished, bool success, ErrorInfo errorInfo) PassChar(char input, int offset, ParserContext context);
        public abstract ISyntaxElement GetThisElement(string literal, ParserContext context);
    }

    public class EscapeTransferCondition : TransferCondition
    {
        public EscapeTransferCondition(string literal)
        {
            EscapeLiteral = literal;
        }

        public string EscapeLiteral { get; }

        public override (bool finished, bool success, ErrorInfo errorInfo) PassChar(char input, int offset, ParserContext context)
        {
            if (context.EscapeMap.TryGetValue(EscapeLiteral, out var list))
            {
                foreach (var character in list)
                {
                    if (input == character) return (true, true, null);
                }
                return (true, false, new ErrorInfo("No Escape Matched."));
            }
            else
            {
                return (true, false, new ErrorInfo("Escape has not defined."));
            }
        }

        public override ISyntaxElement GetThisElement(string literal, ParserContext context)
        {
            return new EscapeTransferCondition(literal);
        }

        public static bool operator ==(EscapeTransferCondition condition1, EscapeTransferCondition condition2)
        {
            if (condition1 is null || condition2 is null) return false;
            return condition1.EscapeLiteral == condition2.EscapeLiteral;
        }

        public static bool operator !=(EscapeTransferCondition condition1, EscapeTransferCondition condition2)
        {
            return !(condition1 == condition2);
        }

        public override bool Equals(object obj)
        {
            if (obj is EscapeTransferCondition condition) return condition.EscapeLiteral == EscapeLiteral;
            return false;
        }

        public override int GetHashCode()
        {
            return EscapeLiteral.GetHashCode();
        }
    }

    public class StringTransferCondition : TransferCondition
    {
        public StringTransferCondition(string literal)
        {
            CompareReference = literal;
        }

        public String CompareReference
        {
            get;
        }

        public override (bool finished, bool success, ErrorInfo errorInfo) PassChar(char input, int offset, ParserContext context)
        {
            if (CompareReference[offset] == input)
            {
                if (offset == CompareReference.Length - 1)
                    return (true, true, null);
                return (false, true, null);
            }

            return (true, false, new ErrorInfo("Syntax Error: String Not Match."));
        }

        public static bool operator ==(StringTransferCondition condition1, StringTransferCondition condition2)
        {
            if (condition1 is null || condition2 is null) return false;
            return condition1.CompareReference == condition2.CompareReference;
        }

        public static bool operator !=(StringTransferCondition condition1, StringTransferCondition condition2)
        {
            return !(condition1 == condition2);
        }

        public override bool Equals(object obj)
        {
            if (obj is StringTransferCondition condition) return condition.CompareReference == CompareReference;
            return false;
        }

        public override int GetHashCode()
        {
            return CompareReference.GetHashCode();
        }

        public override ISyntaxElement GetThisElement(string literal, ParserContext context)
        {
            return new StringTransferCondition(literal);
        }
    }

    public class SymbolTransferCondition : TransferCondition, IParseAsSymbol
    {
        public SymbolTransferCondition(Symbol literal)
        {
            CompareReference = literal;
        }

        public Symbol CompareReference
        {
            get;
        }

        public override (bool finished, bool success, ErrorInfo errorInfo) PassChar(char input, int offset, ParserContext context)
        {
            throw new ParserInnerException(@"Parser Error: A symbol transfer cannot be transfered directly.");
        }

        public override ISyntaxElement GetThisElement(string literal, ParserContext context)
        {
            return new SymbolTransferCondition(context.SymbolHost.GetSymol(literal));
        }

        public Symbol SymbolIdentity
        {
            get => CompareReference;
        }

        public static bool operator ==(SymbolTransferCondition condition1, SymbolTransferCondition condition2)
        {
            if (condition1 is null || condition2 is null) return false;
            return condition1.CompareReference == condition2.CompareReference;
        }

        public static bool operator !=(SymbolTransferCondition condition1, SymbolTransferCondition condition2)
        {
            return !(condition1 == condition2);
        }

        public override bool Equals(object obj)
        {
            if (obj is SymbolTransferCondition condition) return condition.CompareReference == CompareReference;
            return false;
        }

        public override int GetHashCode()
        {
            return CompareReference.GetHashCode();
        }
    }

    public class EpsilonTransferCondition:TransferCondition, IParserAsEpsilon
    {
        private EpsilonTransferCondition()
        { }

        private static EpsilonTransferCondition _instance;
        public static EpsilonTransferCondition Instance
        {
            get
            {
                _instance = _instance ?? new EpsilonTransferCondition();
                return _instance;
            }
        }

        public override (bool finished, bool success, ErrorInfo errorInfo) PassChar(char input, int offset, ParserContext context)
        {
            throw new ParserInnerException(@"Parser Error: A symbol transfer cannot be transfered directly.");
        }

        public override ISyntaxElement GetThisElement(string literal, ParserContext context)
        {
            return _instance;
        }
    }
}