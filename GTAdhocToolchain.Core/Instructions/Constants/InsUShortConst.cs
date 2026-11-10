//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes a signed short onto the stack.
    /// </summary>
    public class InsUShortConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.U_SHORT_CONST;

        public override string InstructionName => "U_SHORT_CONST";

        public ushort Value { get; set; }

        public InsUShortConst(ushort value)
        {
            Value = value;
        }

        public InsUShortConst()
        {

        }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteUInt16(Value);
        }

        public override void Deserialize(AdhocStream stream)
        {
            Value = stream.ReadUInt16();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: {Value} (0x{Value:X4})";
    }
}
