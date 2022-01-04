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
            if (args.Length < 3)
                return;

            DateTime t = new FileInfo(args[1]).LastWriteTime;
            while (true)
            {
                var current = new FileInfo(args[1]).LastWriteTime;
                if (t >= current)
                {
                    Thread.Sleep(2000);
                    continue;
                }

                t = current;

                var source = File.ReadAllText(args[1]);
                var parser = new AdhocAbstractSyntaxTree(source);
               var program = parser.ParseScript();

                var compiler = new AdhocScriptCompiler();
                compiler.SetProjectDirectory(args[0]);
                compiler.SetSourcePath(compiler.SymbolMap, "www/gt7sp/adhoc/get_car_parameter.ad");
                compiler.CompileScript(program);

                AdhocCodeGen codeGen = new AdhocCodeGen(compiler, compiler.SymbolMap);
                codeGen.Generate();
                codeGen.SaveTo(args[2]);

                Console.WriteLine($"Compiled {args[1]}");
            }
        }
    }
}
