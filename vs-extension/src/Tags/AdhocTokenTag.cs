using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace AdhocLanguage
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("adhoc")]
    [TagType(typeof(AdhocTokenTag))]
    internal sealed class AdhocTokenTagProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return new AdhocTokenTagger(buffer) as ITagger<T>;
        }
    }

    public class AdhocTokenTag : ITag 
    {
        public AdhocTokenType type { get; private set; }

        public AdhocTokenTag(AdhocTokenType type)
        {
            this.type = type;
        }
    }

    internal sealed class AdhocTokenTagger : ITagger<AdhocTokenTag>
    {
        ITextBuffer _buffer;
        IDictionary<string, AdhocTokenType> _adhocTypes;

        internal AdhocTokenTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _adhocTypes = new Dictionary<string, AdhocTokenType>();
            _adhocTypes["attribute"] = AdhocTokenType.Attribute;
            _adhocTypes["false"] = AdhocTokenType.Boolean;
            _adhocTypes["true"] = AdhocTokenType.Boolean;
            _adhocTypes["method"] = AdhocTokenType.Method;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add { }
            remove { }
        }

        public IEnumerable<ITagSpan<AdhocTokenTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (SnapshotSpan curSpan in spans)
            {
                // Check current line for all tokens
                ITextSnapshotLine containingLine = curSpan.Start.GetContainingLine();
                int curLoc = containingLine.Start.Position;
                string[] tokens = containingLine.GetText().ToLower().Split(' ');

                for (int i = 0; i < tokens.Length; i++)
                {
                    string adhocToken = tokens[i];

                    // Get rid of semicolon
                    string token = adhocToken.TrimEnd(';');

                    if (token.StartsWith("\"") && token.EndsWith("\""))
                    {
                        var tokenSpan = new SnapshotSpan(curSpan.Snapshot, new Span(curLoc, token.Length));
                        yield return new TagSpan<AdhocTokenTag>(tokenSpan, new AdhocTokenTag(AdhocTokenType.String));
                    }

                    if (_adhocTypes.ContainsKey(token))
                    {
                        var tokenSpan = new SnapshotSpan(curSpan.Snapshot, new Span(curLoc, token.Length));

                        // Is cursor on actual token span?
                        if (tokenSpan.IntersectsWith(curSpan))
                        {
                            // It's a hit
                            yield return new TagSpan<AdhocTokenTag>(tokenSpan, new AdhocTokenTag(_adhocTypes[token]));
                        }
                    }

                    var tSpan = new SnapshotSpan(curSpan.Snapshot, new Span(curLoc, token.Length));
                    if (tSpan.IntersectsWith(curSpan))
                    {
                        // Check for previous keywords
                        if (i > 1 && tokens[i - 1] == "method")
                        {
                            yield return new TagSpan<AdhocTokenTag>(tSpan, new AdhocTokenTag(AdhocTokenType.Method));
                        }
                    }


                    //add an extra char location because of the space
                    curLoc += adhocToken.Length + 1;
                }
            }
        }
    }
}
