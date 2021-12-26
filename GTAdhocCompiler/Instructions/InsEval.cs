using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Represents an evaluation for an iterator.
    /// </summary>
    public class InsEval : InstructionBase
    {
        public readonly static InsEval Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.EVAL;

        public override string InstructionName => "EVAL";
    }
}
