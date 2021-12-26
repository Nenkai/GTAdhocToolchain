using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Pushes an array const into the variable storage.
    /// </summary>
    public class InsArrayConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.ARRAY_CONST;

        public override string InstructionName => "ARRAY_CONST";

        public uint ArraySize { get; set; }

        public InsArrayConst(uint size)
        {
            ArraySize = size;
        }
    }
}
