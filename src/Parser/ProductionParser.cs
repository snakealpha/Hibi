using System.Collections.Generic;
using Elecelf.Hibiki.Parser.GrammarGraph;

namespace Elecelf.Hibiki.Parser
{
    public class ParserContext
    {
        private readonly SymbolHelper _symbolHost = new SymbolHelper();
        public SymbolHelper SymbolHost => _symbolHost;

        private uint _stateCounter = 0;
        public uint StateCounter => _stateCounter;

        public readonly Dictionary<string, string[]> EscapeMap = new Dictionary<string, string[]>()
        {
            {@"nl" , new []{"\r\n", "\n"}},
            {@"w", new []{" ", "\t"}},
            {@"n", new []{"\n"}},
            {@"t", new []{"\t"}},
        };

        public uint GetNextStateIndex()
        {
            return ++_stateCounter;
        }

        // Productions Manager

        public readonly Dictionary<string, ProductionsGroup> Productions = new Dictionary<string, ProductionsGroup>();

        public void AppendProduction(string productionName, GrammarAutomata production)
        {
            if(Productions.TryGetValue(productionName, out var productionGroup))
                productionGroup.Add(production);
            else
                Productions[productionName] = new ProductionsGroup(productionName, production);
        }

        public GrammarAutomata StartProduction
        {
            get;
            set;
        }
    }

    public class ProductionsGroup : List<GrammarAutomata>
    {
        public ProductionsGroup(string productionName)
        {
            ProductionName = productionName;
        }

        public ProductionsGroup(string productionName, params GrammarAutomata[] productions)
        {
            ProductionName = productionName;
            AddRange(productions);
        }

        public string ProductionName
        {
            get;
        }
    }
}
