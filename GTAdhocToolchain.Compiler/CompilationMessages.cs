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
        public const string Warning_UsingVaCall_Code = "USING_VACALL";
        public static Dictionary<string, string> Warnings = new()
        {
            { Warning_UsingAwait_Code, "Async/Await is only available in GT6 and above" },
            { Warning_UsingVaCall_Code, "Variable Calls/Spread Syntax (<function>.(...<arg>) aka VA_CALL instruction) is only available in GT6 and above" },
        };
    }
}
