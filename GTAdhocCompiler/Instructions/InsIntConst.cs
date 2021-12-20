using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    public class InsIntConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.INT_CONST;

        public override string InstructionName => "INT_CONST";

        public int Value { get; set; }

        public InsIntConst(int value)
        {
            Value = value;
        }
    }
}
