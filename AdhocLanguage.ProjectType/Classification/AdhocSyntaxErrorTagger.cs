using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;

using AdhocLanguage.Intellisense.Completions;

using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

using AdhocLanguage.Intellisense;

using Esprima;
using System.Drawing;
using System.Linq;

namespace AdhocLanguage.Classification
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(AdhocErrorTag))]
    [ContentType("adhoc")]
    internal class AdhocSyntaxErrorTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return new AdhocSyntaxErrorTagger(buffer) as ITagger<T>;
        }
    }

    internal sealed class AdhocSyntaxErrorTagger : ITagger<AdhocErrorTag>
    {
        private ITextBuffer _buffer;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        private Timer _delayRefreshTimer;

        public List<ParseError> _errors;

        public AdhocSyntaxErrorTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _buffer.Changed += BufferChanged;
            IntellisenseProvider.Refresh(buffer.CurrentSnapshot.GetText(), force: true);
            _errors = IntellisenseProvider.LastParsedResult.ErrorHandler.Errors;

            ReParse(buffer.CurrentSnapshot);
        }

        void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // If this isn't the most up-to-date version of the buffer, then ignore it for now (we'll eventually get another change event).
            if (e.After != _buffer.CurrentSnapshot)
                return;

            _delayRefreshTimer?.Dispose();
            _delayRefreshTimer = new Timer(OnRefreshDelayElapsed, e, 500, System.Threading.Timeout.Infinite);
            this.ReParse(e.After);
        }

        public void OnRefreshDelayElapsed(object state)
        {
            TextContentChangedEventArgs args = (TextContentChangedEventArgs)state;
            ReParse(args.After);
        }

        public void ReParse(ITextSnapshot newSnapshot)
        {
            IntellisenseProvider.Refresh(_buffer.CurrentSnapshot.GetText());

            if (this.TagsChanged != null)
                this.TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(newSnapshot, 0, newSnapshot.Length)));
        }

        public IEnumerable<ITagSpan<AdhocErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            ITextSnapshot currentSnapshot = spans[0].Snapshot;
            SnapshotSpan entire = new SnapshotSpan(spans[0].Start, spans[spans.Count - 1].End).TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive);
            int start = entire.Start.Position;
            int end = entire.End.Position;

            foreach (ParseError err in IntellisenseProvider.LastParsedResult.ErrorHandler.Errors)
            {
                if (err.StartIndex <= end && err.StartIndex >= start)
                {
                    var startPosition = err.StartIndex;
                    var endPosition = err.EndIndex;

                    if (startPosition < currentSnapshot.Length && endPosition < currentSnapshot.Length)
                    {
                        yield return new TagSpan<AdhocErrorTag>(
                            new SnapshotSpan(currentSnapshot, startPosition, endPosition - startPosition), 
                            new AdhocErrorTag(err.Description));
                    }
                }
            }
        }
    }

    internal class AdhocErrorTag : ErrorTag
    {
        public AdhocErrorTag() : base(PredefinedErrorTypeNames.SyntaxError) { }

        public AdhocErrorTag(string message) : base(PredefinedErrorTypeNames.SyntaxError, message) { }
    }
}
