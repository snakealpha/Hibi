using System;
using System.Collections;
using System.Collections.Generic;
using Elecelf.Hibiki.Parser.SyntaxParser;

namespace Elecelf.Hibiki.Parser.GrammarGraph
{
    public class Grammar
    {
        public readonly Symbol Symbol;

        public Grammar(Symbol symbol)
        {
            Symbol = symbol;
        }

        public List<GrammarAutomata> Grammars { get; } = new List<GrammarAutomata>();
    }

    public class GraphToken
    {
        public string Literal { set; get; }
        public Grammar Grammer { set; get; }
    }

    public partial class GrammarAutomata : IParseable
    {


        private enum ParseBlockState
        {
            Escape,
            Outline,
            RawChar,
            Grammar,
            String,
            // used for OR operator and Kleen Star
            Or,
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
                return $"{TransferType} - {Literal}";
            }
        }

        public const char FinializeSymbol = (char)3;

        /// <summary>
        /// Start state of the automata.
        /// </summary>
        public readonly GrammarState StartState;

        IState IParseable.StartState { get=>StartState;}

        public GrammarAutomata(Symbol? automataSymbol)
        {
            StartState = automataSymbol != null ? new GrammarState(automataSymbol.Value) : new GrammarState();
        }

        public GrammarAutomata()
        {
            StartState = new GrammarState();
        }
    }

    internal class GrammarStateTransferList : IList<GrammarTransfer>
    {
        private readonly HashSet<Symbol> _notDirtySet = new HashSet<Symbol>();
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

        public int Count => _innerList.Count;

        public bool IsReadOnly => false;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void ClearDirty(Symbol symbol)
        {
            _notDirtySet.Add(symbol);
        }

        public bool IsDirty(Symbol symbol)
        {
            return !_notDirtySet.Contains(symbol);
        }
    }

    public class GrammarState : IState
    {
        private readonly GrammarStateTransferList _transfers = new GrammarStateTransferList();

        /// <summary>
        /// Transfers from this state.
        /// </summary>
        public IList<GrammarTransfer> Transfers => _transfers;

        IEnumerable<ITransfer> IState.Transfers => _transfers;

        private readonly  GrammarStateTransferList _backtraceTransfers = new GrammarStateTransferList();

        /// <summary>
        /// Transfers to this state.
        /// </summary>
        public IList<GrammarTransfer> Backtransfers => _backtraceTransfers;

        /// <summary>
        /// Is this state a terminal state?
        /// </summary>
        public bool SelfIsTerminal { set; get; }

        public Symbol? Symbol { get; }

        public GrammarState(Symbol symbol)
        {
            Symbol = symbol;
        }

        public GrammarState()
        {
            
        }

        #region Basic overwrites
        public override string ToString()
        {
            return Symbol == null ? "" : Symbol.ToString();
        }
        #endregion
        
        #region Accessibility Check
        private readonly Dictionary<Symbol, bool> _accessibility = new Dictionary<Symbol, bool>();

        /// <summary>
        /// Get accessibility state of a specied source.
        /// </summary>
        /// <param name="idSymbol">A symbol used to regnize source.</param>
        /// <returns>Accessibility</returns>
        public bool GetAccessibility(Symbol idSymbol)
        {
            if (!_accessibility.ContainsKey(idSymbol))
                _accessibility[idSymbol] = false;

            return _accessibility[idSymbol];
        }

        /// <summary>
        /// Set accessibility state of a specied source.
        /// </summary>
        /// <param name="idSymbol">A symbol used to regnize source.</param>
        public void SetAccessibility(Symbol idSymbol)
        {
            _accessibility[idSymbol] = true;
        }

        /// <summary>
        /// Clear accessibility state of a specied source.
        /// </summary>
        /// <param name="idSymbol">>A symbol used to regnize source.</param>
        public void RemoveAccessibility(Symbol idSymbol)
        {
            _accessibility.Remove(idSymbol);
        }
        #endregion

        #region Join epsilon transfers' states
        private GrammarTransfer[] _usableTransfers;
        private bool _isTerminal;

        private void RebuildUsableTransfers()
        {
            lock (_transfers)
            {
                Symbol accessed = new Symbol("rut_accessed", 1001u, this);
                if (_usableTransfers == null || _transfers.IsDirty(accessed))
                {
                    _transfers.ClearDirty(accessed);

                    List<GrammarState> accessedStates = new List<GrammarState>();

                    Queue<GrammarState> states = new Queue<GrammarState>();
                    states.Enqueue(this);

                    _isTerminal = SelfIsTerminal;

                    List<GrammarTransfer> usableTransfers = new List<GrammarTransfer>();

                    while (states.Count > 0)
                    {
                        var state = states.Dequeue();

                        if (state.GetAccessibility(accessed))
                            continue;

                        state.SetAccessibility(accessed);

                        _isTerminal = _isTerminal | state.SelfIsTerminal;

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

                    this._usableTransfers = usableTransfers.ToArray();

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
                return _usableTransfers;
            }
        }

        public bool IsTerminal
        {
            get
            {
                RebuildUsableTransfers();
                return _isTerminal;
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

    public class GrammarTransfer : ITransfer
    {
        public GrammarTransfer(TransferCondition condition)
        {
            TransferCondition = condition;
        }

        public TransferCondition TransferCondition { get; }
        public GrammarState TransfedState { get; set; }
        public GrammarState BacktraceState { get; set; }

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

        ISyntaxElement ITransfer.SyntaxElement => TransferCondition;
        IState ITransfer.TransfedState => TransfedState;
    }
}