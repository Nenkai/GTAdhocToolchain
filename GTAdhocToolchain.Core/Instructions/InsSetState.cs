using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Sets the current frame's state.
    /// </summary>
    public class InsSetState : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.SET_STATE;

        public override string InstructionName => "SET_STATE";

        public AdhocRunState State { get; set; }

        public InsSetState(AdhocRunState state)
        {
            State = state;
        }

        public InsSetState()
        {

        }

        public override void Deserialize(AdhocStream stream)
        {
            State = (AdhocRunState)stream.ReadByte();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: State={State} ({(byte)State})";
    }
}
