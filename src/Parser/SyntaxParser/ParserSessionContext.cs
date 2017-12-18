using System.Collections.Generic;

namespace Elecelf.Hibiki.Parser.SyntaxParser
{
    public class ParserScriptInfo
    {
        public ParserScriptInfo(TokenSource sourceType, string sourcePath, IEnumerable<char> provider)
        {
            if(sourcePath is null) sourcePath = "";
            if(provider is null) throw new ParseErrorException(@"Script provider cannot be null.");

            SourceType = sourceType;
            SourcePath = sourcePath;
            SourceProvider = provider;
        }

        public TokenSource SourceType { get; }
        public string SourcePath { get; }
        public IEnumerable<char> SourceProvider { get; }
    }

    /// <summary>
    /// A SessionContext is used to record current parse state of a parse process.
    /// </summary>
    public class ParserSessionContext
    {
        private ParserScriptInfo _scriptInfo;
        private IEnumerator<char> _charPrivoiderEnumerator;

        public (char nextChar, bool finished) GetNextChar()
        {
            return (_charPrivoiderEnumerator.Current, _charPrivoiderEnumerator.MoveNext());
        }

        private int charIndex = 0;

        public ParserScriptInfo ScriptInfo
        {
            get { return _scriptInfo; }
            set
            {
                _scriptInfo = value;
                _charPrivoiderEnumerator = _scriptInfo.SourceProvider.GetEnumerator();
                charIndex = 0;
            }
        }
    }

    /// <summary>
    /// A current parse route.
    /// When a state has more than one possible transfers with a newly inputed char in predict set, either need a ParserThread object to record its parse state.
    /// </summary>
    public class ParserSegment
    {
#region Object pool constructs
        private static readonly int MaxSegments = 64;
        private static readonly Queue<ParserSegment> Segments = new Queue<ParserSegment>(16);

        public static ParserSegment GetSegment()
        {
            return Segments.Count > 0 ? Segments.Dequeue().ClearState() : new ParserSegment();
        }

        public static void ReleaseSegment(ParserSegment thread)
        {
            if(Segments.Count < MaxSegments) Segments.Enqueue(thread);
        }
#endregion

        private readonly Queue<char> _uncompletedTokenQueue = new Queue<char>(16);

        private ParserSegment()
        {
            
        }

        public ParserSegment ClearState()
        {
            _uncompletedTokenQueue.Clear();
            return this;
        }

        public int CurrentPosition => _uncompletedTokenQueue.Count;

        /// <summary>
        /// Input next character of script.
        /// </summary>
        /// <param name="character">Character inputed.</param>
        /// <returns>Current position of current thread token.</returns>
        public int EnqueueCharacter(char character)
        {
            _uncompletedTokenQueue.Enqueue(character);
            return CurrentPosition;
        }

        /// <summary>
        /// Clear state of a thread to get ready for next toke parsing process.
        /// </summary>
        /// <returns>Token Literal.</returns>
        public string FinishToken()
        {
            string result = _uncompletedTokenQueue.Count > 0 ? new string(_uncompletedTokenQueue.ToArray()) : null;
            _uncompletedTokenQueue.Clear();
            NextParserSegments.Clear();

            return result;
        }

        public readonly List<ParserSegment> NextParserSegments = new List<ParserSegment>();
    }
}
