using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsModuleConstructor : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.MODULE_CONSTRUCTOR;

        public override string InstructionName => "MODULE_CONSTRUCTOR";

        public InsModuleConstructor()
        {
            
        }

        public override void Deserialize(AdhocStream stream)
        {
            
        }

        public override string ToString()
           => InstructionName.ToString();
    }
}
