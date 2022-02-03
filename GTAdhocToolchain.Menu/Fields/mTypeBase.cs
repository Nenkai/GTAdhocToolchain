using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace GTAdhocToolchain.Menu.Fields
{
    public abstract class mTypeBase
    {
        public string Name { get; set; }
        public FieldType TypeOld { get; set; }
        public FieldType TypeNew { get; set; }

        public abstract void Read(MBinaryIO io);

        public abstract void Read(MTextIO io);

        public abstract void Write(MBinaryWriter writer);

        public abstract void WriteText(MTextWriter writer);

    }
}
