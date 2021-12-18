using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    public class InsVariableEvaluation : InstructionBase
    {
        public AdhocSymbol VariableSymbol { get; set; }

        public int StackIndex { get; set; }
    }
}
