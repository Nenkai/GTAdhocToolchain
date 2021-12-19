using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    public class InsSetState : InstructionBase
    {
        public override string InstructionName => "SET_STATE";

        public int State { get; set; }

        public InsSetState(int state)
        {
            State = state;
        }
    }
}
