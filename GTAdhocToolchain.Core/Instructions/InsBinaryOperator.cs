using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
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

        public InsBinaryOperator()
        {

        }

        public override void Deserialize(AdhocStream stream)
        {
            Operator = stream.ReadSymbol();
        }

        public override string ToString()
            => $"{InstructionType}: {Utils.OperatorNameToPunctuator(Operator.Name)} ({Operator.Name})";

    }
}
