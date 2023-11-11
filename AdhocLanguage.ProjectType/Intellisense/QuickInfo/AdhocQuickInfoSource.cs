using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using System.Threading.Tasks;
using System.Threading;

namespace AdhocLanguage.Intellisense.QuickInfo
{
    /// <summary>
    /// Factory for quick info sources
    /// </summary>
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [ContentType("adhoc")]
    [Name("adhocQuickInfo")]
    class AdhocQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {

        [Import]
        IBufferTagAggregatorFactoryService aggService = null;

        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new AdhocQuickInfoSource(textBuffer, aggService.CreateTagAggregator<AdhocTokenTag>(textBuffer));
        }
    }

    /// <summary>
    /// Provides QuickInfo information to be displayed in a text buffer
    /// </summary>
    class AdhocQuickInfoSource : IAsyncQuickInfoSource
    {
        private ITagAggregator<AdhocTokenTag> _aggregator;
        private ITextBuffer _buffer;
        private bool _disposed = false;


        public AdhocQuickInfoSource(ITextBuffer buffer, ITagAggregator<AdhocTokenTag> aggregator)
        {
            _aggregator = aggregator;
            _buffer = buffer;
        }

        
        /// <summary>
        /// Determine which pieces of Quickinfo content should be displayed
        /// </summary>
        public Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException("TestQuickInfoSource");

            var triggerPoint = (SnapshotPoint) session.GetTriggerPoint(_buffer.CurrentSnapshot);

            if (triggerPoint == null)
                return Task.FromResult<QuickInfoItem>(null);

            /*
            foreach (IMappingTagSpan<AdhocTokenTag> curTag in _aggregator.GetTags(new SnapshotSpan(triggerPoint, triggerPoint)))
            {
                if (curTag.Tag.type == AdhocTokenType.Attribute)
                {
                    var tagSpan = curTag.Span.GetSpans(_buffer).First();
                    applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(tagSpan, SpanTrackingMode.EdgeExclusive);
                    quickInfoContent.Add("Module/Class Member");
                }
                else if (curTag.Tag.type == AdhocTokenType.String)
                {
                    var tagSpan = curTag.Span.GetSpans(_buffer).First();
                    applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(tagSpan, SpanTrackingMode.EdgeExclusive);
                    quickInfoContent.Add("String");
                }
                else if (curTag.Tag.type == AdhocTokenType.Boolean)
                {
                    var tagSpan = curTag.Span.GetSpans(_buffer).First();
                    applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(tagSpan, SpanTrackingMode.EdgeExclusive);
                    quickInfoContent.Add("Boolean");
                }
                else if (curTag.Tag.type == AdhocTokenType.Method)
                {
                    var tagSpan = curTag.Span.GetSpans(_buffer).First();
                    applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(tagSpan, SpanTrackingMode.EdgeExclusive);
                    quickInfoContent.Add("Method");
                }
            }*/

            return Task.FromResult<QuickInfoItem>(null);
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}

