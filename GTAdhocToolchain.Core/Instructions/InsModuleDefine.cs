using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsModuleDefine : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.MODULE_DEFINE;

        public override string InstructionName => "MODULE_DEFINE";

        public List<AdhocSymbol> Names { get; set; } = new();

        public InsModuleDefine()
        {
            
        }
    }
}
