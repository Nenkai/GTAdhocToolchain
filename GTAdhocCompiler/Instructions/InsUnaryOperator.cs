using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Represents a unary operator instruction.
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
