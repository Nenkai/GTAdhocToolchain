using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;

using GTAdhocToolchain.Core.Instructions;
using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.CodeGen
{
    public class AdhocCodeGen
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public AdhocCodeFrame MainBlock { get; set; }
        public AdhocSymbolMap SymbolMap { get; set; }

        private AdhocStream stream;

        public AdhocCodeGen(AdhocCodeFrame mainBlock, AdhocSymbolMap symbols)
        {
            MainBlock = mainBlock;
            SymbolMap = symbols;
        }

        public void Generate()
        {
            Logger.Info("Generating code...");
            var ms = new MemoryStream();
            stream = new AdhocStream(ms, MainBlock.Version);

            // ADhoc Compiled Header?
            stream.WriteString($"ADCH{MainBlock.Version:D3}", StringCoding.ZeroTerminated); // ADCH012

            if (MainBlock.Version >= 9)
                SerializeSymbolTable();

            MainBlock.Write(stream);

            Logger.Debug($"Code generated (Size: {stream.Length} bytes, {MainBlock.Instructions.Count} main instructions)");
        }

        public void SaveTo(string path)
        {
            File.WriteAllBytes(path, (stream.BaseStream as MemoryStream).ToArray());
            Logger.Info($"Compiled script -> '{path}'");
        }

        private void SerializeSymbolTable()
        {
            Logger.Debug($"Serializing symbol table ({SymbolMap.Symbols.Count} symbols)");

            stream.WriteVarInt(SymbolMap.Symbols.Count);
            foreach (AdhocSymbol symb in SymbolMap.Symbols.Values)
                stream.WriteVarString(symb.Name, !symb.HasHexEscape);
        }
    }
}
