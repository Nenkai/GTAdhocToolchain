using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
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
        public bool IsStatic => VariableSymbols.Count > 1;

        public InsVariableEvaluation(int index)
        {
            VariableStorageIndex = index;
        }

        public InsVariableEvaluation()
        {

        }

        public override void Deserialize(AdhocStream stream)
        {
            VariableSymbols = stream.ReadSymbols();
            VariableStorageIndex = stream.ReadInt32();
        }

        public override string ToString()
        {
            if (IsStatic)
                return $"{InstructionType}: {string.Join(',', VariableSymbols.Select(e => e.Name))}, Static:{VariableStorageIndex}";
            else
                return $"{InstructionType}: {string.Join(',', VariableSymbols.Select(e => e.Name))}, Local:{VariableStorageIndex}";
        }
    }
}
