using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace GTAdhocToolchain.Core.Instructions
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Pops 2 pointers off the stack, and copies the second item to the first item
    /// </summary>
    public class InsAssign : InstructionBase
    {
        public readonly static InsAssign Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.ASSIGN;

        public override string InstructionName => "ASSIGN";

        public override void Serialize(AdhocStream stream)
        {

        }

        public override void Deserialize(AdhocStream strema)
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
