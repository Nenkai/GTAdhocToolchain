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
using Esprima.Ast;

namespace GTAdhocToolchain.CLI;

public class Program
{
    public static readonly Version Version = new(1, 1, 1);

    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public static void Main(string[] args)
    {
        Console.WriteLine("---------------------------------------------");
        Console.WriteLine($"- GTAdhocToolchain {Version} by Nenkai");
        Console.WriteLine("---------------------------------------------");
        Console.WriteLine("- https://github.com/Nenkai");
        Console.WriteLine("---------------------------------------------");

        if (args.Length == 1 && args[0] != "build")
        {
            if (Directory.Exists(args[0]))
            {
                int exitCode = 0;
                foreach (var file in Directory.GetFiles(args[0], "*", SearchOption.AllDirectories))
                {
                    exitCode = ProcessFile(file);
                    if (exitCode == -1)
                        break;
                }

                Environment.ExitCode = exitCode;
            }
            else
            {
                Environment.ExitCode = ProcessFile(args[0]);
            }

            return;
        }

        Parser.Default.ParseArguments<BuildVerbs, PackVerbs, UnpackVerbs, MProjectToBinVerbs, MProjectToTextVerbs>(args)
            .WithParsed<BuildVerbs>(Build)
            .WithParsed<PackVerbs>(Pack)
            .WithParsed<UnpackVerbs>(Unpack)
            .WithParsed<MProjectToBinVerbs>(MProjectToBin)
            .WithParsed<MProjectToTextVerbs>(MProjectToText)
            .WithNotParsed(HandleNotParsedArgs);
    }

    private static int ProcessFile(string file)
    {
        try
        {
            if (file.ToLower().EndsWith(".adc"))
            {
                List<AdhocFile> scripts = null;
                try
                {
                    scripts = AdhocFile.ReadFromFile(file);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Errored while reading: {e.Message}");
                    return -1;
                }

                foreach (var adc in scripts)
                {
                    adc.Disassemble(Path.ChangeExtension(file, ".ad.diss"));

                    if (adc.Version == 12)
                        adc.PrintStrings(Path.ChangeExtension(file, ".strings"));
                }
            }
            else if (file.ToLower().EndsWith(".gpb"))
            {
                var gpb = GpbBase.ReadFile(file);
                if (gpb is null)
                {
                    Console.WriteLine("Could not parse GPB Header.");
                    return -1;
                }

                string fileName = Path.GetFileNameWithoutExtension(file);
                string dir = Path.GetDirectoryName(file);

                gpb.Unpack(Path.GetFileNameWithoutExtension(file), Path.Combine(dir, fileName), convertImages: true);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e}");
            return -1;
        }

        return 0;
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
        if (!string.IsNullOrWhiteSpace(buildVerbs.InputPath) && File.Exists(buildVerbs.InputPath))
        {
            buildVerbs.InputPath = buildVerbs.InputPath;
        }
        else
        {
            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.yaml", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                Logger.Error("No target project to compile in the current directory and no script file was specified. Specify the project (or script) to compile.");
                Environment.ExitCode = -1;
                return;
            }

            if (files.Length > 1)
            {
                Logger.Error("More than one target project in the current directory. Specify the project (or script) to compile.");
                foreach (var file in files)
                    Logger.Error($"- {Path.GetFileName(file)}");

                Environment.ExitCode = -1;
                return;
            }

            buildVerbs.InputPath = files[0];
        }

        if (Path.GetExtension(buildVerbs.InputPath) == ".yaml")
        {
            BuildProject(buildVerbs);
        }
        else if (Path.GetExtension(buildVerbs.InputPath) == ".ad")
        {
            string output = !string.IsNullOrEmpty(buildVerbs.OutputPath) ? buildVerbs.OutputPath : buildVerbs.InputPath;
            BuildScript(buildVerbs.InputPath, Path.ChangeExtension(output, ".adc"), buildVerbs.Version, buildVerbs.WriteExceptionsToFile, buildVerbs.PreprocessOnly, buildVerbs.BaseIncludeFolder);
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
        if (Directory.Exists(unpackVerbs.InputPath))
        {
            foreach (var file in Directory.GetFiles(unpackVerbs.InputPath, "*", SearchOption.TopDirectoryOnly))
            {
                UnpackFile(file, unpackVerbs.OutputPath, unpackVerbs.ConvertGPBFiles);
            }
        }
        else if (File.Exists(unpackVerbs.InputPath))
        {
            UnpackFile(unpackVerbs.InputPath, unpackVerbs.OutputPath, unpackVerbs.ConvertGPBFiles);
        }
        else
        {
            Console.WriteLine("Found nothing to unpack - ensure the provided input file has the proper file extension (gpb/mpackage)");
            Environment.ExitCode = -1;
        }
    }

