﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core;

public class AdhocModule
{
    public string Name { get; set; } = "<none>";

    public List<AdhocSymbol> DefinedStaticVariables { get; set; } = [];

    public List<AdhocSymbol> DefinedAttributeMembers { get; set; } = [];

    public List<AdhocSymbol> DefinedMethods { get; set; } = [];

    public AdhocModule ParentModule { get; set; }

    public bool DefineStatic(AdhocSymbol symbol)
    {
        if (DefinedStaticVariables.Contains(symbol))
            return false;

        DefinedStaticVariables.Add(symbol);
        return true;
    }

    public bool DefineMethod(AdhocSymbol symbol)
    {
        if (DefinedMethods.Contains(symbol))
            return false;

        DefinedMethods.Add(symbol);
        return true;
    }

    public bool DefineAttribute(AdhocSymbol symbol)
    {
        if (DefinedAttributeMembers.Contains(symbol))
            return false;

        DefinedAttributeMembers.Add(symbol);
        return true;
    }

    public IEnumerable<AdhocSymbol> GetAllMembers()
    {
        return DefinedMethods.Concat(DefinedAttributeMembers);
    }

    public bool IsDefinedStaticMember(AdhocSymbol symbol)
        => DefinedStaticVariables.Contains(symbol);

    public bool IsDefinedAttributeMember(AdhocSymbol symbol)
        => DefinedAttributeMembers.Contains(symbol);
}
