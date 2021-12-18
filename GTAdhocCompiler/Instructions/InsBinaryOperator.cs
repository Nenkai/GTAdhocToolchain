using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Represents a binary operator instruction.
    /// </summary>
    public class InsBinaryOperator : InstructionBase
    {
        public AdhocSymbol Operator { get; set; }

        public InsBinaryOperator(AdhocSymbol opSymbol)
        {
            Operator = opSymbol;
        }
    }
}
