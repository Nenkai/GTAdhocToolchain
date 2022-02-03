using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core
{
    public class AdhocModule
    {
        public string Name { get; set; } = "<none>";

        public List<AdhocSymbol> DefinedStaticVariables { get; set; } = new();

        public List<AdhocSymbol> DefinedAttributeMembers { get; set; } = new();

        public bool DefineStatic(AdhocSymbol symbol)
        {
            if (DefinedStaticVariables.Contains(symbol))
                return false;

            DefinedStaticVariables.Add(symbol);
            return true;
        }

        public bool DefineAttribute(AdhocSymbol symbol)
        {
            if (DefinedAttributeMembers.Contains(symbol))
                return false;

            DefinedAttributeMembers.Add(symbol);
            return true;
        }

        public bool IsDefinedStaticMember(AdhocSymbol symbol)
            => DefinedStaticVariables.Contains(symbol);

        public bool IsDefinedAttributeMember(AdhocSymbol symbol)
            => DefinedAttributeMembers.Contains(symbol);
    }
}
