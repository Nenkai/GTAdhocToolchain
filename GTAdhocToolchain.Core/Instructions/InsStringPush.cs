using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Represents a string join/push instruction.
    /// </summary>
    public class InsStringPush : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.STRING_PUSH;

        public override string InstructionName => "STRING_PUSH";

        public int StringCount { get; set; }

        public InsStringPush(int stringCount)
        {
            StringCount = stringCount;
        }

        public InsStringPush()
        {

        }

        public override void Deserialize(AdhocStream stream)
        {
            StringCount = stream.ReadInt32();
        }

        public override string ToString()
            => $"{InstructionType}: StringIndex={StringCount}";
    }
}
