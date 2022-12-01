using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Undefines a symbol.
    /// </summary>
    public class InsUndef : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.UNDEF;

        public override string InstructionName => "UNDEF";

        public List<AdhocSymbol> Symbols { get; set; } = new();

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteSymbols(Symbols);
        }

        public override void Deserialize(AdhocStream stream)
        {
            Symbols = stream.ReadSymbols();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: {string.Join(",", Symbols.Select(e => e.Name))}";
    }
}
