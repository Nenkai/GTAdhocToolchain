using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsSetState : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.SET_STATE;

        public override string InstructionName => "SET_STATE";

        public AdhocRunState State { get; set; }

        public InsSetState(AdhocRunState state)
        {
            State = state;
        }
    }
}
