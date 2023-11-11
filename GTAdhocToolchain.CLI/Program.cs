using System;
using System.Collections.Generic;
using System.IO;

using Esprima;

using CommandLine;
using NLog;

using GTAdhocToolchain.Compiler;
using GTAdhocToolchain.CodeGen;
using GTAdhocToolchain.Project;
using GTAdhocToolchain.Disasm;
using GTAdhocToolchain.Menu;
using GTAdhocToolchain.Menu.Resources;
using GTAdhocToolchain.Packaging;
using GTAdhocToolchain.Core.Instructions;
using GTAdhocToolchain.Menu.Fields;
using GTAdhocToolchain.Preprocessor;
using GTAdhocToolchain.Analyzer;

namespace GTAdhocToolchain.CLI
{
    public class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void Main(string[] args)
        {
            Console.WriteLine("[-- GTAdhocToolchain by Nenkai#9075 -- ]");

            if (args.Length == 1)
            {
                if (args[0].ToLower().EndsWith(".adc"))
                {
                    List<AdhocFile> scripts = null;
                    try
                    {
                        scripts = AdhocFile.ReadFromFile(args[0]);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Errored while reading: {e.Message}");
                        Environment.ExitCode = -1;
                        return;
                    }

                    foreach (var adc in scripts)
                    {
                        adc.Disassemble(Path.ChangeExtension(args[0], ".ad.diss"));

                        if (adc.Version == 12)
                            adc.PrintStrings(Path.ChangeExtension(args[0], ".strings"));
                    }

                    Environment.ExitCode = 0;
                    return;

                }
                else if (args[0].ToLower().EndsWith(".gpb"))
                {
                    var gpb = GpbBase.ReadFile(args[0]);
                    if (gpb is null)
                    {
                        Console.WriteLine("Could not parse GPB Header.");
                        Environment.ExitCode = -1;
                        return;
                    }

                    string fileName = Path.GetFileNameWithoutExtension(args[0]);
                    string dir = Path.GetDirectoryName(args[0]);

                    gpb.Unpack(Path.GetFileNameWithoutExtension(args[0]), null);
                    Environment.ExitCode = 0;
                    return;
                }
            }

            Parser.Default.ParseArguments<BuildVerbs, PackVerbs, UnpackVerbs, MProjectToBinVerbs, MProjectToTextVerbs>(args)
                .WithParsed<BuildVerbs>(Build)
                .WithParsed<PackVerbs>(Pack)
                .WithParsed<UnpackVerbs>(Unpack)
                .WithParsed<MProjectToBinVerbs>(MProjectToBin)
                .WithParsed<MProjectToTextVerbs>(MProjectToText)
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

        public static void Build(BuildVerbs buildVerbs)
        {
            if (!File.Exists(buildVerbs.InputPath))
            {
                Logger.Error($"File {buildVerbs.InputPath} does not exist.");
                Environment.ExitCode = -1;
                return;
            }

            if (Path.GetExtension(buildVerbs.InputPath) == ".yaml")
            {
                BuildProject(buildVerbs);
            }
            else if (Path.GetExtension(buildVerbs.InputPath) == ".ad")
            {
                string output = !string.IsNullOrEmpty(buildVerbs.OutputPath) ? buildVerbs.OutputPath : buildVerbs.InputPath;
                BuildScript(buildVerbs.InputPath, Path.ChangeExtension(output, ".adc"), buildVerbs.Version, buildVerbs.WriteExceptionsToFile, buildVerbs.PreprocessOnly);
            }
            else
            {
                Logger.Error("Input File is not a project or script.");
                Environment.ExitCode = -1;
            }
        }

        public static void Pack(PackVerbs packVerbs)
        {
            if (packVerbs.OutputPath.ToLower().EndsWith("gpb"))
            {
                try
                {
                    var gpb = new GpbData3();
                    gpb.AddFilesFromFolder(packVerbs.InputPath, packVerbs.GT5PackMode);
                    gpb.Pack(packVerbs.OutputPath, !packVerbs.LittleEndian);
                    Environment.ExitCode = 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to pack gpb - {e.Message}");
                    Environment.ExitCode = -1;
                }
            }
            else if (packVerbs.OutputPath.EndsWith("mpackage"))
            {
                try
                {
                    AdhocPackage.PackFromFolder(packVerbs.InputPath, packVerbs.OutputPath);
                    Environment.ExitCode = 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to pack mpackage - {e.Message}");
                    Environment.ExitCode = -1;
                }
            }
            else
            {
                Console.WriteLine("Found nothing to pack - ensure the provided output path has the proper file extension (gpb/mpackage)");
                Environment.ExitCode = -1;
            }
        }

