using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Pops a string value from the stack and imports it as a module and loads its adhoc code file if needed.
    /// </summary>
    public class InsRequire : InstructionBase
    {
        public readonly static InsRequire Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.REQUIRE;

        public override string InstructionName => "REQUIRE";

        public InsRequire()
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
