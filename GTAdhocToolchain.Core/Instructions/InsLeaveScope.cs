using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Signals scope leaving to rewind the stack to prior context. Normally used when exiting scopes.
    /// </summary>
    public class InsLeaveScope : InstructionBase
    {
        public override AdhocInstructionType InstructionType => AdhocInstructionType.LEAVE;

        public override string InstructionName => "LEAVE";

        /// <summary>
        /// Rewinds the depth to a certain point.
        /// NOT USED AFTER GT5
        /// </summary>
        public int ModuleOrClassDepthRewindIndex { get; set; }

        /// <summary>
        /// Rewinds the variable storage to a certain point (sets all to nil).
        /// When set to 1 and depth value is set, this is ignored.
        /// </summary>
        public int VariableStorageRewindIndex { get; set; }

        public override void Serialize(AdhocStream stream)
        {
            stream.WriteInt32(ModuleOrClassDepthRewindIndex);
            stream.WriteInt32(VariableStorageRewindIndex);
        }

        public override void Deserialize(AdhocStream stream)
        {
            ModuleOrClassDepthRewindIndex = stream.ReadInt32();
            VariableStorageRewindIndex = stream.ReadInt32();
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
           => $"{InstructionType}: Depth:{ModuleOrClassDepthRewindIndex}, RewindLocalsStorageTo:{VariableStorageRewindIndex}";
    }
}
