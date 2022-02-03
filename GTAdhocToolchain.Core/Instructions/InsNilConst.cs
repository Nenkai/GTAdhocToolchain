using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsNilConst : InstructionBase
    {
        public static readonly InsNilConst Empty = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.NIL_CONST;

        public override string InstructionName => "NIL_CONST";
    }
}
