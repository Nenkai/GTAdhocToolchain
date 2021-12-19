using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    public class InsImport : InstructionBase
    {
        public override string InstructionName => "IMPORT";

        public AdhocSymbol ImportNamespace { get; set; }
        public AdhocSymbol Target { get; set; }

        public InsImport(AdhocSymbol importNamespace, AdhocSymbol target)
        {
            ImportNamespace = importNamespace;
            Target = target;
        }
    }
}
