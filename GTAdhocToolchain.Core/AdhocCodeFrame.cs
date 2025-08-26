using GTAdhocToolchain.Core.Instructions;
using GTAdhocToolchain.Core.Stack;
using GTAdhocToolchain.Core.Variables;

using System.Text;

namespace GTAdhocToolchain.Core
{
    /// <summary>
    /// Represents a frame of instructions. May be a function, method, or main.
    /// </summary>
    public class AdhocCodeFrame
    {
        /// <summary>
        /// GT7.
        /// </summary>
        public const int ADHOC_VERSION_LATEST = 15;

        /// <summary>
        /// Default adhoc version, 12. Compatible with GT5P, GT5, GTPSP, GT6, GT7SP.
        /// </summary>
        public const int ADHOC_VERSION_DEFAULT = 12;

        public int Version { get; set; } = ADHOC_VERSION_DEFAULT;

        /// <summary>
        /// Current instructions for this block.
        /// </summary>
        public List<InstructionBase> Instructions { get; set; } = new();

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
        public IAdhocStack Stack { get; set; }

        /// <summary>
        /// Parent frame for this frame, if exists.
        /// </summary>
        public AdhocCodeFrame ParentFrame { get; set; }

        /// <summary>
        /// Modules defined within this frame.
        /// </summary>
        public Stack<AdhocModule> Modules { get; set; } = new();

        /// <summary>
        /// Used for function variables/callbacks. Should be true for function consts.
        /// </summary>
        public bool ContextAllowsVariableCaptureFromParentFrame { get; set; }

        /// <summary>
        /// Source file for this block.
        /// </summary>
        public AdhocSymbol SourceFilePath { get; set; }

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
        public bool IsCurrentScopeTopScope => CurrentScopes.Count == 1;

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
        public bool HasDebuggingInformation { get; set; } = true;

        /// <summary>
        /// Version 12 only. Indicatesd a subroutine with a rest/params[] element/argument.
        /// </summary>
        public bool HasRestElement { get; set; }

        /// <summary>
        /// Creates the stack (depending on the adhoc version).
        /// </summary>
        public void CreateStack()
        {
            if (Version >= 11)
                Stack = new AdhocStack();
            else
                Stack = new AdhocStackOld();
        }

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
                case AdhocInstructionType.BYTE_CONST:
                case AdhocInstructionType.U_BYTE_CONST:
                case AdhocInstructionType.SHORT_CONST:
                case AdhocInstructionType.U_SHORT_CONST:
                    Stack.IncrementStackCounter(); 
                    break;
                case AdhocInstructionType.FUNCTION_DEFINE:
                    InsFunctionDefine func = ins as InsFunctionDefine;
                    if (Version >= 8)
                        Stack.DecreaseStackCounter(func.CodeFrame.FunctionParameters.Count);

                    if (Version >= 8)
                        Stack.DecreaseStackCounter(func.CodeFrame.CapturedCallbackVariables.Count);
                    break;
                case AdhocInstructionType.METHOD_DEFINE:
                    InsMethodDefine method = ins as InsMethodDefine;
                    if (Version >= 8)
                        Stack.DecreaseStackCounter(method.CodeFrame.FunctionParameters.Count);

                    if (Version >= 8)
                        Stack.DecreaseStackCounter(method.CodeFrame.CapturedCallbackVariables.Count);
                    break;
                case AdhocInstructionType.FUNCTION_CONST:
                    InsFunctionConst funcConst = ins as InsFunctionConst;
                    if (Version >= 8)
                        Stack.DecreaseStackCounter(funcConst.CodeFrame.FunctionParameters.Count); // Ver > 8

                    if (Version >= 8)
                        Stack.DecreaseStackCounter(funcConst.CodeFrame.CapturedCallbackVariables.Count);

                    Stack.IncrementStackCounter(); // Function itself
                    break;
                case AdhocInstructionType.METHOD_CONST:
                    InsMethodConst methodConst = ins as InsMethodConst;
                    if (Version >= 8)
                        Stack.DecreaseStackCounter(methodConst.CodeFrame.FunctionParameters.Count); // Ver > 8

