using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsFloatConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.FLOAT_CONST;

        public override string InstructionName => "FLOAT_CONST";

        public float Value { get; set; }

        public InsFloatConst(float value)
        {
            Value = value;
        }
    }
}
