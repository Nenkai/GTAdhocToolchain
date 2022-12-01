using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Represents an array or map element push.
    /// </summary>
    public class InsElementPush : InstructionBase
    {
        public readonly static InsElementPush Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.ELEMENT_PUSH;

        public override string InstructionName => "ELEMENT_PUSH";

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