                    if (Version >= 8)
                        Stack.DecreaseStackCounter(methodConst.CodeFrame.CapturedCallbackVariables.Count);

                    Stack.IncrementStackCounter(); // method itself
                    break;
                case AdhocInstructionType.POP:
                case AdhocInstructionType.POP_OLD:
                case AdhocInstructionType.ASSIGN:
                case AdhocInstructionType.JUMP_IF_FALSE:
                case AdhocInstructionType.JUMP_IF_TRUE:
                case AdhocInstructionType.JUMP_IF_NIL:
                case AdhocInstructionType.REQUIRE:
                case AdhocInstructionType.ARRAY_PUSH:
                case AdhocInstructionType.MODULE_CONSTRUCTOR:
                case AdhocInstructionType.THROW:
                    Stack.DecrementStackCounter();
                    break;

                case AdhocInstructionType.ATTRIBUTE_DEFINE: // Note: Investigate check potential pop by attribute_define only if Version > 6
                    if (Version > 6)
                        Stack.DecrementStackCounter();
                    break;

                case AdhocInstructionType.ASSIGN_POP:
                case AdhocInstructionType.MAP_INSERT:
                    Stack.DecreaseStackCounter(2);
                    break;
                case AdhocInstructionType.ATTRIBUTE_PUSH:
                // Verify these two later
                case AdhocInstructionType.LOGICAL_AND:
                case AdhocInstructionType.LOGICAL_OR:
                case AdhocInstructionType.LOGICAL_AND_OLD:
                case AdhocInstructionType.LOGICAL_OR_OLD:
                case AdhocInstructionType.UNARY_ASSIGN_OPERATOR:
                case AdhocInstructionType.UNARY_OPERATOR:
                    Stack.DecrementStackCounter();
                    Stack.IncrementStackCounter();
                    break;
                case AdhocInstructionType.ASSIGN_OLD:
                case AdhocInstructionType.BINARY_ASSIGN_OPERATOR:
                case AdhocInstructionType.BINARY_OPERATOR:
                case AdhocInstructionType.OBJECT_SELECTOR:
                case AdhocInstructionType.ELEMENT_PUSH:
                case AdhocInstructionType.ELEMENT_EVAL:
                case AdhocInstructionType.VA_CALL:
                    Stack.DecreaseStackCounter(2);
                    Stack.IncrementStackCounter();
                    break;
                case AdhocInstructionType.CALL:
                    InsCall call = ins as InsCall;
                    Stack.DecrementStackCounter(); // Function
                    Stack.DecreaseStackCounter(call.ArgumentCount);
                    Stack.IncrementStackCounter(); // Return value
                    break;
                case AdhocInstructionType.LIST_ASSIGN_OLD:
                    // TODO
                    // Arg count remove
                    Stack.IncrementStackCounter();
                    break;
                case AdhocInstructionType.PRINT:
                    // TODO
                    // Arg count remove
                    Stack.IncrementStackCounter();
                    break;
                case AdhocInstructionType.MAP_CONST_OLD:
                    // TODO
                    // Arg count remove * 2
                    Stack.IncrementStackCounter();
                    break;
                case AdhocInstructionType.ARRAY_CONST_OLD:
                    var arrayConstOld = ins as InsArrayConstOld;
                    Stack.DecreaseStackCounter((int)arrayConstOld.ArraySize);
                    Stack.IncrementStackCounter();
                    break;
                case AdhocInstructionType.STRING_PUSH:
                    InsStringPush push = ins as InsStringPush;
                    Stack.DecreaseStackCounter(push.StringCount);
                    Stack.IncrementStackCounter();
                    break;
                case AdhocInstructionType.LIST_ASSIGN:
                    InsListAssign listAssign = ins as InsListAssign;
                    Stack.DecreaseStackCounter(listAssign.VariableCount);
                    Stack.DecrementStackCounter(); // Array
                    Stack.IncrementStackCounter();
                    break;

                case AdhocInstructionType.SET_STATE:
                    InsSetState state = ins as InsSetState;
                    if (state.State == AdhocRunState.RETURN || state.State == AdhocRunState.YIELD)
                        Stack.DecrementStackCounter();
                    break;

