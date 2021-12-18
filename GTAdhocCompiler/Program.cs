using System;
using System.Collections.Generic;
using System.IO;

using Esprima;
using Esprima.Utils;

using Newtonsoft.Json;

using GTAdhocCompiler;

namespace GTAdhocCompiler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var source = File.ReadAllText(@"D:\Modding_Research\Gran_Turismo\Gran_Turismo_Sport_Closed_Beta_1.08\CUSA07836-app\EXTRACTED\SCRIPTS\WWW\GT7SP\ADHOC\GET_CAR_PARAMETER.AD.src");
            var parser = new AdhocAbstractSyntaxTree(source);
            var program = parser.ParseScript();


            var compiler = new AdhocScriptCompiler();
            compiler.Compile(program);
        }
    }
}
