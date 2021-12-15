using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esprima.Compiler.Instructions
{
    public abstract class InstructionBase
    {
        public int LineNumber { get; set; }
    }
}
