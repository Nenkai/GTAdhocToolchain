using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes string onto the stack.
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

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteSymbol(String);
        }

        public override void Deserialize(AdhocStream stream)
        {
            String = stream.ReadSymbol();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: {String.Name}";
    }
}
