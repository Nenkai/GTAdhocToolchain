using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes an array const into the variable storage.
    /// </summary>
    public class InsArrayConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.ARRAY_CONST;

        public override string InstructionName => "ARRAY_CONST";

        public uint ArraySize { get; set; }

        public InsArrayConst(uint size)
        {
            ArraySize = size;
        }

        public InsArrayConst()
        {

        }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteUInt32(ArraySize);
        }

        public override void Deserialize(AdhocStream stream)
        {
            ArraySize = stream.ReadUInt32();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: [{ArraySize}]";
    }
}
