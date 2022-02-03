using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Deconstructs an array into elements.
    /// </summary>
    public class InsListAssign : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.LIST_ASSIGN;

        public override string InstructionName => "LIST_ASSIGN";

        public int VariableCount { get; set; }

        public bool Unk { get; set; }

        public InsListAssign(int varCount)
        {
            VariableCount = varCount;
        }
    }
}
