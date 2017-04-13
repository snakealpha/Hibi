using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elecelf.Hibiki.Scanner
{
    public class Scanner
    {
    }

    public class ScannerContext
    {
        private readonly SymolHelper _symolHost = new SymolHelper();
        public SymolHelper SymolHost => _symolHost;

        public readonly Dictionary<string, string[]> EscapeMap = new Dictionary<string, string[]>()
        {
            {@"nl" , new []{"\r\n", "\n"}},
            {@"w", new []{" ", "\t"}},
            {@"n", new []{"\n"}},
            {@"t", new []{"\t"}},
        };
    }
}
