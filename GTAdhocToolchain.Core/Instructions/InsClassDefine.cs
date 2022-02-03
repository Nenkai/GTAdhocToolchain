using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsClassDefine : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.CLASS_DEFINE;

        public override string InstructionName => "CLASS_DEFINE";

        public AdhocSymbol Name { get; set; }

        public List<AdhocSymbol> ExtendsFrom { get; set; } = new();

        public InsClassDefine()
        {
            
        }
    }
}
