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
            if (args.Length < 2)
                return;

            DateTime t = new FileInfo(args[0]).LastWriteTime;
            while (true)
            {
                var current = new FileInfo(args[0]).LastWriteTime;
                if (t >= current)
                {
                    Thread.Sleep(2000);
                    continue;
                }

                t = current;

                var source = File.ReadAllText(args[0]);
                var parser = new AdhocAbstractSyntaxTree(source);
                var program = parser.ParseScript();

                var compiler = new AdhocScriptCompiler();
                compiler.SetSourcePath(compiler.SymbolMap, "www/gt7sp/adhoc/get_car_parameter.ad");
                compiler.Compile(program);

                AdhocCodeGen codeGen = new AdhocCodeGen(compiler, compiler.SymbolMap);
                codeGen.Generate();
                codeGen.SaveTo(args[1]);

                Console.WriteLine($"Compiled {args[1]}");
            }
        }
    }
}
