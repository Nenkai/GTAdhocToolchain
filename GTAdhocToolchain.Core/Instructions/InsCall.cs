using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Represents a function or method call. Will pop the amount of provided arguments from the stack, plus the function object itself. (Arg Count + 1).
    /// </summary>
    public class InsCall : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.CALL;

        public override string InstructionName => "CALL";

        public int ArgumentCount { get; set; }

        public InsCall(int argumentCount)
        {
            ArgumentCount = argumentCount;
        }

        public InsCall()
        {

        }

        public override void Deserialize(AdhocStream stream)
        {
            ArgumentCount = stream.ReadInt32();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: ArgCount={ArgumentCount}";
    }
}
