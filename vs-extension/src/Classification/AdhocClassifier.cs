using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace AdhocLanguage
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("adhoc")]
    [TagType(typeof(ClassificationTag))]
    internal sealed class AdhocClassifierProvider : ITaggerProvider
    {
        // Declare that .ad is bind to content type "adhoc" globally
        [Export]
        [FileExtension(".ad")]
        [ContentType("adhoc")]
        internal static FileExtensionToContentTypeDefinition AdhocFileType = null;

        // Define the content type
        [Export]
        [Name("adhoc")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)] // This properly marks the language as "use code, but use textmate file for syntax highlighting"
        internal static ContentTypeDefinition AdhocContentType = null;

        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistry = null;

        [Import]
        internal IBufferTagAggregatorFactoryService aggregatorFactory = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            ITagAggregator<AdhocTokenTag> adhocTagAggregator = aggregatorFactory.CreateTagAggregator<AdhocTokenTag>(buffer);
            return new AdhocClassifier(buffer, adhocTagAggregator, ClassificationTypeRegistry) as ITagger<T>;
        }
    }

    internal sealed class AdhocClassifier : ITagger<ClassificationTag>
    {
        ITextBuffer _buffer;
        ITagAggregator<AdhocTokenTag> _aggregator;
        IDictionary<AdhocTokenType, IClassificationType> _adhocTypes;

        /// <summary>
        /// Construct the classifier and define search tokens
        /// </summary>
        internal AdhocClassifier(ITextBuffer buffer,
                               ITagAggregator<AdhocTokenTag> adhocTagAggregator,
                               IClassificationTypeRegistryService typeService)
        {
            _buffer = buffer;
            _aggregator = adhocTagAggregator;
            _adhocTypes = new Dictionary<AdhocTokenType, IClassificationType>();
            _adhocTypes[AdhocTokenType.String] = typeService.GetClassificationType("string");
            _adhocTypes[AdhocTokenType.Attribute] = typeService.GetClassificationType("attribute");
            _adhocTypes[AdhocTokenType.Boolean] = typeService.GetClassificationType("boolean");
            _adhocTypes[AdhocTokenType.Method] = typeService.GetClassificationType("method");
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add { }
            remove { }
        }

        /// <summary>
        /// Search the given span for any instances of classified tags
        /// </summary>
        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var tagSpan in _aggregator.GetTags(spans))
            {
                var tagSpans = tagSpan.Span.GetSpans(spans[0].Snapshot);
                yield return new TagSpan<ClassificationTag>(tagSpans[0], new ClassificationTag(_adhocTypes[tagSpan.Tag.type]));
            }
        }
    }
}
