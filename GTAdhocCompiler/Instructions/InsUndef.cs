using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Undefines a symbol.
    /// </summary>
    public class InsUndef : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.UNDEF;

        public override string InstructionName => "UNDEF";

        public List<AdhocSymbol> Symbols { get; set; } = new();
    }
}
