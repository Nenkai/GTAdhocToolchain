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

        public string FullProjectPath { get; set; }

        public string ProjectFilePath { get; set; }

        public string BaseIncludeFolder { get; set; }

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

            return prj;
        }

        public void PrintInfo()
        {
            Logger.Info($"Project: {ProjectName}");
            Logger.Info($"Version: {Version}");
            Logger.Info($"Output File: {OutputName}");
        }

        public void Build()
        {
            // Convert project folder into full path
            FullProjectPath = Path.GetFullPath(Path.Combine(ProjectFilePath, ProjectFolder));
            SourceProjectFolder = ProjectFolder.TrimStart('.', '/'); // Trim ../

            string tmpFileName = $"_tmp_{OutputName}.ad";
            LinkFiles(tmpFileName);

            string tmpFilePath = Path.Combine(FullProjectPath, tmpFileName);
            if (!File.Exists(tmpFilePath))
            {
                Logger.Error($"Temp project file is missing at {tmpFilePath}.");
                return;
            }

            // Begin compilation
            string source = File.ReadAllText(Path.Combine(FullProjectPath, tmpFileName));

            var parser = new AdhocAbstractSyntaxTree(source);
            var program = parser.ParseScript();

            var compiler = new AdhocScriptCompiler();
            compiler.SetProjectDirectory(Path.GetFullPath(Path.Combine(ProjectFilePath, BaseIncludeFolder)));
            compiler.SetSourcePath(compiler.SymbolMap, ProjectFolder + "/" + tmpFileName);
            compiler.CompileScript(program);

            AdhocCodeGen codeGen = new AdhocCodeGen(compiler, compiler.SymbolMap);
            codeGen.Generate();
            codeGen.SaveTo(Path.Combine(FullProjectPath, OutputName + ".adc"));

            Logger.Info("Project build successful.");
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

            if (Version != 12)
                return (false, "Only Project version 12 is supported.");
            
            if (FilesToCompile.Length == 0)
                return (false, "No files to compile provided.");

            return (true, string.Empty);
        }

        private void LinkFiles(string tmpFileName)
        {
            // Merge files together
            // This is how the game actually does it
            Logger.Info($"Merging ({FilesToCompile.Length}) files: [{string.Join(", ", FilesToCompile.Select(e => e.Name))}]");
            using StreamWriter mergedFile = new StreamWriter(Path.Combine(FullProjectPath, tmpFileName));
            foreach (AdhocProjectFile srcFile in FilesToCompile)
            {
                string srcFilePath = Path.Combine(FullProjectPath, srcFile.Name);

                if (!srcFile.IsMain)
                {
                    mergedFile.WriteLine($"module {ProjectName} // Compiler Generated");
                    mergedFile.WriteLine("{");
                }

                string srcPath = SourceProjectFolder + "/" + srcFile.Name;
                mergedFile.WriteLine($"#source " + "\"" + srcPath + "\"");
                if (!File.Exists(srcFilePath))
                    throw new FileNotFoundException($"Source file {srcFile.Name} for linking was not found.");

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
        }
    }
}
