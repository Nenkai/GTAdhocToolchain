using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Defines a new function as a variable.
    /// </summary>
    public class InsFunctionConst : SubroutineBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.FUNCTION_CONST;

        public override string InstructionName => "FUNCTION_CONST";
    }
}
