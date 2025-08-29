using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions;

/// <summary>
/// Defines a new class within the current scope.
/// </summary>
public class InsClassDefine : InstructionBase
{
    public override AdhocInstructionType InstructionType => AdhocInstructionType.CLASS_DEFINE;

    public override string InstructionName => "CLASS_DEFINE";

    public AdhocSymbol Name { get; set; }

    public List<AdhocSymbol> ExtendsFrom { get; set; } = [];

    public InsClassDefine()
    {
        
    }

    public override void Serialize(AdhocStream stream)
    {
        stream.WriteSymbol(Name);
        stream.WriteSymbols(ExtendsFrom);
    }

    public override void Deserialize(AdhocStream stream)
    {
        Name = stream.ReadSymbol();
        ExtendsFrom = stream.ReadSymbols();
    }

    public override string ToString()
    {
        return Disassemble(asCompareMode: false);
    }

    public override string Disassemble(bool asCompareMode = false)
        => $"{InstructionType}: {Name.Name} extends {string.Join(",", ExtendsFrom.Select(e => e.Name))}";
}
