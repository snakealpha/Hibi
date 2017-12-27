using System;
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
        private static readonly Queue<ParserSegment> Segments = new Queue<ParserSegment>(256);

        public static ParserSegment GetSegment(int startPosition, ITransfer expectTransfer, uint astDepth, ParserContext context, IEnumerable<ITransfer> exitTransfers)
        {
            return Segments.Count > 0 ? 
                    Segments.Dequeue().ResetState(startPosition, expectTransfer, astDepth, context, exitTransfers) :
                    new ParserSegment(startPosition, expectTransfer, astDepth, context, exitTransfers);
        }

        public static void ReleaseSegment(ParserSegment segment)
        {
            if(Segments.Count < MaxSegments) Segments.Enqueue(segment);
        }
#endregion

        private ParserSegment(int startPosition, ITransfer expectTransfer, uint astDepth, ParserContext context, IEnumerable<ITransfer> exitTransfers)
        {
            ResetState(startPosition, expectTransfer, astDepth, context, exitTransfers);
        }

        public ParserSegment ResetState(int startPosition, ITransfer expectTransfer, uint astDepth, ParserContext context, IEnumerable<ITransfer> exitTransfers)
        {
            StartPosition = startPosition;
            NextPosition = startPosition;
            ExpectTransfer = expectTransfer;
            AstDepth = astDepth;
            Completed = false;
            Context = context;
            ExiTransfers = exitTransfers;

            return this;
        }

        public int StartPosition
        {
            get;
            private set;
        }

        public int NextPosition
        {
            get;
            private set;
        }

        public ITransfer ExpectTransfer
        {
            get;
            private set;
        }

        public uint AstDepth
        {
            get;
            private set;
        }

        public bool Completed
        {
            get;
            private set;
        }

        public ParserContext Context
        {
            get;
            private set;
        }

        public IEnumerable<ITransfer> ExiTransfers
        {
            get;
            private set;
        }
        
        public bool EnqueueCharacter(char character)
        {
            if (Completed)
            {
                
            }
            else
            {
                (bool finished, bool success, ErrorInfo errorInfo) =
                    ExpectTransfer.SyntaxElement.PassChar(character, NextPosition - StartPosition, Context);
            }

            //return NextPosition++;
            throw new NotImplementedException();
        }

        private readonly List<ParserSegment> _predictList = new List<ParserSegment>();

        public IList<ParserSegment> PredictList => _predictList;
    }
}
