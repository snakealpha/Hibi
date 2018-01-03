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

        public (char nextChar, bool finished) GetNextChar() => (_charPrivoiderEnumerator.Current, _charPrivoiderEnumerator.MoveNext());

        public ParserScriptInfo ScriptInfo
        {
            get => _scriptInfo;
            set
            {
                _scriptInfo = value;
                _charPrivoiderEnumerator = _scriptInfo.SourceProvider.GetEnumerator();
                _charPrivoiderEnumerator.MoveNext();
            }
        }

        public readonly Queue<ParserSegment> PeekingSegments = new Queue<ParserSegment>(64);
    }

    /// <summary>
    /// A current parse route.
    /// When a state has more than one possible transfers with a newly inputed char in predict set, either need a ParserThread object to record its parse state.
    /// </summary>
    public class ParserSegment
    {
#region Object pool constructs

        private const int MaxSegments = 64;
        private static readonly Queue<ParserSegment> Segments = new Queue<ParserSegment>(256);

        public static ParserSegment GetSegment(int startPosition, ITransfer expectTransfer, ParserContext context, ParserSessionContext sessionContext, ParserSegment parentSegment)
        {
            return Segments.Count > 0 ? 
                    Segments.Dequeue().ResetState(startPosition, expectTransfer, context, sessionContext, parentSegment) :
                    new ParserSegment(startPosition, expectTransfer, context, sessionContext, parentSegment);
        }

        public static void ReleaseSegment(ParserSegment segment)
        {
            if(Segments.Count < MaxSegments) Segments.Enqueue(segment);
        }
#endregion

        private ParserSegment(int startPosition, ITransfer expectTransfer, ParserContext context, ParserSessionContext sessionContext, ParserSegment parentSegment)
        {
            ResetState(startPosition, expectTransfer, context, sessionContext, parentSegment);
        }

        private ParserSegment ResetState(int startPosition, ITransfer expectTransfer, ParserContext context, ParserSessionContext sessionContext, ParserSegment parentSegment)
        {
            StartPosition = startPosition;
            NextPosition = startPosition;
            ExpectTransfer = expectTransfer;
            Completed = false;
            Context = context;
            ParentSegment = parentSegment;
            LayerTraceback = 0;
            _success = false;
            _failed = false;
            _pasrserSessionContext = sessionContext;

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

        public int Length => NextPosition - StartPosition;

        public ITransfer ExpectTransfer
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

        public ParserSegment ParentSegment
        {
            get;
            private set;
        }

        public uint LayerTraceback
        {
            get;
            private set;
        }

        private bool _success = false;
        private bool _failed = false;

        public bool Success
        {
            get
            {
                if (ExpectTransfer.SyntaxElement is IParseAsSymbol)
                {
                    if (PredictList.Count == 0)
                        return false;

                    foreach (var segment in PredictList)
                    {
                        if (!segment.Success)
                            return false;
                    }
                }
                return Completed;
            }
        }

        private ParserSessionContext _pasrserSessionContext;
        
        public void EnqueueCharacter(char character)
        {
            _pasrserSessionContext.PeekingSegments.Clear();

            _pasrserSessionContext.PeekingSegments.Enqueue(this);
            while (_pasrserSessionContext.PeekingSegments.Count > 0)
            {
                var segment = _pasrserSessionContext.PeekingSegments.Dequeue();
                segment.EnqueueCharacter(character, _pasrserSessionContext.PeekingSegments);
            }
        }

        public bool Prune()
        {
            var failedPredictList = _predictList.FindAll(segment => !segment.Prune());
            _predictList.RemoveAll(segment => failedPredictList.Contains(segment));
            foreach (var segment in failedPredictList)
            {
                segment.Release();
            }
            return Success;
        }
        
        private void EnqueueCharacter(char character, Queue<ParserSegment> segmentQueue)
        {
            if (Completed)
            {
                foreach (var segment in _predictList)
                {
                    segmentQueue.Enqueue(segment);
                }

                return;
            }

            // else

            // impossible be epsilon, epsilon transfer should be ignored when define predict set.
            System.Diagnostics.Debug.Assert(!(ExpectTransfer.SyntaxElement is IParserAsEpsilon), @"Cannot be a epsilon transfer!!!");

            if (ExpectTransfer.SyntaxElement is IParseAsSymbol symbolElement)
            {
                // Enter another production!!!
                Completed = true;
                var productionGroup = Context.Productions[symbolElement.SymbolIdentity];
                foreach (var production in productionGroup)
                {
                    foreach (var predictTransfer in production.StartState.PredictTransfers)
                    {
                        var segment = GetSegment(
                            NextPosition,
                            predictTransfer,
                            Context,
                            _pasrserSessionContext,
                            this);
                        segmentQueue.Enqueue(segment);
                        PredictList.Add(segment);
                    }
                }
            }
            else
            {
                // string parsing.

                if (_failed)
                {
                    NextPosition++;
                    return;
                }


                bool finished;

                (finished, _success, _) =
                    ExpectTransfer.SyntaxElement.PassChar(character, NextPosition - StartPosition, Context);

                Completed = finished && _success;
                _failed = finished && !_success;

                if (Completed)
                {
                    // Transfer inside a production
                    var predictTransfers = ExpectTransfer.TransfedState.PredictTransfers;
                    foreach (var predictTransfer in predictTransfers)
                    {
                        var segment = GetSegment(
                            NextPosition + 1,
                            predictTransfer,
                            Context,
                            _pasrserSessionContext,
                            ParentSegment);
                        PredictList.Add(segment);
                    }

                    // Leave a production
                    if (ExpectTransfer.TransfedState.IsTerminal)
                    {
                        LayerTraceback = 0;
                        var tracebackNode = ParentSegment;
                        while (tracebackNode != null)
                        {
                            LayerTraceback++;
                            foreach (var transfer in tracebackNode.ExpectTransfer.TransfedState.PredictTransfers)
                            {
                                var segment = GetSegment(
                                    NextPosition + 1,
                                    transfer,
                                    Context,
                                    _pasrserSessionContext,
                                    tracebackNode.ParentSegment);
                                PredictList.Add(segment);
                            }

                            if (tracebackNode.ExpectTransfer.TransfedState.IsTerminal)
                            {
                                tracebackNode = tracebackNode.ParentSegment;
                            }
                            else
                            {
                                tracebackNode = null;
                            }
                        }
                    }
                }
            }

            NextPosition++;
        }

        public void Release()
        {
            foreach (var segment in PredictList)
            {
                segment.Release();
            }

            ReleaseSegment(this);
        }

        private readonly List<ParserSegment> _predictList = new List<ParserSegment>();

        public IList<ParserSegment> PredictList => _predictList;
    }
}
