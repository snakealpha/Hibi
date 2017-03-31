using System;
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

    public class Word
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
            throw new NotImplementedException();
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
        /// <param name="word">Word to input to current state.</param>
        /// <returns>Tuple: is transfer available and which state tranfering to.</returns>
        public (bool, GrammarState) InputWord(Word word, ScannerContext context)
        {
            foreach (var grammarTransfer in _transfers)
            {
                var transferResult = grammarTransfer.InputWord(word, context);
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

        public (bool, GrammarState) InputWord(Word word, ScannerContext context)
        {
            return (TransferCondition.Pass(word, context), TransfedState);
        }
    }

    public abstract class TransferCondition
    {
        public abstract bool Pass(Word word, ScannerContext context);
    }

    public class EpsilonTransferCondition:TransferCondition
    {
        public override bool Pass(Word word, ScannerContext context)
        {
            if (word.Grammer.Symol == context.SymolHost.GetSymol("eps"))
                return true;

            return false;
        }
    }

    //public class StringTransferCondition : TransferCondition
    //{
    //    public string CompareReference { get; set; }

    //    public override bool Pass(Word word, ScannerContext context)
    //    {
    //        return this.CompareReference == word.
    //    }
    //}
}
