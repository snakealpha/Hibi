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
    }
}
