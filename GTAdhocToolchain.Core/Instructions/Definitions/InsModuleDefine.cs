using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions;

/// <summary>
/// Defines a new module for the current scope.
/// </summary>
public class InsModuleDefine : InstructionBase
{
    public override AdhocInstructionType InstructionType => AdhocInstructionType.MODULE_DEFINE;

    public override string InstructionName => "MODULE_DEFINE";

    public List<AdhocSymbol> Names { get; set; } = [];

    public InsModuleDefine()
    {
        
    }

    public override void Serialize(AdhocStream stream)
    {
        stream.WriteSymbols(Names);
    }

    public override void Deserialize(AdhocStream stream)
    {
        Names = stream.ReadSymbols();
    }

    public override string ToString()
    {
        return Disassemble(asCompareMode: false);
    }

    public override string Disassemble(bool asCompareMode = false)
       => $"{InstructionType}: {string.Join(",", Names.Select(e => e.Name))}";
}
