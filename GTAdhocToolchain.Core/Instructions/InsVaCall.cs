using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsVaCall : InstructionBase
    {
        public static readonly InsVoidConst Empty = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.VA_CALL;

        public override string InstructionName => "VA_CALL";

        public uint Value { get; set; }

        public override void Deserialize(AdhocStream stream)
        {
            Value = stream.ReadUInt32();
        }

        public override string ToString()
            => $"{InstructionType}: Value={Value}";

    }
}


