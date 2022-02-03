using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class Ins69 : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.UNK_69;

        public override string InstructionName => "UNK_69";

        public AdhocSymbol Value { get; set; }

        public override void Deserialize(AdhocStream stream)
        {
            Value = stream.ReadSymbol();
        }

        public override string ToString()
            => $"{InstructionType}: {Value}";
    }
}
