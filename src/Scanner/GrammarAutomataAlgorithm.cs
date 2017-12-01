using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Elecelf.Hibiki.Parser
{
    public partial class GrammarAutomata
    {
        /// <summary>
        /// Parse a string to a automata.
        /// </summary>
        /// <param name="rawString">String to be parsed.</param>
        /// <param name="context">Parse context.</param>
        /// <param name="grammarName"></param>
        /// <returns>Parsed automata.</returns>
        public static GrammarAutomata Parse(string rawString, ParserContext context, string grammarName = null)
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
            ParseTokens(tokens, 0, automata.StartState, context, grammarName);

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
                            TransferType = ParseBlockState.Or,
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
        /// <param name="context">Parse context passed to this parse process.</param>
        /// <param name="grammarName">Name of parsed</param>
        /// <returns>1- Sub automata parsed from this token collection; 2- Start position of next state of current state.</returns>
        private static (SubGrammarAutomata, int) ParseTokens(IList<ScannerToken> tokens, int startPosition, GrammarState sourceState, ParserContext context, string grammarName)
        {
            var baseGroupLevel = tokens[startPosition].GroupLevel;
            var currentPosition = startPosition;
            var currentState = sourceState;

            GrammarState orEndState = null;
            // Branch id of or-state automata. Used to ensure branch production's name.
            var branchNum = 0;

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

            // Record group number to make grouped automata's production name.
            var groupNum = 0;

            while (currentPosition < tokens.Count)
            {
                var currentToken = tokens[currentPosition];

                if (currentToken.GroupLevel > baseGroupLevel)
                {
                    string newAutomataName = $"{grammarName ?? "base"}_group-{++groupNum}";
                    var newAutomata = new GrammarAutomata(context.SymolHost.GetSymol(newAutomataName));

                    // Token has higher group level: this token is in a inner layer of group.
                    // Get a sub automata of later tokens.
                    (SubGrammarAutomata _, int newPosition) =
                        ParseTokens(tokens, currentPosition, newAutomata.StartState, context, newAutomataName);

                    // Add group into productions as it's a new production.
                    context.AppendProduction(newAutomataName, newAutomata);

                    currentPosition = newPosition;

                    // Instead high level part with a new grammar block
                    lastBlockStartState = currentState;

                    currentState = TransferState(
                        new SymolTransferCondition(context.SymolHost.GetSymol(newAutomataName)),
                        currentState,
                        newStateSymol: context.SymolHost.GetSymol("State_" + context.GetNextStateIndex()));
                    currentSubAutomata.EndState = currentState;

                    lastBlockEndState = currentState;

                    // Immediately apply new currentPosition
                    continue;
                }
                if (currentToken.GroupLevel < baseGroupLevel)
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
// process current token

                // Linear Automata
                if (currentToken.TransferType == ParseBlockState.Escape)
                {
                    lastBlockStartState = currentState;

                    currentState = TransferState(
                        new EscapeTransferCondition(currentToken.Literal),
                        currentState,
                        newStateSymol: context.SymolHost.GetSymol("State_" + context.GetNextStateIndex()));
                    currentSubAutomata.EndState = currentState;

                    lastBlockEndState = currentState;
                }
                else if (currentToken.TransferType == ParseBlockState.Grammar)
                {
                    lastBlockStartState = currentState;

                    currentState = TransferState(
                        new SymolTransferCondition(context.SymolHost.GetSymol(currentToken.Literal)),
                        currentState,
                        newStateSymol: context.SymolHost.GetSymol("State_" + context.GetNextStateIndex()));
                    currentSubAutomata.EndState = currentState;

                    lastBlockEndState = currentState;
                }
                else if (currentToken.TransferType == ParseBlockState.String)
                {
                    lastBlockStartState = currentState;

                    currentState = TransferState(
                        new StringTransferCondition(currentToken.Literal),
                        currentState,
                        newStateSymol: context.SymolHost.GetSymol("State_" + context.GetNextStateIndex()));
                    currentSubAutomata.EndState = currentState;

                    lastBlockEndState = currentState;
                }
                // Branch or kleen star Automata
                else if (currentToken.TransferType == ParseBlockState.KleenStar)
                {
                    System.Diagnostics.Debug.Assert(lastBlockStartState != null, "Kleen Star Cannot be first token in a grammar.");

                    TransferState(
                        EpsilonTransferCondition.Instance,
                        lastBlockStartState,
                        currentSubAutomata.EndState);
                    TransferState(
                        EpsilonTransferCondition.Instance,
                        lastBlockEndState,
                        lastBlockStartState);
                }
                else if (currentToken.TransferType == ParseBlockState.Or)
                {
                    currentSubAutomataType = SubGrammarAutomata.SubGrammarAutomataType.OR;
                    if (orEndState == null)
                        orEndState = new GrammarState(context.SymolHost.GetSymol("OR-End_State_" + context.GetNextStateIndex()));

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
        
        private static GrammarState TransferState(
            TransferCondition condition,
            GrammarState currentState,
            GrammarState targetState = null,
            Symol? newStateSymol = null)
        {
            var transferCondition = condition;
            var newState = targetState ?? (newStateSymol == null ? new GrammarState() : new GrammarState(newStateSymol.Value));
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
}
