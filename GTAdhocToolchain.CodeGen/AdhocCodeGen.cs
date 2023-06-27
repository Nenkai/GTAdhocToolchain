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

        public AdhocCodeFrame Frame { get; set; }
        public AdhocSymbolMap SymbolMap { get; set; }

        private AdhocStream stream;

        public AdhocCodeGen(AdhocCodeFrame frame, AdhocSymbolMap symbols)
        {
            Frame = frame;
            SymbolMap = symbols;
        }

        public void Generate()
        {
            Logger.Info("Generating code...");
            var ms = new MemoryStream();
            stream = new AdhocStream(ms, Frame.Version);

            // ADhoc Compiled Header?
            stream.WriteString($"ADCH{Frame.Version:D3}", StringCoding.ZeroTerminated); // ADCH012

            if (Frame.Version >= 9)
                SerializeSymbolTable();

            Frame.Write(stream);

            Logger.Info($"Code generated (Size: {stream.Length} bytes, {Frame.Instructions.Count} main instructions)");
            Logger.Debug($"[Stack] Stack Size: {Frame.Stack.GetStackSize()} - Locals: {Frame.Stack.GetLocalVariableStorageSize()}");
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
