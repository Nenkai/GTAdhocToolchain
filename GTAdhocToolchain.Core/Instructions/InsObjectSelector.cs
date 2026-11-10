//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Unknown. Pops two values from the stack, does something with them and pushes a new value.
    /// </summary>
    public class InsObjectSelector : InstructionBase
    {
        public readonly static InsObjectSelector Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.OBJECT_SELECTOR;

        public override string InstructionName => "OBJECT_SELECTOR";

        public InsObjectSelector()
        {
            
        }

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
