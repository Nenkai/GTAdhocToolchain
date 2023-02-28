using Syroot.BinaryData;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    public class DelegateDefine : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.DELEGATE_DEFINE;

        public override string InstructionName => "DELEGATE_DEFINE";

        public AdhocSymbol Delegate { get; set; }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteSymbol(Delegate);
        }

        public override void Deserialize(AdhocStream stream)
        {
            Delegate = stream.ReadSymbol();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: {Delegate}";
    }
}
