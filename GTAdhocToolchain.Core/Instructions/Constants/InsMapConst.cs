//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes a map/dictionary/kv collection const into the variable storage.
    /// </summary>
    public class InsMapConst : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.MAP_CONST;

        public override string InstructionName => "MAP_CONST";

        public InsMapConst()
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
           => InstructionName.ToString();
    }
}
