using Elecelf.Hibiki.Parser;
using Elecelf.Hibiki.Parser.GrammarGraph;
using Elecelf.Hibiki.Parser.SyntaxParser;

namespace Elecelf.Hibiki.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            //CreateParseTree();

            ParseScript();
        }

        static void CreateParseTree()
        {
            ParserContext context = new ParserContext();

            string testText = @"start%r%{te}(ab|ba)@@{rr}";

            string testText2 = @"(ab|ba|(ar|ra)*)*{aa}{rr}";

            var automata1 = GrammarAutomata.ParseProduction(testText, context, "TestGrammar");
            var automata2 = GrammarAutomata.ParseProduction(testText2, context, "TestGrammar2");
        }

        static void ParseScript()
        {
            ParserContext context = new ParserContext();

            string production_A = @"a%num%%num%*b";
            string script_A = @"a669b";

            context.AppendProduction(context.SymbolHost.GetSymol("st"), GrammarAutomata.ParseProduction(production_A, context, "pA"));
            context.StartProductionSymbol = context.SymbolHost.GetSymol("st");
            (var ast, var success) = context.Parse(TokenSource.Thunk, "thunk", script_A);
        }
    }
}
