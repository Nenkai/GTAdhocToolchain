using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsULongConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.U_LONG_CONST;

        public override string InstructionName => "U_LONG_CONST";

        public ulong Value { get; set; }

        public InsULongConst(ulong value)
        {
            Value = value;
        }

        public InsULongConst()
        {

        }

        public override void Deserialize(AdhocStream stream)
        {
            Value = stream.ReadUInt64();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: {Value})";
    }
}
