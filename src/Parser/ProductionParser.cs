using System;
using System.Collections.Generic;
using Elecelf.Hibiki.Parser.GrammarGraph;
using Elecelf.Hibiki.Parser.SyntaxParser;

namespace Elecelf.Hibiki.Parser
{
    public class ParserContext
    {
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

        public IParseable StartProduction
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
        public static (SyntaxNode astRootNode, bool successed) Parse(
            this ParserContext context, 
            TokenSource sourceType,
            string source, 
            IEnumerable<char> script)
        {
            var sessionContext = new ParserSessionContext(){ScriptInfo = new ParserScriptInfo(sourceType, source, script)};
            var startupProduction = context.StartProduction;

            throw new NotImplementedException();
        }
    }
}
