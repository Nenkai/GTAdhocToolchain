//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes a double onto the stack.
    /// </summary>
    public class InsDoubleConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.DOUBLE_CONST;

        public override string InstructionName => "DOUBLE_CONST";

        public double Value { get; set; }

        public InsDoubleConst(double value)
        {
            Value = value;
        }

        public InsDoubleConst()
        {

        }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteDouble(Value);
        }

        public override void Deserialize(AdhocStream stream)
        {
            Value = stream.ReadDouble();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
           => $"{InstructionType}: Value={Value}";
    }
}
