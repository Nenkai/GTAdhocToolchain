using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes an unsigned integer onto the stack.
    /// </summary>
    public class InsUIntConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.U_INT_CONST;

        public override string InstructionName => "U_INT_CONST";

        public uint Value { get; set; }

        public InsUIntConst(uint value)
        {
            Value = value;
        }

        public InsUIntConst()
        {

        }

        public override void Deserialize(AdhocStream stream)
        {
            Value = stream.ReadUInt32();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: {Value})";
    }
}
