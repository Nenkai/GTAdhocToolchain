using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    public class InsFunctionConst : InstructionBase
    {
        public List<AdhocSymbol> Parameters { get; set; } = new List<AdhocSymbol>();
    }
}
