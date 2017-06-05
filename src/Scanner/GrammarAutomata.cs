using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;

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
        }

        private struct ScannerToken
        {
            public string Literal;
            public ParseBlockState TransferType;
            public int GroupLevel;
        }

        public const char FinializeSymbol = (char)3;

        /// <summary>
        /// Start state of the automata.
        /// </summary>
        public readonly GrammarState StartState = new GrammarState();

        /// <summary>
        /// Parse a string to a automata.
        /// </summary>
        /// <param name="rawString">String to be parsed.</param>
        /// <param name="context">Parse context.</param>
        /// <returns>Parsed automata.</returns>
        public static GrammarAutomata Parse(string rawString, bool trimEpsilon, ScannerContext context)
        {
            var automata = new GrammarAutomata();

            // Phase 1: Make string to tokens and make the collection of tokens.
            var tokens = ParseString(rawString);

            // Phase 2: Make tokens to automata.
            var (segment, _) = ParseTokens(tokens, 0, automata.StartState, context);

            //// Phase3: Trim epsilon transfers from automata.
            //if(trimEpsilon)
            //    TrimEpsilon(segment, context);

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

            
            uint lookAroundPoint = 1;
            for (; lookAroundPoint <= rawString.Length; lookAroundPoint++)
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
                else if (currentBlockState == ParseBlockState.RawChar)
                {
                    holdingChars.Enqueue(currentChar);
                    currentBlockState = ParseBlockState.String;
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
                else if (currentBlockState == ParseBlockState.String)
                {
                    if (currentChar == '@')
                        currentBlockState = ParseBlockState.RawChar;
                    else
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

                // cost all chars:
                if (currentChar == null)
                {
                    // holding chars are not empty: throw exception.
                    if (holdingChars.Count > 0)
                    {
                        var holdingCharsString = MakeStringFromQueue(holdingChars);

                        throw new ParseErrorException("Some chars are not included in a legal state.", "Illegal String", holdingCharsString);
                    }

                    // or, parse completed.
                    break;
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
        private static (SubGrammarAutomata, int) ParseTokens(IList<ScannerToken> tokens, int startPosition, GrammarState sourceState, ScannerContext context)
        {
            var baseGroupLevel = tokens[startPosition].GroupLevel;
            var currentPosition = startPosition;
            ScannerToken currentToken;
            var currentState = sourceState;
            GrammarState orEndState = null;

            var currentSubAutomata = new SubGrammarAutomata()
            {
                StartState = sourceState,
                EndState = currentState,
                GroupLevel = baseGroupLevel,
            };

            var currentSubAutomataType = SubGrammarAutomata.SubGrammarAutomataType.Linear;

            while (currentPosition<tokens.Count)
            {
                currentToken = tokens[currentPosition];

                if (currentToken.GroupLevel > baseGroupLevel)
                {
                    // Token has higher group level: this token is in a inner layer of group.
                    // Get a sub automata of later tokens.
                    (SubGrammarAutomata subAutomata, int newPosition) =
                    ParseTokens(tokens, currentPosition, currentState, context);

                    currentPosition = newPosition;
                    currentState = subAutomata.EndState;
                    currentSubAutomata.EndState = subAutomata.EndState;
                }
                else if (currentToken.GroupLevel<baseGroupLevel)
                {
                    // Token has lower group level: current group is ended.
                    // Return current parsed automata.

                    // If it is a branch automata, complete this automata firstly.
                    if (currentSubAutomataType == SubGrammarAutomata.SubGrammarAutomataType.OR)
                    {
                        currentState = TransferState(
                            new EpsilonTransferCondition(),
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
                        currentState = TransferState(
                            new EscapeTransferCondition() {EscapeLiteral = currentToken.Literal}, 
                            currentState);
                        currentSubAutomata.EndState = currentState;
                    }
                    else if (currentToken.TransferType == ParseBlockState.Grammar)
                    {
                        currentState = TransferState(
                            new SymolTransferCondition() {CompareReference = context.SymolHost.GetSymol(currentToken.Literal)}, 
                            currentState);
                        currentSubAutomata.EndState = currentState;
                    }
                    else if (currentToken.TransferType == ParseBlockState.String)
                    {
                        currentState = TransferState(
                            new StringTransferCondition() { CompareReference = currentToken.Literal},
                            currentState);
                        currentSubAutomata.EndState = currentState;
                    }
                    // Branch or kleen star Automata
                    else if (currentToken.TransferType == ParseBlockState.KleenStar)
                    {
                        //var lastSubAutomata = subAutomatas[subAutomatas.Count - 1];
                        //ReplaceState(lastSubAutomata, lastSubAutomata.EndState, lastSubAutomata.StartState, context);

                        currentSubAutomata.EndState.Transfers.Add(new GrammarTransfer()
                        {
                            TransfedState = currentSubAutomata.StartState,
                            TransferCondition = new EpsilonTransferCondition()
                        });
                        currentSubAutomata.EndState.Transfers.Add(new GrammarTransfer()
                        {
                            TransfedState = currentSubAutomata.StartState,
                            TransferCondition = new EpsilonTransferCondition()
                        });
                    }
                    else if (currentToken.TransferType == ParseBlockState.OR)
                    {
                        currentSubAutomataType = SubGrammarAutomata.SubGrammarAutomataType.OR;
                        if(orEndState == null)
                            orEndState = new GrammarState();

                        TransferState(
                            new EpsilonTransferCondition(),
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
                    new EpsilonTransferCondition(),
                    currentState,
                    orEndState);
            }

            // Return when used up all tokens.
            currentState.IsTerminal = true;
            return (new SubGrammarAutomata()
            {
                StartState = sourceState,
                EndState = currentState,
                GroupLevel = baseGroupLevel,
            }, currentPosition);
        }

        private static GrammarState TransferState(
            TransferCondition condition, 
            GrammarState currentState,
            GrammarState targetState = null)
        {
            var transferCondition = condition;
            var newState = targetState ?? new GrammarState();
            var newTransfer = new GrammarTransfer()
            {
                TransfedState = newState,
                TransferCondition = transferCondition
            };
            currentState.Transfers.Add(newTransfer);
            return newState;
        }

        private static string MakeStringFromQueue(IEnumerable<char?> queue)
        {
            var holdingCharsArray = (from c in queue where c.HasValue && c.Value != FinializeSymbol select c.Value).ToArray();
            return new string(holdingCharsArray);
        }

        private static void ReplaceState(IGrammarAutomataSegment segment, GrammarState fromState, GrammarState toState, ScannerContext context)
        {
            var symol = context.SymolHost.GetSymol("rs_" + Thread.CurrentThread.ManagedThreadId);
            List<GrammarState> accessedStates = new List<GrammarState>();
            Queue<GrammarState> toAccessQueue = new Queue<GrammarState>();
            toAccessQueue.Enqueue(segment.StartState);
            while (toAccessQueue.Count > 0)
            {
                var state = toAccessQueue.Dequeue();
                foreach (var transfer in state.Transfers)
                {
                    var transfedState = transfer.TransfedState;
                    if (transfedState != segment.EndState)
                        if (!transfedState.GetAccessibility(symol))
                        {
                            toAccessQueue.Enqueue(transfedState);
                        }

                    if (transfer.TransfedState == fromState)
                        transfer.TransfedState = toState;
                }

                state.SetAccessibility(symol);
                accessedStates.Add(state);
            }

            foreach (var state in accessedStates)
            {
                state.RemoveAccessibility(symol);
            }
        }

        ///// <summary>
        ///// Struct to record epsilon transfers' link list.
        ///// </summary>
        //private struct ReversedEpsilonLink
        //{
        //    public GrammarState FromState;
        //    public GrammarState ToState;
        //    public GrammarTransfer LinkTransfer;
        //}

        ///// <summary>
        ///// Struct to trace transfer while traversing grammar automata.
        ///// </summary>
        //private struct AccessPathFrame
        //{
        //    public GrammarState State;
        //    public int CurrentIndex;
        //}

        //private static void TrimEpsilon(IGrammarAutomataSegment segment, ScannerContext context)
        //{
        //    // Get all epsilon transfer link-list with reversed info.
        //    var traverseSymol = context.SymolHost.GetSymol("te_" + Thread.CurrentThread.ManagedThreadId);
        //    List<GrammarState> accessedStates = new List<GrammarState>();

        //    var processSymol = context.SymolHost.GetSymol("proc_" + Thread.CurrentThread.ManagedThreadId);
        //    List<GrammarState> processedStates = new List<GrammarState>();

        //    var currentState = segment.StartState;
        //    var accessPath = new Stack<AccessPathFrame>();
        //    accessPath.Push(new AccessPathFrame {State = segment.StartState, CurrentIndex = -1});
        //    while (true)
        //    {
        //        if (accessPath.Count == 0)
        //            break;

        //        var currentFrame = accessPath.Pop();
        //        int toAccessIndex = currentFrame.CurrentIndex + 1;
        //        for (; toAccessIndex < currentFrame.State.Transfers.Count; toAccessIndex++)
        //        {
        //            var transfer = currentState.Transfers[toAccessIndex];

        //            if (transfer.TransferCondition is EpsilonTransferCondition)
        //            {
        //                // TODO Do something record a epsilon transfer link list.
        //            }

        //            if (transfer.TransfedState.GetAccessibility(traverseSymol))
        //                continue;

        //            transfer.TransfedState.SetAccessibility(traverseSymol);
        //            accessedStates.Add(transfer.TransfedState);
        //            accessPath.Push(new AccessPathFrame { State = transfer.TransfedState, CurrentIndex = -1 });
        //            break;
        //        }
        //    }

        //    // Clear states' access state.
        //    foreach(var state in accessedStates)
        //        state.RemoveAccessibility(traverseSymol);
        //    foreach (var state in processedStates)
        //        state.RemoveAccessibility(processSymol);
        //}
    }

    internal class GrammarStateList : IList<GrammarState>
    {
        public bool IsDirty = false;
        private List<GrammarState> innerList;

        public GrammarStateList() => innerList = new List<GrammarState>();

        public GrammarStateList(IEnumerable<GrammarState> collection) => innerList = new List<GrammarState>(collection);

        public GrammarStateList(int capacity) => innerList = new List<GrammarState>(capacity);

        public GrammarState this[int index]
        {
            get
            {
                return innerList[index];
            }
            set
            {
                innerList[index] = value;
                IsDirty = true;
            }
        }

        public void Add(GrammarState state)
        {
            IsDirty = true;
            innerList.Add(state);
        }

        public bool Remove(GrammarState state)
        {
            IsDirty = true;
            return innerList.Remove(state);
        }


    }

    public class GrammarState
    {
        private readonly IList<GrammarTransfer> _transfers = new List<GrammarTransfer>();

        /// <summary>
        /// Transfers from this state.
        /// </summary>
        public IList<GrammarTransfer> Transfers => _transfers;

        /// <summary>
        /// Is this state a terminal state?
        /// </summary>
        public bool IsTerminal { set; get; } = false;

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
        public TransferCondition TransferCondition { get; set; }
        public GrammarState TransfedState { get; set; }

        public (bool, GrammarState) InputWord(Token token, ScannerContext context)
        {
            return (TransferCondition.Pass(token, context), TransfedState);
        }
    }

    // Transfer Conditions

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
            return CompareReference == word.Grammer.Symol;
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



///
/// Note
/// 2017/5/18
/// 是否需要从扫描器自动机中去除ε？
/// 考虑到最终需要构建转移表，似乎没有这种必要。然而如果允许直接通过自动机构建前端，ε会带来额外的麻烦。
/// --暂且制作一个用以从自动机中去除ε的方法，但是暂时不在ParseToken方法中使用。
