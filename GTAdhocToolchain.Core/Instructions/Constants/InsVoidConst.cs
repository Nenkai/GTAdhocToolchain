//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes a void onto the stack.
    /// </summary>
    public class InsVoidConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.VOID_CONST;

        public override string InstructionName => "VOID_CONST";

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
