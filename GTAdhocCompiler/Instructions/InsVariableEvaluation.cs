using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Evaluates a symbol, puts or gets it into the specified variable storage index. Will push the value onto the stack.
    /// </summary>
    public class InsVariableEvaluation : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.VARIABLE_EVAL;

        public override string InstructionName => "VARIABLE_EVAL";

        public List<AdhocSymbol> VariableSymbols { get; set; } = new();

        public int VariableStorageIndex { get; set; }

        public InsVariableEvaluation(int index)
        {
            VariableStorageIndex = index;
        }

        public InsVariableEvaluation()
        {

        }
    }
}
