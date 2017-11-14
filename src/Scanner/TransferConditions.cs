using System;

namespace Elecelf.Hibiki.Parser
{
    public abstract class TransferCondition
    {
        public abstract bool Pass(Token word, ParserContext context);
    }

    public class EscapeTransferCondition : TransferCondition
    {
        public EscapeTransferCondition(string literal)
        {
            EscapeLiteral = literal;
        }

        public string EscapeLiteral { get; }

        public override bool Pass(Token word, ParserContext context)
        {
            var hasMatchList = context.EscapeMap.TryGetValue(EscapeLiteral, out var matchList);

            if (hasMatchList)
            {
                foreach(var item in matchList)
                    if (word.Literal == item)
                        return true;

                return false;
            }
            return false;
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

        public override bool Pass(Token word, ParserContext context)
        {
            return CompareReference == word.Literal;
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
    }

    public class SymolTransferCondition : TransferCondition
    {
        public SymolTransferCondition(Symol literal)
        {
            CompareReference = literal;
        }

        public Symol CompareReference
        {
            get;
        }

        public override bool Pass(Token word, ParserContext context)
        {
            return CompareReference == word.Grammer.Symol;
        }

        public static bool operator ==(SymolTransferCondition condition1, SymolTransferCondition condition2)
        {
            if (condition1 is null || condition2 is null) return false;
            return condition1.CompareReference == condition2.CompareReference;
        }

        public static bool operator !=(SymolTransferCondition condition1, SymolTransferCondition condition2)
        {
            return !(condition1 == condition2);
        }

        public override bool Equals(object obj)
        {
            if (obj is SymolTransferCondition condition) return condition.CompareReference == CompareReference;
            return false;
        }

        public override int GetHashCode()
        {
            return CompareReference.GetHashCode();
        }
    }

    public class EpsilonTransferCondition:TransferCondition
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

        public override bool Pass(Token token, ParserContext context)
        {
            if (token.Grammer.Symol == context.SymolHost.GetSymol("eps"))
                return true;

            return false;
        }
    }
}