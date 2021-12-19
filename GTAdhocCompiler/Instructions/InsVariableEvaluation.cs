using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    public class InsVariableEvaluation : InstructionBase
    {
        public override string InstructionName => "VARIABLE_EVAL";

        public List<AdhocSymbol> VariableSymbols { get; set; } = new();

        public int StackIndex { get; set; }
    }
}
