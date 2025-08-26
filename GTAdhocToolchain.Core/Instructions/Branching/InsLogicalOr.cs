using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Logical or instruction.
    /// </summary>
    public class InsLogicalOr : InsLogicalBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.LOGICAL_OR;

        public override string InstructionName => "LOGICAL_OR";
    }
}
