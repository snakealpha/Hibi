using System.Collections.Generic;

namespace Elecelf.Hibiki.Parser.SyntaxParser
{
    interface IParseable
    {
        /// <summary>
        /// ParseProduction a segment of script.
        /// </summary>
        /// <param name="sourceType">Source type.</param>
        /// <param name="source">Source Id.</param>
        /// <param name="script">Script collection.</param>
        /// <returns>Root Node of generated ast and is parse process successed.</returns>
        (SyntaxNode astRootNode, bool successed) Parse(TokenSource sourceType, string source, IEnumerable<char> script);
    }
}