        public static void Unpack(UnpackVerbs unpackVerbs)
        {
            if (!File.Exists(unpackVerbs.InputPath))
            {
                Console.WriteLine("File does not exist.");
                Environment.ExitCode = -1;
                return;
            }

            if (unpackVerbs.InputPath.ToLower().EndsWith("gpb"))
            {
                Console.WriteLine("Assuming input is GPB");
                
                var gpb = GpbBase.ReadFile(unpackVerbs.InputPath);
                if (gpb is null)
                {
                    Console.WriteLine("Could not parse GPB Header.");
                    Environment.ExitCode = -1;
                    return;
                }

                if (string.IsNullOrEmpty(unpackVerbs.OutputPath))
                    unpackVerbs.OutputPath = Path.GetDirectoryName(unpackVerbs.InputPath);

                try
                {
                    gpb.Unpack(Path.GetFileNameWithoutExtension(unpackVerbs.InputPath), unpackVerbs.OutputPath, unpackVerbs.ConvertGPBFiles);
                    Environment.ExitCode = 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to unpack gpb - {e.Message}.");
                    Environment.ExitCode = -1;
                }
            }
            else if (unpackVerbs.InputPath.EndsWith("mpackage"))
            {
                Console.WriteLine("Assuming input is MPackage");
                try
                {
                    AdhocPackage.ExtractPackage(unpackVerbs.InputPath);
                    Environment.ExitCode = 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to unpack mpackage - {e.Message}.");
                    Environment.ExitCode = -1;
                }
            }
            else
            {
                Console.WriteLine("Found nothing to unpack - ensure the provided input file has the proper file extension (gpb/mpackage)");
                Environment.ExitCode = -1;
            }
        }

        public static void HandleNotParsedArgs(IEnumerable<Error> errors)
        {
            Environment.ExitCode = -1;
        }

        private static void BuildProject(BuildVerbs buildVerbs)
        {
            AdhocProject prj;
            try
            {
                prj = AdhocProject.Read(buildVerbs.InputPath);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load project file - {e.Message}");
                Environment.ExitCode = -1;
                return;
            }

            Logger.Info($"Project file: {buildVerbs.InputPath}");
            prj.PrintInfo();

            Logger.Info("Started project build.");
            if (!prj.Build(buildVerbs.WriteExceptionsToFile))
            {
                Logger.Error("Project build failed.");
                Environment.ExitCode = -1;
            }
            else
            {
                Logger.Info("Project build successful.");
                Environment.ExitCode = 0;
            }
        }

        private static void BuildScript(string inputPath, string output, int version = 12, bool debugExceptions = false, bool preprocessOnly = false)
        {
            var source = File.ReadAllText(inputPath);
            var time = new FileInfo(inputPath).LastWriteTime;

            try
            {
                var preprocessor = new AdhocScriptPreprocessor();
                preprocessor.SetBaseDirectory(Path.GetDirectoryName(inputPath));
                preprocessor.SetCurrentFileName(inputPath);
                preprocessor.SetCurrentFileTimestamp(time);

                string preprocessed = preprocessor.Preprocess(source);
                if (preprocessOnly)
                {
                    Console.Write(preprocessed);
                    Environment.ExitCode = 0;
                    return;
                }

                Logger.Info($"Started script build ({inputPath}).");

                var errorHandler = new AdhocErrorHandler();
                var parser = new AdhocAbstractSyntaxTree(preprocessed, new ParserOptions()
                {
                    ErrorHandler = errorHandler
                });
                parser.SetFileName(inputPath);

                var program = parser.ParseScript();
                if (errorHandler.HasErrors())
                {
                    foreach (ParseError error in errorHandler.Errors)
                        Logger.Error($"Syntax error: {error.Description} at {error.Source}:{error.LineNumber}");

                    Environment.ExitCode = -1;
                    return;
                }

                var analyzer = new AdhocScriptAnalyzer(parser);
                analyzer.ParseScript(program);
                analyzer.GetScopesFromPosition(47);

                var compiler = new AdhocScriptCompiler();
                compiler.SetSourcePath(inputPath);
                compiler.Setup(version);

                if (debugExceptions)
                    compiler.BuildTryCatchDebugStatements();

                compiler.CompileScript(program);

                AdhocCodeGen codeGen = new AdhocCodeGen(compiler.MainFrame, compiler.SymbolMap);
                codeGen.Generate();
                codeGen.SaveTo(output);

                Logger.Info($"Script build successful.");
                Environment.ExitCode = 0;
                return;
            }
            catch (PreprocessorException preprocessException)
            {
                Logger.Error($"{preprocessException.FileName}:{preprocessException.Token.Location.Start.Line}: preprocess error: {preprocessException.Message}");
            }
            catch (ParserException parseException)
            {
                Logger.Error($"Syntax error: {parseException.Description} at {parseException.SourceText}:{parseException.LineNumber}");
            }
            catch (AdhocCompilationException compileException)
            {
                Logger.Error($"Compilation error: {compileException.Message}");
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "Internal error in compilation");
            }

            Logger.Error("Script build failed.");
            Environment.ExitCode = -1;
        }

