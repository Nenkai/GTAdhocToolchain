using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Adds a new module constructor for a variable.
    /// </summary>
    public class InsModuleConstructor : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.MODULE_CONSTRUCTOR;

        public override string InstructionName => "MODULE_CONSTRUCTOR";

        public InsModuleConstructor()
        {
            
        }

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
           => InstructionName.ToString();
    }
}
