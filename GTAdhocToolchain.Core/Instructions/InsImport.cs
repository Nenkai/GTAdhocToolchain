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
            => $"{InstructionType}: {ImportNamespaceParts[^1].Name}, Unk2={ModuleValue.Name}, Unk3={ImportAs.Name}";
    }
}
