//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Sets the current frame's state.
    /// </summary>
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

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteByte((byte)State);
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
