using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Represents a unary assignment operator instruction.
    /// </summary>
    public class InsUnaryAssignOperator : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.UNARY_ASSIGN_OPERATOR;

        public override string InstructionName => "UNARY_ASSIGN_OPERATOR";

        public AdhocSymbol Operator { get; set; }

        public InsUnaryAssignOperator(AdhocSymbol opSymbol)
        {
            Operator = opSymbol;
        }
    }
}
