using Elecelf.Hibiki.Scanner;

namespace Elecelf.Hibiki.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            ScannerContext context = new ScannerContext();

            string testText = @"start%r%{te}(ab|ba)@@{rr}";

            var tokens = GrammarAutomata.Parse(testText, context);

            return;
        }
    }

    /// TODO:
    /// 1 - @@ transfer has lost.
    /// 2 - Add symol for each states to simpfy debug.
}
