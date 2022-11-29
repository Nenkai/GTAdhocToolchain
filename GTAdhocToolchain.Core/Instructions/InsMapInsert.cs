using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes an object to a map variable.
    /// </summary>
    public class InsMapInsert : InstructionBase
    {
        public readonly static InsMapInsert Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.MAP_INSERT;

        public override string InstructionName => "MAP_INSERT";

        public InsMapInsert()
        {
            
        }

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
