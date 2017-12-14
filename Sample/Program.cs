using Elecelf.Hibiki.Parser;
using Elecelf.Hibiki.Parser.GrammarGraph;

namespace Elecelf.Hibiki.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            ParserContext context = new ParserContext();

            string testText = @"start%r%{te}(ab|ba)@@{rr}";

            string testText2 = @"(ab|ba|(ar|ra)*)*{aa}{rr}";

            var automata1 = GrammarAutomata.ParseProduction(testText, context, "TestGrammar");
            var automata2 = GrammarAutomata.ParseProduction(testText2, context, "TestGrammar2");



            return;
        }
    }
}
