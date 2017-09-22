using Elecelf.Hibiki.Scanner;

namespace Elecelf.Hibiki.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            ScannerContext context = new ScannerContext();

            string testText = @"start%r%{te}(ab|ba)@@{rr}";

            string testText2 = @"(ab|ba|(ar|ra)*)*{aa}{rr}";

            var tokens = GrammarAutomata.Parse(testText, context, "TestGrammar");
            var tokens2 = GrammarAutomata.Parse(testText2, context, "TestGrammar2");

            return;
        }
    }
}
