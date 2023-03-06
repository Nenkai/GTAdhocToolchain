using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Preprocessor
{
    public class BuiltinDefines
    {
        public static Dictionary<string, string> CompilerProvidedConstants = new()
        {
            { "INT_MIN", $"{int.MinValue}" },
            { "INT_MAX", $"{int.MaxValue}" },
            { "UINT_MIN", $"{uint.MinValue}u" },
            { "UINT_MAX", $"{uint.MaxValue}u" },
            { "LONG_MIN", $"{long.MinValue}L" },
            { "LONG_MAX", $"{long.MaxValue}L" },
            { "ULONG_MIN", $"{ulong.MinValue}UL" },
            { "ULONG_MAX", $"{ulong.MaxValue}UL" },

            { "CELL_PAD_CTRL_L3_LEFT", $"{0xFF51}" },
            { "CELL_PAD_CTRL_L3_UP", $"{0xFF52}" },
            { "CELL_PAD_CTRL_L3_RIGHT", $"{0xFF53}" },
            { "CELL_PAD_CTRL_L3_DOWN", $"{0xFF54}" },

            { "CELL_PAD_CTRL_R3_LEFT", $"{0xFFB4}" },
            { "CELL_PAD_CTRL_R3_UP", $"{0xFFB8}" },
            { "CELL_PAD_CTRL_R3_RIGHT", $"{0xFFB6}" },
            { "CELL_PAD_CTRL_R3_DOWN", $"{0xFFB2}" },

            { "CELL_PAD_CTRL_CROSS", $"{0xFF0D}" },
            { "CELL_PAD_CTRL_SQUARE", $"{0xFFBF}" },
            { "CELL_PAD_CTRL_TRIANGLE", $"{0xFFBE}" },
            { "CELL_PAD_CTRL_CIRCLE", $"{0xFF1B}" },

            { "CELL_PAD_CTRL_SELECT", $"{0xFF63}" },
            { "CELL_PAD_CTRL_START", $"{0xFF8D}" },

            { "CELL_PAD_CTRL_L1", $"{0xFFD7}" },
            { "CELL_PAD_CTRL_L2", $"{0xFFD8}" },
            { "CELL_PAD_CTRL_L3", $"{0xFFD9}" },
            { "CELL_PAD_CTRL_R1", $"{0xFFDC}" },
            { "CELL_PAD_CTRL_R2", $"{0xFFDD}" },
            { "CELL_PAD_CTRL_R3", $"{0xFFDE}" },

            { "COLOR_DEFAULT", $"{0x10}" },
            { "COLOR_WHITE", $"{0x11}" },
            { "COLOR_RED", $"{0x12}" },
            { "COLOR_GREEN", $"{0x13}" },
            { "COLOR_BLUE", $"{0x14}" },
            { "COLOR_YELLOW", $"{0x15}" },
            { "COLOR_CYAN", $"{0x16}" },
            { "COLOR_BLACK", $"{0x17}" },

            { "STATE_EXIT", $"{AdhocRunState.EXIT}" },
            { "STATE_RETURN", $"{AdhocRunState.RETURN}" },
            { "STATE_YIELD", $"{AdhocRunState.YIELD}" },
            { "STATE_EXCEPTION", $"{AdhocRunState.EXCEPTION}" },
            { "STATE_CALL", $"{AdhocRunState.CALL}" },
            { "STATE_RUN", $"{AdhocRunState.RUN}" },

            // mScrollClip - scroll_mode
            { "SCROLL_MODE_FOLLOW_FOCUS", $"{0}" }, // follow_focus
            { "SCROLL_MODE_FLOATING", $"{1}" }, // floating
            { "SCROLL_MODE_MANUAL", $"{2}" }, // manual
            { "SCROLL_MODE_FOLLOW_MODE", $"{3}" }, // follow_mode

            // sqlite3.h
            { "SQLITE_OK",          $"{0}" },   /* Successful result */
            /* beginning-of-error-codes */
            { "SQLITE_ERROR",       $"{ 1}"},   /* Generic error */
            { "SQLITE_INTERNAL",    $"{ 2}"},   /* Internal logic error in SQLite */
            { "SQLITE_PERM",        $"{ 3}"},   /* Access permission denied */
            { "SQLITE_ABORT",       $"{ 4}"},   /* Callback routine requested an abort */
            { "SQLITE_BUSY",        $"{ 5}"},   /* The database file is locked */
            { "SQLITE_LOCKED",      $"{ 6}"},   /* A table in the database is locked */
            { "SQLITE_NOMEM",       $"{ 7}"},   /* A malloc() failed */
            { "SQLITE_READONLY",    $"{ 8}"},   /* Attempt to write a readonly database */
            { "SQLITE_INTERRUPT",   $"{ 9}"},   /* Operation terminated by sqlite3_interrupt()*/
            { "SQLITE_IOERR",       $"{10}"},   /* Some kind of disk I/O error occurred */
            { "SQLITE_CORRUPT",     $"{11}"},   /* The database disk image is malformed */
            { "SQLITE_NOTFOUND",    $"{12}"},   /* Unknown opcode in sqlite3_file_control() */
            { "SQLITE_FULL",        $"{13}"},   /* Insertion failed because database is full */
            { "SQLITE_CANTOPEN",    $"{14}"},   /* Unable to open the database file */
            { "SQLITE_PROTOCOL",    $"{15}"},   /* Database lock protocol error */
            { "SQLITE_EMPTY",       $"{16}"},   /* Internal use only */
            { "SQLITE_SCHEMA",      $"{17}"},   /* The database schema changed */
            { "SQLITE_TOOBIG",      $"{18}"},   /* String or BLOB exceeds size limit */
            { "SQLITE_CONSTRAINT",  $"{19}"},   /* Abort due to constraint violation */
            { "SQLITE_MISMATCH",    $"{20}"},   /* Data type mismatch */
            { "SQLITE_MISUSE",      $"{21}"},   /* Library used incorrectly */
            { "SQLITE_NOLFS",       $"{22}"},   /* Uses OS features not supported on host */
            { "SQLITE_AUTH",        $"{23}"},   /* Authorization denied */
            { "SQLITE_FORMAT",      $"{24}"},   /* Not used */
            { "SQLITE_RANGE",       $"{25}"},   /* 2nd parameter to sqlite3_bind out of range */
            { "SQLITE_NOTADB",      $"{26}"},   /* File opened that is not a database file */
            { "SQLITE_NOTICE",      $"{27}"},   /* Notifications from sqlite3_log() */
            { "SQLITE_WARNING",     $"{28}"},   /* Warnings from sqlite3_log() */
            { "SQLITE_ROW",         $"{100}"},  /* sqlite3_step() has another row ready */
            { "SQLITE_DONE",        $"{101}"},  /* sqlite3_step() has finished executing */
        };
    }
}
