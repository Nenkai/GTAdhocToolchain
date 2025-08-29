using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Instructions;

/// <summary>
/// Imports (copies) a symbol (or all with wildcard) from a path onto the current scope.
/// </summary>
public class InsImport : InstructionBase
{
    public override AdhocInstructionType InstructionType => AdhocInstructionType.IMPORT;

    public override string InstructionName => "IMPORT";

    public List<AdhocSymbol> ModulePath { get; set; } = [];
    public AdhocSymbol ModuleValue { get; set; }
    public AdhocSymbol ImportAs { get; set; }

    public InsImport()
    {
        
    }

    public override void Serialize(AdhocStream stream)
    {
        stream.WriteSymbols(ModulePath);
        stream.WriteSymbol(ModuleValue);

        if (stream.Version.SupportsImportAlias())
            stream.WriteSymbol(ImportAs);
    }

    public override void Deserialize(AdhocStream stream)
    {
        ModulePath = stream.ReadSymbols();
        ModuleValue = stream.ReadSymbol();

        if (stream.Version.SupportsImportAlias())
            ImportAs = stream.ReadSymbol();
    }

    public override string ToString()
    {
        return Disassemble(asCompareMode: false);
    }

    public override string Disassemble(bool asCompareMode = false)
    {
        string str = $"{InstructionType}: Path:{ModulePath[^1].Name}, Property:{ModuleValue.Name}";
        if (ImportAs is not null)
            str += $", ImportAs:{ImportAs.Name}";
        return str;
    }
}
