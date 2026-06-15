using Esprima;

using System;
using System.Collections.Frozen;
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

    // GT7 1.00: 30DFE50 (mDeclValue::BuildPath)
    public string GetFullPath()
    {
        string str = string.Empty;
        if (ParentModule is not null)
        {
            str = ParentModule.GetFullPath();
            str += "::";
        }

        return str + Name;
    }
    // GT7 1.00: 30DC470 (mDeclValue::dump)
    public virtual void Dump(StringBuilder sb, int depth = 0)
    {
        if (depth > 0)
            sb.Append(' ', depth * 2);

        sb.Append($"@{Utils.VariableTypeToKeyword[Type]} {Name};\n");
    }
}
