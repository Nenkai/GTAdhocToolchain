using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Adds a boolean variable onto the stack.
    /// </summary>
    public class InsBoolConst : InstructionBase
    {
        public static readonly InsBoolConst True = new(true);
        public static readonly InsBoolConst False = new(false);

        public override AdhocInstructionType InstructionType => AdhocInstructionType.BOOL_CONST;

        public override string InstructionName => "BOOL_CONST";

        public bool Value { get; set; }
        public InsBoolConst(bool value)
        {
            Value = value;
        }

        public InsBoolConst()
        {

        }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteBoolean(Value, BooleanCoding.Byte);
        }

        public override void Deserialize(AdhocStream stream)
        {
            Value = stream.ReadBoolean();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: {Value}";

    }
}
