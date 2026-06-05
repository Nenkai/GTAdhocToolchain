using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes an array const into the variable storage.
    /// </summary>
    public class InsArrayConstOld : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.ARRAY_CONST_OLD;

        public override string InstructionName => "ARRAY_CONST_OLD";

        public int ArraySize { get; set; }

        public InsArrayConstOld(int size)
        {
            ArraySize = size;
        }

        public InsArrayConstOld()
        {

        }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteInt32(ArraySize);
        }

        public override void Deserialize(AdhocStream stream)
        {
            ArraySize = stream.ReadInt32();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: [{ArraySize}]";
    }
}