                // These have no effect on stack
                case AdhocInstructionType.CLASS_DEFINE:
                case AdhocInstructionType.EVAL:
                case AdhocInstructionType.IMPORT:
                case AdhocInstructionType.JUMP:
                case AdhocInstructionType.LOCAL_DEFINE:
                case AdhocInstructionType.MODULE_DEFINE:
                    break;

                case AdhocInstructionType.STATIC_DEFINE:
                    break;

                case AdhocInstructionType.NOP:
                case AdhocInstructionType.SET_STATE_OLD:
                case AdhocInstructionType.TRY_CATCH:
                case AdhocInstructionType.UNDEF:
                case AdhocInstructionType.ATTRIBUTE_EVAL:
                case AdhocInstructionType.SOURCE_FILE:
                case AdhocInstructionType.CODE_EVAL:
                case AdhocInstructionType.LEAVE: // FIX ME LATER
                case AdhocInstructionType.DELEGATE_DEFINE:
                    break;

                case AdhocInstructionType.LOGICAL_OPTIONAL:
                    break; // To figure if it has any effect

                default:
                    throw new Exception($"Unimplemented instruction handling for stack calculation: {ins.InstructionType}");
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

        /// <summary>
        /// Adds a new scope variable to this frame.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="isAssignment"></param>
        /// <param name="isStatic"></param>
        /// <param name="isLocalDeclaration"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public int AddScopeVariable(AdhocSymbol symbol,
            bool isAssignment = false, 
            bool isStatic = false, 
            bool isLocalDeclaration = false)
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
                    if (IsVariableCapturedFromParentScope(symbol))
                    {
                        return AddCapturedVariableFromParentScope(symbol);
                    }

