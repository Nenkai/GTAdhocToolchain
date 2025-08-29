﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Defines a new subroutine within the scope. Pops all the arguments before the definition.
    /// </summary>
    public abstract class SubroutineBase : InstructionBase
    {
        public AdhocSymbol Name { get; set; }

        public AdhocCodeFrame CodeFrame { get; set; }

        public SubroutineBase() { }
        public SubroutineBase(AdhocVersion version)
        {
            CodeFrame = new AdhocCodeFrame(version);
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
        {
            return $"{InstructionType}: '{Name.Name}' {CodeFrame.Instructions.Count} Instructions";
        }
    }
}
