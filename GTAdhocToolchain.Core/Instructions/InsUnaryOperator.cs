using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Represents a unary operator instruction. Pops the last value into the stack, applies the operand and pushes back into the stack.
    /// </summary>
    public class InsUnaryOperator : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.UNARY_OPERATOR;

        public override string InstructionName => "UNARY_OPERATOR";

        public AdhocSymbol Operator { get; set; }

        public InsUnaryOperator(AdhocSymbol opSymbol)
        {
            Operator = opSymbol;
        }
    }
}
