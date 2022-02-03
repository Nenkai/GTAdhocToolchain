using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.IO.Compression;
using Syroot.BinaryData;

namespace GTAdhocToolchain.Packaging
{
    public class AdhocPackageFile
    {
        public uint FileNameOffset { get; set; }
        public uint CompressedDataOffset { get; set; }
        public uint CompressedDataSize { get; set; }
        public string RawFileName { get; set; }
        public string ProjectFileName { get; set; }
    }
}
