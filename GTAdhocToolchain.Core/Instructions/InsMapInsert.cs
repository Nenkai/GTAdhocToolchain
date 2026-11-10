//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pushes an object to a map variable.
    /// </summary>
    public class InsMapInsert : InstructionBase
    {
        public readonly static InsMapInsert Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.MAP_INSERT;

        public override string InstructionName => "MAP_INSERT";

        public InsMapInsert()
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
