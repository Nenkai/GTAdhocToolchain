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

                BuildScript(input, output);
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
                string output = !string.IsNullOrEmpty(project.OutputPath) ? project.OutputPath : project.InputPath;
                BuildScript(project.InputPath, Path.ChangeExtension(output, ".adc"));
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

        private static void BuildScript(string inputPath, string output)
        {
            var source = File.ReadAllText(inputPath);
            var parser = new AdhocAbstractSyntaxTree(source);
            var program = parser.ParseScript();

            Logger.Info($"Started script build ({inputPath}).");
            try
            {
                var compiler = new AdhocScriptCompiler();
                compiler.SetSourcePath(compiler.SymbolMap, inputPath);
                compiler.CompileScript(program);

                AdhocCodeGen codeGen = new AdhocCodeGen(compiler, compiler.SymbolMap);
                codeGen.Generate();
                codeGen.SaveTo(output);

                Logger.Info($"Script build successful.");
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

            Logger.Error("Script build failed.");
        }
    }

    [Verb("build", HelpText = "Builds a project.")]
    public class BuildVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input project file or source script.")]
        public string InputPath { get; set; }

        [Option('o', "output", Required = false, HelpText = "Output compiled scripts when compiling standalone scripts (not projects).")]
        public string OutputPath { get; set; }
    }
}
