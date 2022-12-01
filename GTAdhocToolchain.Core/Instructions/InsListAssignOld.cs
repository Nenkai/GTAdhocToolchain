using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Deconstructs an array into elements.
    /// </summary>
    public class InsListAssignOld : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.LIST_ASSIGN_OLD;

        public override string InstructionName => "LIST_ASSIGN_OLD";

        public int VariableCount { get; set; }

        public InsListAssignOld(int varCount)
        {
            VariableCount = varCount;
        }

        public InsListAssignOld()
        {

        }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteInt32(VariableCount);
        }

        public override void Deserialize(AdhocStream stream)
        {
            VariableCount = stream.ReadInt32();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
           => $"{InstructionType}: Unk={VariableCount}";
    }
}
