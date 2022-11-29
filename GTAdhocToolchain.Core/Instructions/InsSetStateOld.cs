using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsSetStateOld : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.SET_STATE_OLD;

        public override string InstructionName => "SET_STATE_OLD";

        public AdhocRunState State { get; set; }

        public InsSetStateOld(AdhocRunState state)
        {
            State = state;
        }

        public InsSetStateOld()
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
