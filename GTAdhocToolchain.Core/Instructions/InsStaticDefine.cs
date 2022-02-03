using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsStaticDefine : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.STATIC_DEFINE;

        public override string InstructionName => "STATIC_DEFINE";

        public AdhocSymbol Name { get; set; }
        public InsStaticDefine(AdhocSymbol name)
        {
            Name = name;
        }
    }
}
