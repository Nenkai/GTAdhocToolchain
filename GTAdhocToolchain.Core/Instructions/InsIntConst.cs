using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
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

        public InsIntConst()
        {

        }

        public override void Deserialize(AdhocStream stream)
        {
            Value = stream.ReadInt32();
        }

        public override string ToString()
            => $"{InstructionType}: {Value} (0x{Value:X2})";
    }
}
