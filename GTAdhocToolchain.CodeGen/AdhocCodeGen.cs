using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Syroot.BinaryData;

using GTAdhocToolchain.Core;
using GTAdhocToolchain.Core.Instructions;

using PDTools.Crypto;

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

            if (Frame.Version >= 13)
            {
                // Start MD5 here
                stream.Position += 0x10; // Skip MD5 for now

                stream.WriteVarInt(SymbolMap.Symbols.Count);

                stream.StartCurrentScriptMD5();
                stream.StartCompiledFileMD5();
                stream.WriteVarInt(1); // Script count
            }

            if (Frame.Version >= 9 && Frame.Version <= 12)
                SerializeSymbolTable();

            Frame.Write(stream);

            if (Frame.Version >= 13)
            {
                var scriptHash = stream.FinishCurrentScriptMD5();
                stream.Write(scriptHash);

                var fullFileHash = stream.FinishCompiledFileMD5();
                stream.Position = 0x08;
                stream.Write(fullFileHash);

                if (Frame.Version >= 15)
                {
                    // Encrypt
                    byte[] encBuffer = new byte[stream.Length - stream.Position];
                    stream.ReadExactly(encBuffer);

                    ChaCha20 state = ScramblerState.CreateFromHash(fullFileHash);
                    state.DecryptBytes(encBuffer, encBuffer.Length);

                    stream.Position = 0x18;
                    stream.Write(encBuffer);
                }

                stream.Position = stream.Length;
            }

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
