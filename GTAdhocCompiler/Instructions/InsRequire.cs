using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Pops a string value from the stack and imports it as a module and loads its adhoc code file if needed.
    /// </summary>
    public class InsRequire : InstructionBase
    {
        public readonly static InsRequire Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.REQUIRE;

        public override string InstructionName => "REQUIRE";

        public InsRequire()
        {
            
        }
    }
}
