using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
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

        public InsUnaryAssignOperator()
        {

        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override void Deserialize(AdhocStream stream)
        {
            Operator = stream.ReadSymbol();
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: {Utils.OperatorNameToPunctuator(Operator.Name)} ({Operator.Name})";

    }
}
