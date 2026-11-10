//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsTryCatch : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.TRY_CATCH;

        public override string InstructionName => "TRY_CATCH";

        public int InstructionIndex { get; set; }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteInt32(InstructionIndex);
        }

        public override void Deserialize(AdhocStream stream)
        {
            InstructionIndex = stream.ReadInt32();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
           => $"{InstructionType}: {InstructionIndex}";
    }
}
