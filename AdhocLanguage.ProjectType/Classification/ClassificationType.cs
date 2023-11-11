using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace AdhocLanguage
{
    internal static class OrdinaryClassificationDefinition
    {
        #region Type definition

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("attribute")]
        internal static ClassificationTypeDefinition adhocAttribute = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("boolean")]
        internal static ClassificationTypeDefinition adhocBoolean = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("method")]
        internal static ClassificationTypeDefinition adhocMethod = null;

        #endregion
    }
}
