using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Sets the current file name for the thread, used for debugging for exceptions.
    /// </summary>
    public class InsSourceFile : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.SOURCE_FILE;

        public override string InstructionName => "SOURCE_FILE";

        public AdhocSymbol FileName { get; set; }
        public InsSourceFile(AdhocSymbol fileName)
        {
            FileName = fileName;
        }

        public InsSourceFile()
        {

        }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteSymbol(FileName);
        }

        public override void Deserialize(AdhocStream stream)
        {
            FileName = stream.ReadSymbol();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
            => $"{InstructionType}: {FileName.Name}";
    }
}
