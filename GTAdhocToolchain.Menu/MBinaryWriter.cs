using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;

using GTAdhocToolchain.Core;
using GTAdhocToolchain.Menu.Fields;

namespace GTAdhocToolchain.Menu
{
    public class MBinaryWriter : IDisposable
    {
        public string OutputFileName { get; set; }

        public bool Debug { get; set; }

        public BinaryStream Stream { get; set; }

        public int Version { get; set; }

        public MBinaryWriter(string fileName)
        {
            OutputFileName = fileName;
        }

        public void WriteNode(mNode node)
        {
            using var fs = new FileStream(OutputFileName, FileMode.Create);

            Stream = new BinaryStream(fs, ByteConverter.Big);
            Stream.WriteString("MPRJ", StringCoding.Raw);
            Stream.WriteVarInt(Version);

            node.Write(this);
        }

        public void Dispose()
        {
            Stream.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
