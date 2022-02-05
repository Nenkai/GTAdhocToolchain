using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Compiler
{
    public class CompilationMessages
    {
        public const string Warning_UsingAwait_Code = "USING_AWAIT";
        public static Dictionary<string, string> Warnings = new()
        {
            { Warning_UsingAwait_Code, "Async/Await is only usable in GT6 and above." },
        };
    }
}
