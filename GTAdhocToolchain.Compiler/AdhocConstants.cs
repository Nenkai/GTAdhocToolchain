using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTAdhocToolchain.Core.Instructions;
using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Compiler
{
    public class AdhocConstants
    {
        public static Dictionary<string, InstructionBase> CompilerProvidedConstants = new()
        {
            { "INT_MIN", new InsIntConst(int.MinValue) },
            { "INT_MAX", new InsIntConst(int.MaxValue) },
            { "UINT_MIN", new InsUIntConst(uint.MinValue) },
            { "UINT_MAX", new InsUIntConst(uint.MaxValue) },
            { "LONG_MIN", new InsLongConst(long.MinValue) },
            { "LONG_MAX", new InsLongConst(long.MaxValue) },
            { "ULONG_MIN", new InsULongConst(ulong.MinValue) },
            { "ULONG_MAX", new InsULongConst(ulong.MaxValue) },

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
            { "CELL_PAD_CTRL_R3", new InsIntConst(0xFFDE) },

            { "COLOR_DEFAULT", new InsStringConst(new AdhocSymbol(((char)0x10).ToString())) },
            { "COLOR_WHITE", new InsStringConst(new AdhocSymbol(((char)0x11).ToString())) },
            { "COLOR_RED", new InsStringConst(new AdhocSymbol( ((char)0x12).ToString()))},
            { "COLOR_GREEN", new InsStringConst(new AdhocSymbol(((char)0x13).ToString())) },
            { "COLOR_BLUE", new InsStringConst(new AdhocSymbol(((char)0x14).ToString())) },
            { "COLOR_YELLOW", new InsStringConst(new AdhocSymbol(((char)0x15).ToString())) },
            { "COLOR_CYAN", new InsStringConst(new AdhocSymbol(((char)0x16).ToString())) },
            { "COLOR_BLACK", new InsStringConst(new AdhocSymbol(((char)0x17).ToString())) },

            { "STATE_EXIT", new InsIntConst((int)AdhocRunState.EXIT) },
            { "STATE_RETURN", new InsIntConst((int)AdhocRunState.RETURN) },
            { "STATE_YIELD", new InsIntConst((int)AdhocRunState.YIELD) },
            { "STATE_EXCEPTION", new InsIntConst((int)AdhocRunState.EXCEPTION) },
            { "STATE_CALL", new InsIntConst((int)AdhocRunState.CALL) },
            { "STATE_RUN", new InsIntConst((int)AdhocRunState.RUN) },

            // mScrollClip - scroll_mode
            { "SCROLL_MODE_FOLLOW_FOCUS", new InsIntConst(0) }, // follow_focus
            { "SCROLL_MODE_FLOATING", new InsIntConst(1) }, // floating
            { "SCROLL_MODE_MANUAL", new InsIntConst(2) }, // manual
            { "SCROLL_MODE_FOLLOW_MODE", new InsIntConst(3) }, // follow_mode

            // sqlite3.h
            { "SQLITE_OK",          new InsIntConst(0) },   /* Successful result */
            /* beginning-of-error-codes */
            { "SQLITE_ERROR",       new InsIntConst( 1)},   /* Generic error */
            { "SQLITE_INTERNAL",    new InsIntConst( 2)},   /* Internal logic error in SQLite */
            { "SQLITE_PERM",        new InsIntConst( 3)},   /* Access permission denied */
            { "SQLITE_ABORT",       new InsIntConst( 4)},   /* Callback routine requested an abort */
            { "SQLITE_BUSY",        new InsIntConst( 5)},   /* The database file is locked */
            { "SQLITE_LOCKED",      new InsIntConst( 6)},   /* A table in the database is locked */
            { "SQLITE_NOMEM",       new InsIntConst( 7)},   /* A malloc() failed */
            { "SQLITE_READONLY",    new InsIntConst( 8)},   /* Attempt to write a readonly database */
            { "SQLITE_INTERRUPT",   new InsIntConst( 9)},   /* Operation terminated by sqlite3_interrupt()*/
            { "SQLITE_IOERR",       new InsIntConst(10)},   /* Some kind of disk I/O error occurred */
            { "SQLITE_CORRUPT",     new InsIntConst(11)},   /* The database disk image is malformed */
            { "SQLITE_NOTFOUND",    new InsIntConst(12)},   /* Unknown opcode in sqlite3_file_control() */
            { "SQLITE_FULL",        new InsIntConst(13)},   /* Insertion failed because database is full */
            { "SQLITE_CANTOPEN",    new InsIntConst(14)},   /* Unable to open the database file */
            { "SQLITE_PROTOCOL",    new InsIntConst(15)},   /* Database lock protocol error */
            { "SQLITE_EMPTY",       new InsIntConst(16)},   /* Internal use only */
            { "SQLITE_SCHEMA",      new InsIntConst(17)},   /* The database schema changed */
            { "SQLITE_TOOBIG",      new InsIntConst(18)},   /* String or BLOB exceeds size limit */
            { "SQLITE_CONSTRAINT",  new InsIntConst(19)},   /* Abort due to constraint violation */
            { "SQLITE_MISMATCH",    new InsIntConst(20)},   /* Data type mismatch */
            { "SQLITE_MISUSE",      new InsIntConst(21)},   /* Library used incorrectly */
            { "SQLITE_NOLFS",       new InsIntConst(22)},   /* Uses OS features not supported on host */
            { "SQLITE_AUTH",        new InsIntConst(23)},   /* Authorization denied */
            { "SQLITE_FORMAT",      new InsIntConst(24)},   /* Not used */
            { "SQLITE_RANGE",       new InsIntConst(25)},   /* 2nd parameter to sqlite3_bind out of range */
            { "SQLITE_NOTADB",      new InsIntConst(26)},   /* File opened that is not a database file */
            { "SQLITE_NOTICE",      new InsIntConst(27)},   /* Notifications from sqlite3_log() */
            { "SQLITE_WARNING",     new InsIntConst(28)},   /* Warnings from sqlite3_log() */
            { "SQLITE_ROW",         new InsIntConst(100)},  /* sqlite3_step() has another row ready */
            { "SQLITE_DONE",        new InsIntConst(101)},  /* sqlite3_step() has finished executing */
        };

        static AdhocConstants()
        {
            foreach (var i in Enum.GetValues<Keycodes>())
            {
                CompilerProvidedConstants.Add(i.ToString(), new InsIntConst((int)i));
            }
        }
    }
}