                    // Are we trying to assign to a local variable?
                    if (Stack.HasLocalVariable(symbol))
                    {
                        // Assigning to a local that already exists
                        bool added = Stack.TryAddLocalVariable(symbol, out newVariable);
                        if (added)
                            lastScope.LocalScopeVariables.Add(symbol.Name, symbol);
                    }
                    else
                    {
                        // Nope. Assuming this is a static
                        bool added = Stack.TryAddStaticVariable(symbol, out newVariable);
                        if (added)
                            lastScope.StaticScopeVariables.Add(symbol.Name, symbol);
                    }
                }
                else // Actually declaring a new local 
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
                if (IsVariableCapturedFromParentScope(symbol))
                {
                    return AddCapturedVariableFromParentScope(symbol);
                }

                if (isStatic || !Stack.HasLocalVariable(symbol))
                {
                    bool added = Stack.TryAddStaticVariable(symbol, out newVariable);
                    if (added)
                        lastScope.StaticScopeVariables.TryAdd(symbol.Name, symbol);
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

        /// <summary>
        /// Checks whether a certain symbol is a captured variable.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        private bool IsVariableCapturedFromParentScope(AdhocSymbol symbol)
        {
            if (!ContextAllowsVariableCaptureFromParentFrame)
                return false;

            if (ParentFrame is null) // No parent? Not capturing anything
                return false;

            if (!ParentFrame.Stack.HasLocalVariable(symbol)) // Does the symbol exist in parent? If not, not capturing anything
                return false;

            // Check conflicts with other locals or params
            return (!Stack.HasLocalVariable(symbol) && !FunctionParameters.Contains(symbol) || CapturedCallbackVariables.Contains(symbol));
        }


        /// <summary>
        /// Adds a captured symbol to the current frame.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="newVariable"></param>
        /// <returns>Returns stack index of captured variable</returns>
        private int AddCapturedVariableFromParentScope(AdhocSymbol symbol)
        {
            if (!CapturedCallbackVariables.Contains(symbol))
            {
                CapturedCallbackVariables.Add(symbol);

                // Add captured variable to current frame
                Stack.TryAddLocalVariable(symbol, out _);
            }

            // Captured variables have backward indices
            return -(CapturedCallbackVariables.IndexOf(symbol) + 1); // 0 -> -1, 1 -> -2 etc
        }

        public bool IsStaticModuleFieldOrAttribute(AdhocSymbol symbol, AdhocModule relativeTo)
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

            return HasStatic(symbol, relativeTo);
        }

        public void AddAttributeOrStaticMemberVariable(AdhocSymbol symbol)
        {
            var newVar = new StaticVariable() { Symbol = symbol };

            // TODO: Refactor me maybe?
            Stack.AddStaticVariable(newVar);
            CurrentScope.StaticScopeVariables.TryAdd(symbol.Name, symbol);
        }

        /// <summary>
        /// Returns whether a symbol is a static variable for this frame and module context.
        /// </summary>
        /// <param name="symb"></param>
        /// <param name="relativeTo"></param>
        /// <returns></returns>
        public bool IsStaticVariable(AdhocSymbol symb, AdhocModule relativeTo)
        {
            if (symb.Name == AdhocConstants.SELF)
                return false;

            if (Stack.HasLocalVariable(symb))
                return false; // Priorize local variables

            if (IsStaticModuleFieldOrAttribute(symb, relativeTo))
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

        public void Write(AdhocStream stream)
        {
            if (Version >= 8 && Version <= 12)
            {
                stream.WriteBoolean(HasDebuggingInformation);
                stream.WriteByte((byte)Version);
            }

            if (SourceFilePath != null)
                stream.WriteSymbol(SourceFilePath);

            if (Version >= 12)
                stream.WriteBoolean(HasRestElement); // Not sure for Version 14

            if (Version > 3)
            {
                stream.WriteInt32(FunctionParameters.Count);
                for (int i = 0; i < FunctionParameters.Count; i++)
                {
                    stream.WriteSymbol(FunctionParameters[i]);

                    if (Version >= 8)
                        stream.WriteInt32(1 + i); // TODO: Proper Index?
                }
            }

            if (Version >= 8)
            {
                stream.WriteInt32(CapturedCallbackVariables.Count);
                for (int i = 0; i < CapturedCallbackVariables.Count; i++)
                {
                    AdhocSymbol capturedVariable = CapturedCallbackVariables[i];
                    stream.WriteSymbol(capturedVariable);
                    stream.WriteInt32(-(i + 1));
                }

                stream.WriteInt32(0); // Some stack variable index
            }


            if (Version <= 10)
            {
                stream.WriteInt32(Stack.GetLocalVariableStorageSize());
                stream.WriteInt32(Stack.GetStackSize());
            }
            else
            {
                // Actual stack size
                stream.WriteInt32(Stack.GetStackSize());

                /* These two are combined to make the size of the storage for variables */
                stream.WriteInt32(Stack.GetLocalVariableStorageSize());
                stream.WriteInt32(Stack.GetStaticVariableStorageSize());
            }

            stream.WriteInt32(Instructions.Count);
            foreach (var instruction in Instructions)
            {
                stream.WriteUInt32(instruction.LineNumber);
                stream.WriteByte((byte)instruction.InstructionType);
                instruction.Serialize(stream);
            }
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
                        FunctionParameters.Add(stream.ReadSymbol());
                }
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

                uint unkVarStackIndex = stream.ReadUInt32();
            }

            if (Version <= 10)
            {
                var stackOld = Stack as AdhocStackOld;
                stackOld.VariableStorageSize = stream.ReadInt32();
                stackOld.StackSize = stream.ReadInt32();
            }
            else
            {
                var stack = Stack as AdhocStack;
                stack.StackSize = stream.ReadInt32();
                stack.LocalVariableStorageSize = stream.ReadInt32();
                stack.StaticVariableStorageSize = stream.ReadInt32();
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

        public string Dissasemble(bool asCompareMode = false)
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

                if (HasRestElement)
                    sb.Append("...");
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

            sb.AppendLine();

            sb.Append("  > Instruction Count: ").Append(Instructions.Count);
            if (!asCompareMode)
                sb.Append(" (").Append(InstructionCountOffset.ToString("X2")).Append(')');
            sb.AppendLine();

            sb.Append($"  > Stack Size: {Stack.GetStackSize()} - Variable Heap Size: {Stack.GetLocalVariableStorageSize()} - Variable Heap Size Static: {(Version <= 10 ? "=Variable Heap Size" : $"{Stack.GetStaticVariableStorageSize()}")}");

            return sb.ToString();
        }
    }
}
