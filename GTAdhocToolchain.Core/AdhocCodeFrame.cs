using GTAdhocToolchain.Core.Instructions;
using GTAdhocToolchain.Core.Variables;

using System.Text;

namespace GTAdhocToolchain.Core
{
    /// <summary>
    /// Represents a frame of instructions. May be a function, method, or main.
    /// </summary>
    public class AdhocCodeFrame
    {
        public const int ADHOC_VERSION_CURRENT = 12;

        public int Version { get; set; } = ADHOC_VERSION_CURRENT;

        /// <summary>
        /// Parent frame for this frame, if exists.
        /// </summary>
        public AdhocCodeFrame ParentFrame { get; set; }

        /// <summary>
        /// Module for this frame.
        /// </summary>
        public AdhocModule CurrentModule { get; set; }

        /// <summary>
        /// Used for function variables/callbacks. Should be true for function consts.
        /// </summary>
        public bool ContextAllowsVariableCaptureFromParentFrame { get; set; }

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
        public List<AdhocSymbol> CapturedCallbackVariables { get; set; } = new();

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
        /// Whether the current block has a return statement, to manually add the return instructions if false.
        /// </summary>
        public bool HasTopLevelReturnValue { get; set; }

        /// <summary>
        /// Gets the current/last loop.
        /// </summary>
        /// <returns></returns>
        public LoopContext GetLastLoop() => CurrentLoops.Peek();

        public uint InstructionCountOffset { get; set; }
        public bool HasDebuggingInformation { get; set; }

        /// <summary>
        /// Version 12 only. Indicatesd a subroutine with a rest/params[] element/argument.
        /// </summary>
        public bool HasRestElement { get; set; }

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
            ins.LineNumber = (uint)lineNumber;
            Instructions.Add(ins);

            switch (ins.InstructionType)
            {
                case AdhocInstructionType.ARRAY_CONST:
                case AdhocInstructionType.MAP_CONST:
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
                case AdhocInstructionType.METHOD_CONST:
                    InsMethodConst methodConst = ins as InsMethodConst;
                    Stack.StackStorageCounter -= methodConst.CodeFrame.FunctionParameters.Count; // Ver > 8
                    Stack.StackStorageCounter -= methodConst.CodeFrame.CapturedCallbackVariables.Count; // Ver > 7
                    Stack.StackStorageCounter++; // method itself
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
                case AdhocInstructionType.ATTRIBUTE_DEFINE: // Note: Investigate check potential pop by attribute_define only if Version > 6
                    Stack.StackStorageCounter--; break;
                case AdhocInstructionType.ASSIGN_POP:
                case AdhocInstructionType.MAP_INSERT:
                    Stack.StackStorageCounter -= 2;
                    break;
                case AdhocInstructionType.ATTRIBUTE_PUSH:
                // Verify these two later
                case AdhocInstructionType.LOGICAL_AND:
                case AdhocInstructionType.LOGICAL_OR:
                case AdhocInstructionType.UNARY_ASSIGN_OPERATOR:
                case AdhocInstructionType.UNARY_OPERATOR:
                    Stack.StackStorageCounter--;
                    Stack.StackStorageCounter++;
                    break;
                case AdhocInstructionType.ASSIGN_OLD:
                case AdhocInstructionType.BINARY_ASSIGN_OPERATOR:
                case AdhocInstructionType.BINARY_OPERATOR:
                case AdhocInstructionType.OBJECT_SELECTOR:
                case AdhocInstructionType.ELEMENT_PUSH:
                case AdhocInstructionType.ELEMENT_EVAL:
                case AdhocInstructionType.VA_CALL:
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

                case AdhocInstructionType.SET_STATE:
                    InsSetState state = ins as InsSetState;
                    if (state.State == AdhocRunState.RETURN || state.State == AdhocRunState.YIELD)
                        Stack.StackStorageCounter--;
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
                case AdhocInstructionType.LEAVE: // FIX ME LATER
                    break;
                default:
                    throw new Exception("Not implemented");
            }


            // CALL_OLD
            // LEAVE
        }

        public int GetLastInstructionIndex()
        {
            return Instructions.Count;
        }

        public void FreeLocalVariable(AdhocSymbol symbol)
        {
            var localVarToRemove = Stack.GetLocalVariableBySymbol(symbol);
            Stack.FreeLocalVariable(localVarToRemove);
        }

        public void FreeStaticVariable(AdhocSymbol symbol)
        {
            var staticVarToRemove = Stack.GetStaticVariableBySymbol(symbol);
            Stack.FreeStaticVariable(staticVarToRemove);
        }

