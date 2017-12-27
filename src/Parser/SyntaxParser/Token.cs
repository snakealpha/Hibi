using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elecelf.Hibiki.Parser.SyntaxParser
{
    public enum TokenSource
    {
        Other,
        File,
        Thunk,
    }

    public struct Token
    {
        public uint LiteralStart { get; set; }
        public uint LiteralLength { get; set; }
    }
}
