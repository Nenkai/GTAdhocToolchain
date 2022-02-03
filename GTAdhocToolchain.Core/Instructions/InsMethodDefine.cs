using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Defines a new method within the scope. Pops all the arguments before the definition.
    /// </summary>
    public class InsMethodDefine : SubroutineBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.METHOD_DEFINE;

        public override string InstructionName => "METHOD_DEFINE";
    }
}
