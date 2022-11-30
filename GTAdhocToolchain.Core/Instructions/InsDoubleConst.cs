using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes a double onto the stack.
    /// </summary>
    public class InsDoubleConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.DOUBLE_CONST;

        public override string InstructionName => "DOUBLE_CONST";

        public double Value { get; set; }

        public InsDoubleConst(double value)
        {
            Value = value;
        }

        public InsDoubleConst()
        {

        }

        public override void Deserialize(AdhocStream stream)
        {
            Value = stream.ReadDouble();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
           => $"{InstructionType}: Value={Value}";
    }
}
