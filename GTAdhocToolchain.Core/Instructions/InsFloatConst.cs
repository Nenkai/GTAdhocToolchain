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

        public InsFloatConst()
        {

        }

        public override void Deserialize(AdhocStream stream)
        {
            Value = stream.ReadSingle();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: Value={Value}";
    }
}
