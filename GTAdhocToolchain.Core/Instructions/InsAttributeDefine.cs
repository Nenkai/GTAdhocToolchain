using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Defines a new attribute within a module or class.
    /// </summary>
    public class InsAttributeDefine : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.ATTRIBUTE_DEFINE;

        public override string InstructionName => "ATTRIBUTE_DEFINE";

        public AdhocSymbol AttributeName { get; set; }

        public InsAttributeDefine(AdhocSymbol attrSymbol)
        {
            AttributeName = attrSymbol;
        }

        public InsAttributeDefine()
        {

        }

        public override void Deserialize(AdhocStream stream)
        {
            AttributeName = stream.ReadSymbol();
        }

        public override string ToString()
            => $"{InstructionType}: {AttributeName.Name}";
    }
}
