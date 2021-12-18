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
        public int RewindIndex { get; set; }
    }
}
