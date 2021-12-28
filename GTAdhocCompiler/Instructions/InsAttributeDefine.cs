using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Defines a new attribute within a module or class.
    /// </summary>
    public class InsAttributeDefine : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.ATTRIBUTE_DEFINE;

        public override string InstructionName => "ATTRIBUTE_DEFINE";

        public AdhocSymbol AttributeName { get; set; }
    }
}
