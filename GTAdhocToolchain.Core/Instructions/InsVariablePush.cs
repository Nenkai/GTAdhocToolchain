using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes a value into a certain symbol. Value will be pushed on the stack.
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
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
        {
            if (asCompareMode)
            {
                if (IsStatic)
                    return $"{InstructionType}: {string.Join(',', VariableSymbols.Select(e => e.Name.Split("#")[0]))}, Static:{VariableStorageIndex}";
                else
                    return $"{InstructionType}: {string.Join(',', VariableSymbols.Select(e => e.Name.Split("#")[0]))}, Local:{VariableStorageIndex}";
            }
            else
            {
                if (IsStatic)
                    return $"{InstructionType}: {string.Join(',', VariableSymbols.Select(e => e.Name))}, Static:{VariableStorageIndex}";
                else
                    return $"{InstructionType}: {string.Join(',', VariableSymbols.Select(e => e.Name))}, Local:{VariableStorageIndex}";
            }
        }
    }
}
