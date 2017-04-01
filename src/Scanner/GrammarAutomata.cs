using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elecelf.Hibiki.Scanner
{
    public class Grammar
    {
        public readonly Symol Symol;

        public Grammar(Symol symol)
        {
            Symol = symol;
        }

        private readonly List<GrammarAutomata> _grammars = new List<GrammarAutomata>();
        public List<GrammarAutomata> Grammars => _grammars;
    }

    public class Token
    {
        public string Literal { set; get; }
        public Grammar Grammer { set; get; }
    }

    public class GrammarAutomata
    {
        /// <summary>
        /// Start state of the automata.
        /// </summary>
        public readonly GrammarState StartState = new GrammarState();

        /// <summary>
        /// Parse a string to a automata.
        /// </summary>
        /// <param name="rawString">String to be parsed.</param>
        /// <returns>Parsed automata.</returns>
        public static GrammarAutomata Parse(string rawString)
        {
            //throw new NotImplementedException();

            if(string.IsNullOrEmpty(rawString))
                throw new ParseErrorException("Raw String is null or empty.");

            // Look-around 1 char
            char? currentChar = rawString[0];
            char? lookaroundChar = rawString.Length > 1 ? (char?)rawString[1] : null;
            bool isInGrammarBlock = false;
            bool isInEscapeBlock = false;
            Queue<char?> holdingChars = new Queue<char?>();

            uint lookAroundPoint = 1;
            for (; lookAroundPoint <= rawString.Length; lookAroundPoint++)
            {

            }
            
            return null;
        }
    }

    public class GrammarState
    {
        private readonly ICollection<GrammarTransfer> _transfers = new List<GrammarTransfer>();

        /// <summary>
        /// Transfers from this state.
        /// </summary>
        public ICollection<GrammarTransfer> Transfers => _transfers;

        /// <summary>
        /// Input a word to drive the state transfer to next state.
        /// </summary>
        /// <param name="token">Word to input to current state.</param>
        /// <returns>Tuple: is transfer available and which state tranfering to.</returns>
        public (bool, GrammarState) InputWord(Token token, ScannerContext context)
        {
            foreach (var grammarTransfer in _transfers)
            {
                var transferResult = grammarTransfer.InputWord(token, context);
                if(transferResult.Item1)
                    return transferResult;
            }

            return (false, null);
        }
    }

    public class GrammarTransfer
    {
        public TransferCondition TransferCondition { get; set; }
        public GrammarState TransfedState { get; set; }

        public (bool, GrammarState) InputWord(Token token, ScannerContext context)
        {
            return (TransferCondition.Pass(token, context), TransfedState);
        }
    }

    public abstract class TransferCondition
    {
        public abstract bool Pass(Token word, ScannerContext context);
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

    public class SymolTransferCondition : TransferCondition
    {
        public Symol CompareReference
        {
            get; set;
        }

        public override bool Pass(Token word, ScannerContext context)
        {
            return this.CompareReference == word.Grammer.Symol;
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

    // Exceptions

    public class ParseErrorException : Exception
    {
        public ParseErrorException(string info) : base(info)
        {

        }
    }
}
