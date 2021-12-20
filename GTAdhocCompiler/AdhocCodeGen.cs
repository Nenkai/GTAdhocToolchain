using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;

using GTAdhocCompiler.Instructions;

namespace GTAdhocCompiler
{
    public class AdhocCodeGen
    {
        public const string Magic = "ADCH";

        public byte Version { get; set; } = 12;
        public bool WriteDebugInformation { get; set; } = true;

        public AdhocInstructionBlock MainBlock { get; set; }
        public AdhocSymbolMap SymbolMap { get; set; }

        private AdhocStream stream;

        public AdhocCodeGen(AdhocInstructionBlock mainBlock, AdhocSymbolMap symbols)
        {
            MainBlock = mainBlock;
            SymbolMap = symbols;
        }

        public void Generate()
        {
            var ms = new MemoryStream();
            stream = new AdhocStream(ms, Version);

            stream.WriteString(Magic, StringCoding.Raw);
            stream.WriteString(Version.ToString("D3"), StringCoding.Raw); // "012"
            stream.WriteByte(0);

            if (Version >= 9)
                SerializeSymbolTable();

            WriteCodeBlock(MainBlock);

            File.WriteAllBytes("test.adc", (stream.BaseStream as MemoryStream).ToArray());
        }

        private void WriteCodeBlock(AdhocInstructionBlock block)
        {
            if (Version >= 8)
            {
                stream.WriteBoolean(WriteDebugInformation);
                stream.WriteByte(Version);

                if (Version > 9 && WriteDebugInformation)
                {
                    if (block.SourceFilePath != null)
                        stream.WriteVarInt(block.SourceFilePath.Id);
                    else
                        stream.WriteVarInt(0);
                }

                if (Version >= 12)
                    stream.WriteByte(0);

                stream.WriteInt32(block.Parameters.Count);
                for (int i = 0; i < block.Parameters.Count; i++)
                {
                    stream.WriteSymbol(block.Parameters[i]);
                    stream.WriteInt32(1 + i); // TODO: Proper Index?
                }

                stream.WriteInt32(block.CallbackParameters.Count);
                for (int i = 0; i < block.CallbackParameters.Count; i++)
                {
                    stream.WriteSymbol(block.CallbackParameters[i]);
                    stream.WriteInt32(1 + i); // TODO: Proper Index?
                }

                stream.WriteInt32(0); // Unk
            }

            if (Version <= 10)
            {
                stream.WriteInt32(0);
                stream.WriteInt32(0);
            }
            else
            {
                stream.WriteInt32(0);
                stream.WriteInt32(0);
                stream.WriteInt32(0);
            }

            stream.WriteInt32(block.Instructions.Count);
            foreach (var instruction in block.Instructions)
            {
                WriteInstruction(instruction);
            }
        }

        private void WriteInstruction(InstructionBase instruction)
        {
            stream.WriteInt32(instruction.LineNumber);
            stream.WriteByte((byte)instruction.InstructionType);

            switch (instruction.InstructionType)
            {
                case AdhocInstructionType.IMPORT:
                    WriteImport(instruction as InsImport); break;
                case AdhocInstructionType.FUNCTION_DEFINE:
                    WriteFunction(instruction as InsFunctionDefine); break;
                case AdhocInstructionType.VARIABLE_EVAL:
                    WriteVariableEval(instruction as InsVariableEvaluation); break;
                case AdhocInstructionType.VARIABLE_PUSH:
                    WriteVariablePush(instruction as InsVariablePush); break;
                case AdhocInstructionType.CALL:
                    WriteCall(instruction as InsCall); break;
                case AdhocInstructionType.UNARY_OPERATOR:
                    WriteUnaryOperator(instruction as InsUnaryOperator); break;
                case AdhocInstructionType.BINARY_OPERATOR:
                    WriteBinaryOperator(instruction as InsBinaryOperator); break;
                case AdhocInstructionType.LOGICAL_AND:
                    WriteLogicalAnd(instruction as InsLogicalAnd); break;
                case AdhocInstructionType.LOGICAL_OR:
                    WriteLogicalOr(instruction as InsLogicalOr); break;
                case AdhocInstructionType.JUMP_IF_FALSE:
                    WriteJumpIfFalse(instruction as InsJumpIfFalse); break;
                case AdhocInstructionType.JUMP:
                    WriteJump(instruction as InsJump); break;
                case AdhocInstructionType.STRING_CONST:
                    WriteStringConst(instruction as InsStringConst); break;
                case AdhocInstructionType.INT_CONST:
                    WriteIntConst(instruction as InsIntConst); break;
                case AdhocInstructionType.SET_STATE:
                    WriteSetState(instruction as InsSetState); break;
                case AdhocInstructionType.LEAVE:
                    WriteLeave(instruction as InsLeaveScope); break;
                case AdhocInstructionType.NIL_CONST:
                case AdhocInstructionType.ASSIGN_POP:
                case AdhocInstructionType.POP:
                case AdhocInstructionType.ELEMENT_EVAL:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void WriteFunction(InsFunctionDefine function)
        {
            stream.WriteSymbol(function.Name);
            WriteCodeBlock(function.FunctionBlock);
        }

        private void WriteSetState(InsSetState setState)
        {
            stream.WriteByte(setState.State);
        }

        private void WriteIntConst(InsIntConst intConst)
        {
            stream.WriteInt32(intConst.Value);
        }

        private void WriteStringConst(InsStringConst stringConst)
        {
            stream.WriteSymbol(stringConst.String);
        }

        private void WriteJumpIfFalse(InsJumpIfFalse jif) // unrelated but gif is pronounced like the name of this parameter (:
        {
            stream.WriteInt32(jif.JumpIndex);
        }

        private void WriteJump(InsJump jump)
        {
            stream.WriteInt32(jump.JumpInstructionIndex);
        }

        private void WriteBinaryOperator(InsBinaryOperator binaryOperator)
        {
            stream.WriteSymbol(binaryOperator.Operator);
        }

        private void WriteUnaryOperator(InsUnaryOperator unaryOperator)
        {
            stream.WriteSymbol(unaryOperator.Operator);
        }

        private void WriteLogicalAnd(InsLogicalAnd logicalAnd)
        {
            stream.WriteInt32(logicalAnd.InstructionJumpIndex);
        }

        private void WriteLogicalOr(InsLogicalOr logicalOr)
        {
            stream.WriteInt32(logicalOr.InstructionJumpIndex);
        }

        private void WriteCall(InsCall call)
        {
            stream.WriteInt32(call.ArgumentCount);
        }

        private void WriteLeave(InsLeaveScope leave)
        {
            stream.WriteInt32(0); // Unused
            stream.WriteInt32(leave.StackRewindIndex);
        }

        private void WriteVariableEval(InsVariableEvaluation variableEval)
        {
            stream.WriteSymbols(variableEval.VariableSymbols);
            stream.WriteInt32(variableEval.StackIndex);
        }

        private void WriteVariablePush(InsVariablePush variablePush)
        {
            stream.WriteSymbols(variablePush.VariableSymbols);
            stream.WriteInt32(variablePush.StackIndex);
        }

        private void WriteImport(InsImport import)
        {
            stream.WriteSymbols(import.ImportNamespaceParts);
            stream.WriteSymbol(import.Target);
            stream.WriteSymbol(SymbolMap.Symbols["nil"]);
        }

        private void SerializeSymbolTable()
        {
            stream.WriteVarInt(SymbolMap.Symbols.Count);
            foreach (AdhocSymbol symb in SymbolMap.Symbols.Values)
                stream.WriteVarString(symb.Name);
        }
    }
}
