using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsUIntConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.U_INT_CONST;

        public override string InstructionName => "U_INT_CONST";

        public uint Value { get; set; }

        public InsUIntConst(uint value)
        {
            Value = value;
        }
    }
}
