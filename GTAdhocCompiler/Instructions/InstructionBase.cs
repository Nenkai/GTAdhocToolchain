using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    public abstract class InstructionBase
    {
        public abstract string InstructionName { get; }

        public int LineNumber { get; set; }
    }
}
