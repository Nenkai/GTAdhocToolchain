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
    public class InsMapConst : InstructionBase
    {
        public readonly static InsMapConst Default = new();

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
