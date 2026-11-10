//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Throws an exception and sets the thread state to <see cref="AdhocRunState.EXCEPTION"/>. Pops the last variable from the stack and throws it.
    /// </summary>
    public class InsThrow : InstructionBase
    {
        public readonly static InsThrow Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.THROW;

        public override string InstructionName => "THROW";

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
