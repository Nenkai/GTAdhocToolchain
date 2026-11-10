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
    public class InsMapConstOld : InstructionBase
    {
        public readonly static InsMapConst Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.MAP_CONST_OLD;

        public override string InstructionName => "MAP_CONST_OLD";

        public int Value { get; set; }

        public InsMapConstOld()
        {
            
        }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteInt32(Value);
        }

        public override void Deserialize(AdhocStream stream)
        {
            Value = stream.ReadInt32();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
           => $"{InstructionType}: {Value}";
    }
}
