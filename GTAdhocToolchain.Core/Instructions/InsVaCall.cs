using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Represents a function call where one argument is an array that represents all the function arguments.
    /// </summary>
    public class InsVaCall : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.VA_CALL;

        public override string InstructionName => "VA_CALL";

        /// <summary>
        /// Should always be two. (function object + unique & single spread expression)
        /// </summary>
        public uint PopObjectCount { get; set; }

        public override void Deserialize(AdhocStream stream)
        {
            PopObjectCount = stream.ReadUInt32();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: Value={PopObjectCount}";

    }
}


