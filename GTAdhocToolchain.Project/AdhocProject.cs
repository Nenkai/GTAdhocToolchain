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
using GTAdhocToolchain.Menu.Fields;
using GTAdhocToolchain.Preprocessor;

namespace GTAdhocToolchain.Project
{
    public class AdhocProject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public string ProjectName { get; set; }

        public string OutputName { get; set; }

        /// <summary>
        /// Scripts linked to the project to compile.
        /// </summary>
        public AdhocProjectFile[] FilesToCompile { get; set; } = new AdhocProjectFile[0];

        /// <summary>
        /// Extra resources/widgets to link with the project.
        /// </summary>
        public AdhocProjectExtraResource[] ExtraWidgetResources { get; set; } = new AdhocProjectExtraResource[0];

        public int Version { get; set; } = 12; // Default to version 12, used by GTPSP, GT5, GT6, GT Sport

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
        /// Whether to merge mprojects into an mproject (when not building a package).
        /// </summary>
        public bool MergeWidget { get; set; }


        /// <summary>
        /// Version to serialize the components to
        /// </summary>
        public int SerializeComponentsVersion { get; set; }

        /// <summary>
        /// Defines to pass to the preprocessor
        /// </summary>
        public Dictionary<string, string> Defines { get; set; } = new();

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

            foreach (var extraResource in prj.ExtraWidgetResources)
            {
                extraResource.FullPath = Path.GetFullPath(Path.Combine(prj.ProjectDir, extraResource.Name));
            }

