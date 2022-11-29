using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Evaluates an attribute, puts or gets it into the specified variable storage index. Will push the value onto the stack.
    /// </summary>
    public class InsAttributeEvaluation : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.ATTRIBUTE_EVAL;

        public override string InstructionName => "ATTRIBUTE_EVAL";

        public List<AdhocSymbol> AttributeSymbols { get; set; } = new();

        public override void Deserialize(AdhocStream stream)
        {
            AttributeSymbols = stream.ReadSymbols();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: {string.Join(',', AttributeSymbols.Select(e => e.Name))}";
    }
}
