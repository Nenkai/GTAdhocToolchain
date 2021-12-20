using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Pushes a new variable onto the stack.
    /// </summary>
    public class InsVariablePush : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.VARIABLE_PUSH;

        public override string InstructionName => "VARIABLE_PUSH";

        public List<AdhocSymbol> VariableSymbols { get; set; } = new();

        public int StackIndex { get; set; }
    }
}
