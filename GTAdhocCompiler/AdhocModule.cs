using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler
{
    public class AdhocModule
    {
        public string Name { get; set; } = "<none>";

        public List<AdhocSymbol> DefinedStaticVariables { get; set; } = new();

        public bool DefineStatic(AdhocSymbol symbol)
        {
            if (DefinedStaticVariables.Contains(symbol))
                return false;

            DefinedStaticVariables.Add(symbol);
            return true;
        }

        public bool IsDefinedStatic(AdhocSymbol symbol)
            => DefinedStaticVariables.Contains(symbol);
    }
}
