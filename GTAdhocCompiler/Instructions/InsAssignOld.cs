using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Pops 2 pointers off the stack, and copies the second item to the first item
    /// </summary>
    public class InsAssignOld : InstructionBase
    {
        public readonly static InsAssignOld Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.ASSIGN_OLD;

        public override string InstructionName => "ASSIGN_OLD";
    }
}
