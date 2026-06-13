// Copyright (c) 2026 Nenkai
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using System.CommandLine;

using Esprima;

using NLog;

using GTAdhocToolchain.CodeGen;
using GTAdhocToolchain.Compiler;
using GTAdhocToolchain.Core.Instructions;
using GTAdhocToolchain.Disasm;
using GTAdhocToolchain.Menu;
using GTAdhocToolchain.Menu.Fields;
using GTAdhocToolchain.Menu.Resources;
using GTAdhocToolchain.Packaging;
using GTAdhocToolchain.Preprocessor;
using GTAdhocToolchain.Project;
using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.CLI;

public class Program
{
    public static Version? GetExecutableVersion() => Assembly.GetEntryAssembly()?.GetName().Version;

    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("---------------------------------------------");
        Console.WriteLine($"- GTAdhocToolchain {GetExecutableVersion()?.ToString() ?? "vUnknown"} by Nenkai");
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

            return 0;
        }

        var buildCommand = new Command("build", "Builds/Compiles a project or script file.")
        {
            new Option<string>("--input", aliases: ["-i"]) { Description = "Input project file or source script. If not specified, attempts to find a .yaml file from the current directory." },
            new Option<string>("--output", aliases: ["-o"]) { Description = "Output compiled scripts when compiling standalone scripts or projects." },
            new Option<uint>("--version", ["-v"]) { DefaultValueFactory = (res) => 12, Description = "Adhoc compile version (for files, not projects)." },
            new Option<bool>("--preprocess-only") { Description = "Preprocess only and output to stdout. Only for compiling scripts." },
            new Option<string>("--base-include-folder", aliases: ["-b"]) { Description = "Set the root path for #include statements (for files, not projects)." },
            new Option<bool>("--write-exceptions-to-file") { Description = "Artificially creates try/catch instructions to all code blocks compiled which will print adhoc exceptions to /APP_DATA_RAW/exceptions.txt (aka USRDIR) when thrown.\n" +
                "Very useful if the game does not normally print any error on adhoc exceptions and you do not have access to a debugger to breakpoint on a certain function to check errors.\n" +
                "This will only work for GT5 and above. Use carefully. Not entirely tested, may break or not work at all." },
        };
        buildCommand.SetAction(Build);

        var replCommand = new Command("disassembly-repl", "Starts a disassembler repl for quickly disassembling input adhoc source code.")
        {
            new Option<uint>("--version", ["-v"]) { DefaultValueFactory = (res) => 12, Description = "Adhoc version. Defaults to 12." },
        };
        replCommand.SetAction(DissasemblyRepl);

        var packCommand = new Command("pack", "Pack files like gpb's, or mpackage's.")
        {
            new Option<string>("--input", aliases: ["-i"]) { Required = true, Description = "Input folder." },
            new Option<string>("--output", aliases: ["-o"]) { Required = true, Description = "Output folder for packed files." },
            new Option<bool>("--le") { Description = "Pack as little endian?" },
            new Option<bool>("--gt5") { Description = "Use GT5 pack mode (no leading /'s)" }
        };
        packCommand.SetAction(Pack);

        var unpackCommand = new Command("unpack", "Unpack files like gpb's, or mpackage's.")
        {
            new Option<FileInfo>("--input", aliases: ["-i"]) { Required = true, Description = "Input file. (GPB's, mPackages)" },
            new Option<string>("--output", aliases: ["-o"]) { Description = "Output folder for unpacked files." },
            new Option<bool>("--convert-gpb-files") { Description = "Whether to convert GPB texture files to their original formats (png, dds)." }
        };
        unpackCommand.SetAction(Unpack);

        var mprojectToBinCommand = new Command("mproject-to-bin", "Read mwidget/mproject and outputs it to a binary version of it.")
        {
            new Option<string>("--input", aliases: ["-i"]) { Required = true, Description = "Input folder." },
            new Option<string>("--output", aliases: ["-o"]) { Required = true, Description = "Output folder." },
            new Option<int>("--version", aliases: ["-v"]) { DefaultValueFactory = (res) => 1, Description = "Version of the binary file. Default is 1. (0 is currently unsupported, used for GT5 and under. 1 is GT6 and above." }
        };
        mprojectToBinCommand.SetAction(MProjectToBin);

        var mprojectToTextCommand = new Command("mproject-to-text", "Read mwidget/mproject and outputs it to a text version of it.")
        {
            new Option<string>("--input", aliases: ["-i"]) { Required = true, Description = "Input folder." },
            new Option<string>("--output", aliases: ["-o"]) { Required = true, Description = "Output folder." },
            new Option<bool>("--debug", aliases: ["-d"]) { Description = "Write debug info to the output text file. Note: This will produce a non-working text mproject file." }
        };
        mprojectToTextCommand.SetAction(MProjectToText);

        var rootCommand = new RootCommand("adhoc")
        {
            buildCommand,
            replCommand,
            packCommand,
            unpackCommand,
            mprojectToBinCommand,
            mprojectToTextCommand
        };

        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static int ProcessFile(string file)
    {
        try
        {
            if (file.ToLower().EndsWith(".adc"))
            {
                List<AdhocFile>? scripts = null;
                try
                {
                    scripts = AdhocFile.ReadFromFile(file);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Errored while reading {}:", file);
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
                    Logger.Error("Could not parse GPB Header.");
                    return -1;
                }

                string fileName = Path.GetFileNameWithoutExtension(file);
                string? dir = Path.GetDirectoryName(file);

                gpb.Unpack(Path.GetFileNameWithoutExtension(file), Path.Combine(dir ?? string.Empty, fileName), convertImages: true);
            }
        }
        catch (Exception e)
        {
            Logger.Error(e, "Errored while processing file {}", file);
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

    public static int Build(ParseResult parseResult)
    {
        string? inputPath = parseResult.GetValue<string>("--input");
        string? outputPath = parseResult.GetValue<string>("--output");

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.yaml", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                Logger.Error("No target project to compile in the current directory and no script file was specified. Specify the project (or script) to compile.");
                return -1;
            }

            if (files.Length > 1)
            {
                Logger.Error("More than one target project in the current directory. Specify the project (or script) to compile.");
                foreach (var file in files)
                    Logger.Error($"- {Path.GetFileName(file)}");

                return -1;
            }

            inputPath = files[0];
        }
        else
        {
            if (!File.Exists(inputPath))
            {
                Logger.Error("Specified input file does not exist.");
                return -1;
            }
        }

        uint version = parseResult.GetValue<uint>("--version");
        bool writeExceptionsToFile = parseResult.GetValue<bool>("--write-exceptions-to-file");
        bool preprocessOnly = parseResult.GetValue<bool>("--preprocess-only");
        string? baseIncludeFolder = parseResult.GetValue<string>("--base-include-folder");

        if (Path.GetExtension(inputPath) == ".yaml")
        {
            return BuildProject(inputPath, outputPath, writeExceptionsToFile);
        }
        else if (Path.GetExtension(inputPath) == ".ad")
        {
            string output = !string.IsNullOrEmpty(outputPath) ? outputPath : inputPath;
            return BuildScript(inputPath, Path.ChangeExtension(output, ".adc"), version, writeExceptionsToFile, preprocessOnly, baseIncludeFolder);
        }
        else
        {
            Logger.Error("Input File is not a project or script.");
            return -1;
        }
    }

    public static int Pack(ParseResult parseResult)
    {
        string inputPath = parseResult.GetRequiredValue<string>("--input");
        string outputPath = parseResult.GetRequiredValue<string>("--output");
        bool gt5PackMode = parseResult.GetValue<bool>("--gt5");
        bool littleEndian = parseResult.GetValue<bool>("--le");

        if (outputPath.ToLower().EndsWith("gpb"))
        {
            try
            {
                var gpb = new GpbData3();
                gpb.AddFilesFromFolder(inputPath, gt5PackMode);
                gpb.Pack(outputPath, !littleEndian);
                return 0;
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to pack gpb - {e.Message}");
                return -1;
            }
        }
        else if (outputPath.EndsWith("mpackage"))
        {
            try
            {
                AdhocPackage.PackFromFolder(inputPath, outputPath);
                return 0;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to pack mpackage {}", inputPath);
                return -1;
            }
        }
        else
        {
            Logger.Error("Found nothing to pack - ensure the provided output path has the proper file extension (gpb/mpackage)");
            return -1;
        }
    }

    public static int Unpack(ParseResult parseResult)
    {
        string? inputPath = parseResult.GetValue<string>("--input");
        string? outputPath = parseResult.GetValue<string>("--output");
        bool convertGpbFiles = parseResult.GetValue<bool>("--convert-gpb-files");

        if (Directory.Exists(inputPath))
        {
            bool hasError = false;
            foreach (var file in Directory.GetFiles(inputPath, "*", SearchOption.TopDirectoryOnly))
            {
                if (UnpackFile(file, outputPath, convertGpbFiles) != 0)
                {
                    hasError = true;
                }
            }

            return hasError ? -1 : 0;
        }
        else if (File.Exists(inputPath))
        {
            return UnpackFile(inputPath, outputPath, convertGpbFiles);
        }
        else
        {
            Logger.Error("Found nothing to unpack - ensure the provided input file has the proper file extension (gpb/mpackage)");
            return -1;
        }
    }

    private static int UnpackFile(string inputFile, string? outputPath, bool convertGpbFiles)
    {
        if (inputFile.ToLower().EndsWith("gpb"))
        {
            Logger.Info($"[:] {inputFile} - assuming input is GPB");
            ExtractGpb(inputFile, outputPath, convertGpbFiles);
            return 0;
        }
        else if (inputFile.EndsWith("mpackage"))
        {
            Logger.Info($"[:] {inputFile} - assuming input is MPackage");
            try
            {
                AdhocPackage.ExtractPackage(inputFile);
                return 0;
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to unpack mpackage - {e.Message}.");
                return -1;
            }
        }

        return 0; // No file, skipped
    }

    private static int ExtractGpb(string inputFile, string? outputPath, bool convertGpbFiles)
    {
        var gpb = GpbBase.ReadFile(inputFile);
        if (gpb is null)
        {
            Logger.Error("Could not parse GPB Header.");
            return -1;
        }

        if (string.IsNullOrEmpty(outputPath))
            outputPath = Path.GetDirectoryName(inputFile);

        if (string.IsNullOrEmpty(outputPath))
        {
            Logger.Error("Could not determine an output directory.");
            return -1;
        }

        try
        {
            gpb.Unpack(Path.GetFileNameWithoutExtension(inputFile), outputPath, convertGpbFiles);
            Logger.Info("Extracted to {outputPath}", outputPath);
            return 0;
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to unpack gpb {}", inputFile);
            return -1;
        }
    }

    private static int BuildProject(string inputPath, string? outputPath, bool writeExceptionsToFile = false)
    {
        AdhocProject? prj;
        try
        {
            prj = AdhocProject.Read(inputPath);
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to load project file - {e.Message}");
            return -1;
        }

        if (prj is null)
        {
            Logger.Error($"Unable to verify project file.");
            return -1;
        }

        Logger.Info($"Project file: {inputPath}");
        prj.PrintInfo();

        Logger.Info("Started project build.");
        if (!prj.Build(writeExceptionsToFile, outputPath))
        {
            Logger.Error("Project build failed.");
            return -1;
        }
        else
        {
            Logger.Info("Project build successful.");
            return 0;
        }
    }

    private static int BuildScript(string inputPath, string output, uint version = 12, bool debugExceptions = false, bool preprocessOnly = false, string? baseIncludeFolder = "")
    {
        var source = File.ReadAllText(inputPath);
        var time = new FileInfo(inputPath).LastWriteTime;

        try
        {
            string? absoluteIncludePath = Path.GetDirectoryName(inputPath);
            if (string.IsNullOrWhiteSpace(absoluteIncludePath))
            {
                Logger.Error("Could not determine base directory of input file?");
                return -1;
            }

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
                return 0;
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

                return -1;
            }

            var compiler = new AdhocScriptCompiler(version);
            if (!string.IsNullOrWhiteSpace(baseIncludeFolder))
            {
                compiler.SetBaseIncludeFolder(absoluteIncludePath);
            }

            if (debugExceptions)
                compiler.BuildTryCatchDebugStatements();

            AdhocCodeFrame codeFrame = compiler.CompileScript(program, inputPath);

            AdhocCodeGen codeGen = new AdhocCodeGen(codeFrame, compiler.SymbolMap);
            codeGen.Generate();
            codeGen.SaveTo(output);

            Logger.Info($"Script build successful.");
            return 0;
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
        return -1;
    }

    private static int DissasemblyRepl(ParseResult parseResult)
    {
        uint version = parseResult.GetValue<uint>("--version");
        Console.Clear();
        Console.WriteLine("REPL mode. Start typing adhoc code to dissasemble it. Enter /? for more commands.");
        Console.WriteLine($"Adhoc Version: {version}");

        while (true)
        {
            Console.Write(">");
            string? line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("/?"))
            {
                Console.WriteLine("/clear - Clears the console");
                Console.WriteLine("/version - Sets adhoc version");
                continue;
            }
            else if (line.StartsWith("/clear"))
            {
                Console.Clear();
                continue;
            }
            else if (line.StartsWith("/version"))
            {
                ReadOnlySpan<char> range = line.AsSpan()["/version".Length..].Trim();
                if (range.IsEmpty)
                {
                    Console.WriteLine($"Current adhoc version is set to {version}.");
                }
                else if (uint.TryParse(range, out version))
                {
                    Console.WriteLine($"Now compiling for adhoc version {version}");
                }
                else
                {
                    Console.WriteLine("Invalid version. Usage: /version <adhoc version>");
                }

                continue;
            }


            var preprocessor = new AdhocScriptPreprocessor();
            preprocessor.SetCurrentFileName("temp.ad");

            string preprocessed = preprocessor.Preprocess(line);

            var errorHandler = new AdhocErrorHandler();
            var parser = new AdhocAbstractSyntaxTree(preprocessed, new ParserOptions()
            {
                ErrorHandler = errorHandler
            });
            parser.SetFileName("temp.ad");

            var program = parser.ParseScript();
            if (errorHandler.HasErrors())
            {
                foreach (ParseError error in errorHandler.Errors)
                    Logger.Error($"Syntax error: {error.Description} at {error.Source}:{error.LineNumber}");
            }
            else
            {
                try
                {
                    var compiler = new AdhocScriptCompiler(version);
                    AdhocCodeFrame codeFrame = compiler.CompileScript(program, "test.ad");

                    AdhocCodeGen codeGen = new AdhocCodeGen(codeFrame, compiler.SymbolMap);
                    codeGen.Generate();

                    for (int i = 0; i < codeGen.Frame.Instructions.Count; i++)
                    {
                        InstructionBase inst = codeGen.Frame.Instructions[i];
                        Dissasemble(inst, i, 0);
                    }

                    void Dissasemble(InstructionBase inst, int instNumber, int depth)
                    {
                        Console.WriteLine($"{new string(' ', depth * 2)} {instNumber,3} | {inst}");
                        if (inst.IsFunctionOrMethod())
                        {
                            SubroutineBase subroutine = (SubroutineBase)inst;
                            for (int i = 0; i < subroutine.CodeFrame.Instructions.Count; i++)
                            {
                                InstructionBase subInst = subroutine.CodeFrame.Instructions[i];
                                Dissasemble(subInst, i, depth + 1);
                            }
                        }
                    }
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
            }
        }
    }

    public static int MProjectToBin(ParseResult parseResult)
    {
        uint version = parseResult.GetValue<uint>("--version");
        if (version == 0)
        {
            Logger.Error("Version 0 is not currently supported.");
            return -1;
        }
        else if (version > 1 || version < 0)
        {
            Logger.Error("Version must be 0 or 1. (0 not currently supported).");
            return -1;
        }

        string inputPath = parseResult.GetRequiredValue<string>("--input");
        string outputPath = parseResult.GetRequiredValue<string>("--output");

        var mbin = new MBinaryIO(inputPath);
        mNode? rootNode = mbin.Read();

        if (rootNode is null)
        {
            var mtext = new MTextIO(inputPath);
            rootNode = mtext.Read();

            if (rootNode is null)
            {
                Logger.Error("Could not read mproject.");
                return -1;
            }
        }

        try
        {
            MBinaryWriter writer = new MBinaryWriter(outputPath);
            writer.Version = (int)version;
            writer.WriteNode(rootNode);

            Logger.Info($"Done. Exported to '{outputPath}'.");
            return 0;
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to export to '{}'", outputPath);
            return -1;
        }
    }

    public static int MProjectToText(ParseResult parseResult)
    {
        string? inputPath = parseResult.GetRequiredValue<string>("--input");
        string? outputPath = parseResult.GetRequiredValue<string>("--output");
        bool debug = parseResult.GetValue<bool>("--debug");

        var mbin = new MBinaryIO(inputPath);
        mNode? rootNode = mbin.Read();

        if (rootNode is null)
        {
            var mtext = new MTextIO(inputPath);
            rootNode = mtext.Read();

            if (rootNode is null)
            {
                Logger.Error("Could not read mproject.");
                return -1;
            }
        }

        try
        {
            using MTextWriter writer = new MTextWriter(outputPath);
            writer.Debug = debug;
            writer.WriteNode(rootNode);

            Logger.Info("Done. Exported to '{}'.", outputPath);
            return 0;
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to export to '{}'", outputPath);
            return -1;
        }
    }
}