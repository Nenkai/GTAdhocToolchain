using Syroot.BinaryData;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Prints text onto the game's console. Debug stripped.
    /// </summary>
    public class InsPrint : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.PRINT;

        public override string InstructionName => "PRINT";

        public int ArgCount { get; set; }

        public InsPrint()
        {

        }

        public InsPrint(int argCount)
        {
            ArgCount = argCount;
        }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteInt32(ArgCount);
        }

        public override void Deserialize(AdhocStream stream)
        {
            ArgCount = stream.ReadInt32();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: ArgCount={ArgCount}";

    }
}
