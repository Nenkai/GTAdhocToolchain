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
    public class InsListAssign : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.LIST_ASSIGN;

        public override string InstructionName => "LIST_ASSIGN";

        public int VariableCount { get; set; }

        public bool HasRestElement { get; set; }

        public InsListAssign(int varCount)
        {
            VariableCount = varCount;
        }

        public InsListAssign()
        {

        }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteInt32(VariableCount);
            if (stream.Version.SupportsRestElement())
                stream.WriteBoolean(HasRestElement);
        }

        public override void Deserialize(AdhocStream stream)
        {
            VariableCount = stream.ReadInt32();
            if (stream.Version.SupportsRestElement())
                HasRestElement = stream.ReadBoolean();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
           => $"{InstructionType}: ElemCount={VariableCount}, HasRestElement={HasRestElement}";
    }
}
