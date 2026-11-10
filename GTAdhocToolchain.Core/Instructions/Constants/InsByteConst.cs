//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes a signed byte onto the stack.
    /// </summary>
    public class InsByteConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.BYTE_CONST;

        public override string InstructionName => "BYTE_CONST";

        public sbyte Value { get; set; }

        public InsByteConst(sbyte value)
        {
            Value = value;
        }

        public InsByteConst()
        {

        }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteSByte(Value);
        }

        public override void Deserialize(AdhocStream stream)
        {
            Value = stream.ReadSByte();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: {Value} (0x{Value:X2})";
    }
}
