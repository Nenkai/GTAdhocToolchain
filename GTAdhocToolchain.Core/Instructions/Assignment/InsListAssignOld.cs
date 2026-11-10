//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Deconstructs an array into elements.
    /// </summary>
    public class InsListAssignOld : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.LIST_ASSIGN_OLD;

        public override string InstructionName => "LIST_ASSIGN_OLD";

        public int VariableCount { get; set; }

        public InsListAssignOld(int varCount)
        {
            VariableCount = varCount;
        }

        public InsListAssignOld()
        {

        }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteInt32(VariableCount);
        }

        public override void Deserialize(AdhocStream stream)
        {
            VariableCount = stream.ReadInt32();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
           => $"{InstructionType}: ElemCount={VariableCount}";
    }
}
