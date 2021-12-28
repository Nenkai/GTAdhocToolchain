using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Defines a new method within the scope. Pops all the arguments before the definition.
    /// </summary>
    public class InsMethodDefine : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.METHOD_DEFINE;

        public override string InstructionName => "METHOD_DEFINE";

        public AdhocSymbol Name { get; set; }

        public AdhocInstructionBlock MethodBlock { get; set; } = new();

        public override string ToString()
        {
            return $"METHOD_DEFINE: '{Name.Name}' {MethodBlock.Instructions.Count} Instructions";
        }
    }
}
