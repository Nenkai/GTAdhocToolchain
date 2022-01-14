using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Signals scope leaving to rewind the stack to prior context.
    /// </summary>
    public class InsLeaveScope : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.LEAVE;

        public override string InstructionName => "LEAVE";

        public int ModuleOrClassDepth { get; set; }

        public int VariableStorageRewindIndex { get; set; }
    }
}