        public int AddScopeVariable(AdhocSymbol symbol, bool isAssignment = false, bool isStatic = false, bool isLocalDeclaration = false)
        {
            Variable newVariable;
            var lastScope = CurrentScope;

            if (isAssignment)
            {
                if (isStatic)
                {
                    bool added = Stack.TryAddStaticVariable(symbol, out newVariable);
                    if (added)
                        lastScope.StaticScopeVariables.Add(symbol.Name, symbol);
                }
                else if (!isLocalDeclaration) // Assigning to a symbol without a declaration, i.e 'hello = "world"'
                {
                    // Check if the symbol is a static reference, a direct reference to a module attribute, and if it doesn't match, check if it doesn't overlap with any scope locals
                    if (Stack.HasStaticVariable(symbol) || IsStaticModuleFieldOrAttribute(symbol) || !Stack.HasLocalVariable(symbol))
                    {
                        bool added = Stack.TryAddStaticVariable(symbol, out newVariable);
                        if (added)
                            lastScope.StaticScopeVariables.Add(symbol.Name, symbol);
                    }
                    else
                    {
                        // Assigning to a local that already exists
                        bool added = Stack.TryAddLocalVariable(symbol, out newVariable);
                        if (added)
                            lastScope.LocalScopeVariables.Add(symbol.Name, symbol);
                    }
                }
                else // Actually declaring a new (?) local 
                {
                    bool added = Stack.TryAddLocalVariable(symbol, out newVariable);
                    if (added)
                        lastScope.LocalScopeVariables.Add(symbol.Name, symbol);
                }
            }
            else
            {
                // Undeclared variable accesses

                // Captured variable from parent function?
                if (ContextAllowsVariableCaptureFromParentFrame && ParentFrame is not null
                    && ParentFrame.Stack.HasLocalVariable(symbol) // Symbol exists in parent? If so, we are actually capturing
                    && (!Stack.HasLocalVariable(symbol) && !FunctionParameters.Contains(symbol) || CapturedCallbackVariables.Contains(symbol))) // Do not conflict with locals/params
                {
                    if (!CapturedCallbackVariables.Contains(symbol))
                    {
                        CapturedCallbackVariables.Add(symbol);

                        // Add captured variable to current frame
                        Stack.TryAddLocalVariable(symbol, out newVariable);
                    }

                    // Captured variables have backward indices
                    return -(CapturedCallbackVariables.IndexOf(symbol) + 1); // 0 -> -1, 1 -> -2 etc
                }

                if (isStatic || !Stack.HasLocalVariable(symbol))
                {
                    bool added = Stack.TryAddStaticVariable(symbol, out newVariable);
                    if (added)
                        lastScope.StaticScopeVariables.Add(symbol.Name, symbol);
                }
                else
                {
                    // Variable already exists, just get the index
                    Stack.TryAddLocalVariable(symbol, out newVariable);
                }
            }

            // Grab our variable index, whether it's been added or not
            if (newVariable is LocalVariable local)
            {
                var idx = Stack.GetLocalVariableIndex(local);
                if (idx == -1)
                    throw new Exception($"Could not find local variable index for {local}");

                return idx;
            }
            else if (newVariable is StaticVariable staticVar)
            {
                var idx = Stack.GetStaticVariableIndex(staticVar);
                if (idx == -1)
                    throw new Exception($"Could not find static variable index for {staticVar}");
                return idx;
            }
            else
                throw new Exception("Variable is not a local or static variable..?");
        }

        public bool IsStaticModuleFieldOrAttribute(AdhocSymbol symbol)
        {
            // Recursively check all modules
            bool HasStatic(AdhocSymbol symbol, AdhocModule module)
            {
                if (module.IsDefinedStaticMember(symbol) || module.IsDefinedAttributeMember(symbol))
                    return true;
                else if (module.ParentModule is not null)
                    return HasStatic(symbol, module.ParentModule);
                else
                    return false;
            }

            return HasStatic(symbol, CurrentModule);
        }

        public void AddAttributeOrStaticMemberVariable(AdhocSymbol symbol)
        {
            var newVar = new StaticVariable() { Symbol = symbol };

            // TODO: Refactor me maybe?
            Stack.AddStaticVariable(newVar);
            CurrentScope.StaticScopeVariables.Add(symbol.Name, symbol);
        }

