using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Imports (copies) a symbol (or all with wildcard) from a path onto the current scope.
    /// </summary>
    public class InsImport : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.IMPORT;

        public override string InstructionName => "IMPORT";

        public List<AdhocSymbol> ImportNamespaceParts { get; set; } = new();
        public AdhocSymbol ModuleValue { get; set; }
        public AdhocSymbol ImportAs { get; set; }

        public InsImport()
        {
            
        }

        public override void Deserialize(AdhocStream stream)
        {
            ImportNamespaceParts = stream.ReadSymbols();
            ModuleValue = stream.ReadSymbol();

            if (stream.Version > 9)
                ImportAs = stream.ReadSymbol();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
        {
            string str = $"{InstructionType}: Path:{ImportNamespaceParts[^1].Name}, Property:{ModuleValue.Name}";
            if (ImportAs is not null)
                str += $", ImportAs:{ImportAs.Name}";
            return str;
        }
    }
}
