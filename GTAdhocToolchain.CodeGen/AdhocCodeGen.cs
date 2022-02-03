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

        public const string Magic = "ADCH";

        public byte Version { get; set; } = 12;
        public bool WriteDebugInformation { get; set; } = true;

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
            stream = new AdhocStream(ms, Version);

            stream.WriteString(Magic, StringCoding.Raw);
            stream.WriteString(Version.ToString("D3"), StringCoding.Raw); // "012"
            stream.WriteByte(0);

            if (Version >= 9)
                SerializeSymbolTable();

            WriteCodeBlock(MainBlock);

            Logger.Info($"Code generated (Size: {stream.Length} bytes, {MainBlock.Instructions.Count} main instructions)");
        }

        public void SaveTo(string path)
        {
            Logger.Info($"Saving compiled script to {path}");
            File.WriteAllBytes(path, (stream.BaseStream as MemoryStream).ToArray());
        }

        private void WriteCodeBlock(AdhocCodeFrame block)
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

                stream.WriteInt32(block.FunctionParameters.Count);
                for (int i = 0; i < block.FunctionParameters.Count; i++)
                {
                    stream.WriteSymbol(block.FunctionParameters[i]);
                    stream.WriteInt32(1 + i); // TODO: Proper Index?
                }

                stream.WriteInt32(block.CapturedCallbackVariables.Count);
                for (int i = 0; i < block.CapturedCallbackVariables.Count; i++)
                {
                    AdhocSymbol capturedVariable = block.CapturedCallbackVariables[i];
                    stream.WriteSymbol(capturedVariable);
                    stream.WriteInt32(-(i + 1));
                }

                stream.WriteInt32(0); // Some stack variable index
            }

            if (Version <= 10)
            {
                stream.WriteInt32(block.Stack.StackSize);
                stream.WriteInt32(block.Stack.LocalVariableStorageSize);
            }
            else
            {
                // Actual stack size
                stream.WriteInt32(block.Stack.StackSize);

                /* These two are combined to make the size of the storage for variables */
                stream.WriteInt32(block.Stack.LocalVariableStorageSize);
                stream.WriteInt32(block.Stack.StaticVariableStorageSize);
            }

            stream.WriteInt32(block.Instructions.Count);
            foreach (var instruction in block.Instructions)
            {
                WriteInstruction(instruction);
            }
        }

        private void WriteInstruction(InstructionBase instruction)
        {
            stream.WriteInt32((int)instruction.LineNumber);
            stream.WriteByte((byte)instruction.InstructionType);

            switch (instruction.InstructionType)
            {
                case AdhocInstructionType.CLASS_DEFINE:
                    WriteClassDefine(instruction as InsClassDefine); break;
                case AdhocInstructionType.MODULE_DEFINE:
                    WriteModuleDefine(instruction as InsModuleDefine); break;
                case AdhocInstructionType.IMPORT:
                    WriteImport(instruction as InsImport); break;
                case AdhocInstructionType.FUNCTION_DEFINE:
                    WriteFunctionDefine(instruction as InsFunctionDefine); break;
                case AdhocInstructionType.FUNCTION_CONST:
                    WriteFunctionConst(instruction as InsFunctionConst); break;
                case AdhocInstructionType.METHOD_CONST:
                    WriteMethodConst(instruction as InsMethodConst); break;
                case AdhocInstructionType.METHOD_DEFINE:
                    WriteMethod(instruction as InsMethodDefine); break;
                case AdhocInstructionType.VARIABLE_EVAL:
                    WriteVariableEval(instruction as InsVariableEvaluation); break;
                case AdhocInstructionType.ATTRIBUTE_EVAL:
                    WriteAttributeEval(instruction as InsAttributeEvaluation); break;
                case AdhocInstructionType.VARIABLE_PUSH:
                    WriteVariablePush(instruction as InsVariablePush); break;
                case AdhocInstructionType.ATTRIBUTE_PUSH:
                    WriteAttributePush(instruction as InsAttributePush); break;
                case AdhocInstructionType.ATTRIBUTE_DEFINE:
                    WriteAttributeDefine(instruction as InsAttributeDefine); break;
                case AdhocInstructionType.CALL:
                    WriteCall(instruction as InsCall); break;
                case AdhocInstructionType.UNARY_OPERATOR:
                    WriteUnaryOperator(instruction as InsUnaryOperator); break;
                case AdhocInstructionType.BINARY_OPERATOR:
                    WriteBinaryOperator(instruction as InsBinaryOperator); break;
                case AdhocInstructionType.BINARY_ASSIGN_OPERATOR:
                    WriteBinaryOperator(instruction as InsBinaryAssignOperator); break;
                case AdhocInstructionType.UNARY_ASSIGN_OPERATOR:
                    WriteBinaryOperator(instruction as InsUnaryAssignOperator); break;
                case AdhocInstructionType.LOGICAL_AND:
                    WriteLogicalAnd(instruction as InsLogicalAnd); break;
                case AdhocInstructionType.LOGICAL_OR:
                    WriteLogicalOr(instruction as InsLogicalOr); break;
                case AdhocInstructionType.JUMP_IF_FALSE:
                    WriteJumpIfFalse(instruction as InsJumpIfFalse); break;
                case AdhocInstructionType.JUMP_IF_TRUE:
                    WriteJumpIfTrue(instruction as InsJumpIfTrue); break;
                case AdhocInstructionType.JUMP:
                    WriteJump(instruction as InsJump); break;
                case AdhocInstructionType.STRING_CONST:
                    WriteStringConst(instruction as InsStringConst); break;
                case AdhocInstructionType.STRING_PUSH:
                    WriteStringPush(instruction as InsStringPush); break;
                case AdhocInstructionType.INT_CONST:
                    WriteIntConst(instruction as InsIntConst); break;
                case AdhocInstructionType.U_INT_CONST:
                    WriteUIntConst(instruction as InsUIntConst); break;
                case AdhocInstructionType.LONG_CONST:
                    WriteLongConst(instruction as InsLongConst); break;
                case AdhocInstructionType.U_LONG_CONST:
                    WriteULongConst(instruction as InsULongConst); break;
                case AdhocInstructionType.FLOAT_CONST:
                    WriteFloatConst(instruction as InsFloatConst); break;
                case AdhocInstructionType.SYMBOL_CONST:
                    WriteSymbolConst(instruction as InsSymbolConst); break;
                case AdhocInstructionType.SET_STATE:
                    WriteSetState(instruction as InsSetState); break;
                case AdhocInstructionType.LEAVE:
                    WriteLeave(instruction as InsLeaveScope); break;
                case AdhocInstructionType.BOOL_CONST:
                    WriteBoolConst(instruction as InsBoolConst); break;
                case AdhocInstructionType.ARRAY_CONST:
                    WriteArrayConst(instruction as InsArrayConst); break;
                case AdhocInstructionType.STATIC_DEFINE:
                    WriteStaticDefine(instruction as InsStaticDefine); break;
                case AdhocInstructionType.SOURCE_FILE:
                    WriteSourceFile(instruction as InsSourceFile); break;
                case AdhocInstructionType.LIST_ASSIGN:
                    WriteListAssign(instruction as InsListAssign); break;
                case AdhocInstructionType.TRY_CATCH:
                    WriteTryCatch(instruction as InsTryCatch); break;
                case AdhocInstructionType.UNDEF:
                    WriteUndef(instruction as InsUndef); break;
                case AdhocInstructionType.NIL_CONST:
                case AdhocInstructionType.VOID_CONST:
                case AdhocInstructionType.ASSIGN_POP:
                case AdhocInstructionType.ARRAY_PUSH:
                case AdhocInstructionType.POP:
                case AdhocInstructionType.ELEMENT_EVAL:
                case AdhocInstructionType.ELEMENT_PUSH:
                case AdhocInstructionType.MAP_CONST:
                case AdhocInstructionType.MAP_INSERT:
                case AdhocInstructionType.EVAL:
                case AdhocInstructionType.REQUIRE:
                case AdhocInstructionType.THROW:
                case AdhocInstructionType.ASSIGN:
                case AdhocInstructionType.OBJECT_SELECTOR:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void WriteUndef(InsUndef undef)
        {
            stream.WriteSymbols(undef.Symbols);
        }

        private void WriteTryCatch(InsTryCatch tryCatch)
        {
            stream.WriteInt32(tryCatch.InstructionIndex);
        }

        private void WriteListAssign(InsListAssign listAssign)
        {
            stream.WriteInt32(listAssign.VariableCount);
            stream.WriteBoolean(listAssign.Unk);
        }

        private void WriteSourceFile(InsSourceFile srcFile)
        {
            stream.WriteSymbol(srcFile.FileName);
        }

        private void WriteAttributeDefine(InsAttributeDefine attrDefine)
        {
            stream.WriteSymbol(attrDefine.AttributeName);
        }

        private void WriteArrayConst(InsArrayConst arrayConst)
        {
            stream.WriteUInt32(arrayConst.ArraySize);
        }

        private void WriteUIntConst(InsUIntConst uintConst)
        {
            stream.WriteUInt32(uintConst.Value);
        }

        private void WriteLongConst(InsLongConst longConst)
        {
            stream.WriteInt64(longConst.Value);
        }

        private void WriteULongConst(InsULongConst ulongConst)
        {
            stream.WriteUInt64(ulongConst.Value);
        }

        private void WriteStaticDefine(InsStaticDefine staticDefine)
        {
            stream.WriteSymbol(staticDefine.Name);
        }

        private void WriteFunctionDefine(InsFunctionDefine function)
        {
            stream.WriteSymbol(function.Name);
            WriteCodeBlock(function.CodeFrame);
        }

        private void WriteFunctionConst(InsFunctionConst functionConst)
        {
            WriteCodeBlock(functionConst.CodeFrame);
        }

        private void WriteMethodConst(InsMethodConst methodConst)
        {
            WriteCodeBlock(methodConst.CodeFrame);
        }

        private void WriteMethod(InsMethodDefine method)
        {
            stream.WriteSymbol(method.Name);
            WriteCodeBlock(method.CodeFrame);
        }

        private void WriteClassDefine(InsClassDefine classDefine)
        {
            stream.WriteSymbol(classDefine.Name);
            stream.WriteSymbols(classDefine.ExtendsFrom);
        }

        private void WriteModuleDefine(InsModuleDefine module)
        {
            stream.WriteSymbols(module.Names);
        }

        private void WriteBoolConst(InsBoolConst boolConst)
        {
            stream.WriteBoolean(boolConst.Value, BooleanCoding.Byte);
        }

        private void WriteSetState(InsSetState setState)
        {
            stream.WriteByte((byte)setState.State);
        }

        private void WriteIntConst(InsIntConst intConst)
        {
            stream.WriteInt32(intConst.Value);
        }

        private void WriteFloatConst(InsFloatConst floatConst)
        {
            stream.WriteSingle(floatConst.Value);
        }

        private void WriteStringConst(InsStringConst stringConst)
        {
            stream.WriteSymbol(stringConst.String);
        }

        private void WriteSymbolConst(InsSymbolConst symbolConst)
        {
            stream.WriteSymbol(symbolConst.String);
        }

        private void WriteStringPush(InsStringPush strPush)
        {
            stream.WriteInt32(strPush.StringCount);
        }

        private void WriteJumpIfFalse(InsJumpIfFalse jif) // unrelated but gif is pronounced like the name of this parameter (:
        {
            stream.WriteInt32(jif.JumpIndex);
        }

        private void WriteJumpIfTrue(InsJumpIfTrue jit)
        {
            stream.WriteInt32(jit.JumpIndex);
        }

        private void WriteJump(InsJump jump)
        {
            stream.WriteInt32(jump.JumpInstructionIndex);
        }

        private void WriteBinaryOperator(InsBinaryOperator binaryOperator)
        {
            stream.WriteSymbol(binaryOperator.Operator);
        }

        private void WriteBinaryOperator(InsBinaryAssignOperator binaryAssignOperator)
        {
            stream.WriteSymbol(binaryAssignOperator.Operator);
        }

        private void WriteBinaryOperator(InsUnaryAssignOperator unaryAssignOperator)
        {
            stream.WriteSymbol(unaryAssignOperator.Operator);
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
            stream.WriteInt32(leave.ModuleOrClassDepthRewindIndex); // Unused by GT6 and above
            stream.WriteInt32(leave.VariableStorageRewindIndex);
        }

        private void WriteVariableEval(InsVariableEvaluation variableEval)
        {
            stream.WriteSymbols(variableEval.VariableSymbols);
            stream.WriteInt32(variableEval.VariableStorageIndex);
        }

        private void WriteAttributeEval(InsAttributeEvaluation attributeEval)
        {
            stream.WriteSymbols(attributeEval.AttributeSymbols);
        }

        private void WriteVariablePush(InsVariablePush variablePush)
        {
            stream.WriteSymbols(variablePush.VariableSymbols);
            stream.WriteInt32(variablePush.VariableStorageIndex);
        }

        private void WriteAttributePush(InsAttributePush attributePush)
        {
            stream.WriteSymbols(attributePush.AttributeSymbols);
        }

        private void WriteImport(InsImport import)
        {
            stream.WriteSymbols(import.ImportNamespaceParts);
            stream.WriteSymbol(import.ModuleValue);
            stream.WriteSymbol(SymbolMap.Symbols["nil"]);
        }

        private void SerializeSymbolTable()
        {
            Logger.Debug($"Serializing symbol table ({SymbolMap.Symbols.Count} symbols)");

            stream.WriteVarInt(SymbolMap.Symbols.Count);
            foreach (AdhocSymbol symb in SymbolMap.Symbols.Values)
                stream.WriteVarString(symb.Name);
        }
    }
}
