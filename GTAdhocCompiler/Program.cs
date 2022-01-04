using System;
using System.Collections.Generic;
using System.IO;

using Esprima;

using GTAdhocCompiler;
using GTAdhocCompiler.Project;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GTAdhocCompiler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {

            }
            else
            {
                if (Path.GetExtension(args[0]) == ".yaml")
                {
                    if (!File.Exists(args[0]))
                    {
                        Console.WriteLine("Project file does not exist.");
                    }

                    AdhocProject prj = AdhocProject.Read(args[0]);
                    prj.ProjectFilePath = args[0];
                    prj.Compile();
                }
            }
        }

        public void WatchAndCompile(string projectDir, string input, string output)
        {
            DateTime t = new FileInfo(input).LastWriteTime;
            while (true)
            {
                var current = new FileInfo(input).LastWriteTime;
                if (t >= current)
                {
                    Thread.Sleep(2000);
                    continue;
                }

                t = current;

                var source = File.ReadAllText(input);
                var parser = new AdhocAbstractSyntaxTree(source);
                var program = parser.ParseScript();

                var compiler = new AdhocScriptCompiler();
                compiler.SetProjectDirectory(projectDir);
                compiler.SetSourcePath(compiler.SymbolMap, "www/gt7sp/adhoc/get_car_parameter.ad");
                compiler.CompileScript(program);

                AdhocCodeGen codeGen = new AdhocCodeGen(compiler, compiler.SymbolMap);
                codeGen.Generate();
                codeGen.SaveTo(output);

                Console.WriteLine($"Compiled {input}");
            }
        }
    }
}
