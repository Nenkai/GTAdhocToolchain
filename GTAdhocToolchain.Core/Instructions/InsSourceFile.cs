using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Current source file name update.
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

        public override void Deserialize(AdhocStream stream)
        {
            FileName = stream.ReadSymbol();
        }

        public override string ToString()
            => $"{InstructionType}: {FileName.Name}";
    }
}
