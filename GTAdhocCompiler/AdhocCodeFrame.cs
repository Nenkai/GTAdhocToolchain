using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Esprima.Ast;
using GTAdhocCompiler.Instructions;

namespace GTAdhocCompiler
{
    /// <summary>
    /// Represents a frame of instructions. May be a function, method, or main.
    /// </summary>
    public class AdhocCodeFrame
    {
        /// <summary>
        /// Parent frame for this frame, if exists.
        /// </summary>
        public AdhocCodeFrame ParentFrame { get; set; }

        /// <summary>
        /// Used for function variables/callbacks. Should be true for function consts.
        /// </summary>
        public bool CanCaptureVariablesFromParentFrame { get; set; }

        /// <summary>
        /// Current instructions for this block.
        /// </summary>
        public List<InstructionBase> Instructions { get; set; } = new();

        /// <summary>
        /// Source file for this block.
        /// </summary>
        public AdhocSymbol SourceFilePath { get; set; }

        /// <summary>
        /// Function or method parameters
        /// </summary>
        public List<AdhocSymbol> FunctionParameters { get; set; } = new List<AdhocSymbol>();

        /// <summary>
        /// Captured variables for function consts
        /// </summary>
        public Dictionary<AdhocSymbol, int> CapturedCallbackVariables { get; set; } = new();

        /// <summary>
        /// Current stack for the block.
        /// </summary>
        public AdhocStack Stack { get; set; } = new();

        /// <summary>
        /// To keep track of the current loops, to compile continue and break statements at the end of one.
        /// </summary>
        public Stack<LoopContext> CurrentLoops { get; set; } = new();

        /// <summary>
        /// To keep track of the current scope depth within the block. Used to whether a return or not has been writen to insert one later for instance.
        /// </summary>
        public Stack<ScopeContext> CurrentScopes { get; set; } = new();

        /// <summary>
        /// Gets the current scope.
        /// </summary>
        public ScopeContext CurrentScope => CurrentScopes.Peek();

        /// <summary>
        /// Returns whether the current scope is the top block level.
        /// </summary>
        public bool IsTopLevel => CurrentScopes.Count == 1;

        /// <summary>
        /// Used to keep track of all declared variables, mostly for telling whether a variable is static or not
        /// </summary>
        public List<string> DeclaredVariables { get; set; } = new();

        /// <summary>
        /// Whether the current block has a return statement, to manually add the return instructions if false.
        /// </summary>
        public bool HasTopLevelReturnValue { get; set; }

        /// <summary>
        /// Gets the current/last loop.
        /// </summary>
        /// <returns></returns>
        public LoopContext GetLastLoop() => CurrentLoops.Peek();

        public void SetSourcePath(AdhocSymbolMap symbolMap, string path)
        {
            SourceFilePath = symbolMap.RegisterSymbol(path);
        }

        /// <summary>
        /// Adds a new instruction and updates the stack counter.
        /// </summary>
        /// <param name="ins"></param>
        /// <param name="lineNumber"></param>
        /// <exception cref="Exception"></exception>
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
                case AdhocInstructionType.VARIABLE_PUSH:
                case AdhocInstructionType.VARIABLE_EVAL:
                case AdhocInstructionType.SYMBOL_CONST:
                case AdhocInstructionType.VOID_CONST:
                    Stack.StackStorageCounter++; break;
                case AdhocInstructionType.FUNCTION_DEFINE:
                    InsFunctionDefine func = ins as InsFunctionDefine;
                    Stack.StackStorageCounter -= func.CodeFrame.FunctionParameters.Count; // Ver > 8
                    Stack.StackStorageCounter -= func.CodeFrame.CapturedCallbackVariables.Count; // Ver > 7
                    break;
                case AdhocInstructionType.METHOD_DEFINE:
                    InsMethodDefine method = ins as InsMethodDefine;
                    Stack.StackStorageCounter -= method.CodeFrame.FunctionParameters.Count; // Ver > 8
                    Stack.StackStorageCounter -= method.CodeFrame.CapturedCallbackVariables.Count; // Ver > 7
                    break;
                case AdhocInstructionType.FUNCTION_CONST:
                    InsFunctionConst funcConst = ins as InsFunctionConst;
                    Stack.StackStorageCounter -= funcConst.CodeFrame.FunctionParameters.Count; // Ver > 8
                    Stack.StackStorageCounter -= funcConst.CodeFrame.CapturedCallbackVariables.Count; // Ver > 7
                    Stack.StackStorageCounter++; // Function itself
                    break;
                case AdhocInstructionType.POP:
                case AdhocInstructionType.POP_OLD:
                case AdhocInstructionType.ASSIGN:
                case AdhocInstructionType.JUMP_IF_FALSE:
                case AdhocInstructionType.JUMP_IF_TRUE:
                case AdhocInstructionType.REQUIRE:
                case AdhocInstructionType.ARRAY_PUSH:
                case AdhocInstructionType.MODULE_CONSTRUCTOR:
                case AdhocInstructionType.THROW:
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
                case AdhocInstructionType.LIST_ASSIGN:
                    InsListAssign listAssign = ins as InsListAssign;
                    Stack.StackStorageCounter -= listAssign.VariableCount;
                    Stack.StackStorageCounter--; // Array
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

            // FUNCTION_CONST
            // METHOD_CONST
            // LIST_ASSIGN
            // CALL_OLD
            // LEAVE
            // SET_STATE
            // VA_CALL
        }

        public int GetLastInstructionIndex()
        {
            return Instructions.Count;
        }

        public int AddScopeVariable(AdhocSymbol symbol, bool isVariableDeclaration = true)
        {
            var scope = CurrentScope;
            if (!isVariableDeclaration && CanCaptureVariablesFromParentFrame && ParentFrame is not null)
            {
                // Is a captured variable for function const?
                if (ParentFrame.DeclaredVariables.Contains(symbol.Name) && !CapturedCallbackVariables.ContainsKey(symbol)
                    && !DeclaredVariables.Contains(symbol.Name))
                {
                    if (Stack.TryAddOrGetVariableIndex(symbol, out int varIndex))
                        CapturedCallbackVariables.Add(symbol, varIndex);

                    return varIndex;
                }

            }

            if (!Stack.TryAddOrGetVariableIndex(symbol, out int index))
                return index;

            // We added a variable to the stack storage, add it to scope variables
            scope.ScopeVariables.Add(symbol.Name, symbol);

            // And declared variables if it is a declaration
            if (isVariableDeclaration && !DeclaredVariables.Contains(symbol.Name))
                DeclaredVariables.Add(symbol.Name);

            return index;
        }


        public ScopeContext GetLastBreakControlledScope()
        {
            foreach (var scope in CurrentScopes)
            {
                if (scope is LoopContext || scope is SwitchContext)
                    return scope;
            }

            return null;
        }
    }
}
