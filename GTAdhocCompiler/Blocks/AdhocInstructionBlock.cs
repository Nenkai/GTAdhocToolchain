using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTAdhocCompiler.Instructions;

namespace GTAdhocCompiler
{
    /// <summary>
    /// Represents a block of instructions.
    /// </summary>
    public class AdhocInstructionBlock
    {
        public List<InstructionBase> Instructions { get; set; } = new();
        public AdhocSymbol SourceFilePath { get; private set; }

        public List<AdhocSymbol> Parameters { get; set; } = new List<AdhocSymbol>();
        public List<AdhocSymbol> CallbackParameters { get; set; } = new List<AdhocSymbol>();

        public AdhocStack Stack { get; set; } = new();

        public List<string> VariableHeap { get; set; } = new()
        {
            null, // Always an empty one
        };

        /// <summary>
        /// Used to keep track of static and non static members, as static members have two symbols in their variable push/eval
        /// </summary>
        public List<string> DeclaredVariables { get; set; } = new();

        public int AddSymbolToHeap(string variableName)
        {
            if (VariableHeap.Contains(variableName))
                return VariableHeap.IndexOf(variableName);

            VariableHeap.Add(variableName);
            return VariableHeap.Count - 1;
        }

        public void SetSourcePath(AdhocSymbolMap symbolMap, string path)
        {
            SourceFilePath = symbolMap.RegisterSymbol(path);
        }

        public void AddInstruction(InstructionBase ins, int lineNumber)
        {
            ins.LineNumber = lineNumber;
            Instructions.Add(ins);

            switch (ins.InstructionType)
            {
                case AdhocInstructionType.ARRAY_CONST:
                case AdhocInstructionType.MAP_CONST:
                case AdhocInstructionType.ATTRIBUTE_DEFINE: // Note: Investigate check potential pop by attribute_define only if Version > 6
                case AdhocInstructionType.FLOAT_CONST:
                case AdhocInstructionType.INT_CONST:
                case AdhocInstructionType.U_INT_CONST:
                case AdhocInstructionType.NIL_CONST:
                case AdhocInstructionType.STRING_CONST:
                case AdhocInstructionType.LONG_CONST:
                case AdhocInstructionType.U_LONG_CONST:
                case AdhocInstructionType.BOOL_CONST:
                case AdhocInstructionType.DOUBLE_CONST:
                case AdhocInstructionType.THROW:
                case AdhocInstructionType.VARIABLE_PUSH:
                case AdhocInstructionType.VARIABLE_EVAL:
                case AdhocInstructionType.SYMBOL_CONST:
                case AdhocInstructionType.VOID_CONST:
                    Stack.StackStorageCounter++; break;
                case AdhocInstructionType.FUNCTION_DEFINE:
                    InsFunctionDefine func = ins as InsFunctionDefine;
                    Stack.StackStorageCounter -= func.FunctionBlock.Parameters.Count; // Ver > 8
                    Stack.StackStorageCounter -= func.FunctionBlock.CallbackParameters.Count; // Ver > 7
                    break;
                case AdhocInstructionType.POP:
                case AdhocInstructionType.POP_OLD:
                case AdhocInstructionType.ASSIGN:
                case AdhocInstructionType.JUMP_IF_FALSE:
                case AdhocInstructionType.JUMP_IF_TRUE:
                case AdhocInstructionType.REQUIRE:
                case AdhocInstructionType.ARRAY_PUSH:
                case AdhocInstructionType.MODULE_CONSTRUCTOR:
                    Stack.StackStorageCounter--; break;
                case AdhocInstructionType.ASSIGN_POP:
                case AdhocInstructionType.MAP_INSERT:
                    Stack.StackStorageCounter -= 2;
                    break;
                case AdhocInstructionType.ATTRIBUTE_PUSH:
                case AdhocInstructionType.LOGICAL_AND_OLD:
                case AdhocInstructionType.LOGICAL_OR_OLD:
                    // Verify these two later
                case AdhocInstructionType.LOGICAL_AND:
                case AdhocInstructionType.LOGICAL_OR:
                case AdhocInstructionType.UNARY_ASSIGN_OPERATOR:
                case AdhocInstructionType.UNARY_OPERATOR:
                    Stack.StackStorageCounter++;
                    Stack.StackStorageCounter--;
                    break;
                case AdhocInstructionType.ASSIGN_OLD:
                case AdhocInstructionType.BINARY_ASSIGN_OPERATOR:
                case AdhocInstructionType.BINARY_OPERATOR:
                case AdhocInstructionType.OBJECT_SELECTOR:
                case AdhocInstructionType.ELEMENT_PUSH:
                case AdhocInstructionType.ELEMENT_EVAL:
                    Stack.StackStorageCounter -= 2;
                    Stack.StackStorageCounter++;
                    break;
                case AdhocInstructionType.CALL:
                    InsCall call = ins as InsCall;
                    Stack.StackStorageCounter--; // Function
                    Stack.StackStorageCounter -= call.ArgumentCount;
                    Stack.StackStorageCounter++; // Return value
                    break;
                case AdhocInstructionType.LIST_ASSIGN_OLD:
                    // TODO
                    // Arg count remove
                    Stack.StackStorageCounter++;
                    break;
                case AdhocInstructionType.PRINT:
                    // TODO
                    // Arg count remove
                    Stack.StackStorageCounter++;
                    break;
                case AdhocInstructionType.MAP_CONST_OLD:
                    // TODO
                    // Arg count remove * 2
                    Stack.StackStorageCounter++;
                    break;
                case AdhocInstructionType.STRING_PUSH:
                    InsStringPush push = ins as InsStringPush;
                    Stack.StackStorageCounter -= push.StringCount;
                    Stack.StackStorageCounter++;
                    break;
                // These have no effect on stack
                case AdhocInstructionType.CLASS_DEFINE:
                case AdhocInstructionType.EVAL:
                case AdhocInstructionType.IMPORT:
                case AdhocInstructionType.JUMP:
                case AdhocInstructionType.LOCAL_DEFINE:
                case AdhocInstructionType.MODULE_DEFINE:
                case AdhocInstructionType.STATIC_DEFINE:
                case AdhocInstructionType.NOP:
                case AdhocInstructionType.SET_STATE_OLD:
                case AdhocInstructionType.TRY_CATCH:
                case AdhocInstructionType.UNDEF:
                case AdhocInstructionType.ATTRIBUTE_EVAL:
                case AdhocInstructionType.SOURCE_FILE:
                case AdhocInstructionType.CODE_EVAL:
                case AdhocInstructionType.SET_STATE: // FIX ME LATER
                case AdhocInstructionType.LEAVE: // FIX ME LATER
                    break;
                default:
                    throw new Exception("Not implemented");
            }

            // METHOD_DEFINE
            // FUNCTION_CONST
            // METHOD_CONST
            // LIST_ASSIGN
            // CALL_OLD
            // LEAVE
            // SET_STATE
            // LOGICAL_AND
            // LOGICAL_OR
            // VA_CALL
        }


        public int GetLastInstructionIndex()
        {
            return Instructions.Count;
        }
    }
}
