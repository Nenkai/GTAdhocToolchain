using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes a signed short onto the stack.
    /// </summary>
    public class InsShortConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.SHORT_CONST;

        public override string InstructionName => "SHORT_CONST";

        public short Value { get; set; }

        public InsShortConst(short value)
        {
            Value = value;
        }

        public InsShortConst()
        {

        }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteInt16(Value);
        }

        public override void Deserialize(AdhocStream stream)
        {
            Value = stream.ReadInt16();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: {Value} (0x{Value:X4})";
    }
}
