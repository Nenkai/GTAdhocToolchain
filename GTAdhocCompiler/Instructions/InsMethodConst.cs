using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Defines a new method as a variable.
    /// </summary>
    public class InsMethodConst : SubroutineBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.METHOD_CONST;

        public override string InstructionName => "METHOD_CONST";
    }
}
