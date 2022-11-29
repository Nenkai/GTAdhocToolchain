using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
