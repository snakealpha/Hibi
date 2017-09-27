using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;

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

        /// <summary>
        /// Parse a string to a automata.
        /// </summary>
        /// <param name="rawString">String to be parsed.</param>
        /// <param name="context">Parse context.</param>
        /// <returns>Parsed automata.</returns>
        public static GrammarAutomata Parse(string rawString, ScannerContext context, string grammarName = null)
        {
            var automata = grammarName != null
                ? new GrammarAutomata(context.SymolHost.GetSymol(grammarName + "_Start"))
                : new GrammarAutomata();

            // Phase 1: Make string to tokens and make the collection of tokens.
            var tokens = ParseString(rawString);

            // Insert a token to set a initial group level
            tokens.Insert(0, new ScannerToken()
            {
                GroupLevel = 0,
                Literal = @"/",
                TransferType = ParseBlockState.InitializeGroupLevel
            });

            // Phase 2: Make tokens to automata.
            var (segment, _) = ParseTokens(tokens, 0, automata.StartState, context, grammarName);

            TrimAutomata(automata.StartState);

            return automata;
        }
        private static IList<ScannerToken> ParseString(string rawString)
        {
            if (string.IsNullOrEmpty(rawString))
                throw new ParseErrorException("Raw String is null or empty.");

            // Look-around 1 char
            char? currentChar = rawString[0];
            char? lookaroundChar = rawString.Length > 1 ? (char?)rawString[1] : null;
            var currentBlockState = ParseBlockState.Outline;
            Queue<char?> holdingChars = new Queue<char?>();


            List<ScannerToken> tokens = new List<ScannerToken>();
            int currentGroupLevel = 0;

            
            uint lookAroundPoint = 2;
            for (; lookAroundPoint <= rawString.Length + 1; lookAroundPoint++)
            {
                // Outline:
                // On the start state of a automata, or after ending a grammar or a escape block, it's unable to decide a speical state of the automata. It's outline then.
                // Outline state is a Start State and a Finalize State in a grammar automata.
                if (currentBlockState == ParseBlockState.Outline)
                {
                    // Raw char "@w" equals to char 'w' no matter w is any key word
                    if (currentChar == '@')
                        currentBlockState = ParseBlockState.RawChar;
                    // Escape format as "%escape word%"
                    else if (currentChar == '%')
                    {
                        currentBlockState = ParseBlockState.Escape;
                    }
                    // Grammar format as "{Grammar Name}"
                    else if (currentChar == '{')
                    {
                        currentBlockState = ParseBlockState.Grammar;
                    }
                    // Flow control: branch
                    else if (currentChar == '|')
                    {
                        tokens.Add(new ScannerToken()
                        {
                            GroupLevel = currentGroupLevel,
                            TransferType = ParseBlockState.OR,
                        });
                    }
                    // Flow control: kleen star
                    else if (currentChar == '*')
                    {
                        var lastToken = tokens[tokens.Count - 1];
                        if (lastToken.GroupLevel == currentGroupLevel)
                            lastToken.GroupLevel = currentGroupLevel + 1;

                        tokens.Add(new ScannerToken()
                        {
                            GroupLevel = currentGroupLevel,
                            TransferType = ParseBlockState.KleenStar,
                        });
                    }
                    // Change group level which is used to recongnize sub group
                    else if (currentChar == '(')
                    {
                        currentGroupLevel++;
                    }
                    else if (currentChar == ')')
                    {
                        if (currentGroupLevel == 0)
                            throw new ParseErrorException("Group dose not have a begin position.");

                        currentGroupLevel--;
                    }
                    // Other: string
                    else
                    {
                        holdingChars.Enqueue(currentChar);
                        currentBlockState = ParseBlockState.String;
                    }

                }
                else if (currentBlockState == ParseBlockState.Escape)
                {
                    if (currentChar == '%')
                    {
                        var holdingString = MakeStringFromQueue(holdingChars);
                        holdingChars.Clear();

                        tokens.Add(new ScannerToken()
                        {
                            GroupLevel = currentGroupLevel,
                            TransferType = ParseBlockState.Escape,
                            Literal = holdingString,
                        });

                        currentBlockState = ParseBlockState.Outline;
                    }
                    else
                    {
                        holdingChars.Enqueue(currentChar);
                    }
                }
                else if (currentBlockState == ParseBlockState.Grammar)
                {
                    if (currentChar == '}')
                    {
                        var holdingString = MakeStringFromQueue(holdingChars);
                        holdingChars.Clear();

                        tokens.Add(new ScannerToken()
                        {
                            GroupLevel = currentGroupLevel,
                            TransferType = ParseBlockState.Grammar,
                            Literal = holdingString,
                        });

                        currentBlockState = ParseBlockState.Outline;
                    }
                    else
                    {
                        holdingChars.Enqueue(currentChar);
                    }
                }
                else if (currentBlockState == ParseBlockState.String || currentBlockState == ParseBlockState.RawChar)
                {
                    if (currentBlockState == ParseBlockState.String && currentChar == '@')
                        currentBlockState = ParseBlockState.RawChar;
                    else
                    {
                        if (currentBlockState == ParseBlockState.String)
                        {
                            // While a string end with a kleen star, only last char should be repeated.
                            // Since then, last char should be splited from the string, and make a string to two different string transfers.
                            if (lookaroundChar == '*')
                            {
                                var holdingString = MakeStringFromQueue(holdingChars);
                                holdingChars.Clear();

                                tokens.Add(new ScannerToken()
                                {
                                    GroupLevel = currentGroupLevel + 1,
                                    TransferType = ParseBlockState.String,
                                    Literal = holdingString,
                                });
                            }
                        }

                        holdingChars.Enqueue(currentChar);

                        if (lookaroundChar == '{' ||
                            lookaroundChar == '%' ||
                            lookaroundChar == '|' ||
                            lookaroundChar == '*' ||
                            lookaroundChar == '(' ||
                            lookaroundChar == ')')
                        {
                            var holdingString = MakeStringFromQueue(holdingChars);
                            holdingChars.Clear();
                            tokens.Add(new ScannerToken()
                            {
                                GroupLevel = currentGroupLevel,
                                TransferType = ParseBlockState.String,
                                Literal = holdingString,
                            });

                            currentBlockState = ParseBlockState.Outline;
                        }
                    }
                }

                // End of char processing.
                currentChar = lookaroundChar;

                if (lookAroundPoint == rawString.Length)
                {
                    // A ETX char used to sign end of a automation. Also close a string state.
                    lookaroundChar = FinializeSymbol;
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
            }

            // cost all chars:
            if (currentChar == FinializeSymbol || currentChar == null)
            {
                // holding chars are not empty: throw exception.
                if (holdingChars.Count > 0)
                {
                    var holdingCharsString = MakeStringFromQueue(holdingChars);

                    throw new ParseErrorException("Some chars are not included in a legal state.", "Illegal String", holdingCharsString);
                }
            }

            return tokens;
        }

        /// <summary>
        /// Construct a part of grammar automata from a collection of tokens.
        /// </summary>
        /// <param name="tokens">Tokens to be parsed.</param>
        /// <param name="startPosition">Start position of this sub automata in token list.</param>
        /// <param name="sourceState">Start state of this grammar automata.</param>
        /// <returns>1- Sub automata parsed from this token collection; 2- Start position of next state of current state.</returns>
        private static (SubGrammarAutomata, int) ParseTokens(IList<ScannerToken> tokens, int startPosition, GrammarState sourceState, ScannerContext context, string grammarName)
        {
            var baseGroupLevel = tokens[startPosition].GroupLevel;
            var currentPosition = startPosition;
            var currentState = sourceState;
            GrammarState orEndState = null;

            // record last block's start and end states, may be used by a kleen star.
            GrammarState lastBlockStartState = null;
            GrammarState lastBlockEndState = null;

            var currentSubAutomata = new SubGrammarAutomata()
            {
                StartState = sourceState,
                EndState = currentState,
                GroupLevel = baseGroupLevel,
            };

            var currentSubAutomataType = SubGrammarAutomata.SubGrammarAutomataType.Linear;

            while (currentPosition<tokens.Count)
            {
                var currentToken = tokens[currentPosition];

                if (currentToken.GroupLevel > baseGroupLevel)
                {
                    // Token has higher group level: this token is in a inner layer of group.
                    // Get a sub automata of later tokens.
                    (SubGrammarAutomata subAutomata, int newPosition) =
                    ParseTokens(tokens, currentPosition, currentState, context, grammarName);

                    currentPosition = newPosition;
                    currentState = subAutomata.EndState;
                    currentSubAutomata.EndState = subAutomata.EndState;

                    lastBlockStartState = subAutomata.StartState;
                    lastBlockEndState = subAutomata.EndState;

                    // Immediately apply new currentPosition
                    continue;
                }
                else if (currentToken.GroupLevel<baseGroupLevel)
                {
                    // Token has lower group level: current group is ended.
                    // Return current parsed automata.

                    // If it is a branch automata, complete this automata firstly.
                    if (currentSubAutomataType == SubGrammarAutomata.SubGrammarAutomataType.OR)
                    {
                        currentState = TransferState(
                            EpsilonTransferCondition.Instance,
                            currentState,
                            orEndState);
                    }

                    return (new SubGrammarAutomata()
                    {
                        StartState = sourceState,
                        EndState = currentState,
                        GroupLevel = baseGroupLevel,
                    }, currentPosition);
                }
                else
                {
                    // process current token

                    // Linear Automata
                    if (currentToken.TransferType == ParseBlockState.Escape)
                    {
                        lastBlockStartState =  currentState;

                        currentState = TransferState(
                            new EscapeTransferCondition(currentToken.Literal), 
                            currentState,
                            newStateSymol:context.SymolHost.GetSymol("State_"+context.GetNextStateIndex()));
                        currentSubAutomata.EndState = currentState;

                        lastBlockEndState = currentState;
                    }
                    else if (currentToken.TransferType == ParseBlockState.Grammar)
                    {
                        lastBlockStartState = currentState;

                        currentState = TransferState(
                            new SymolTransferCondition(context.SymolHost.GetSymol(currentToken.Literal)), 
                            currentState,
                            newStateSymol:context.SymolHost.GetSymol("State_"+context.GetNextStateIndex()));
                        currentSubAutomata.EndState = currentState;

                        lastBlockEndState = currentState;
                    }
                    else if (currentToken.TransferType == ParseBlockState.String)
                    {
                        lastBlockStartState = currentState;

                        currentState = TransferState(
                            new StringTransferCondition(currentToken.Literal),
                            currentState,
                            newStateSymol:context.SymolHost.GetSymol("State_"+context.GetNextStateIndex()));
                        currentSubAutomata.EndState = currentState;

                        lastBlockEndState = currentState;
                    }
                    // Branch or kleen star Automata
                    else if (currentToken.TransferType == ParseBlockState.KleenStar)
                    {
                        System.Diagnostics.Debug.Assert(lastBlockStartState!=null, "Kleen Star Cannot be first token in a grammar.");
                        
                        TransferState(
                            EpsilonTransferCondition.Instance,
                            lastBlockStartState,
                            currentSubAutomata.EndState);
                        TransferState(
                            EpsilonTransferCondition.Instance,
                            lastBlockEndState,
                            lastBlockStartState);
                    }
                    else if (currentToken.TransferType == ParseBlockState.OR)
                    {
                        currentSubAutomataType = SubGrammarAutomata.SubGrammarAutomataType.OR;
                        if(orEndState == null)
                            orEndState = new GrammarState(context.SymolHost.GetSymol("OR-End_State_"+context.GetNextStateIndex()));

                        TransferState(
                            EpsilonTransferCondition.Instance,
                            currentState,
                            orEndState);

                        currentState = sourceState;
                        currentSubAutomata = new SubGrammarAutomata()
                        {
                            StartState = sourceState,
                            EndState = currentState,
                            GroupLevel = baseGroupLevel,
                        };
                    }
                }

                // Add current position
                currentPosition++;
            }

            if (currentSubAutomataType == SubGrammarAutomata.SubGrammarAutomataType.OR)
            {
                currentState = TransferState(
                    EpsilonTransferCondition.Instance,
                    currentState,
                    orEndState);
            }

            // Return when used up all tokens.
            currentState.SelfIsTerminal = true;
            return (new SubGrammarAutomata()
            {
                StartState = sourceState,
                EndState = currentState,
                GroupLevel = baseGroupLevel,
            }, currentPosition);
        }

        /// <summary>
        /// Trim epsilon transfers and duplicated states.
        /// </summary>
        /// <param name="startState">Start state of automata to be trimmed. Only states linked after this state will be trimmed.</param>
        private static void TrimAutomata(GrammarState startState)
        {
            // TODO: Trim Method
        }


        /// <summary>
        /// Remove a transfer. Note that if any other transfers are equipotential to current transfer, they'll be removed all the same.
        /// </summary>
        /// <param name="transfer">Transfer which will be removed from automata.</param>
        private static void RemoveTransfer(GrammarTransfer transfer)
        {
            var removeList = new List<GrammarTransfer>();

            // Remove transfer from backtrace state.
            for(var i = 0; i != transfer.BacktraceState.Transfers.Count;i++)
                if(transfer.BacktraceState.Transfers[i]==transfer)
                    removeList.Add(transfer.BacktraceState.Transfers[i]);
            foreach (var grammarTransfer in removeList)
            {
                transfer.BacktraceState.Transfers.Remove(grammarTransfer);
            }

            // Remove transfer from transfered state.
            for (var i = 0; i != transfer.TransfedState.Transfers.Count; i++)
                if (transfer.TransfedState.Transfers[i] == transfer)
                    removeList.Add(transfer.TransfedState.Transfers[i]);
            foreach (var grammarTransfer in removeList)
            {
                transfer.TransfedState.Transfers.Remove(grammarTransfer);
            }
        }

        /// <summary>
        /// Trim redundant transfers between two states, left only one.
        /// </summary>
        /// <param name="transfer"></param>
        private static void TrimRedundantTransfer(GrammarTransfer transfer)
        {
            RemoveTransfer(transfer);
            transfer.BacktraceState.Transfers.Add(transfer);
            transfer.TransfedState.Backtransfers.Add(transfer);
        }

        private static GrammarState TransferState(
            TransferCondition condition, 
            GrammarState currentState,
            GrammarState targetState = null,
            Symol? newStateSymol = null)
        {
            var transferCondition = condition;
            var newState = targetState ?? (newStateSymol == null? new GrammarState():new GrammarState(newStateSymol.Value));
            var newTransfer = new GrammarTransfer(transferCondition)
            {
                TransfedState = newState,
                BacktraceState = currentState
            };
            currentState.Transfers.Add(newTransfer);
            newState.Backtransfers.Add(newTransfer);
            return newState;
        }

        private static string MakeStringFromQueue(IEnumerable<char?> queue)
        {
            var holdingCharsArray = (from c in queue where c.HasValue && c.Value != FinializeSymbol select c.Value).ToArray();
            return new string(holdingCharsArray);
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

        public (bool, GrammarState) InputWord(Token token, ScannerContext context)
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