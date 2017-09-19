using Elecelf.Hibiki.Scanner;

namespace Elecelf.Hibiki.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            ScannerContext context = new ScannerContext();

            string testText = @"start%r%{te}(ab|ba)@@{rr}";

            string testText2 = @"(ab|ba|cc)*{aa}{rr}";

            string testText3 = @"{rr}*";

            var tokens = GrammarAutomata.Parse(testText, context, "TestGrammar");
            var tokens2 = GrammarAutomata.Parse(testText2, context, "TestGrammar2");
            var tokens3 = GrammarAutomata.Parse(testText3, context, "TestGrammar3");

            return;
        }
    }
}
