using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Elecelf.Hibiki.Parser
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

    public partial class GrammarAutomata
    {
        private enum ParseBlockState
        {
            Escape,
            Outline,
            RawChar,
            Grammar,
            String,
            // used for OR operator and Kleen Star
            OR,
            KleenStar,
            // used to set a default group level of a group of tokens, 
            // void  parser gets a wrong initial group level if tokens start with a token with high group level.
            InitializeGroupLevel,
        }

        private struct ScannerToken
        {
            public string Literal;
            public ParseBlockState TransferType;
            public int GroupLevel;

            public override string ToString()
            {
                return $"{TransferType.ToString()} - {Literal}";
            }
        }

        public const char FinializeSymbol = (char)3;

        /// <summary>
        /// Start state of the automata.
        /// </summary>
        public readonly GrammarState StartState;

        public GrammarAutomata(Symol? automataSymol)
        {
            StartState = automataSymol != null ? new GrammarState(automataSymol.Value) : new GrammarState();
        }

        public GrammarAutomata()
        {
            StartState = new GrammarState();
        }

    }

    internal class GrammarStateTransferList : IList<GrammarTransfer>
    {
        private readonly HashSet<Symol> _notDirtySet = new HashSet<Symol>();
        private readonly List<GrammarTransfer> _innerList;

        public GrammarStateTransferList() => _innerList = new List<GrammarTransfer>();

        public GrammarStateTransferList(IEnumerable<GrammarTransfer> collection) => _innerList = new List<GrammarTransfer>(collection);

        public GrammarStateTransferList(int capacity) => _innerList = new List<GrammarTransfer>(capacity);

        public int IndexOf(GrammarTransfer item)
        {
            return _innerList.IndexOf(item);
        }

        public void Insert(int index, GrammarTransfer item)
        {
            _notDirtySet.Clear();
            _innerList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _notDirtySet.Clear();
            _innerList.RemoveAt(index);
        }

        public GrammarTransfer this[int index]
        {
            get => _innerList[index];
            set
            {
                _innerList[index] = value;
                _notDirtySet.Clear();
            }
        }

        public void Add(GrammarTransfer state)
        {
            _notDirtySet.Clear();
            _innerList.Add(state);
        }

        public void Clear()
        {
            _notDirtySet.Clear();
            _innerList.Clear();
        }

        public bool Contains(GrammarTransfer item)
        {
            return _innerList.Contains(item);
        }

        public void CopyTo(GrammarTransfer[] array, int arrayIndex)
        {
            _innerList.CopyTo(array, arrayIndex);
        }

        public bool Remove(GrammarTransfer state)
        {
            _notDirtySet.Clear();
            return _innerList.Remove(state);
        }

        public IEnumerator<GrammarTransfer> GetEnumerator()
        {
            return _innerList.GetEnumerator();
        }

        public int Count
        {
            get =>
                _innerList.Count;
        }

        public bool IsReadOnly
        {
            get => false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void ClearDirty(Symol symol)
        {
            _notDirtySet.Add(symol);
        }

        public bool IsDirty(Symol symol)
        {
            return !_notDirtySet.Contains(symol);
        }
    }

    public class GrammarState
    {
        private readonly GrammarStateTransferList _transfers = new GrammarStateTransferList();

        /// <summary>
        /// Transfers from this state.
        /// </summary>
        public IList<GrammarTransfer> Transfers => _transfers;

        private readonly  GrammarStateTransferList _backtraceTransfers = new GrammarStateTransferList();

        /// <summary>
        /// Transfers to this state.
        /// </summary>
        public IList<GrammarTransfer> Backtransfers => _backtraceTransfers;

        /// <summary>
        /// Is this state a terminal state?
        /// </summary>
        public bool SelfIsTerminal { set; get; } = false;

        public Symol? Symol { private set; get; } = null;

        public GrammarState(Symol symol)
        {
            Symol = symol;
        }

        public GrammarState()
        {
            
        }

        #region Basic overwrites
        public override string ToString()
        {
            return Symol == null ? "" : Symol.ToString();
        }
        #endregion

        /// <summary>
        /// Input a word to drive the state transfer to next state.
        /// </summary>
        /// <param name="token">Word to input to current state.</param>
        /// <param name="context">Context of grammar states.</param>
        /// <returns>Tuple: is transfer available and which state tranfering to.</returns>
        public (bool, GrammarState) InputWord(Token token, ParserContext context)
        {
            foreach (var grammarTransfer in _transfers)
            {
                var transferResult = grammarTransfer.InputWord(token, context);
                if(transferResult.Item1)
                    return transferResult;
            }

            return (false, null);
        }

        #region Accessibility Check
        private readonly Dictionary<Symol, bool> _accessibility = new Dictionary<Symol, bool>();

        /// <summary>
        /// Get accessibility state of a specied source.
        /// </summary>
        /// <param name="idSymol">A symol used to regnize source.</param>
        /// <returns>Accessibility</returns>
        public bool GetAccessibility(Symol idSymol)
        {
            if (!_accessibility.ContainsKey(idSymol))
                _accessibility[idSymol] = false;

            return _accessibility[idSymol];
        }

        /// <summary>
        /// Set accessibility state of a specied source.
        /// </summary>
        /// <param name="idSymol">A symol used to regnize source.</param>
        public void SetAccessibility(Symol idSymol)
        {
            _accessibility[idSymol] = true;
        }

        /// <summary>
        /// Clear accessibility state of a specied source.
        /// </summary>
        /// <param name="idSymol">>A symol used to regnize source.</param>
        public void RemoveAccessibility(Symol idSymol)
        {
            _accessibility.Remove(idSymol);
        }
        #endregion

        #region Join epsilon transfers' states
        private GrammarTransfer[] usableTransfers;
        private bool isTerminal = false;

        private void RebuildUsableTransfers()
        {
            lock (_transfers)
            {
                Symol accessed = new Symol("rut_accessed", 42u, this);
                if (usableTransfers == null || _transfers.IsDirty(accessed))
                {
                    _transfers.ClearDirty(accessed);

                    List<GrammarState> accessedStates = new List<GrammarState>();

                    Queue<GrammarState> states = new Queue<GrammarState>();
                    states.Enqueue(this);

                    isTerminal = SelfIsTerminal;

                    List<GrammarTransfer> usableTransfers = new List<GrammarTransfer>();

                    while (states.Count > 0)
                    {
                        var state = states.Dequeue();

                        if (state.GetAccessibility(accessed))
                            continue;

                        state.SetAccessibility(accessed);

                        isTerminal = isTerminal | state.SelfIsTerminal;

                        foreach (var transfer in state.Transfers)
                        {
                            if (transfer.TransferCondition is EpsilonTransferCondition)
                            {
                                states.Enqueue(transfer.TransfedState);
                            }
                            else
                            {
                                usableTransfers.Add(transfer);
                            }
                        }
                    }

                    this.usableTransfers = usableTransfers.ToArray();

                    foreach(var item in accessedStates)
                        item.RemoveAccessibility(accessed);
                }
            }
        }

        public GrammarTransfer[] UsableTransfers
        {
            get
            {
                RebuildUsableTransfers();
                return usableTransfers;
            }
        }

        public bool IsTerminal
        {
            get
            {
                RebuildUsableTransfers();
                return isTerminal;
            }
        }
        #endregion
    }

    public interface IGrammarAutomataSegment
    {
        GrammarState StartState { get; }
        GrammarState EndState { get; }
    }

    /// <summary>
    /// A sub automata is a part of a whole grammar automata.
    /// </summary>
    public class SubGrammarAutomata:IGrammarAutomataSegment
    {
        public enum SubGrammarAutomataType
        {
            Linear,
            OR,
        }

        public GrammarState StartState
        {
            get; set;
        }

        public GrammarState EndState
        {
            get; set;
        }

        public int GroupLevel;
    }

    public class GrammarTransfer
    {
        public GrammarTransfer(TransferCondition condition)
        {
            TransferCondition = condition;
        }

        public TransferCondition TransferCondition { get; }
        public GrammarState TransfedState { get; set; }
        public GrammarState BacktraceState { get; set; }

        public (bool, GrammarState) InputWord(Token token, ParserContext context)
        {
            return (TransferCondition.Pass(token, context), TransfedState);
        }

        public static bool operator ==(GrammarTransfer transfer1, GrammarTransfer transfer2)
        {
            if (transfer1 is null || transfer2 is null)
                return false;

            return transfer1.BacktraceState == transfer2.BacktraceState &&
                   transfer1.TransfedState == transfer2.TransfedState &&
                   transfer1.TransferCondition == transfer2.TransferCondition;
        }

        public static bool operator !=(GrammarTransfer transfer1, GrammarTransfer transfer2)
        {
            return !(transfer1 == transfer2);
        }

        public override bool Equals(object obj)
        {
            if (obj is GrammarTransfer transfer) return this == transfer;
            return false;
        }

        public override int GetHashCode()
        {
            return TransferCondition.GetHashCode();
        }
    }
}

/// Memo 2017-9-19
/// TODO: Trim on epsilon transfers and duplicated states.