//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pops two values from the stack, pushes the first value to the second one which should be an array.
    /// Basically pushes a value into an array.
    /// </summary>
    public class InsArrayPush : InstructionBase
    {
        public readonly static InsArrayPush Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.ARRAY_PUSH;

        public override string InstructionName => "ARRAY_PUSH";

        public override void Serialize(AdhocStream stream)
        {

        }

        public override void Deserialize(AdhocStream stream)
        {

        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}";
    }
}
