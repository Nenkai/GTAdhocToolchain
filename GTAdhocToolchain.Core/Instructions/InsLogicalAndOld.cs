using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Logical and instruction.
    /// </summary>
    public class InsLogicalAndOld : InsLogicalBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.LOGICAL_AND_OLD;

        public override string InstructionName => "LOGICAL_AND_OLD";
    }
}
