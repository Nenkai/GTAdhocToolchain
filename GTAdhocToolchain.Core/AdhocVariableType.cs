using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core;

public enum AdhocVariableType
{
    Attribute = 0,
    Class = 1,
    Delegate = 2,
    Function = 3,
    LocalVariable = 4,
    Method = 5,
    Module = 6,
    Static = 7,
    Undef = 8,
    Unknown = 9,
}
