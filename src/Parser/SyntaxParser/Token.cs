using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elecelf.Hibiki.Parser.SyntaxParser
{
    public enum TokenSource
    {
        Othoer,
        File,
        Thunk,
    }

    public class Token
    {
        public string Literal { set; get; }

        public IEnumerable<int> Range;

        public TokenSource SourceType;

        public string Source;
    }
}
