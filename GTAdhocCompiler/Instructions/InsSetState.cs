using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    public class InsSetState : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.SET_STATE;

        public override string InstructionName => "SET_STATE";

        public byte State { get; set; }

        public InsSetState(byte state)
        {
            State = state;
        }
    }
}
