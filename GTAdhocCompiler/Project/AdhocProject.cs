using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Esprima;

namespace GTAdhocCompiler.Project
{
    public class AdhocProject
    {
        public string ProjectName { get; set; }

        public string OutputName { get; set; }

        public AdhocProjectFile[] FilesToCompile { get; set; }

        public int Version { get; set; } = 12;

        public string ProjectFolder { get; set; }

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

            return prj;
        }

        public void Compile()
        {
            // Convert project folder into full path
            FullProjectPath = Path.GetFullPath(Path.Combine(ProjectFilePath, ProjectFolder));

            string tmpFileName = $"_tmp_{OutputName}.ad";
            LinkFiles(tmpFileName);

            // Begin compilation
            string source = File.ReadAllText(Path.Combine(FullProjectPath, tmpFileName));
            var parser = new AdhocAbstractSyntaxTree(source);
            var program = parser.ParseScript();

            var compiler = new AdhocScriptCompiler();
            compiler.SetProjectDirectory(Path.GetFullPath(Path.Combine(ProjectFilePath, BaseIncludeFolder)));
            compiler.SetSourcePath(compiler.SymbolMap, Path.Combine(ProjectFolder, tmpFileName));
            compiler.CompileScript(program);

            AdhocCodeGen codeGen = new AdhocCodeGen(compiler, compiler.SymbolMap);
            codeGen.Generate();
            codeGen.SaveTo(Path.Combine(FullProjectPath, OutputName + ".adc"));
        }

        private void LinkFiles(string tmpFileName)
        {
            // Merge files together
            // This is how the game actually does it
            using StreamWriter mergedFile = new StreamWriter(Path.Combine(FullProjectPath, tmpFileName));
            foreach (AdhocProjectFile srcFile in FilesToCompile)
            {
                if (!srcFile.IsMain)
                {
                    mergedFile.WriteLine($"module {ProjectName} // Compiler Generated");
                    mergedFile.WriteLine("{");
                }

                using var fileReader = new StreamReader(Path.Combine(FullProjectPath, srcFile.Name));
                string line;
                while ((line = fileReader.ReadLine()) != null)
                    mergedFile.WriteLine(line);

                if (!srcFile.IsMain)
                {
                    mergedFile.WriteLine("}");
                }
            }
        }
    }
}
