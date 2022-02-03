using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pops 2 pointers off the stack, and copies the second item to the first item
    /// </summary>
    public class InsAssign : InstructionBase
    {
        public readonly static InsAssign Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.ASSIGN;

        public override string InstructionName => "ASSIGN";

        public override void Deserialize(AdhocStream strema)
        {

        }

        public override string ToString()
            => $"{InstructionType}";
    }
}
