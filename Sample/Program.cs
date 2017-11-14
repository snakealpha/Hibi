using Elecelf.Hibiki.Parser;

namespace Elecelf.Hibiki.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            ParserContext context = new ParserContext();

            string testText = @"start%r%{te}(ab|ba)@@{rr}";

            string testText2 = @"(ab|ba|(ar|ra)*)*{aa}{rr}";

            var automata1 = GrammarAutomata.Parse(testText, context, "TestGrammar");
            var automata2 = GrammarAutomata.Parse(testText2, context, "TestGrammar2");



            return;
        }
    }
}
