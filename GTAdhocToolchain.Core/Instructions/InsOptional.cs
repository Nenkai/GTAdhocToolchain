using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsOptional : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.LOGICAL_OPTIONAL;

        public override string InstructionName => "LOGICAL_OPTIONAL";

        public override void Serialize(AdhocStream stream)
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
             => InstructionType.ToString();
    }
}
