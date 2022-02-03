using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTAdhocToolchain.Core.Instructions;

namespace GTAdhocToolchain.Compiler
{
    public class AdhocConstants
    {
        private static Dictionary<string, InstructionBase> aa = new()
        {
            { "CELL_PAD_CTRL_R1", new InsIntConst(0x100) },
            { "CELL_PAD_CTRL_L1", new InsIntConst(0x800) },
            { "CELL_PAD_CTRL_LEFT", new InsIntConst(0xFF52) },
            { "CELL_PAD_CTRL_UP", new InsIntConst(0xFF52) },
            { "CELL_PAD_CTRL_RIGHT", new InsIntConst(0xFF53) },
            { "CELL_PAD_CTRL_SELECT", new InsIntConst(0xFF63) },
            { "CELL_PAD_CTRL_START", new InsIntConst(0xFF8D) },
        };
    }
}