        public bool IsStaticVariable(AdhocSymbol symb)
        {
            if (symb.Name == "self")
                return false;

            if (Stack.HasLocalVariable(symb))
                return false; // Priorize local variables

            if (IsStaticModuleFieldOrAttribute(symb))
                return true;

            return true;
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

        public void Read(AdhocStream stream)
        {
            if (Version < 8)
            {
                HasDebuggingInformation = true; // Not in code paths, but its forced to read
                SourceFilePath = stream.ReadSymbol();

                if (Version > 3)
                {
                    uint argCount = stream.ReadUInt32();
                    for (int i = 0; i < argCount; i++)
                        CapturedCallbackVariables.Add(stream.ReadSymbol());
                }
            }
            else
            {
                if (Version >= 13)
                {
                    HasDebuggingInformation = true;

                    SourceFilePath = stream.ReadSymbol();
                    stream.ReadByte();
                }
                else
                {
                    HasDebuggingInformation = stream.ReadBoolean();
                    Version = stream.ReadByte();


                    if (Version != 8) // Why PDI? Changed your mind after 8?
                    {
                        if (HasDebuggingInformation)
                            SourceFilePath = stream.ReadSymbol();
                    }


                    if (Version >= 12)
                        HasRestElement = stream.ReadBoolean();
                }

                uint argCount = stream.ReadUInt32();

                if (argCount > 0)
                {
                    for (int i = 0; i < argCount; i++)
                    {
                        var symbol = stream.ReadSymbol();
                        FunctionParameters.Add(new AdhocSymbol(stream.ReadInt32(), symbol.Name));
                    }
                }

                uint funcArgs = stream.ReadUInt32();
                if (funcArgs > 0)
                {
                    for (int i = 0; i < funcArgs; i++)
                    {
                        var symbol = stream.ReadSymbol();
                        CapturedCallbackVariables.Add(new AdhocSymbol(stream.ReadInt32(), symbol.Name));

                    }
                }

                uint unkVarHeapIndex = stream.ReadUInt32();
                
            }

            if (Version <= 10)
            {
                Stack.LocalVariableStorageSize = stream.ReadInt32();
                Stack.StackSize = stream.ReadInt32();
                Stack.StaticVariableStorageSize = Stack.LocalVariableStorageSize;
            }
            else
            {

                Stack.StackSize = stream.ReadInt32();
                Stack.LocalVariableStorageSize = stream.ReadInt32();
                Stack.StaticVariableStorageSize = stream.ReadInt32();
            }

            InstructionCountOffset = (uint)stream.Position;
            uint instructionCount = stream.ReadUInt32();
            if (instructionCount < 0x40000000)
            {
                for (int i = 0; i < instructionCount; i++)
                {
                    uint originalLineNumber = 0;
                    if (HasDebuggingInformation)
                        originalLineNumber = stream.ReadUInt32();

                    AdhocInstructionType type = (AdhocInstructionType)stream.ReadByte();

                    ReadInstruction(stream, originalLineNumber, type);
                }
            }
        }

        public void ReadInstruction(AdhocStream stream, uint lineNumber, AdhocInstructionType type)
        {
            InstructionBase ins = InstructionBase.GetByType(type);
            if (ins != null)
            {
                ins.InstructionOffset = (uint)stream.Position + 4;
                ins.LineNumber = lineNumber;
                ins.Deserialize(stream);
                Instructions.Add(ins);
            }
        }

        public string Dissasemble()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            if (FunctionParameters.Count != 0)
            {
                for (int i = 0; i < FunctionParameters.Count; i++)
                {
                    sb.Append(FunctionParameters[i].Name);//.Append($"[{FunctionParameters[i].Id}]");
                    if (i != FunctionParameters.Count - 1)
                        sb.Append(", ");
                }
            }
            sb.Append(")");
            if (CapturedCallbackVariables.Count != 0)
            {
                sb.Append("[");
                for (int i = 0; i < CapturedCallbackVariables.Count; i++)
                {
                    sb.Append(CapturedCallbackVariables[i].Name);//.Append($"[{CapturedCallbackVariables[i].Id}]");
                    if (i != CapturedCallbackVariables.Count - 1)
                        sb.Append(", ");
                }
                sb.Append("]");
            }

            if (HasRestElement)
                sb.Append(" params[]");

            sb.AppendLine();

            sb.Append("  > Instruction Count: ").Append(Instructions.Count).Append(" (").Append(InstructionCountOffset.ToString("X2")).Append(')').AppendLine();
            sb.Append($"  > Stack Size: {Stack.StackSize} - Variable Heap Size: {Stack.LocalVariableStorageSize} - Variable Heap Size Static: {(Version < 10 ? "=Variable Heap Size" : $"{Stack.StaticVariableStorageSize}")}");

            return sb.ToString();
        }
    }
}
