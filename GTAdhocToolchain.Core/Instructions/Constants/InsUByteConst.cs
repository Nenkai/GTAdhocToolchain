using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes an unsigned byte onto the stack.
    /// </summary>
    public class InsUByteConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.U_BYTE_CONST;

        public override string InstructionName => "UBYTE_CONST";

        public byte Value { get; set; }

        public InsUByteConst(byte value)
        {
            Value = value;
        }

        public InsUByteConst()
        {

        }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteByte(Value);
        }

        public override void Deserialize(AdhocStream stream)
        {
            Value = stream.Read1Byte();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: {Value} (0x{Value:X2})";
    }
}
