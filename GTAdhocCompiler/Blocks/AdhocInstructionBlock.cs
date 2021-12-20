using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTAdhocCompiler.Instructions;

namespace GTAdhocCompiler
{
    /// <summary>
    /// Represents a block of instructions.
    /// </summary>
    public class AdhocInstructionBlock
    {
        public List<InstructionBase> Instructions { get; set; } = new();
        public AdhocSymbol SourceFilePath { get; private set; }

        public List<AdhocSymbol> Parameters { get; set; } = new List<AdhocSymbol>();
        public List<AdhocSymbol> CallbackParameters { get; set; } = new List<AdhocSymbol>();

        public void SetSourcePath(AdhocSymbolMap symbolMap, string path)
        {
            SourceFilePath = symbolMap.RegisterSymbol(path);
        }

        public void AddInstruction(InstructionBase ins, int lineNumber)
        {
            ins.LineNumber = lineNumber;
            Instructions.Add(ins);
        }

        public int GetLastInstructionIndex()
        {
            return Instructions.Count;
        }
    }
}
