using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    public class InsImport : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.IMPORT;

        public override string InstructionName => "IMPORT";

        public List<AdhocSymbol> ImportNamespaceParts { get; set; } = new();
        public AdhocSymbol Target { get; set; }

        public InsImport()
        {
            
        }
    }
}
