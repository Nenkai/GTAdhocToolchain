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
    public class InsAssignPop : InstructionBase
    {
        public readonly static InsAssignPop Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.ASSIGN_POP;

        public override string InstructionName => "ASSIGN_POP";

        public override void Deserialize(AdhocStream stream)
        {

        }

        public override string ToString()
            => $"{InstructionType}";
    }
}