        public static void MProjectToBin(MProjectToBinVerbs verbs)
        {
            if (verbs.Version == 0)
            {
                Console.WriteLine("Version 0 is not currently supported.");
                Environment.ExitCode = -1;
                return;
            }
            else if (verbs.Version > 1 || verbs.Version < 0)
            {
                Console.WriteLine("Version must be 0 or 1. (0 not current supported).");
                Environment.ExitCode = -1;
                return;
            }

            var mbin = new MBinaryIO(verbs.InputPath);
            mNode rootNode = mbin.Read();

            if (rootNode is null)
            {
                var mtext = new MTextIO(verbs.InputPath);
                rootNode = mtext.Read();

                if (rootNode is null)
                {
                    Console.WriteLine("Could not read mproject.");
                    return;
                }
            }

            try
            {
                MBinaryWriter writer = new MBinaryWriter(verbs.OutputPath);
                writer.Version = verbs.Version;
                writer.WriteNode(rootNode);

                Console.WriteLine($"Done. Exported to '{verbs.OutputPath}'.");
                Environment.ExitCode = 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to export - {e.Message}");
                Environment.ExitCode = -1;
            }
        }

        public static void MProjectToText(MProjectToTextVerbs verbs)
        {
            var mbin = new MBinaryIO(verbs.InputPath);
            mNode rootNode = mbin.Read();

            if (rootNode is null)
            {
                var mtext = new MTextIO(verbs.InputPath);
                rootNode = mtext.Read();

                if (rootNode is null)
                {
                    Console.WriteLine("Could not read mproject.");
                    return;
                }
            }

            try
            {
                using MTextWriter writer = new MTextWriter(verbs.OutputPath);
                writer.Debug = verbs.Debug;
                writer.WriteNode(rootNode);

                Console.WriteLine($"Done. Exported to '{verbs.OutputPath}'.");
                Environment.ExitCode = 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to export - {e.Message}");
                Environment.ExitCode = -1;
            }
        }
    }

    [Verb("build", HelpText = "Builds/Compiles a project or script file.")]
    public class BuildVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input project file or source script.")]
        public string InputPath { get; set; }

        [Option('o', "output", Required = false, HelpText = "Output compiled scripts when compiling standalone scripts (not projects).")]
        public string OutputPath { get; set; }

        [Option('v', "version", Required = false, Default = 12, HelpText = "Adhoc compile version (for files, not projects).")]
        public int Version { get; set; }

        [Option("write-exceptions-to-file", Required = false, HelpText = "Artificially creates try/catch instructions to all code blocks compiled which will print adhoc exceptions to /APP_DATA_RAW/exceptions.txt (aka USRDIR) when thrown. " +
            "Very useful if the game does not normally print any error on adhoc exceptions and you do not have access to a debugger to breakpoint on a certain function to check errors.\n" +
            "This will only work for GT5 and above. Use carefully. Not entirely tested, may break or not work at all.")]
        public bool WriteExceptionsToFile { get; set; }

        [Option('e', Required = false, HelpText = "Preprocess only and output to stdout. Only for compiling scripts.")]
        public bool PreprocessOnly { get; set; }
    }

    [Verb("pack", HelpText = "Pack files like gpb's, or mpackage's.")]
    public class PackVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input folder.")]
        public string InputPath { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output folder for packed files.")]
        public string OutputPath { get; set; }

        [Option("le", HelpText = "Pack as little endian?")]
        public bool LittleEndian { get; set; }

        [Option("gt5", HelpText = "Use GT5 pack mode (no leading /'s)")]
        public bool GT5PackMode { get; set; }
    }

    [Verb("unpack", HelpText = "Unpack files like gpb's, or mpackage's.")]
    public class UnpackVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input file. (GPB's, mPackages)")]
        public string InputPath { get; set; }

        [Option('o', "output", HelpText = "Output folder for unpacked files.")]
        public string OutputPath { get; set; }

        [Option("convert-gpb-files", HelpText = "Whether to convert GPB texture files to their original formats (png, dds).")]
        public bool ConvertGPBFiles { get; set; } = true;
    }

    [Verb("mproject-to-bin", HelpText = "Read mwidget/mproject and outputs it to a binary version of it.")]
    public class MProjectToBinVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input folder.")]
        public string InputPath { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output folder.")]
        public string OutputPath { get; set; }

        [Option('v', "version", HelpText = "Version of the binary file. Default is 1. (0 is currently unsupported, used for GT5 and under. 1 is GT6 and above.")]
        public int Version { get; set; }
    }

    [Verb("mproject-to-text", HelpText = "Read mwidget/mproject and outputs it to a text version of it.")]
    public class MProjectToTextVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input folder.")]
        public string InputPath { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output folder.")]
        public string OutputPath { get; set; }

        [Option('d', "debug", HelpText = "Write debug info to the output text file. Note: This will produce a non-working text mproject file.")]
        public bool Debug { get; set; }
    }
}
