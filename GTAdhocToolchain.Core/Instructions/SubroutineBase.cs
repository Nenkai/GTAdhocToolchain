using System;
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

        public AdhocCodeFrame CodeFrame { get; set; } = new();

        public override string ToString()
        {
            return $"{InstructionType}: '{Name.Name}' {CodeFrame.Instructions.Count} Instructions";
        }
    }
}
