using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes a Nil pointer on the stack.
    /// </summary>
    public class InsNilConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.NIL_CONST;

        public override string InstructionName => "NIL_CONST";

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