            return prj;
        }

        public void PrintInfo()
        {
            Logger.Info($"Project: {ProjectName}");
            Logger.Info($"Version: {Version}");
            Logger.Info($"Output File: {OutputName}");
        }

        /// <summary>
        /// Builds the project.
        /// </summary>
        /// <returns></returns>
        public bool Build(bool debug = false)
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
                Logger.Info($"Building project '{mergedScriptName}' from {FilesToCompile.Length} files: [{string.Join(", ", FilesToCompile.Select(e => e.Name))}]");
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
                var time = new FileInfo(Path.Combine(ProjectDir, tmpFileName)).LastWriteTime;

                var preprocessor = new AdhocScriptPreprocessor();
                preprocessor.SetBaseDirectory(BaseIncludeFolder);
                preprocessor.SetCurrentFileName(Path.Combine(SourceProjectFolder, $"_tmp_{OutputName}.ad").Replace('\\', '/'));
                preprocessor.SetCurrentFileTimestamp(time);
                preprocessor.AddDefines(Defines);

                var preprocessed = preprocessor.Preprocess(source);

                var parser = new AdhocAbstractSyntaxTree(preprocessed);
                var program = parser.ParseScript();

                var compiler = new AdhocScriptCompiler();
                compiler.SetBaseIncludeFolder(BaseIncludeFolder);
                compiler.SetProjectDirectory(ProjectDir);
                compiler.SetSourcePath(compiler.SymbolMap, ProjectFolder + "/" + tmpFileName);
                compiler.SetVersion(Version);
                compiler.CreateStack();

                if (debug)
                    compiler.BuildTryCatchDebugStatements();

                compiler.CompileScript(program);

                AdhocCodeGen codeGen = new AdhocCodeGen(compiler, compiler.SymbolMap);
                codeGen.Generate();
                codeGen.SaveTo(Path.Combine(ProjectDir, OutputName + ".adc"));

                if (MergeWidget)
                {
                    bool res = MergeRootWidgets(Path.ChangeExtension(Path.Combine(ProjectDir, OutputName), ".mproject"), SerializeComponents);
                    return res;
                }

                return true;
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

        /// <summary>
        /// Builds a compressed .mpackage file containing all scripts and mwidgets.
        /// </summary>
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
            try
            {
                for (int i = 0; i < FilesToCompile.Length; i++)
                {
                    AdhocProjectFile srcFile = FilesToCompile[i];
                    Logger.Info($"[{(i + 1).ToString().PadLeft(logPadLen)}/{FilesToCompile.Length}] Compiling: {srcFile.Name}");


                    string source = File.ReadAllText(srcFile.FullPath);
                    var time = new FileInfo(srcFile.FullPath).LastWriteTime;

                    var preprocessor = new AdhocScriptPreprocessor();
                    preprocessor.SetBaseDirectory(BaseIncludeFolder);
                    preprocessor.SetCurrentFileName(srcFile.SourcePath);
                    preprocessor.SetCurrentFileTimestamp(time);
                    preprocessor.AddDefines(Defines);

                    var preprocessed = preprocessor.Preprocess(source);

                    var parser = new AdhocAbstractSyntaxTree(preprocessed);
                    var program = parser.ParseScript();

                    var compiler = new AdhocScriptCompiler();
                    compiler.SetBaseIncludeFolder(BaseIncludeFolder);
                    compiler.SetProjectDirectory(ProjectDir);
                    compiler.SetSourcePath(compiler.SymbolMap, srcFile.SourcePath);
                    compiler.SetVersion(Version);
                    compiler.CreateStack();
                    compiler.CompileScript(program);

                    AdhocCodeGen codeGen = new AdhocCodeGen(compiler, compiler.SymbolMap);
                    codeGen.Generate();
                    codeGen.SaveTo(Path.Combine(pkgContentPath, Path.ChangeExtension(srcFile.Name, ".adc")));

                    if (!srcFile.IsMain)
                    {
                        string componentName = Path.ChangeExtension(srcFile.FullPath, ".mwidget");
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
                }

                // Create package mproject with external references
                CreatePackageMProject(pkgContentPath);

                Logger.Info($"Packaging...");
                AdhocPackage.PackFromFolder(pkgPath, Path.Combine(ProjectDir, pkgName));
                Logger.Info($"Packaging successful -> {pkgName}");
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

        /// <summary>
        /// Creates a package's mproject which contains the external references to the widgets.
        /// </summary>
        /// <param name="pkgContentPath"></param>
        private void CreatePackageMProject(string pkgContentPath)
        {
            var mprojectRoot = new mNode();
            mprojectRoot.TypeName = "Project";

            var proj_component = FilesToCompile.FirstOrDefault(e => e.ProjectComponent);
            if (proj_component is not null)
            {
                mprojectRoot.Child.Add(new mExternalRef()
                {
                    Name = "project_component",
                    ExternalRefName = Path.GetFileNameWithoutExtension(proj_component.Name),
                });
            }
            mprojectRoot.Child.Add(new mString() { Name = "name", String = ProjectName });
            mprojectRoot.Child.Add(new mBool() { Name = "has_script", Value = true });

            var children = new mArray() { Name = "children" };
            var root_window = new mArray() { Name = "root_window" };

            foreach (var file in FilesToCompile)
            {
                if (file.IsMain || file.ProjectComponent)
                    continue;

                children.Elements.Add(new mExternalRef() { ExternalRefName = Path.GetFileNameWithoutExtension(file.Name) });
                root_window.Elements.Add(new mString() { String = Path.GetFileNameWithoutExtension(file.Name) });
            }

            mprojectRoot.Child.Add(children);
            mprojectRoot.Child.Add(root_window);

            string outputMprojectName = Path.ChangeExtension(Path.Combine(pkgContentPath, OutputName), ".mproject");
            if (SerializeComponents)
            {
                MBinaryWriter writer = new MBinaryWriter(outputMprojectName);
                writer.Version = SerializeComponentsVersion;
                writer.WriteNode(mprojectRoot);
                writer.Dispose();
            }
            else
            {
                MTextWriter writer = new MTextWriter(outputMprojectName);
                writer.WriteNode(mprojectRoot);
                writer.Dispose();
            }
        }

        /// <summary>
        /// Merges all mwidgets into an mproject for non-package building.
        /// </summary>
        private bool MergeRootWidgets(string outputFile, bool convertToBin)
        {
            Logger.Info($"Merging all mwidgets into single mproject...");

            var mprojectRoot = new mNode();
            mprojectRoot.TypeName = "Project";

            mNode project_component = null;

            var projectRoots = new mArray();
            projectRoots.Name = "children";
            projectRoots.TypeNew = FieldType.ArrayMaybe;

            MTextWriter writer = new MTextWriter(outputFile);

            foreach (var file in FilesToCompile)
            {
                if (file.IsMain)
                    continue;

                string componentName = Path.ChangeExtension(file.FullPath, ".mwidget");
                if (File.Exists(componentName))
                {
                    MTextIO io = new MTextIO(componentName);
                    var root = io.Read();

                    if (file.ProjectComponent)
                    {
                        project_component = root;
                        project_component.Name = "project_component";
                    }
                    else
                    {
                        projectRoots.Elements.Add(root);
                    }
                }
                else
                {
                    Logger.Error($"Component name '{componentName}' was missing.");
                    return false;
                }
            }

            foreach (var file in ExtraWidgetResources)
            {
                string componentName = Path.ChangeExtension(file.FullPath, ".mwidget");
                if (File.Exists(componentName))
                {
                    MTextIO io = new MTextIO(componentName);
                    var root = io.Read();

                    projectRoots.Elements.Add(root);
                }
                else
                {
                    Logger.Error($"Extra resource name '{componentName}' was missing.");
                    return false;
                }
            }

            if (project_component != null)
                mprojectRoot.Child.Add(project_component);

            // Prepare for roots
            mprojectRoot.Child.Add(new mString() { Name = "name", String = ProjectName });
            mprojectRoot.Child.Add(new mBool() { Name = "has_script", Value = true });

            mprojectRoot.Child.Add(projectRoots);
            writer.WriteNode(mprojectRoot);
            writer.Dispose();

            if (convertToBin)
            {
                MTextIO io = new MTextIO(outputFile);
                var root = io.Read();

                MBinaryWriter binaryWriter = new MBinaryWriter(outputFile);
                binaryWriter.Version = SerializeComponentsVersion;
                binaryWriter.WriteNode(root);

                Logger.Info($"Merged widgets (as binary) -> {outputFile}");
            }
            else
            {
                Logger.Info($"Merged widgets (as text) -> {outputFile}");
            }

            return true;
        }

        /// <summary>
        /// Links all project script files together.
        /// </summary>
        /// <param name="tmpFileName"></param>
        /// <returns></returns>
        private bool LinkFiles(string tmpFileName)
        {
            // Merge script/roots files together
            // This is how the game actually does it
            
            using StreamWriter mergedFile = new StreamWriter(Path.Combine(ProjectDir, tmpFileName));
            mergedFile.WriteLine($"#define PROJECT {ProjectName}");

            foreach (AdhocProjectFile srcFile in FilesToCompile)
            {
                mergedFile.WriteLine($"#define ROOT {Path.ChangeExtension(srcFile.Name, null)}");
                mergedFile.WriteLine($"#define IMPL ROOT.getImpl()"); // GT Sport

                if (!srcFile.IsMain)
                {
                    mergedFile.WriteLine($"module {ProjectName}");
                    mergedFile.WriteLine("{");
                    mergedFile.WriteLine($"#include \"{srcFile.SourcePath}\"");
                    mergedFile.WriteLine("}");
                }
                else
                {
                    mergedFile.WriteLine($"#include \"{srcFile.SourcePath}\"");
                }

                mergedFile.WriteLine($"#undef ROOT");
                mergedFile.WriteLine($"#undef IMPL");

                mergedFile.WriteLine();
            }

            return true;
        }

        /// <summary>
        /// Verifies the project/solution file.
        /// </summary>
        /// <returns></returns>
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

            if (FilesToCompile.Count(e => e.ProjectComponent) > 1)
                return (false, "Only one root can be marked as a project component.");

            return (true, string.Empty);
        }
    }
}
