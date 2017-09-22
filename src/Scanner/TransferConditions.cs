using System;

namespace Elecelf.Hibiki.Scanner
{
    public abstract class TransferCondition
    {
        public abstract bool Pass(Token word, ScannerContext context);
    }

    public class EscapeTransferCondition : TransferCondition
    {
        public string EscapeLiteral { get; set; }

        public override bool Pass(Token word, ScannerContext context)
        {
            bool hasMatchList = context.EscapeMap.TryGetValue(EscapeLiteral, out var matchList);

            if (hasMatchList)
            {
                foreach(var item in matchList)
                    if (word.Literal == item)
                        return true;

                return false;
            }
            else
            {
                return false;
            }
        }
    }

    public class StringTransferCondition : TransferCondition
    {
        public String CompareReference
        {
            get; set;
        }

        public override bool Pass(Token word, ScannerContext context)
        {
            return this.CompareReference == word.Literal;
        }
    }

    public class SymolTransferCondition : TransferCondition
    {
        public Symol CompareReference
        {
            get; set;
        }

        public override bool Pass(Token word, ScannerContext context)
        {
            return CompareReference == word.Grammer.Symol;
        }
    }

    public class EpsilonTransferCondition:TransferCondition
    {
        public override bool Pass(Token token, ScannerContext context)
        {
            if (token.Grammer.Symol == context.SymolHost.GetSymol("eps"))
                return true;

            return false;
        }
    }
}