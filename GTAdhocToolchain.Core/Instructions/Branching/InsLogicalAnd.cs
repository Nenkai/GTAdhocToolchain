//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Logical and instruction.
    /// </summary>
    public class InsLogicalAnd : InsLogicalBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.LOGICAL_AND;

        public override string InstructionName => "LOGICAL_AND";
    }
}
