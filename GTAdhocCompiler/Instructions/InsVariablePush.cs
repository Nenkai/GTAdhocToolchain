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
        public AdhocSymbol VariableSymbol { get; set; }

        public int StackIndex { get; set; }
    }
}
