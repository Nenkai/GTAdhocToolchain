using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Defines a new function within the scope. Pops all the arguments before the definition.
    /// </summary>
    public class InsFunctionDefine : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.FUNCTION_DEFINE;

        public override string InstructionName => "FUNCTION_DEFINE";

        public AdhocSymbol Name { get; set; }

        public AdhocInstructionBlock FunctionBlock { get; set; } = new();

        public override string ToString()
        {
            return $"FUNCTION_DEFINE: '{Name.Name}' {FunctionBlock.Instructions.Count} Instructions";
        }
    }
}
