﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    /// <summary>
    /// Throws an exception and sets the thread state to <see cref="AdhocRunState.EXCEPTION"/>. Pops the last variable from the stack and throws it.
    /// </summary>
    public class InsThrow : InstructionBase
    {
        public readonly static InsThrow Default = new();

        public override AdhocInstructionType InstructionType => AdhocInstructionType.THROW;

        public override string InstructionName => "THROW";
    }
}