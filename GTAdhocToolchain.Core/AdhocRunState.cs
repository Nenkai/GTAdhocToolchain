using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core;

public enum AdhocRunState : byte
{
    /// <summary>
    /// Script is terminating
    /// </summary>
    EXIT = 0,

    /// <summary>
    /// Script scope is over
    /// </summary>
    RETURN = 1,

    YIELD = 2,

    /// <summary>
    /// Script exception
    /// </summary>
    EXCEPTION = 3,

    CALL = 4,

    RUN = 5,
}
