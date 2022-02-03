using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pops two values from the stack, pushes the first value to the second one which should be an array.
    /// </summary>
    public class InsArrayPush : InstructionBase
    {
        public readonly static InsArrayPush Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.ARRAY_PUSH;

        public override string InstructionName => "ARRAY_PUSH";

        public override void Deserialize(AdhocStream stream)
        {

        }

        public override string ToString()
            => $"{InstructionType}";
    }
}
