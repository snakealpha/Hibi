using Elecelf.Hibiki.Parser;
using Elecelf.Hibiki.Parser.GrammarGraph;
using Elecelf.Hibiki.Parser.SyntaxParser;

namespace Elecelf.Hibiki.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateParseTree();

            ParseScript();
        }

        static void CreateParseTree()
        {
            ParserContext context = new ParserContext();

            string testText = @"start%r%{te}(ab|ba)@@{rr}";

            string testText2 = @"(ab|ba|(ar|ra)*)*{aa}{rr}";

            string testText3 = @"ab|~";

            var automata1 = GrammarAutomata.ParseProduction(testText, context, "TestGrammar");
            var automata2 = GrammarAutomata.ParseProduction(testText2, context, "TestGrammar2");
            var automata3 = GrammarAutomata.ParseProduction(testText3, context, "TestGrammar3");
        }

        static void ParseScript()
        {
            ParserContext context = new ParserContext();

            string production_A = @"st{pro3}ed";
            string pro1 = @"pro1-%num%*-{pro2}";
            string pro2 = @"ab|cd";
            string pro3 = @"~";

            string script = @"stpro1-999-cded";

            string script2 = @"st1ed";

            context.AppendProduction(context.SymbolHost.GetSymol("st"), GrammarAutomata.ParseProduction(production_A, context, "pA"));
            context.AppendProduction(context.SymbolHost.GetSymol("pro1"), GrammarAutomata.ParseProduction(pro1, context, "pA"));
            context.AppendProduction(context.SymbolHost.GetSymol("pro2"), GrammarAutomata.ParseProduction(pro2, context, "pA"));
            context.AppendProduction(context.SymbolHost.GetSymol("pro3"), GrammarAutomata.ParseProduction(pro3, context, "pA"));
            context.StartProductionSymbol = context.SymbolHost.GetSymol("st");
            //(var ast, var success) = context.Parse(TokenSource.Thunk, "thunk", script);

            (var ast2, var success2) = context.Parse(TokenSource.Thunk, "thunk2", script2);
        }
    }
}