    private static void UnpackFile(string inputFile, string outputPath, bool convertGpbFiles)
    {
        if (inputFile.ToLower().EndsWith("gpb"))
        {
            Console.WriteLine($"[:] {inputFile} - assuming input is GPB");
            ExtractGpb(inputFile, outputPath, convertGpbFiles);
        }
        else if (inputFile.EndsWith("mpackage"))
        {
            Console.WriteLine($"[:] {inputFile} - assuming input is MPackage");
            try
            {
                AdhocPackage.ExtractPackage(inputFile);
                Environment.ExitCode = 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: Failed to unpack mpackage - {e.Message}.");
                Environment.ExitCode = -1;
            }
        }
    }

    private static void ExtractGpb(string inputFile, string outputPath, bool convertGpbFiles)
    {
        var gpb = GpbBase.ReadFile(inputFile);
        if (gpb is null)
        {
            Console.WriteLine("Could not parse GPB Header.");
            Environment.ExitCode = -1;
        }

        if (string.IsNullOrEmpty(outputPath))
            outputPath = Path.GetDirectoryName(inputFile);

        try
        {
            gpb.Unpack(Path.GetFileNameWithoutExtension(inputFile), outputPath, convertGpbFiles);
            Environment.ExitCode = 0;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to unpack gpb - {e.Message}.");
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
        if (!prj.Build(buildVerbs.WriteExceptionsToFile, buildVerbs.OutputPath))
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

    private static void BuildScript(string inputPath, string output, int version = 12, bool debugExceptions = false, bool preprocessOnly = false, string baseIncludeFolder = "")
    {
        var source = File.ReadAllText(inputPath);
        var time = new FileInfo(inputPath).LastWriteTime;

        try
        {
            string absoluteIncludePath = Path.GetDirectoryName(inputPath);
            string sourceFile = inputPath;
            if (!string.IsNullOrWhiteSpace(baseIncludeFolder))
            {
                absoluteIncludePath = Path.GetFullPath(Path.Combine(absoluteIncludePath, baseIncludeFolder)); // Returns baseIncludeFolder if it's absolute, otherwise applies it to the source file's location
                sourceFile = Path.GetRelativePath(absoluteIncludePath, inputPath).Replace('\\', '/'); // Rewrite the path to be relative to the base folder, and normalise to forward slashes
            }

            var preprocessor = new AdhocScriptPreprocessor();
            preprocessor.SetBaseDirectory(absoluteIncludePath);
            preprocessor.SetCurrentFileName(sourceFile);
            preprocessor.SetCurrentFileTimestamp(time);

            string preprocessed = preprocessor.Preprocess(source);
            if (preprocessOnly)
            {
                Console.Write(preprocessed);
                Environment.ExitCode = 0;
                return;
            }

            Logger.Info($"Started script build ({inputPath}).");
            Logger.Warn($"NOTE: Compiling for Adhoc Version {version}");

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

            var compiler = new AdhocScriptCompiler();
            if (!string.IsNullOrWhiteSpace(baseIncludeFolder))
            {
                compiler.SetBaseIncludeFolder(absoluteIncludePath);
            }
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
    [Option('i', "input", HelpText = "Input project file or source script. If not specified, attempts to find a .yaml file from the current directory.")]
    public string InputPath { get; set; }

    [Option('o', "output", Required = false, HelpText = "Output compiled scripts when compiling standalone scripts or projects.")]
    public string OutputPath { get; set; }

    [Option('v', "version", Required = false, Default = 12, HelpText = "Adhoc compile version (for files, not projects).")]
    public int Version { get; set; }

    [Option("write-exceptions-to-file", Required = false, HelpText = "Artificially creates try/catch instructions to all code blocks compiled which will print adhoc exceptions to /APP_DATA_RAW/exceptions.txt (aka USRDIR) when thrown. " +
        "Very useful if the game does not normally print any error on adhoc exceptions and you do not have access to a debugger to breakpoint on a certain function to check errors.\n" +
        "This will only work for GT5 and above. Use carefully. Not entirely tested, may break or not work at all.")]
    public bool WriteExceptionsToFile { get; set; }

    [Option("preprocess-only", Required = false, HelpText = "Preprocess only and output to stdout. Only for compiling scripts.")]
    public bool PreprocessOnly { get; set; }

    [Option('b', "base-include-folder", Required = false, HelpText = "Set the root path for #include statements (for files, not projects).")]
    public string BaseIncludeFolder { get; set; }
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
