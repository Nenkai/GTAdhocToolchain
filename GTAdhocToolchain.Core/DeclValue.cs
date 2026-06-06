using Esprima;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core;

public class DeclValue
{
    public DeclModule? ParentModule { get; set; }
    public string Name { get; set; }
    public AdhocVariableType Type { get; set; }
    public Location? Location { get; set; }

    public DeclValue(DeclModule? parent, string name, AdhocVariableType type, Location? location)
    {
        ParentModule = parent;
        Name = name;
        Type = type;
        Location = location;
    }
}
