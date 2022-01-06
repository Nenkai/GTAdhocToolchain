using System;
using System.Collections.Generic;
using System.IO;

using Esprima;

using GTAdhocCompiler;
using GTAdhocCompiler.Project;

using CommandLine;
using NLog;

namespace GTAdhocCompiler
{
    public class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void Main(string[] args)
        {
            Console.WriteLine("[-- GTAdhocCompiler by Nenkai#9075 -- ]");

            Parser.Default.ParseArguments<BuildVerbs>(args)
                .WithParsed<BuildVerbs>(Build)
                .WithNotParsed(HandleNotParsedArgs);
        }

        public static void WatchAndCompile(string projectDir, string input, string output)
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

        public static void Build(BuildVerbs project)
        {
            if (!File.Exists(project.InputPath))
            {
                Logger.Error($"File {project.InputPath} does not exist.");
                return;
            }

            if (Path.GetExtension(project.InputPath) == ".yaml")
            {
                BuildProject(project.InputPath);
            }
            else if (Path.GetExtension(project.InputPath) == ".ad")
            {

            }
            else
            {
                Logger.Error("Input File is not a project or script.");
            }
        }

        public static void HandleNotParsedArgs(IEnumerable<Error> errors)
        {

        }

        private static void BuildProject(string inputPath)
        {
            AdhocProject prj;
            try
            {
                prj = AdhocProject.Read(inputPath);
                prj.ProjectFilePath = inputPath;
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load project file - {e.Message}");
                return;
            }

            Logger.Info($"Project file: {inputPath}");
            prj.PrintInfo();

            try
            {
                Logger.Info("Started project build.");
                prj.Build();
                return;
            }
            catch (AdhocCompilationException compileException)
            {
                Logger.Fatal($"Compilation error: {compileException.Message}");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Internal error in compilation");
            }

            Logger.Error("Project build failed.");
        }

        private static void BuildScript(string inputPath)
        {
            
        }
    }

    [Verb("build", HelpText = "Builds a project.")]
    public class BuildVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input project file or source script.")]
        public string InputPath { get; set; }
    }
}
