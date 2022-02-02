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

        /// <summary>
        /// Rewinds the depth to a certain point.
        /// NOT USED AFTER GT5
        /// </summary>
        public int ModuleOrClassDepthRewindIndex { get; set; }

        /// <summary>
        /// Rewinds the variable storage to a certain point (sets all to nil).
        /// When set to 1 and depth value is set, this is ignored.
        /// </summary>
        public int VariableStorageRewindIndex { get; set; }
    }
}
