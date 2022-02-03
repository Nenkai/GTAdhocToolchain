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
        public override AdhocInstructionType InstructionType => AdhocInstructionType.ELEMENT_PUSH;

        public override string InstructionName => "ELEMENT_PUSH";
    }
}
