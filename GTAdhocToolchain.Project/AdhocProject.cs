using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Esprima;
using Esprima.Ast;

using GTAdhocToolchain.Compiler;
using GTAdhocToolchain.CodeGen;
using GTAdhocToolchain.Packaging;
using GTAdhocToolchain.Menu;

namespace GTAdhocToolchain.Project
{
    public class AdhocProject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public string ProjectName { get; set; }

        public string OutputName { get; set; }

        public AdhocProjectFile[] FilesToCompile { get; set; }

        public int Version { get; set; } = 12;

        /// <summary>
        /// ../../projects/<code>/<project_name>
        /// </summary>
        public string ProjectFolder { get; set; }

        /// <summary>
        /// projects/<code>/<project_name>
        /// </summary>
        public string SourceProjectFolder { get; set; }

        public string ProjectDir { get; set; }

        public string ProjectFilePath { get; set; }

        public string BaseIncludeFolder { get; set; }

        /// <summary>
        /// Whether to build an .mpackage
        /// </summary>
        public bool BuildPackage { get; set; }

        /// <summary>
        /// Whether to serialize project components (.mwidget/.mproject) to binary
        /// </summary>
        public bool SerializeComponents { get; set; }


        /// <summary>
        /// Version to serialize the components to
        /// </summary>
        public int SerializeComponentsVersion { get; set; }

        public static AdhocProject Read(string path)
        {
            var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(UnderscoredNamingConvention.Instance)
                        .IgnoreUnmatchedProperties()
                        .Build();

            string file = File.ReadAllText(path);
            AdhocProject prj = deserializer.Deserialize<AdhocProject>(file);

            var (Result, ErrorMessage) = prj.VerifyProjectFile();
            if (!Result)
            {
                Logger.Error(ErrorMessage);
                return null;
            }

            // Convert project folder into full path
            prj.ProjectFilePath = path;
            prj.ProjectDir = Path.GetFullPath(Path.Combine(prj.ProjectFilePath, prj.ProjectFolder)).Replace('\\', '/');
            prj.BaseIncludeFolder = Path.GetFullPath(Path.Combine(prj.ProjectFilePath, prj.BaseIncludeFolder)).Replace('\\', '/'); // Convert relative include folder to full path
            prj.SourceProjectFolder = prj.ProjectDir.Substring(prj.BaseIncludeFolder.Length).Replace('\\', '/'); // Get the relative path to our source from base include

            foreach (var fileToCompile in prj.FilesToCompile)
            {
                fileToCompile.FullPath = Path.GetFullPath(Path.Combine(prj.ProjectDir, fileToCompile.Name));
                fileToCompile.SourcePath = Path.Combine(prj.SourceProjectFolder, fileToCompile.Name).Replace('\\', '/');
            }

            return prj;
        }

        public void PrintInfo()
        {
            Logger.Info($"Project: {ProjectName}");
            Logger.Info($"Version: {Version}");
            Logger.Info($"Output File: {OutputName}");
        }

        public bool Build()
        {
            if (!Directory.Exists(ProjectDir))
            {
                Logger.Error($"Project directory does not exist ({ProjectDir})");
                return false;
            }


            string tmpFilePath = string.Empty;
            string pkgPath = Path.Combine(ProjectDir, "pkg_tmp");

            try
            {
                if (BuildPackage)
                    BuildPackageFile();

                string mergedScriptName = OutputName + ".adc";
                Logger.Info($"Building merged script '{mergedScriptName}'");

                string tmpFileName = $"_tmp_{OutputName}.ad";
                if (!LinkFiles(tmpFileName))
                    return false;

                tmpFilePath = Path.Combine(ProjectDir, tmpFileName);
                if (!File.Exists(tmpFilePath))
                {
                    Logger.Error($"Temp project file is missing at '{tmpFilePath}'.");
                    return false;
                }

                // Begin compilation
                string source = File.ReadAllText(Path.Combine(ProjectDir, tmpFileName));

                var parser = new AdhocAbstractSyntaxTree(source);
                var program = parser.ParseScript();

                var compiler = new AdhocScriptCompiler();
                compiler.SetBaseIncludeFolder(BaseIncludeFolder);
                compiler.SetProjectDirectory(ProjectDir);
                compiler.SetSourcePath(compiler.SymbolMap, ProjectFolder + "/" + tmpFileName);
                compiler.SetVersion(Version);
                compiler.SetupStack();
                compiler.CompileScript(program);

                AdhocCodeGen codeGen = new AdhocCodeGen(compiler, compiler.SymbolMap);
                codeGen.Generate();
                codeGen.SaveTo(Path.Combine(ProjectDir, OutputName + ".adc"));

                return true;
            }
            catch (ParserException parseException)
            {
                Logger.Fatal($"Syntax error: {parseException.Description} at {parseException.SourceText}:{parseException.LineNumber}");
            }
            catch (AdhocCompilationException compileException)
            {
                Logger.Fatal($"Compilation error: {compileException.Message}");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Internal error in compilation");
            }
            finally
            {
                // Cleanup temp files
                if (!string.IsNullOrEmpty(tmpFilePath) && File.Exists(tmpFilePath))
                    File.Delete(tmpFilePath);

                if (Directory.Exists(pkgPath))
                    Directory.Delete(pkgPath, recursive: true);
            }

            return false;
        }

