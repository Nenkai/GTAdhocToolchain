﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public bool Unk { get; set; }

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
            if (stream.Version > 11)
                stream.WriteBoolean(Unk);
        }

        public override void Deserialize(AdhocStream stream)
        {
            VariableCount = stream.ReadInt32();
            if (stream.Version > 11)
                Unk = stream.ReadBoolean();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
           => $"{InstructionType}: ElemCount={VariableCount}, UnkBool={Unk}";
    }
}
