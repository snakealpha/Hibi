﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
            RawChar,
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
        public static GrammarAutomata Parse(string rawString, ScannerContext context)
        {
            var automata = new GrammarAutomata();

            // Phase 1: Make string to tokens and make the collection of tokens.
            var tokens = ParseTokens(rawString);

            // Phase 2: Make tokens to automata.
            var (endState, _) = ParseTokens(tokens, 0, automata.StartState, context);

            return automata;
        }
        private static IList<ScannerToken> ParseTokens(string rawString)
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
                                GroupLevel = currentGroupLevel,
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
        /// <returns>1- Finialize state of this grammar automata; 2- Start position of next state of current state.</returns>
        private static (GrammarState, uint) ParseTokens(IList<ScannerToken> tokens, uint startPosition, GrammarState sourceState, ScannerContext context)
        {
            throw new NotImplementedException();
        }

        private static GrammarState TransferState(
            TransferCondition condition, 
            GrammarState currentState,
            Stack<GrammarState> grammarStates)
        {
            TransferCondition transferCondition = condition;
            var newState = new GrammarState();
            var newTransfer = new GrammarTransfer()
            {
                TransfedState = newState,
                TransferCondition = transferCondition
            };
            currentState.Transfers.Add(newTransfer);
            grammarStates.Push(currentState);
            return newState;
        }

        private static string MakeStringFromQueue(Queue<char?> queue)
        {
            var holdingCharsArray = (from c in queue where c.HasValue && c.Value != (char)3 select c.Value).ToArray();
            return new string(holdingCharsArray);
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
