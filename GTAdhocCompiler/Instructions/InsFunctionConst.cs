using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    public class InsFunctionConst : InstructionBase
    {
        public override string InstructionName => "FUNCTION_CONST";

        public AdhocSymbol Name { get; set; }
        public List<AdhocSymbol> Parameters { get; set; } = new List<AdhocSymbol>();
        public List<InstructionBase> Instructions { get; set; }

        public override string ToString()
        {
            return $"FUNCTION_CONST: '{Name.Name}' {Instructions.Count} Instructions";
        }
    }
}
