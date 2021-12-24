using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Represents a binary operator instruction. Pops two values from the stack, pushes the result into the stack.
    /// </summary>
    public class InsBinaryOperator : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.BINARY_OPERATOR;

        public override string InstructionName => "BINARY_OPERATOR";

        public AdhocSymbol Operator { get; set; }

        public InsBinaryOperator(AdhocSymbol opSymbol)
        {
            Operator = opSymbol;
        }
    }
}
