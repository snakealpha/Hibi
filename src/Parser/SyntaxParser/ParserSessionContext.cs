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

        public static ParserSegment GetSegment(int startPosition, ITransfer expectTransfer, ParserContext context, ParserSegment parentSegment)
        {
            return Segments.Count > 0 ? 
                    Segments.Dequeue().ResetState(startPosition, expectTransfer, context, parentSegment) :
                    new ParserSegment(startPosition, expectTransfer, context, parentSegment);
        }

        public static void ReleaseSegment(ParserSegment segment)
        {
            if(Segments.Count < MaxSegments) Segments.Enqueue(segment);
        }
#endregion

        private ParserSegment(int startPosition, ITransfer expectTransfer, ParserContext context, ParserSegment parentSegment)
        {
            ResetState(startPosition, expectTransfer, context, parentSegment);
        }

        private ParserSegment ResetState(int startPosition, ITransfer expectTransfer, ParserContext context, ParserSegment parentSegment)
        {
            StartPosition = startPosition;
            NextPosition = startPosition;
            ExpectTransfer = expectTransfer;
            Completed = false;
            Context = context;
            ParentSegment = parentSegment;

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
        
        public bool EnqueueCharacter(char character)
        {
            // After all characters are inputed, a FinializeSymbol will inputed to lop uncompleted predict segments.
            if (character == ParserContext.FinializeSymbol &&
                Completed && PredictList.Count == 0)
                return true;

            if (Completed)
            {
                var failedPredictLists = _predictList.FindAll(segment => !segment.EnqueueCharacter(character));
                foreach (var segment in failedPredictLists)
                {
                    segment.Release();
                }

                _predictList.RemoveAll(segment => failedPredictLists.Contains(segment));
                if (_predictList.Count == 0)
                    return false;

                return true;
            }

            // else

            // impossible be epsilon, epsilon transfer should be ignored when define predict set.
            System.Diagnostics.Debug.Assert(!(ExpectTransfer.SyntaxElement is IParserAsEpsilon), @"Cannot be a epsilon transfer!!!");
            
            bool success = false;

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
                            this);
                        success = segment.EnqueueCharacter(character);
                        PredictList.Add(segment);
                    }
                }
            }
            else
            {
                // string parsing.

                bool finished;

                (finished, success, _) =
                    ExpectTransfer.SyntaxElement.PassChar(character, NextPosition - StartPosition, Context);

                Completed = finished && success;

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

            return success;
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
