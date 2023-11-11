using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdhocLanguage.Intellisense.Completions
{
    public class AdhocCompletionSet : CompletionSet2
    {
        private IList<Completion> _sourceCompletions;

        public AdhocCompletionSet(string moniker, string displayName, ITrackingSpan applicableTo, IList<Completion> completions, IEnumerable<Completion> completionBuilders, IReadOnlyList<IIntellisenseFilter> filters)
            : base(moniker, displayName, applicableTo, completions, completionBuilders, filters)
        {
            _sourceCompletions = completions;
        }

        /// <summary>
        /// Filters the list of completions.
        /// </summary>
        public override void Recalculate()
        {
            ITextSnapshot currentSnapshot = ApplicableTo.TextBuffer.CurrentSnapshot;
            string text = ApplicableTo.GetText(currentSnapshot);

            for (int i = _sourceCompletions.Count - 1; i >= 0; i--)
            {
                Completion4 completion = (Completion4)_sourceCompletions[i];
                if (!completion.DisplayText.AsSpan().Contains(text.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    if (this.WritableCompletions.Contains(completion))
                    {
                        this.WritableCompletions.Remove(completion);
                    }
                }
                else
                {
                    if (!this.WritableCompletions.Contains(completion))
                    {
                        this.WritableCompletions.Add(completion);
                    }
                }

            }
        }
    }
}
