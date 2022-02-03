using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsVoidConst : InstructionBase
    {
        public static readonly InsVoidConst Empty = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.VOID_CONST;

        public override string InstructionName => "VOID_CONST";

        public override void Deserialize(AdhocStream stream)
        {

        }

        public override string ToString()
            => $"{InstructionType}";

    }
}
