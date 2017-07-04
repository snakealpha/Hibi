using Elecelf.Hibiki.Scanner;

namespace Elecelf.Hibiki.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            ScannerContext context = new ScannerContext();

            string testText = @"{te}";

            var tokens = GrammarAutomata.Parse(testText, context);

            return;
        }
    }
}
