using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsJumpIfNil : InsLogicalBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.JUMP_IF_NIL;

        public override string InstructionName => "JUMP_IF_NIL";
    }
}
