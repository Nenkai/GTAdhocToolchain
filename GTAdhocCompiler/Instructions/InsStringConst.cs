using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Represents a string const or string literal instruction.
    /// </summary>
    public class InsStringConst : InstructionBase
    {
        public override string InstructionName => "STRING_CONST";

        public AdhocSymbol String { get; set; }

        public InsStringConst(AdhocSymbol str)
        {
            String = str;
        }
    }
}
