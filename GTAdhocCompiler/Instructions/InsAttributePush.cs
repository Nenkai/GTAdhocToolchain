using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Pushes a new variable into an object's attribute.
    /// </summary>
    public class InsAttributePush : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.ATTRIBUTE_PUSH;

        public override string InstructionName => "ATTRIBUTE_PUSH";

        public List<AdhocSymbol> AttributeSymbols { get; set; } = new();
    }
}
