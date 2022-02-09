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
        public static Dictionary<string, InstructionBase> CompilerProvidedConstants = new()
        {
            { "CELL_PAD_CTRL_L3_LEFT", new InsIntConst(0xFF51) },
            { "CELL_PAD_CTRL_L3_UP", new InsIntConst(0xFF52) },
            { "CELL_PAD_CTRL_L3_RIGHT", new InsIntConst(0xFF53) },
            { "CELL_PAD_CTRL_L3_DOWN", new InsIntConst(0xFF54) },

            { "CELL_PAD_CTRL_R3_LEFT", new InsIntConst(0xFFB4) },
            { "CELL_PAD_CTRL_R3_UP", new InsIntConst(0xFFB8) },
            { "CELL_PAD_CTRL_R3_RIGHT", new InsIntConst(0xFFB6) },
            { "CELL_PAD_CTRL_R3_DOWN", new InsIntConst(0xFFB2) },

            { "CELL_PAD_CTRL_CROSS", new InsIntConst(0xFF0D) },
            { "CELL_PAD_CTRL_SQUARE", new InsIntConst(0xFFBF) },
            { "CELL_PAD_CTRL_TRIANGLE", new InsIntConst(0xFFBE) },
            { "CELL_PAD_CTRL_CIRCLE", new InsIntConst(0xFF1B) },

            { "CELL_PAD_CTRL_SELECT", new InsIntConst(0xFF63) },
            { "CELL_PAD_CTRL_START", new InsIntConst(0xFF8D) },

            { "CELL_PAD_CTRL_L1", new InsIntConst(0xFFD7) },
            { "CELL_PAD_CTRL_L2", new InsIntConst(0xFFD8) },
            { "CELL_PAD_CTRL_L3", new InsIntConst(0xFFD9) },
            { "CELL_PAD_CTRL_R1", new InsIntConst(0xFFDC) },
            { "CELL_PAD_CTRL_R2", new InsIntConst(0xFFDD) },
            { "CELL_PAD_CTRL_R3", new InsIntConst(0xFFDD) },
        };
    }
}
