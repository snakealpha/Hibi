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

        public void AppendProduction(Symbol productionName, IParseable production)
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
            var sourceScript = new StringBuilder(4096);
            foreach (var inputChar in sessionContext.ScriptInfo.SourceProvider)
            {
                sourceScript.Append(inputChar);

                var success = rootParserSegment.EnqueueCharacter(inputChar);
            }

            // At last, input a epilson char to lop uncompleted predict path.
            bool allSuccess = rootParserSegment.EnqueueCharacter(ParserContext.FinializeSymbol);

            // From ParseSegments To AstTree
            Stack<ValueTuple<ParserSegment, IAstNode>> nodeStack = new Stack<ValueTuple<ParserSegment, IAstNode>>(64);
            var traverseParserSegment = rootParserSegment;
            ValueTuple<ParserSegment, IAstNode> recordParserSegment = (null, null);

            IAstNode rootAstNode = null;

            while (traverseParserSegment != null)
            {
                var astNode = new SyntaxNode
                {
                    ProductionSymbol = (traverseParserSegment.ExpectTransfer.SyntaxElement as IParseAsSymbol)?.SymbolIdentity ?? nodeStack.Peek().Item2.ProductionSymbol,
                };
                
                if (traverseParserSegment.ParentSegment == recordParserSegment.Item1)
                {
                    // Dive
                    if (recordParserSegment.Item2 != null)
                    {
                        recordParserSegment.Item2.LeafNodes.Add(astNode);
                        astNode.SetToken((uint)traverseParserSegment.StartPosition,
                            (uint) traverseParserSegment.Length);
                        if (nodeStack.Peek().Item2 != recordParserSegment.Item2)
                            nodeStack.Push(recordParserSegment);
                    }
                    else
                    {
                        nodeStack.Push((traverseParserSegment, astNode));
                    }
                }
                else
                {
                    Debug.Assert(recordParserSegment.Item1 != null, nameof(recordParserSegment) + " != null");
                    
                    nodeStack.Peek().Item2.LeafNodes.Add(astNode);

                    astNode.SetToken((uint)traverseParserSegment.StartPosition,
                        (uint) traverseParserSegment.Length);
                }

                // Float
                for (uint i = 0; i != traverseParserSegment.LayerTraceback; i++)
                {
                    var nodeInfo = nodeStack.Pop();
                    nodeInfo.Item2.SetToken((uint) nodeInfo.Item1.StartPosition,
                        (uint) (traverseParserSegment.NextPosition - nodeInfo.Item1.StartPosition));

                    rootAstNode = nodeInfo.Item2;
                }

                recordParserSegment = (traverseParserSegment, astNode);
                traverseParserSegment =
                    traverseParserSegment.PredictList.Count > 0 ? traverseParserSegment.PredictList[0] : null;
            }

            rootParserSegment.Release();

            if (rootAstNode != null)
                return (rootAstNode, true);

            return (null, false);
        }
    }
}
