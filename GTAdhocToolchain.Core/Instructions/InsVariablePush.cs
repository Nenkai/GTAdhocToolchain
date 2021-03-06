using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes a new variable into the variable storage. Value will be pushed on the stack.
    /// </summary>
    public class InsVariablePush : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.VARIABLE_PUSH;
        public override string InstructionName => "VARIABLE_PUSH";

        public List<AdhocSymbol> VariableSymbols { get; set; } = new();
        public bool IsStatic => VariableSymbols.Count > 1;

        public int VariableStorageIndex { get; set; }

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
