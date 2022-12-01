using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Represents a function or method call. Will pop the amount of provided arguments from the stack, plus the function object itself. (Arg Count + 1).
    /// Will always return a value regardless of void returns.
    /// </summary>
    public class InsCallOld : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.CALL_OLD;

        public override string InstructionName => "CALL_OLD";

        public int ArgumentCount { get; set; }

        public InsCallOld(int argumentCount)
        {
            ArgumentCount = argumentCount;
        }

        public InsCallOld()
        {

        }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteInt32(ArgumentCount);
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
