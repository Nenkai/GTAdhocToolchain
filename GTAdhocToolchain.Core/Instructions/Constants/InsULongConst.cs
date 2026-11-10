//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes an unsigned long (QWORD) onto the stack.
    /// </summary>
    public class InsULongConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.U_LONG_CONST;

        public override string InstructionName => "U_LONG_CONST";

        public ulong Value { get; set; }

        public InsULongConst(ulong value)
        {
            Value = value;
        }

        public InsULongConst()
        {

        }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteUInt64(Value);
        }

        public override void Deserialize(AdhocStream stream)
        {
            Value = stream.ReadUInt64();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: {Value})";
    }
}
