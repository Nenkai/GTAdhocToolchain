using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Represents a string const or string literal instruction.
    /// </summary>
    public class InsStringConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.STRING_CONST;

        public override string InstructionName => "STRING_CONST";

        public AdhocSymbol String { get; set; }

        public InsStringConst(AdhocSymbol str)
        {
            String = str;
        }

        public InsStringConst()
        {

        }

        public override void Deserialize(AdhocStream stream)
        {
            String = stream.ReadSymbol();
        }

        public override string ToString()
            => $"{InstructionType}: {String.Name}";
    }
}
