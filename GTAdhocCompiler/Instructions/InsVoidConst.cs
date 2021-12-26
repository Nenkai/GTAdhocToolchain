using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    public class InsVoidConst : InstructionBase
    {
        public static readonly InsVoidConst Empty = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.VOID_CONST;

        public override string InstructionName => "VOID_CONST";
    }
}
