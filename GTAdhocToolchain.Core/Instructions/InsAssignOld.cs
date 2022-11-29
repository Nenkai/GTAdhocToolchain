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
    public class InsAssignOld : InstructionBase
    {
        public readonly static InsAssignOld Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.ASSIGN_OLD;

        public override string InstructionName => "ASSIGN_OLD";

        public override void Deserialize(AdhocStream stream)
        {

        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}";
    }
}
