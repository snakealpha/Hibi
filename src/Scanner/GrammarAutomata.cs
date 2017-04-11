using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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
        private enum ParseBlockState
        {
            Grammar,
            Escape,
            String,
            Outline,
        }

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
            var currentBlockState = ParseBlockState.Outline;
            Queue<char?> holdingChars = new Queue<char?>();

            var automata = new GrammarAutomata();
            var currentState = automata.StartState;
            Stack<GrammarState> grammarStates = new Stack<GrammarState>();

            uint lookAroundPoint = 1;
            for (; lookAroundPoint <= rawString.Length; lookAroundPoint++)
            {
                // Outline:
                // On the start state of a automata, or after ending a grammar or a escape block, it's unable to decide a speical state of the automata. It's outline then.
                // Outline state is a Start State and a Finalize State in a grammar automata.
                if (currentBlockState == ParseBlockState.Outline)
                {

                }
                else if (currentBlockState == ParseBlockState.Escape)
                {

                }
                else if (currentBlockState == ParseBlockState.Grammar)
                {

                }
                else if (currentBlockState == ParseBlockState.String)
                {

                }

                // End of char processing.
                currentChar = lookaroundChar;

                if (lookAroundPoint == rawString.Length)
                {
                    // A ETX char used to sign end of a automation. Also close a string state.
                    lookaroundChar = (char?)3;
                }
                else if (lookAroundPoint < rawString.Length)
                {
                    lookaroundChar = rawString[(int)lookAroundPoint];
                }
                else
                {
                    lookaroundChar = null;
                    break;
                }

                // Cost off all chars:
                if (currentChar == null)
                {
                    // holding chars are not empty: throw exception.
                    if (holdingChars.Count > 0)
                    {
                        var holdingCharsArray = (from c in holdingChars where c.HasValue && c.Value != (char)3 select c.Value).ToArray();
                        var holdingCharsString = new string(holdingCharsArray);

                        throw new ParseErrorException("Some chars are not included in a legal state.", "Illegal String", holdingCharsString);
                    }

                    // or, parse completed.
                    break;
                }
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
}
