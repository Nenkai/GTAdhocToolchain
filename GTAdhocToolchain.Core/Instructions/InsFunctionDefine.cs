using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Defines a new function within the scope. Pops all the arguments before the definition.
    /// </summary>
    public class InsFunctionDefine : SubroutineBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.FUNCTION_DEFINE;

        public override string InstructionName => "FUNCTION_DEFINE";

        public override void Deserialize(AdhocStream stream)
        {
            Name = stream.ReadSymbol();
            CodeFrame.Read(stream);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(InstructionType.ToString()).Append(" - ").Append(Name.Name);
            sb.Append(CodeFrame.Dissasemble());
            return sb.ToString();
        }
    }
}
