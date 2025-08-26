using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes a long onto the stack.
    /// </summary>
    public class InsLongConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.LONG_CONST;

        public override string InstructionName => "LONG_CONST";

        public long Value { get; set; }

        public InsLongConst(long value)
        {
            Value = value;
        }

        public InsLongConst()
        {

        }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteInt64(Value);
        }

        public override void Deserialize(AdhocStream stream)
        {
            Value = stream.ReadInt64();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: {Value} (0x{Value:X2})";
    }
}
