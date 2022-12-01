using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Does nothing.
    /// </summary>
    public class InsNop : InstructionBase
    {
        public static readonly InsNop Empty = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.NOP;

        public override string InstructionName => "NOP";

        public override void Serialize(AdhocStream stream)
        {

        }

        public override void Deserialize(AdhocStream stream)
        {
            
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}";

    }
}
