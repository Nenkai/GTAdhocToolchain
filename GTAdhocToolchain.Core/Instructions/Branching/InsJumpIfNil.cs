//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core.Instructions
{
    public class InsJumpIfNil : InsLogicalBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.JUMP_IF_NIL;

        public override string InstructionName => "JUMP_IF_NIL";
    }
}
