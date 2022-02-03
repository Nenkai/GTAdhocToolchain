using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Represents a symbol const literal instruction.
    /// </summary>
    public class InsSymbolConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.SYMBOL_CONST;

        public override string InstructionName => "SYMBOL_CONST";

        public AdhocSymbol String { get; set; }

        public InsSymbolConst(AdhocSymbol str)
        {
            String = str;
        }

        public InsSymbolConst()
        {

        }

        public override void Deserialize(AdhocStream stream)
        {
            String = stream.ReadSymbol();
        }

        public override string ToString()
            => $"{InstructionType}: {String.Name}";

    }
}
