using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Defines a new function within the scope. Pops all the arguments before the definition.
    /// </summary>
    public class InsFunctionDefine : SubroutineBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.FUNCTION_DEFINE;

        public override string InstructionName => "FUNCTION_DEFINE";
    }
}
