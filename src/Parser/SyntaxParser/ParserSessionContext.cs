using System.Collections.Generic;

namespace Elecelf.Hibiki.Parser.SyntaxParser
{
    /// <summary>
    /// A SessionContext is used to record current parse state of a parse process.
    /// </summary>
    public class ParserSessionContext
    {
        private IEnumerable<char> _rawCharPrivoider;
        private IEnumerator<char> _charPrivoiderEnumerator;
        public IEnumerable<char> RawCharProvider
        {
            get => _rawCharPrivoider;
            set
            {
                _rawCharPrivoider = value;
                _charPrivoiderEnumerator = _rawCharPrivoider.GetEnumerator();
                charIndex = 0;
            }
        }

        public (char nextChar, bool finished) GetNextChar()
        {
            return (_charPrivoiderEnumerator.Current, _charPrivoiderEnumerator.MoveNext());
        }

        private int charIndex = 0;

        public TokenSource SourceType { get; set; }

        public string Source { get; set; }
    }

    /// <summary>
    /// A current parse route.
    /// When a state has more than one possible transfers with a newly inputed char in predict set, either need a ParserThread object to record its parse state.
    /// </summary>
    public class ParserThread
    {
#region Object pool constructs
        private static readonly int MaxThreads = 64;
        private static readonly Queue<ParserThread> Threads = new Queue<ParserThread>(16);

        public static ParserThread GetThread()
        {
            return Threads.Count > 0 ? Threads.Dequeue() : new ParserThread();
        }

        public static void ReleaseThread(ParserThread thread)
        {
            if(Threads.Count < MaxThreads) Threads.Enqueue(thread);
        }
#endregion

        private readonly Queue<char> _uncompletedTokenQueue = new Queue<char>(16);

        private ParserThread()
        {
            
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

            return result;
        }
    }
}
