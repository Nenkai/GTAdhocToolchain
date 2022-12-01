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

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteSymbol(AttributeName);
        }

        public override void Deserialize(AdhocStream stream)
        {
            AttributeName = stream.ReadSymbol();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: {AttributeName.Name}";
    }
}
