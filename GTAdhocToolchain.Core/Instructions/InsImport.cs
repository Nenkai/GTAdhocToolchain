using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsImport : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.IMPORT;

        public override string InstructionName => "IMPORT";

        public List<AdhocSymbol> ImportNamespaceParts { get; set; } = new();
        public AdhocSymbol ModuleValue { get; set; }

        public InsImport()
        {
            
        }
    }
}