        private void BuildPackageFile()
        {
            string pkgName = $"{OutputName}.mpackage";
            Logger.Info($"Started building package file '{pkgName}'");

            string pkgPath = Path.Combine(ProjectDir, "pkg_tmp");

            if (Directory.Exists(pkgPath))
                Directory.Delete(pkgPath, recursive: true);

            string pkgContentPath = Path.Combine(pkgPath, SourceProjectFolder);
            Directory.CreateDirectory(pkgContentPath);

            int logPadLen = FilesToCompile.Length.ToString().Length;
            for (int i = 0; i < FilesToCompile.Length; i++)
            {
                AdhocProjectFile srcFile = FilesToCompile[i];
                Logger.Info($"[{(i + 1).ToString().PadLeft(logPadLen)}/{FilesToCompile.Length}] Compiling: {srcFile.Name}");

                string source = File.ReadAllText(srcFile.FullPath);

                var parser = new AdhocAbstractSyntaxTree(source);
                var program = parser.ParseScript();

                var compiler = new AdhocScriptCompiler();
                compiler.SetBaseIncludeFolder(BaseIncludeFolder);
                compiler.SetProjectDirectory(ProjectDir);
                compiler.SetSourcePath(compiler.SymbolMap, srcFile.SourcePath);
                compiler.SetVersion(Version);
                compiler.SetupStack();
                compiler.CompileScript(program);

                AdhocCodeGen codeGen = new AdhocCodeGen(compiler, compiler.SymbolMap);
                codeGen.Generate();
                codeGen.SaveTo(Path.Combine(pkgContentPath, Path.ChangeExtension(srcFile.Name, ".adc")));

                string componentName = Path.ChangeExtension(srcFile.FullPath, srcFile.IsMain ? ".mproject" : ".mwidget");
                if (File.Exists(componentName))
                {
                    Logger.Info($"Adding linked component '{Path.GetFileName(componentName)}'");

                    string outTmpComponentFile = Path.Combine(pkgContentPath, Path.GetFileName(componentName));
                    File.Copy(componentName, outTmpComponentFile);

                    if (SerializeComponents)
                    {
                        Logger.Info($"Serializing '{Path.GetFileName(componentName)}' to binary");
                        MTextIO io = new MTextIO(componentName);
                        var root = io.Read();

                        MBinaryWriter writer = new MBinaryWriter(outTmpComponentFile);
                        writer.Version = SerializeComponentsVersion;
                        writer.WriteNode(root);
                    }
                }
                else
                {
                    Logger.Warn($"File '{srcFile.Name}' does not have a linked UI definition file '{Path.GetFileName(componentName)}' (this warning can be ignored if this is not a root)");
                }
            }

            Logger.Info($"Packaging...");
            AdhocPackage.PackFromFolder(pkgPath, Path.Combine(ProjectDir, pkgName));
            Logger.Info($"Packaging successful -> {pkgName}");
        }

        public (bool Result, string ErrorMessage) VerifyProjectFile()
        {
            if (string.IsNullOrEmpty(ProjectName))
                return (false, "Project name is missing or empty.");

            if (string.IsNullOrEmpty(ProjectFolder))
                return (false, "Project folder is missing or empty.");

            if (string.IsNullOrEmpty(BaseIncludeFolder))
                return (false, "Base Include Folder is missing or empty.");

            if (string.IsNullOrEmpty(OutputName))
                return (false, "Output Name is missing or empty.");
            
            if (FilesToCompile.Length == 0)
                return (false, "No files to compile provided.");

            if (SerializeComponents && SerializeComponentsVersion > 1)
                return (false, "Serialize component version must be 0 or 1.");

            return (true, string.Empty);
        }

        private bool LinkFiles(string tmpFileName)
        {
            // Merge files together
            // This is how the game actually does it
            Logger.Info($"Merging ({FilesToCompile.Length}) files: [{string.Join(", ", FilesToCompile.Select(e => e.Name))}]");
            using StreamWriter mergedFile = new StreamWriter(Path.Combine(ProjectDir, tmpFileName));
            foreach (AdhocProjectFile srcFile in FilesToCompile)
            {
                string srcFilePath = Path.Combine(ProjectDir, srcFile.Name);

                if (!srcFile.IsMain)
                {
                    mergedFile.WriteLine($"module {ProjectName} // Compiler Generated");
                    mergedFile.WriteLine("{");
                }

                mergedFile.WriteLine($"#source " + "\"" + srcFile.SourcePath + "\"");
                if (!File.Exists(srcFilePath))
                {
                    Logger.Error($"Source file {srcFile.Name} for linking was not found.");
                    return false;
                }

                Logger.Info($"Merging: {srcFile.Name}");
                using var fileReader = new StreamReader(srcFilePath);
                string line;
                while ((line = fileReader.ReadLine()) != null)
                    mergedFile.WriteLine(line);

                mergedFile.WriteLine($"#resetline");

                if (!srcFile.IsMain)
                {
                    mergedFile.WriteLine("}");
                }
            }

            return true;
        }
    }
}
