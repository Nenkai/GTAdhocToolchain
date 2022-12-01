using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Applies a binary operation for the last two stack variables, and assigns the result to the last value.
    /// </summary>
    public class InsBinaryAssignOperator : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.BINARY_ASSIGN_OPERATOR;

        public override string InstructionName => "BINARY_ASSIGN_OPERATOR";

        public AdhocSymbol Operator { get; set; }

        public InsBinaryAssignOperator(AdhocSymbol opSymbol)
        {
            Operator = opSymbol;
        }

        public InsBinaryAssignOperator()
        {

        }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteSymbol(Operator);
        }

        public override void Deserialize(AdhocStream stream)
        {
            Operator = stream.ReadSymbol();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: {Utils.OperatorNameToPunctuator(Operator.Name)} ({Operator.Name})";
    }
}
