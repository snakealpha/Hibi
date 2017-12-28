using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Elecelf.Hibiki.Parser.GrammarGraph;
using Elecelf.Hibiki.Parser.SyntaxParser;

namespace Elecelf.Hibiki.Parser
{
    public class ParserContext
    {
        public const char FinializeSymbol = (char)3;

        private readonly SymbolHelper _symbolHost = new SymbolHelper();
        public SymbolHelper SymbolHost => _symbolHost;

        private uint _stateCounter = 0;
        public uint StateCounter => _stateCounter;

        public readonly Dictionary<string, IEnumerable<char>> EscapeMap = new Dictionary<string, IEnumerable<char>>()
        {
            {@"new-line" , "\n"},
            {@"space", " \t"},
            {@"n", "\n"},
            {@"tab", "\t"},
            // Space word before a new line word.
            {@"nlsp", "\r \t"},

            {@"num", "0123456789" },
            {@"low-word", "abcdefghijklmnopqrstuvwxyz" },
            {@"upper-word", "ABCDEFGHIJKLMNOPQRSTUVWXYZ" },
            {@"word", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" },
            {@"word-or-num", "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" },
        };

        public uint GetNextStateIndex()
        {
            return ++_stateCounter;
        }

        // Productions Manager

        public readonly Dictionary<Symbol, ProductionsGroup> Productions = new Dictionary<Symbol, ProductionsGroup>();

        public void AppendProduction(Symbol productionName, GrammarAutomata production)
        {
            if(Productions.TryGetValue(productionName, out var productionGroup))
                productionGroup.Add(production);
            else
                Productions[productionName] = new ProductionsGroup(productionName, production);
        }

        public Symbol StartProductionSymbol
        {
            get;
            set;
        }
    }

    public class ProductionsGroup : List<IParseable>
    {
        public ProductionsGroup(Symbol productionName)
        {
            ProductionName = productionName;
        }

        public ProductionsGroup(Symbol productionName, params IParseable[] productions)
        {
            ProductionName = productionName;
            AddRange(productions);
        }

        public Symbol ProductionName
        {
            get;
        }
    }

    public static class ParserExtensionMethods
    {
        /// <summary>
        /// ParseProduction a segment of script.
        /// </summary>
        /// <param name="context">Target static context.</param>
        /// <param name="sourceType">Source type.</param>
        /// <param name="source">Source Id.</param>
        /// <param name="script">Script collection.</param>
        /// <returns>Root Node of generated ast and is parse process successed.</returns>
        public static (IAstNode astRootNode, bool successed) Parse(
            this ParserContext context, 
            TokenSource sourceType,
            string source, 
            IEnumerable<char> script)
        {
            var sessionContext = new ParserSessionContext(){ScriptInfo = new ParserScriptInfo(sourceType, source, script)};

            // Initialize state of parse workflow.
            ITransfer startupTransfer = new GrammarTransfer(new SymbolTransferCondition(context.StartProductionSymbol))
            {
                TransfedState = new GrammarState(){SelfIsTerminal = true}
            };
            var rootParserSegment = ParserSegment.GetSegment(0, startupTransfer, context, null);

            // Input chars
            StringBuilder sourceScript = new StringBuilder(4096);
            (char nextChar, bool canContinue) = sessionContext.GetNextChar();
            while (canContinue)
            {
                sourceScript.Append(nextChar);

                bool success = rootParserSegment.EnqueueCharacter(nextChar);

                (nextChar, canContinue) = sessionContext.GetNextChar();
            }

            // At last, input a epilson char to lop uncompleted predict path.
            bool allSuccess = rootParserSegment.EnqueueCharacter(ParserContext.FinializeSymbol);

            // From ParseSegments To AstTree
            Stack<Tuple<ParserSegment, IAstNode>> nodeStack = new Stack<Tuple<ParserSegment, IAstNode>>(64);
            var traverseParserSegment = rootParserSegment;
            ParserSegment recordParserSegment = null;
            while (traverseParserSegment != null)
            {
                var astNode = new SyntaxNode
                {
                    ProductionSymbol = (traverseParserSegment.ExpectTransfer.SyntaxElement as IParseAsSymbol)?.SymbolIdentity ?? nodeStack.Peek().Item2.ProductionSymbol,
                    Token = new Token
                    {
                        LiteralStart = (uint)traverseParserSegment.StartPosition,
                    }
                };
                
                if (traverseParserSegment.ParentSegment == recordParserSegment)
                {
                    // Add new layer
                    if (nodeStack.Count > 0)
                    {
                        nodeStack.Peek().Item2.LeafNodes.Add(astNode);
                    }
                    nodeStack.Push(new Tuple<ParserSegment, IAstNode>(traverseParserSegment,astNode));
                }
                else
                {
                    Debug.Assert(recordParserSegment != null, nameof(recordParserSegment) + " != null");

                    if(traverseParserSegment.ParentSegment == recordParserSegment.ParentSegment)
                    {
                        // Keep current layer
                        nodeStack.Peek().Item2.LeafNodes.Add(astNode);
                    }
                    else
                    {
                        // Return to last layer
                        nodeStack.Pop();

                        if(nodeStack.Count>0)
                            nodeStack.Peek().Item2.LeafNodes.Add(astNode);
                    }
                }

                recordParserSegment = traverseParserSegment;
                traverseParserSegment =
                    traverseParserSegment.PredictList.Count > 0 ? traverseParserSegment.PredictList[0] : null;
            }

            return (null, false);
        }
    }
}
