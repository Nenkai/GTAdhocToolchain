using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Represents a function or method call.
    /// </summary>
    public class InsCall : InstructionBase
    {
        public int ArgumentCount { get; set; }

        public InsCall(int argumentCount)
        {
            ArgumentCount = argumentCount;
        }
    }
}
