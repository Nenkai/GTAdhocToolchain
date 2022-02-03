using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Defines a new function as a variable.
    /// </summary>
    public class InsFunctionConst : SubroutineBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.FUNCTION_CONST;

        public override string InstructionName => "FUNCTION_CONST";

        public override void Deserialize(AdhocStream stream)
        {
            CodeFrame.Read(stream);
        }

        public InsFunctionConst()
        {

        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(InstructionType.ToString()).Append(" - ");
            sb.Append(CodeFrame.Dissasemble());
            return sb.ToString();
        }
    }
}
