//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Logical or instruction.
    /// </summary>
    public class InsLogicalBase : InstructionBase
    {
        public override AdhocInstructionType InstructionType => throw new NotSupportedException();

        public override string InstructionName => throw new NotSupportedException();

        /// <summary>
        /// Index to jump to if the first operand before the comparison matches.
        /// </summary>
        public int InstructionJumpIndex { get; set; }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteInt32(InstructionJumpIndex);
        }

        public override void Deserialize(AdhocStream stream)
        {
            InstructionJumpIndex = stream.ReadInt32();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
           => $"{InstructionType}: Jump={InstructionJumpIndex}";
    }
}
