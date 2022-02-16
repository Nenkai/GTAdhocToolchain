using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Menu.Resources
{
    public abstract class GpbBase
    {
        public List<GpbPair> Files { get; set; } = new List<GpbPair>();

        public abstract void Read(string fileName);

        public static GpbBase ReadFile(string file)
        {
            GpbBase gpb;
            using (var fs = new FileStream(file, FileMode.Open))
            using (var bs = new BinaryStream(fs)) 
            {
                string magic = bs.ReadString(4);

                gpb = magic switch
                {
                    "3bpg" or "gpb3" => new GpbData3(),
                    "4bpg" or "gpb4" => new GpbData4(),
                    _ => null,
                };
            }

            if (gpb is null)
                return null;

            gpb.Read(file);
            return gpb;
        }

        public void Unpack(string fileName, string outputFolder)
        {
            if (outputFolder is null)
                outputFolder = fileName;

            foreach (var file in Files)
            {
                Console.WriteLine($"[:] GPB: Unpack -> {file.FileName}");

                string path = file.FileName.Substring(1); // Ignore first '/'
                Directory.CreateDirectory(Path.Combine(outputFolder, Path.GetDirectoryName(path)));
                File.WriteAllBytes(Path.Combine(outputFolder, path), file.FileData);
            }
        }

        public void AddFilesFromFolder(string folderName, bool gt5 = false)
        {
            List<string> files = Directory.GetFiles(folderName, "*", SearchOption.AllDirectories).ToList();
            files.Sort(Utils.AlphaNumericStringSorter);

            foreach (var file in files)
            {
                var pair = new GpbPair();

                string fileName = file.Replace('\\', '/'); // Replace to any wanted path separator
                fileName = fileName.Substring(fileName.IndexOf('/')); // Remove the parent
                if (!gt5)
                {
                    if (!fileName.StartsWith('/'))
                        fileName = '/' + fileName; // Ensure it starts with '/' if not GT5
                }
                else
                {
                    if (fileName.StartsWith('/'))
                        fileName = fileName.Substring(1); // Ensure it DOESN'T start with '/' if GT5
                }

                pair.FileName = fileName;
                pair.FileData = File.ReadAllBytes(file);
                Files.Add(pair);
            }
        }

        public static int PairSorter(GpbPair a, GpbPair b)
        {
            return Utils.AlphaNumericStringSorter(a.FileName, b.FileName);
        }
    }
}
