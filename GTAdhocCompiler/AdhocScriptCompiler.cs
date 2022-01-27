
using Esprima;
using Esprima.Ast;

using GTAdhocCompiler.Instructions;
using GTAdhocCompiler.Project;
using GTAdhocCompiler.Variables;

namespace GTAdhocCompiler
{
    /// <summary>
    /// Adhoc script compiler.
    /// </summary>
    public class AdhocScriptCompiler : AdhocCodeFrame
    {
        public AdhocSymbolMap SymbolMap { get; set; } = new();

        public Stack<AdhocModule> Modules { get; set; } = new();

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public int Version { get; set; }

        public AdhocScriptCompiler()
        {
            var topLevelModule = new AdhocModule();
            Modules.Push(topLevelModule); // Top Level Module
            this.CurrentModule = topLevelModule;
        }

        public string ProjectDirectory { get; set; }
        public string BaseDirectory { get; set; }

        public void SetProjectDirectory(string dir)
        {
            ProjectDirectory = dir;
        }

        /// <summary>
        /// Compiles a script.
        /// </summary>
        /// <param name="script"></param>
        public void CompileScript(Script script)
        {
            Logger.Info("Started script compilation.");

            EnterScope(this, script);
            CompileScriptBody(this, script);
            LeaveScope(this);

            // Script done.
            this.AddInstruction(new InsSetState(AdhocRunState.EXIT), 0);

            Logger.Info($"Script successfully compiled.");
        }

        /// <summary>
        /// Compiles a script body into a frame.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="script"></param>
        public void CompileScriptBody(AdhocCodeFrame frame, Script script)
        {
            CompileStatements(frame, script.Body);
        }

        public void CompileStatements(AdhocCodeFrame frame, Node node)
        {
            foreach (var n in node.ChildNodes)
                CompileStatement(frame, n);
        }

        public void CompileStatements(AdhocCodeFrame frame, NodeList<Statement> nodes)
        {
            foreach (var n in nodes)
                CompileStatement(frame, n);
        }

        public void CompileStatement(AdhocCodeFrame frame, Node node)
        {
            switch (node.Type)
            {
                case Nodes.ClassDeclaration:
                    CompileClassDeclaration(frame, node as ClassDeclaration);
                    break;
                case Nodes.FunctionDeclaration:
                    CompileFunctionDeclaration(frame, node as FunctionDeclaration);
                    break;
                case Nodes.MethodDeclaration:
                    CompileMethodDeclaration(frame, node as MethodDeclaration);
                    break;
                case Nodes.ForStatement:
                    CompileFor(frame, node as ForStatement);
                    break;
                case Nodes.ForeachStatement:
                    CompileForeach(frame, node as ForeachStatement);
                    break;
                case Nodes.WhileStatement:
                    CompileWhile(frame, node as WhileStatement);
                    break;
                case Nodes.DoWhileStatement:
                    CompileDoWhile(frame, node as DoWhileStatement);
                    break;
                case Nodes.VariableDeclaration:
                    CompileVariableDeclaration(frame, node as VariableDeclaration);
                    break;
                case Nodes.ReturnStatement:
                    CompileReturnStatement(frame, node as ReturnStatement);
                    break;
                case Nodes.ImportDeclaration:
                    CompileImport(frame, node as ImportDeclaration);
                    break;
                case Nodes.IfStatement:
                    CompileIfStatement(frame, node as IfStatement);
                    break;
                case Nodes.BlockStatement:
                    CompileBlockStatement(frame, node as BlockStatement);
                    break;
                case Nodes.ExpressionStatement:
                    CompileExpressionStatement(frame, node as ExpressionStatement);
                    break;
                case Nodes.SwitchStatement:
                    CompileSwitch(frame, node as SwitchStatement);
                    break;
                case Nodes.ContinueStatement:
                    CompileContinue(frame, node as ContinueStatement);
                    break;
                case Nodes.BreakStatement:
                    CompileBreak(frame, node as BreakStatement);
                    break;
                case Nodes.IncludeStatement:
                    CompileIncludeStatement(frame, node as IncludeStatement);
                    break;
                case Nodes.RequireStatement:
                    CompileRequireStatement(frame, node as RequireStatement);
                    break;
                case Nodes.ThrowStatement:
                    CompileThrowStatement(frame, node as ThrowStatement);
                    break;
                case Nodes.FinalizerStatement:
                    CompileFinalizerStatement(frame, node as FinalizerStatement);
                    break;
                case Nodes.TryStatement:
                    CompileTryStatement(frame, node as TryStatement);
                    break;
                case Nodes.UndefStatement:
                    CompileUndefStatement(frame, node as UndefStatement);
                    break;
                case Nodes.EmptyStatement:
                    break;
                default:
                    ThrowCompilationError(node, "Statement not supported");
                    break;
            }
        }

        public void CompileUndefStatement(AdhocCodeFrame frame, UndefStatement undefStatement)
        {
            InsUndef undefIns = new InsUndef();
            var parts = undefStatement.Symbol.Split("::");

            if (parts.Length > 1)
            {
                foreach (string part in undefStatement.Symbol.Split("::"))
                    undefIns.Symbols.Add(SymbolMap.RegisterSymbol(part));
            }

            undefIns.Symbols.Add(SymbolMap.RegisterSymbol(undefStatement.Symbol)); // full

            frame.AddInstruction(undefIns, undefStatement.Location.Start.Line);
        }

        public void CompileTryStatement(AdhocCodeFrame frame, TryStatement tryStatement)
        {
            InsTryCatch tryCatch = new InsTryCatch();
            frame.AddInstruction(tryCatch, tryStatement.Location.End.Line);

            if (tryStatement.Block.Type != Nodes.BlockStatement)
                ThrowCompilationError(tryStatement.Block, "Expected try body to be a block statement.");

            CompileBlockStatement(frame, tryStatement.Block as BlockStatement);
            frame.AddInstruction(new InsSetState(AdhocRunState.EXIT), 0);

            tryCatch.InstructionIndex = frame.GetLastInstructionIndex();

            if (tryStatement.Handler is not null)
            {
                InsJump catchClauseSkipper = new InsJump();
                frame.AddInstruction(catchClauseSkipper, 0);

                CompileCatchClause(frame, tryStatement.Handler);
                catchClauseSkipper.JumpInstructionIndex = frame.GetLastInstructionIndex();
            }

            if (tryStatement.Finalizer != null)
            {
                if (tryStatement.Finalizer.Type != Nodes.BlockStatement)
                    ThrowCompilationError(tryStatement.Block, "Expected catch clause to be a block statement.");

                CompileStatementWithScope(frame, tryStatement.Finalizer);
            }
        }

        public void CompileCatchClause(AdhocCodeFrame frame, CatchClause catchClause)
        {
            if (catchClause.Param is not null)
            {
                if (catchClause.Param.Type != Nodes.Identifier)
                    ThrowCompilationError(catchClause.Param, "Expected catch clause parameter to be an identifier.");

                // Create temp variable for the exception
                frame.AddInstruction(new InsIntConst(0), 0);
                InsertVariablePush(frame, catchClause.Param as Identifier, true);
                frame.AddInstruction(InsAssign.Default, 0);

                string tmpCaseVariable = $"catch#{SymbolMap.TempVariableCounter++}";
                InsertVariablePush(frame, new Identifier(tmpCaseVariable), true);
                InsertAssignPop(frame);
            }

            CompileBlockStatement(frame, catchClause.Body);
        }

        /// <summary>
        /// Compiles a new frame/scope containing statements.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="BlockStatement"></param>
        /// <param name="insertLeaveInstruction">Whether to compile a leave scope, which isnt needed for function returns.</param>
        public void CompileBlockStatement(AdhocCodeFrame frame, BlockStatement BlockStatement, bool openScope = true, bool insertLeaveInstruction = true)
        {
            if (openScope)
                EnterScope(frame, BlockStatement);

            CompileStatements(frame, BlockStatement.Body);

            LeaveScope(frame, insertLeaveInstruction && BlockStatement.Body.Count > 0);
        }

        public void CompileIncludeStatement(AdhocCodeFrame frame, IncludeStatement include)
        {
            if (string.IsNullOrEmpty(ProjectDirectory))
                ProjectDirectory = Path.GetDirectoryName(frame.SourceFilePath.Name);

            string pathToIncludeFile = Path.Combine(ProjectDirectory, include.Path);
            if (!File.Exists(pathToIncludeFile))
                ThrowCompilationError(include, $"Include file does not exist: {pathToIncludeFile}.");

            Logger.Info($"Linking include file {include.Path} for {frame.SourceFilePath}.");

            string file = File.ReadAllText(pathToIncludeFile);

            var parser = new AdhocAbstractSyntaxTree(file);
            Script includeScript = parser.ParseScript();

            // Alert interpreter that the current source file has changed for debugging
            InsSourceFile srcFileIns = new InsSourceFile(SymbolMap.RegisterSymbol(include.Path, false));
            frame.AddInstruction(srcFileIns, include.Location.Start.Line);

            // Copy include into current frame
            CompileScriptBody(frame, includeScript);

            // Resume
            InsSourceFile ogSrcFileIns = new InsSourceFile(frame.SourceFilePath);
            frame.AddInstruction(ogSrcFileIns, include.Location.Start.Line);
        }

        public void CompileRequireStatement(AdhocCodeFrame frame, RequireStatement require)
        {
            CompileExpression(frame, require.Path);
            frame.AddInstruction(InsRequire.Default, require.Location.Start.Line);
        }

        public void CompileThrowStatement(AdhocCodeFrame frame, ThrowStatement throwStatement)
        {
            CompileExpression(frame, throwStatement.Argument);
            frame.AddInstruction(InsThrow.Default, throwStatement.Location.Start.Line);
        }

        public void CompileBreak(AdhocCodeFrame frame, BreakStatement breakStatement)
        {
            var scope = frame.GetLastBreakControlledScope();
            if (scope is LoopContext loopCtx)
            {
                InsJump breakJmp = new InsJump();
                loopCtx.BreakJumps.Add(breakJmp);
                frame.Instructions.Add(breakJmp);
            }
            else if (scope is SwitchContext swContext)
            {
                InsJump breakJmp = new InsJump();
                swContext.BreakJumps.Add(breakJmp);
                frame.Instructions.Add(breakJmp);
            }
            else
            {
                ThrowCompilationError(breakStatement, "Expected break statement to be in a loop or switch frame.");
            }
        }

        public void CompileClassDeclaration(AdhocCodeFrame frame, ClassDeclaration classDecl)
        {
            CompileNewClass(frame, classDecl.Id, classDecl.SuperClass, classDecl.Body, classDecl.IsModule);
        }

        private void CompileNewClass(AdhocCodeFrame frame, Identifier id, Node superClass, Statement body, bool isModule = false, bool isStaticModule = false)
        {
            if (id is null || id.Type != Nodes.Identifier)
            {
                ThrowCompilationError(id, "Class or module name must have a valid identifier.");
                return;
            }

            Logger.Debug($"L{id.Location.Start.Line} - Compiling {(isModule ? "module" : "class")} '{id.Name}'");
            AdhocModule moduleOrClass = EnterModuleOrClass(frame, body);

            if (isModule)
            {
                InsModuleDefine mod = new InsModuleDefine();

                if (id.Name.Contains("::"))
                {
                    foreach (string identifier in id.Name.Split("::"))
                        mod.Names.Add(SymbolMap.RegisterSymbol(identifier));
                    mod.Names.Add(SymbolMap.RegisterSymbol(id.Name)); // Full
                }
                else
                {
                    mod.Names.Add(SymbolMap.RegisterSymbol(id.Name));

                    // Static modules mean they belong to strictly one path, so main,main in any module context is absolute
                    if (isStaticModule)
                        mod.Names.Add(SymbolMap.RegisterSymbol(id.Name));
                }

                frame.AddInstruction(mod, id.Location.Start.Line);
                moduleOrClass.Name = id.Name;
            }
            else
            {
                if (id.Name.Contains("::"))
                    ThrowCompilationError(superClass, "Class name must be an identifier without a scope path (::).");

                InsClassDefine @class = new InsClassDefine();
                @class.Name = SymbolMap.RegisterSymbol(id.Name);
                moduleOrClass.Name = id.Name;

                var superClassIdent = superClass as Identifier;
                if (superClassIdent != null)
                {
                    @class.ExtendsFrom.Add(SymbolMap.RegisterSymbol(superClassIdent.Name));
                }
                else if (superClass != null)
                {
                    string fullSuperClassName = "";
                    foreach (var node in superClass.ChildNodes)
                    {
                        var nodeIdent = node as Identifier;
                        if (nodeIdent == null)
                        {
                            ThrowCompilationError(superClass, "Superclasses must be identifiers or module::identifiers.");
                            return;
                        }
                        @class.ExtendsFrom.Add(SymbolMap.RegisterSymbol(nodeIdent.Name));
                        fullSuperClassName += $"{nodeIdent.Name}::";
                    }
                    fullSuperClassName = fullSuperClassName.Remove(fullSuperClassName.Length - 2);
                    @class.ExtendsFrom.Add(SymbolMap.RegisterSymbol(fullSuperClassName));
                }
                frame.AddInstruction(@class, id.Location.Start.Line);
            }

            CompileStatementWithScope(frame, body);

            LeaveModuleOrClass(frame);

            // Exit class or module scope. Important.
            InsSetState state = new InsSetState(AdhocRunState.EXIT);
            frame.AddInstruction(state, 0);
        }

        public void CompileContinue(AdhocCodeFrame frame, ContinueStatement continueStatement)
        {
            if (frame.CurrentLoops.Count == 0)
                ThrowCompilationError(continueStatement, "Got continue keyword without loop.");

            LoopContext loop = frame.GetLastLoop();

            InsJump continueJmp = new InsJump();
            frame.AddInstruction(continueJmp, continueStatement.Location.Start.Line);

            loop.ContinueJumps.Add(continueJmp);
        }

        public void CompileIfStatement(AdhocCodeFrame frame, IfStatement ifStatement)
        {
            EnterScope(frame, ifStatement);

            CompileExpression(frame, ifStatement.Test); // if (<test>)

            // Create jump
            InsJumpIfFalse endOrNextIfJump = new InsJumpIfFalse();
            frame.AddInstruction(endOrNextIfJump, 0);

            // Apply frame
            CompileStatementWithScope(frame, ifStatement.Consequent); // if body

            endOrNextIfJump.JumpIndex = frame.GetLastInstructionIndex();

            // else if's..
            if (ifStatement.Alternate is not null)
            {
                // Jump to skip the else if frame if the if was already taken
                InsJump skipAlternateJmp = new InsJump();
                frame.AddInstruction(skipAlternateJmp, 0);

                endOrNextIfJump.JumpIndex = frame.GetLastInstructionIndex();

                CompileStatementWithScope(frame, ifStatement.Alternate);

                skipAlternateJmp.JumpInstructionIndex = frame.GetLastInstructionIndex();
            }
            else
            {
                endOrNextIfJump.JumpIndex = frame.GetLastInstructionIndex();
            }

            LeaveScope(frame, insertLeaveInstruction: false);
        }

        public void CompileFor(AdhocCodeFrame frame, ForStatement forStatement)
        {
            LoopContext loopCtx = EnterLoop(frame, forStatement);

            // Initialization
            if (forStatement.Init is not null)
            {
                switch (forStatement.Init.Type)
                {
                    case Nodes.VariableDeclaration:
                        CompileVariableDeclaration(frame, forStatement.Init as VariableDeclaration); break;
                    case Nodes.AssignmentExpression:
                        CompileAssignmentExpression(frame, forStatement.Init as AssignmentExpression); break;
                    case Nodes.Identifier:
                        CompileIdentifier(frame, forStatement.Init as Identifier); break;
                    default:
                        ThrowCompilationError(forStatement.Init, "Unsupported for statement init type");
                        break;
                }
            }

            int startIndex = frame.GetLastInstructionIndex();

            // Condition
            InsJumpIfFalse jumpIfFalse = null; // will only be inserted if the condition exists, else its essentially a while true loop
            if (forStatement.Test != null)
            {
                CompileExpression(frame, forStatement.Test);

                // Insert jump to the end of loop frame
                jumpIfFalse = new InsJumpIfFalse();
                frame.AddInstruction(jumpIfFalse, 0);
            }

            CompileStatement(frame, forStatement.Body);

            // Reached bottom, proceed to do update
            // But first, process continue if any
            int loopUpdInsIndex = frame.GetLastInstructionIndex();
            foreach (var continueJmp in loopCtx.ContinueJumps)
                continueJmp.JumpInstructionIndex = loopUpdInsIndex;

            // Update Counter
            if (forStatement.Update != null)
            {
                if (forStatement.Update is UpdateExpression)
                    CompileUnaryExpression(frame, forStatement.Update as UpdateExpression, popResult: true);
                else
                    CompileExpression(frame, forStatement.Update);
            }

            // Insert jump to go back to the beginning of the loop
            InsJump startJump = new InsJump();
            startJump.JumpInstructionIndex = startIndex;
            frame.AddInstruction(startJump, 0);

            // Update jump that exits the loop if it exists
            int loopExitInsIndex = frame.GetLastInstructionIndex();
            if (jumpIfFalse != null)
                jumpIfFalse.JumpIndex = loopExitInsIndex;

            // Process break jumps before doing the final exit
            foreach (var breakJmp in loopCtx.BreakJumps)
                breakJmp.JumpInstructionIndex = loopExitInsIndex;

            // Insert final leave
            LeaveLoop(frame);
        }

        public void CompileWhile(AdhocCodeFrame frame, WhileStatement whileStatement)
        {
            LoopContext loopCtx = EnterLoop(frame, whileStatement);

            int loopStartInsIndex = frame.GetLastInstructionIndex();

            InsJumpIfFalse jumpIfFalse = new InsJumpIfFalse(); // End loop jumper

            if (whileStatement.Test is not null)
            {
                Literal literal = whileStatement.Test as Literal;
                if (literal is null || literal.TokenType != TokenType.BooleanLiteral || (literal.Value as bool?) == false)
                {
                    CompileExpression(frame, whileStatement.Test);
                    frame.AddInstruction(jumpIfFalse, 0);
                }
            }
                

            CompileStatementWithScope(frame, whileStatement.Body);

            // Insert jump to go back to the beginning of the loop
            InsJump startJump = new InsJump();
            startJump.JumpInstructionIndex = loopStartInsIndex;
            frame.AddInstruction(startJump, 0);

            // Reached bottom, proceed to do update
            // But first, process continue if any
            foreach (var continueJmp in loopCtx.ContinueJumps)
                continueJmp.JumpInstructionIndex = loopStartInsIndex;

            // Update jump that exits the loop
            int loopExitInsIndex = frame.GetLastInstructionIndex();
            jumpIfFalse.JumpIndex = loopExitInsIndex;

            // Process break jumps before doing the final exit
            foreach (var breakJmp in loopCtx.BreakJumps)
                breakJmp.JumpInstructionIndex = loopExitInsIndex;

            LeaveLoop(frame);
        }

        public void CompileDoWhile(AdhocCodeFrame frame, DoWhileStatement doWhileStatement)
        {
            LoopContext loopCtx = EnterLoop(frame, doWhileStatement);

            int loopStartInsIndex = frame.GetLastInstructionIndex();

            CompileStatementWithScope(frame, doWhileStatement.Body);

            int testInsIndex = frame.GetLastInstructionIndex();
            CompileExpression(frame, doWhileStatement.Test);

            // Reached bottom, proceed to do update
            // But first, process continue if any
            foreach (var continueJmp in loopCtx.ContinueJumps)
                continueJmp.JumpInstructionIndex = testInsIndex;

            InsJumpIfFalse jumpIfFalse = new InsJumpIfFalse(); // End loop jumper
            frame.AddInstruction(jumpIfFalse, 0);

            InsJump startJmp = new InsJump();
            startJmp.JumpInstructionIndex = loopStartInsIndex;
            frame.AddInstruction(startJmp, 0);

            // Process break jumps before doing the final exit
            int loopEndIndex = frame.GetLastInstructionIndex();
            foreach (var breakJmp in loopCtx.BreakJumps)
                breakJmp.JumpInstructionIndex = loopEndIndex;

            jumpIfFalse.JumpIndex = loopEndIndex;

            LeaveLoop(frame);
        }

        public void CompileForeach(AdhocCodeFrame frame, ForeachStatement foreachStatement)
        {
            LoopContext loopCtx = EnterLoop(frame, foreachStatement);

            CompileExpression(frame, foreachStatement.Right);

            // Access object iterator
            InsAttributeEvaluation attrIns = new InsAttributeEvaluation();
            attrIns.AttributeSymbols.Add(SymbolMap.RegisterSymbol("iterator"));
            frame.AddInstruction(attrIns, foreachStatement.Right.Location.Start.Line);

            // Assign it to a temporary value for the iteration
            string itorVariable = $"in#{SymbolMap.TempVariableCounter++}";
            Identifier itorIdentifier = new Identifier(itorVariable)
            {
                Location = foreachStatement.Right.Location,
            };

            InsertVariablePush(frame, itorIdentifier, true);
            InsertAssignPop(frame);

            // Test - fetch_next returns whether the iterator is done or not
            int testInsIndex = frame.GetLastInstructionIndex();
            InsertVariableEval(frame, itorIdentifier);
            InsAttributeEvaluation fetchNextIns = new InsAttributeEvaluation();
            fetchNextIns.AttributeSymbols.Add(SymbolMap.RegisterSymbol("fetch_next"));
            frame.AddInstruction(fetchNextIns, foreachStatement.Right.Location.Start.Line);

            InsJumpIfFalse exitJump = new InsJumpIfFalse(); // End loop jumper
            frame.AddInstruction(exitJump, 0);

            // Entering body, but we need to get the iterator's value into our declared variable, equivalent to *iterator
            InsertVariableEval(frame, itorIdentifier);
            frame.AddInstruction(InsEval.Default, 0);

            if (foreachStatement.Left is not VariableDeclaration)
                ThrowCompilationError(foreachStatement, "Expected foreach to have a variable declaration.");

            CompileVariableDeclaration(frame, foreachStatement.Left as VariableDeclaration, pushWhenNoInit: true); // We're unboxing, gotta push anyway

            // Compile body.
            CompileStatementWithScope(frame, foreachStatement.Body);

            // continue's...
            foreach (var continueJmp in loopCtx.ContinueJumps)
                continueJmp.JumpInstructionIndex = frame.GetLastInstructionIndex(); // To the jump that jumps to the test back

            // Add the jump back to the test
            InsJump beginJump = new InsJump();
            beginJump.JumpInstructionIndex = testInsIndex;
            frame.AddInstruction(beginJump, 0);

            // Main exit...
            int loopExitIndex = frame.GetLastInstructionIndex();
            exitJump.JumpIndex = loopExitIndex;

            // break's...
            foreach (var breakJmp in loopCtx.BreakJumps)
                breakJmp.JumpInstructionIndex = loopExitIndex;

            LeaveLoop(frame);
        }

        public void CompileSwitch(AdhocCodeFrame frame, SwitchStatement switchStatement)
        {
            CompileExpression(frame, switchStatement.Discriminant); // switch (type)
            SwitchContext switchCtx = EnterSwitch(frame, switchStatement);

            // Create a label for the temporary switch variable
            string tmpCaseVariable = $"case#{SymbolMap.TempVariableCounter++}";
            AdhocSymbol labelSymb = InsertVariablePush(frame, new Identifier(tmpCaseVariable), true);
            InsertAssignPop(frame);

            LocalVariable caseVariable = frame.Stack.GetLocalVariableBySymbol(labelSymb);
            int caseVariableStoreIdx = frame.Stack.GetGlobalVariableIndex(caseVariable);

            Dictionary<SwitchCase, InsJumpIfTrue> caseBodyJumps = new();
            InsJump defaultJump = null;
            bool hasDefault = false;

            // Write switch table jumps
            for (int i = 0; i < switchStatement.Cases.Count; i++)
            {
                SwitchCase swCase = switchStatement.Cases[i];
                if (swCase.Test != null) // Actual case
                {
                    // Get temp variable
                    InsVariableEvaluation tempVar = new InsVariableEvaluation();
                    tempVar.VariableSymbols.Add(labelSymb);
                    tempVar.VariableStorageIndex = caseVariableStoreIdx;
                    frame.AddInstruction(tempVar, swCase.Location.Start.Line);

                    // Write what we are comparing to 
                    CompileExpression(frame, swCase.Test);

                    // Equal check
                    InsBinaryOperator eqOp = new InsBinaryOperator(SymbolMap.RegisterSymbol("=="));
                    frame.AddInstruction(eqOp, swCase.Location.Start.Line);

                    // Write the jump
                    InsJumpIfTrue jit = new InsJumpIfTrue();
                    caseBodyJumps.Add(swCase, jit); // To write the instruction index later on
                    frame.AddInstruction(jit, 0);
                }
                else // Default
                {
                    if (hasDefault)
                        ThrowCompilationError(swCase, "Switch already has a default case.");

                    hasDefault = true;
                    defaultJump = new InsJump();
                    frame.AddInstruction(defaultJump, swCase.Location.Start.Line);
                }
            }

            // Was a default case statement specified?
            if (!hasDefault)
            {
                // Switch statement does not have an explicit default case, add a default jump
                defaultJump = new InsJump();
                frame.AddInstruction(defaultJump, 0);
            }

            // Write bodies
            for (int i = 0; i < switchStatement.Cases.Count; i++)
            {
                SwitchCase swCase = switchStatement.Cases[i];

                // Update body jump location
                if (swCase.Test != null)
                    caseBodyJumps[swCase].JumpIndex = frame.GetLastInstructionIndex();
                else
                    defaultJump.JumpInstructionIndex = frame.GetLastInstructionIndex();

                // Not counting as scopes
                foreach (var statement in swCase.Consequent)
                    CompileStatement(frame, statement);
            }

            // Update non explicit default case to jump to end
            if (!hasDefault)
                defaultJump.JumpInstructionIndex = frame.GetLastInstructionIndex();

            // Update break case jumps
            for (int i = 0; i < switchCtx.BreakJumps.Count; i++)
            {
                InsJump swCase = switchCtx.BreakJumps[i];
                swCase.JumpInstructionIndex = frame.GetLastInstructionIndex();
            }

            // Leave switch frame.
            LeaveScope(frame);
        }

        public void CompileFunctionDeclaration(AdhocCodeFrame frame, FunctionDeclaration funcDecl)
        {
            if (funcDecl.Id is not null)
                CompileSubroutine(frame, funcDecl, funcDecl.Body, funcDecl.Id, funcDecl.Params, isMethod: false);
        }

        public void CompileSubroutine(AdhocCodeFrame frame, Node parentNode, Node body, Identifier id, NodeList<Expression> subParams, bool isMethod)
        {
            if (id is null)
                ThrowCompilationError(parentNode, "Expected subroutine to have an identifier.");

            Logger.Debug($"L{parentNode.Location.Start.Line} - Compiling subroutine '{id.Name}'");

            SubroutineBase subroutine = isMethod ? new InsMethodDefine() : new InsFunctionDefine();
            if (id is not null)
                subroutine.Name = SymbolMap.RegisterSymbol(id.Name);
            subroutine.CodeFrame.SourceFilePath = frame.SourceFilePath;
            subroutine.CodeFrame.ParentFrame = this;
            subroutine.CodeFrame.CurrentModule = frame.CurrentModule;

            foreach (Expression param in subParams)
            {
                if (param.Type == Nodes.Identifier)
                {
                    var paramIdent = param as Identifier;

                    AdhocSymbol paramSymb = SymbolMap.RegisterSymbol(paramIdent.Name);
                    subroutine.CodeFrame.FunctionParameters.Add(paramSymb);

                    // Function param is uninitialized, push nil
                    frame.AddInstruction(InsNilConst.Empty, paramIdent.Location.Start.Line);
                    
                }
                else if (param is AssignmentExpression assignmentExpression)
                {
                    if (assignmentExpression.Left.Type != Nodes.Identifier || assignmentExpression.Right.Type != Nodes.Literal)
                        ThrowCompilationError(parentNode, "Subroutine parameter assignment must be an identifier to a literal. (value = 0)");

                    AdhocSymbol paramSymb = SymbolMap.RegisterSymbol((assignmentExpression.Left as Identifier).Name);
                    subroutine.CodeFrame.FunctionParameters.Add(paramSymb);

                    // Push default value
                    CompileLiteral(frame, assignmentExpression.Right as Literal);
                }
                else if (param.Type == Nodes.AssignmentPattern)
                {
                    var pattern = param as AssignmentPattern;

                    if (pattern.Right.Type != Nodes.Literal && pattern.Right.Type != Nodes.ArrayExpression && pattern.Right.Type != Nodes.MapExpression)
                        ThrowCompilationError(parentNode, "Subroutine parameter assignment must be an identifier to a literal.");

                    AdhocSymbol paramSymb = SymbolMap.RegisterSymbol((pattern.Left as Identifier).Name);
                    subroutine.CodeFrame.FunctionParameters.Add(paramSymb);

                    // Push default value
                    CompileExpression(frame, pattern.Right);
                }
                else
                    ThrowCompilationError(parentNode, "Subroutine definition parameters must all be identifier or assignment to a literal.");
            }

            frame.AddAttributeOrStaticMemberVariable(subroutine.Name);
            frame.AddInstruction(subroutine, parentNode.Location.Start.Line);

            if (body is BlockStatement blockStatement)
            {
                EnterScope(subroutine.CodeFrame, parentNode);
                foreach (var param in subroutine.CodeFrame.FunctionParameters)
                    subroutine.CodeFrame.AddScopeVariable(param, isAssignment: true, isLocalDeclaration: true);
                
                CompileBlockStatement(subroutine.CodeFrame, blockStatement, openScope: false, insertLeaveInstruction: false);
            }
            else
                ThrowCompilationError(body, "Expected subroutine body to be frame statement.");

            InsertFrameExitIfNeeded(subroutine.CodeFrame, body);

            Logger.Debug($"Subroutine '{id.Name}' compiled ({subroutine.CodeFrame.Instructions.Count} ins, " +
                $"Stack Size:{subroutine.CodeFrame.Stack.StackSize}, variable Storage Size: {subroutine.CodeFrame.Stack.LocalVariableStorageSize})");
        }

        /// <summary>
        /// Compiles: 'return <expression>;' .
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="retStatement"></param>
        public void CompileReturnStatement(AdhocCodeFrame frame, ReturnStatement retStatement)
        {
            if (retStatement.Argument is not null) // Return has argument?
            {
                CompileExpression(frame, retStatement.Argument);
                if (retStatement.Argument is AssignmentExpression assignmentExpr)
                    CompileExpression(frame, assignmentExpr.Left); // If we are returning an assignment i.e return <variable or path> += "hi", we need to eval str again
            }
            else
            {
                // Void const is returned
                frame.AddInstruction(InsVoidConst.Empty, retStatement.Location.Start.Line);
            }

            frame.AddInstruction(new InsSetState(AdhocRunState.RETURN), 0);

            // Top level of frame?
            if (frame.IsTopLevel)
                frame.HasTopLevelReturnValue = true;
        }

        public void CompileVariableDeclaration(AdhocCodeFrame frame, VariableDeclaration varDeclaration, bool pushWhenNoInit = false)
        {
            NodeList<VariableDeclarator> declarators = varDeclaration.Declarations;
            VariableDeclarator declarator = declarators[0];

            Expression? initValue = declarator.Init;
            Expression? id = declarator.Id;

            // We need to add the defined value first

            if (initValue != null)
            {
                if (initValue.Type == Nodes.UpdateExpression)
                    CompileUnaryExpression(frame, initValue as UpdateExpression, popResult: true);
                else
                    CompileExpression(frame, initValue);
            }
                

            // Now write the id
            if (id is null)
                ThrowCompilationError(varDeclaration, "Variable declaration for id is null.");

            if (id is Identifier idIdentifier) // var hello [= world];
            {
                if (initValue != null || pushWhenNoInit)
                {
                    // Variable is being defined with a value.
                    InsertVariablePush(frame, idIdentifier, true);

                    // Perform assignment
                    InsertAssignPop(frame);
                }
                else
                {
                    // Variable is declared but not assigned to anything yet. Do not add any variable push.
                    AdhocSymbol varSymb = SymbolMap.RegisterSymbol(idIdentifier.Name);
                    frame.AddScopeVariable(varSymb, isAssignment: true, isLocalDeclaration: true);
                }
            }
            else if (id is ArrayPattern arrayPattern) // var [hello, world] = helloworld; - deconstruct array
            {
                CompileArrayPatternPush(frame, arrayPattern);
            }
            else
            {
                ThrowCompilationError(varDeclaration, "Variable declaration for id is not an identifier.");
            }
        }

        private void CompileArrayPatternPush(AdhocCodeFrame frame, ArrayPattern arrayPattern)
        {
            if (arrayPattern.Elements.Count == 0)
                ThrowCompilationError(arrayPattern, "Array pattern has no elements.");

            foreach (Expression exp in arrayPattern.Elements)
            {
                if (exp.Type != Nodes.Identifier)
                    ThrowCompilationError(exp, "Expected array element to be identifier.");

                Identifier arrElemIdentifier = exp as Identifier;
                InsertVariablePush(frame, arrElemIdentifier, true);
            }

            InsListAssign listAssign = new InsListAssign(arrayPattern.Elements.Count);
            frame.AddInstruction(listAssign, arrayPattern.Location.Start.Line);
            frame.AddInstruction(InsPop.Default, 0); // Pop array from stack 
        }

        /// <summary>
        /// Compiles an import declaration. 'import main::*'
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="import"></param>
        public void CompileImport(AdhocCodeFrame frame, ImportDeclaration import)
        {
            if (import.Specifiers.Count == 0)
            {
                ThrowCompilationError(import, "Import declaration is empty.");
            }

            string fullImportNamespace = "";
            string target = "";

            InsImport importIns = new InsImport();

            for (int i = 0; i < import.Specifiers.Count; i++)
            {
                ImportDeclarationSpecifier specifier = import.Specifiers[i];
                if (i == import.Specifiers.Count - 1)
                {
                    target = specifier.Local.Name;
                    break;
                }

                AdhocSymbol part = SymbolMap.RegisterSymbol(specifier.Local.Name);
                importIns.ImportNamespaceParts.Add(part);
                fullImportNamespace += specifier.Local.Name;

                if (i < import.Specifiers.Count - 1 && import.Specifiers[i + 1].Local.Name != "*")
                    fullImportNamespace += "::";
            }

            AdhocSymbol namespaceSymbol = SymbolMap.RegisterSymbol(fullImportNamespace);
            AdhocSymbol targetNamespace = SymbolMap.RegisterSymbol(target);
            AdhocSymbol nilSymbol = SymbolMap.RegisterSymbol("nil");

            importIns.ImportNamespaceParts.Add(namespaceSymbol);
            importIns.Target = targetNamespace;

            frame.AddInstruction(importIns, import.Location.Start.Line);
        }

        private void CompileExpression(AdhocCodeFrame frame, Expression exp)
        {
            switch (exp.Type)
            {
                
                case Nodes.Identifier:
                    CompileIdentifier(frame, exp as Identifier);
                    break;
                case Nodes.FunctionExpression:
                    CompileFunctionExpression(frame, exp as FunctionExpression);
                    break;
                case Nodes.MethodExpression:
                    CompileMethodExpression(frame, exp as MethodExpression);
                    break;
                case Nodes.CallExpression:
                    CompileCall(frame, exp as CallExpression);
                    break;
                case Nodes.UnaryExpression:
                case Nodes.UpdateExpression:
                    CompileUnaryExpression(frame, exp as UnaryExpression);
                    break;
                case Nodes.BinaryExpression:
                case Nodes.LogicalExpression:
                    CompileBinaryExpression(frame, exp as BinaryExpression);
                    break;
                case Nodes.Literal:
                    CompileLiteral(frame, exp as Literal);
                    break;
                case Nodes.ArrayExpression:
                    CompileArrayExpression(frame, exp as ArrayExpression);
                    break;
                case Nodes.MapExpression:
                    CompileMapExpression(frame, exp as MapExpression);
                    break;
                case Nodes.MemberExpression when exp is ComputedMemberExpression:
                    CompileComputedMemberExpression(frame, exp as ComputedMemberExpression);
                    break;
                case Nodes.MemberExpression when exp is StaticMemberExpression:
                    CompileStaticMemberExpression(frame, exp as StaticMemberExpression);
                    break;
                case Nodes.MemberExpression when exp is AttributeMemberExpression:
                    CompileAttributeMemberExpression(frame, exp as AttributeMemberExpression);
                    break;
                case Nodes.AssignmentExpression:
                    CompileAssignmentExpression(frame, exp as AssignmentExpression);
                    break;
                case Nodes.ConditionalExpression:
                    CompileConditionalExpression(frame, exp as ConditionalExpression);
                    break;
                case Nodes.TemplateLiteral:
                    CompileTemplateLiteral(frame, exp as TemplateLiteral);
                    break;
                case Nodes.TaggedTemplateExpression:
                    CompileTaggedTemplateExpression(frame, exp as TaggedTemplateExpression);
                    break;
                case Nodes.StaticDeclaration:
                    CompileStaticVariableDefinition(frame, exp as StaticVariableDefinition);
                    break;
                case Nodes.AttributeDeclaration:
                    CompileAttributeDefinition(frame, exp as AttributeVariableDefinition);
                    break;
                case Nodes.ClassExpression:
                    CompileClassExpression(frame, exp as ClassExpression);
                    break;
                case Nodes.ArrowFunctionExpression:
                    CompileArrowFunctionExpression(frame, exp as ArrowFunctionExpression);
                    break;
                case Nodes.ImportDeclaration:
                    CompileImport(frame, (exp as ImportExpression).Declaration);
                    break;
                case Nodes.YieldExpression:
                    CompileYield(frame, exp as YieldExpression);
                    break;
                default:
                    ThrowCompilationError(exp, $"Expression {exp.Type} not supported");
                    break;
            }
        }

        private void CompileArrowFunctionExpression(AdhocCodeFrame frame, ArrowFunctionExpression arrowFuncExpr)
        {
            CompileAnonymousSubroutine(frame, arrowFuncExpr, arrowFuncExpr.Body, arrowFuncExpr.Params);
        }

        private void CompileYield(AdhocCodeFrame frame, YieldExpression yield)
        {
            frame.AddInstruction(InsVoidConst.Empty, yield.Location.Start.Line);
            frame.AddInstruction(new InsSetState(AdhocRunState.YIELD), 0);
        }

        /// <summary>
        /// Compiles: .doThing(e => <statement>)
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="arrowFuncExpr"></param>
        private void CompileAnonymousSubroutine(AdhocCodeFrame frame, Node parentNode, Node body, NodeList<Expression> funcParams, bool isMethod = false)
        {
            SubroutineBase subroutine = isMethod ? new InsMethodConst() : new InsFunctionConst();
            subroutine.CodeFrame.ParentFrame = frame;
            subroutine.CodeFrame.SourceFilePath = frame.SourceFilePath;
            subroutine.CodeFrame.CurrentModule = frame.CurrentModule;

            /* Unlike JS, adhoc can capture variables from the parent frame
             * Example:
             *    var arr = [0, 1, 2];
             *    var map = Map();               
             *    arr.each(e => {
             *        map[e.toString()] = e * 100; -> Inserts a new key/value pair into map, which is from the parent frame
             *    });
             */
            subroutine.CodeFrame.ContextAllowsVariableCaptureFromParentFrame = true;

            EnterScope(subroutine.CodeFrame, parentNode);
            foreach (Expression param in funcParams)
            {
                if (param.Type != Nodes.Identifier)
                    ThrowCompilationError(param, "Expected function parameter to be an identifier.");

                Identifier paramIdent = param as Identifier;
                AdhocSymbol paramSymbol = SymbolMap.RegisterSymbol(paramIdent.Name);
                subroutine.CodeFrame.FunctionParameters.Add(paramSymbol);
                subroutine.CodeFrame.AddScopeVariable(paramSymbol, isAssignment: true, isLocalDeclaration: true);
            }

            if (body.Type == Nodes.BlockStatement)
            {
                CompileStatement(subroutine.CodeFrame, body as BlockStatement);
                InsertFrameExitIfNeeded(subroutine.CodeFrame, body);
            }
            else
            {
                CompileExpression(subroutine.CodeFrame, body as Expression);

                // Add implicit return
                subroutine.CodeFrame.AddInstruction(new InsSetState(AdhocRunState.RETURN), 0);
            }

            LeaveScope(subroutine.CodeFrame, insertLeaveInstruction: false);

            for (int i = 0; i < subroutine.CodeFrame.FunctionParameters.Count; i++)
                frame.AddInstruction(InsNilConst.Empty, 0);

            // "Insert" by evaluating each captured variable
            foreach (var capturedVariable in subroutine.CodeFrame.CapturedCallbackVariables)
                InsertVariableEval(frame, new Identifier(capturedVariable.Name));

            frame.AddInstruction(subroutine, parentNode.Location.Start.Line);
        }

        private void CompileClassExpression(AdhocCodeFrame frame, ClassExpression classExpression)
        {
            CompileNewClass(frame, classExpression.Id, classExpression.SuperClass, classExpression.Body, classExpression.IsModule);
        }

        private void CompileStaticVariableDefinition(AdhocCodeFrame frame, StaticVariableDefinition staticExpression)
        {
            if (staticExpression.VarExpression.Type == Nodes.Identifier)
            {
                var ident = staticExpression.VarExpression as Identifier;

                // static definition with no value
                var idSymb = SymbolMap.RegisterSymbol(ident.Name);
                InsStaticDefine staticDefine = new InsStaticDefine(idSymb);
                frame.AddInstruction(staticDefine, staticExpression.Location.End.Line);

                if (!frame.CurrentModule.DefineStatic(idSymb))
                    ThrowCompilationError(staticExpression, $"Static member {idSymb.Name} was already declared in this module.");

                frame.AddAttributeOrStaticMemberVariable(idSymb);
                
            }
            else if (staticExpression.VarExpression is ClassExpression classExp) // Static Modules/Absolute
            {
                if (!classExp.IsModule)
                    ThrowCompilationError(classExp, "Static class declarations are not supported.");

                CompileNewClass(frame, classExp.Id, classExp.SuperClass, classExp.Body, isModule: true, isStaticModule: true);
            }
            else
            {
                // static definition with value
                if (staticExpression.VarExpression.Type != Nodes.AssignmentExpression)
                    ThrowCompilationError(staticExpression, "Expected static keyword to be a variable assignment.");

                AssignmentExpression assignmentExpression = staticExpression.VarExpression as AssignmentExpression;
                if (assignmentExpression.Left is not Identifier)
                    ThrowCompilationError(assignmentExpression, "Expected static declaration to be an identifier.");

                Identifier identifier = assignmentExpression.Left as Identifier;
                var idSymb = SymbolMap.RegisterSymbol(identifier.Name);

                InsStaticDefine staticDefine = new InsStaticDefine(idSymb);
                frame.AddInstruction(staticDefine, staticExpression.Location.End.Line);

                if (!frame.CurrentModule.DefineStatic(idSymb))
                    ThrowCompilationError(staticExpression, $"Static member {idSymb.Name} was already declared in this module.");

                frame.AddAttributeOrStaticMemberVariable(idSymb);

                if (assignmentExpression.Operator == AssignmentOperator.Assign)
                {
                    // Assigning to something new
                    CompileExpression(frame, assignmentExpression.Right);
                    CompileVariableAssignment(frame, assignmentExpression.Left);
                }
                else if (IsAdhocAssignWithOperandOperator(assignmentExpression.Operator))
                {
                    // Assigning to self (+=)
                    InsertVariablePush(frame, assignmentExpression.Left as Identifier, false); // Push current value first
                    CompileExpression(frame, assignmentExpression.Right);

                    InsertBinaryAssignOperator(frame, assignmentExpression, assignmentExpression.Operator, assignmentExpression.Location.Start.Line);
                    frame.AddInstruction(InsPop.Default, 0);
                }
                else
                {
                    ThrowCompilationError(assignmentExpression, $"Unimplemented operator assignment {assignmentExpression.Operator}");
                }
            }
        }


        private void CompileAttributeDefinition(AdhocCodeFrame frame, AttributeVariableDefinition attrVariableDefinition)
        {
            if (attrVariableDefinition.VarExpression.Type == Nodes.Identifier)
            {
                var ident = attrVariableDefinition.VarExpression as Identifier;

                // attribute definition with no value

                // defaults to nil
                frame.AddInstruction(InsNilConst.Empty, ident.Location.Start.Line);

                var idSymb = SymbolMap.RegisterSymbol(ident.Name);
                InsAttributeDefine staticDefine = new InsAttributeDefine(idSymb);
                frame.AddInstruction(staticDefine, attrVariableDefinition.Location.End.Line);

                if (!frame.CurrentModule.DefineAttribute(idSymb))
                    ThrowCompilationError(attrVariableDefinition, "Attribute is already defined.");

                frame.AddAttributeOrStaticMemberVariable(idSymb);
            }
            else
            {
                if (attrVariableDefinition.VarExpression is not AssignmentExpression)
                    ThrowCompilationError(attrVariableDefinition, "Expected attribute keyword to be a variable assignment.");

                AssignmentExpression assignmentExpression = attrVariableDefinition.VarExpression as AssignmentExpression;
                if (assignmentExpression.Left is not Identifier)
                    ThrowCompilationError(assignmentExpression, "Expected attribute declaration to be an identifier.");

                // Value if any
                CompileExpression(frame, assignmentExpression.Right);

                Identifier identifier = assignmentExpression.Left as Identifier;
                var idSymb = SymbolMap.RegisterSymbol(identifier.Name);

                // Declaring a class attribute, so we don't push anything
                InsAttributeDefine attrDefine = new InsAttributeDefine(idSymb);
                frame.AddInstruction(attrDefine, identifier.Location.Start.Line);

                if (!frame.CurrentModule.DefineAttribute(idSymb))
                    ThrowCompilationError(attrVariableDefinition, "Attribute is already defined.");

                frame.AddAttributeOrStaticMemberVariable(idSymb);
            }
        }

        /// <summary>
        /// Compiles: [] or [<expr>,<expr>,...]
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="arrayExpression"></param>
        private void CompileArrayExpression(AdhocCodeFrame frame, ArrayExpression arrayExpression)
        {
            frame.AddInstruction(new InsArrayConst((uint)arrayExpression.Elements.Count), arrayExpression.Location.Start.Line);

            foreach (var elem in arrayExpression.Elements)
            {
                if (elem is null)
                    ThrowCompilationError(arrayExpression, "Unsupported empty element in array declaration.");

                CompileExpression(frame, elem);

                frame.AddInstruction(InsArrayPush.Default, 0);
            }
        }

        /// <summary>
        /// Compiles: [:] or [k:v, k:v, ...]
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="mapExpression"></param>
        private void CompileMapExpression(AdhocCodeFrame frame, MapExpression mapExpression)
        {
            frame.AddInstruction(InsMapConst.Default, mapExpression.Location.Start.Line);

            foreach (var (key, value) in mapExpression.Elements)
            {
                CompileExpression(frame, key);
                CompileExpression(frame, value);
                frame.AddInstruction(InsMapInsert.Default, 0);
            }
        }

        private void CompileExpressionStatement(AdhocCodeFrame frame, ExpressionStatement expStatement)
        {
            if (expStatement.Expression.Type == Nodes.CallExpression)
            {
                // Call expressions need to be directly popped within an expression statement -> 'getThing()' instead of 'var thing = getThing()'.
                // Their return values aren't used.
                CompileCall(frame, expStatement.Expression as CallExpression, popReturnValue: true);
            }
            else if (expStatement.Expression is UpdateExpression upd) // i++
            {
                // We don't care about the result, so we pop it in this context
                CompileUnaryExpression(frame, upd, popResult: true);
            }
            else
            {
                CompileExpression(frame, expStatement.Expression);
            }
        }

        private void CompileMethodDeclaration(AdhocCodeFrame frame, MethodDeclaration methodDefinition)
        {
            CompileSubroutine(frame, methodDefinition, methodDefinition.Body, methodDefinition.Id as Identifier, methodDefinition.Params, isMethod: true);
        }

        private void CompileFunctionExpression(AdhocCodeFrame frame, FunctionExpression funcExp)
        {
            if (funcExp.Id is not null)
            {
                // Assume its a regular function or method
                CompileSubroutine(frame, funcExp, funcExp.Body, funcExp.Id, funcExp.Params, isMethod: false);
            }
            else
            {
                // Assume it's an anonymous function, where variables can be captured
                CompileAnonymousSubroutine(frame, funcExp, funcExp.Body, funcExp.Params);
            }
        }

        private void CompileMethodExpression(AdhocCodeFrame frame, MethodExpression methodExpression)
        {
            // Assume it's an anonymous function, where variables can be captured
            CompileAnonymousSubroutine(frame, methodExpression, methodExpression.Body, methodExpression.Params, isMethod: true);
        }

        // Combination of string literals/templates
        private void CompileTaggedTemplateExpression(AdhocCodeFrame frame, TaggedTemplateExpression taggedTemplate)
        {
            int elemCount = 0;
            BuildStringRecurse(taggedTemplate);

            void BuildStringRecurse(TaggedTemplateExpression taggedTemplateExpression)
            {
                foreach (var node in taggedTemplateExpression.ChildNodes)
                {
                    if (node is TaggedTemplateExpression childExp)
                        BuildStringRecurse(childExp);
                    else if (node is TemplateLiteral literal)
                    {
                        if (literal.Expressions.Count == 0)
                        {
                            TemplateElement element = literal.Quasis[0];
                            AdhocSymbol strSymb = SymbolMap.RegisterSymbol(element.Value.Cooked);
                            InsStringConst strConst = new InsStringConst(strSymb);
                            frame.AddInstruction(strConst, element.Location.Start.Line);

                            elemCount++;
                        }
                        else
                        {
                            // Interpolated
                            List<Node> literalNodes = new List<Node>();
                            literalNodes.AddRange(literal.Quasis);
                            literalNodes.AddRange(literal.Expressions);

                            // A bit hacky 
                            literalNodes = literalNodes.OrderBy(e => e.Location.Start.Column).ThenBy(e => e.Location.Start.Line).ToList();

                            foreach (Node n in literalNodes)
                            {
                                if (n is TemplateElement tElem)
                                {
                                    AdhocSymbol valSymb = SymbolMap.RegisterSymbol(tElem.Value.Cooked);
                                    InsStringConst strConst = new InsStringConst(valSymb);
                                    frame.AddInstruction(strConst, n.Location.Start.Line);
                                }
                                else if (n is Expression exp)
                                {
                                    CompileExpression(frame, exp);
                                }
                                else
                                    ThrowCompilationError(node, "Unexpected template element type");

                                elemCount++;
                            }
                        }
                    }
                    else
                        throw new Exception("aa");
                }
            }

            InsStringPush strPush = new InsStringPush(elemCount);
            frame.AddInstruction(strPush, taggedTemplate.Location.Start.Line);
        }

        /// <summary>
        /// Compiles a string format literal. i.e "hello %{name}!"
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="templateLiteral"></param>
        private void CompileTemplateLiteral(AdhocCodeFrame frame, TemplateLiteral templateLiteral)
        {
            if (templateLiteral.Quasis.Count == 1 && templateLiteral.Expressions.Count == 0)
            {
                // Regular string const
                TemplateElement strElement = templateLiteral.Quasis[0];
                if (string.IsNullOrEmpty(strElement.Value.Cooked))
                {
                    // Empty strings are always a string push with 0 args (aka nil)
                    InsStringPush strPush = new InsStringPush(0);
                    frame.AddInstruction(strPush, strElement.Location.Start.Line);
                }
                else 
                {
                    AdhocSymbol strSymb = SymbolMap.RegisterSymbol(strElement.Value.Cooked, convertToOperand: false);
                    InsStringConst strConst = new InsStringConst(strSymb);
                    frame.AddInstruction(strConst, strElement.Location.Start.Line);
                }
            }
            else
            {
                /* Adhoc expects all literals and interpolated values to be all in a row, one per string push */
                List<Node> literalNodes = new List<Node>();
                literalNodes.AddRange(templateLiteral.Quasis);
                literalNodes.AddRange(templateLiteral.Expressions);

                // A bit hacky 
                literalNodes = literalNodes.OrderBy(e => e.Location.Start.Column).ThenBy(e => e.Location.Start.Line).ToList();

                foreach (Node node in literalNodes)
                {
                    if (node is TemplateElement tElem)
                    {
                        AdhocSymbol valSymb = SymbolMap.RegisterSymbol(tElem.Value.Cooked, convertToOperand: false);
                        InsStringConst strConst = new InsStringConst(valSymb);
                        frame.AddInstruction(strConst, tElem.Location.Start.Line);
                    }
                    else if (node is Expression exp)
                    {
                        CompileExpression(frame, exp);
                    }
                    else
                        ThrowCompilationError(node, "Unexpected template element type");
                }

                // Link strings together
                InsStringPush strPush = new InsStringPush(literalNodes.Count);
                frame.AddInstruction(strPush, templateLiteral.Location.Start.Line);
            }
        }

        private void CompileAssignmentExpression(AdhocCodeFrame frame, AssignmentExpression assignExpression)
        {
            if (assignExpression.Operator == AssignmentOperator.Assign)
            {
                // Assigning to something new
                CompileExpression(frame, assignExpression.Right);
                CompileVariableAssignment(frame, assignExpression.Left);
            }
            else if (IsAdhocAssignWithOperandOperator(assignExpression.Operator))
            {
                // Assigning to self (+=)
                if (assignExpression.Left is Identifier)
                {
                    // Pushing to variable
                    InsertVariablePush(frame, assignExpression.Left as Identifier, false); // Push current value first
                }
                else if (assignExpression.Left is AttributeMemberExpression attr)
                {
                    InsertAttributeMemberAssignmentPush(frame, attr);
                }
                else if (assignExpression.Left is ComputedMemberExpression compExpression)
                {
                    CompileComputedMemberExpressionAssignment(frame, compExpression);
                }
                else
                    ThrowCompilationError(assignExpression, "Unimplemented");

                CompileExpression(frame, assignExpression.Right);

                InsertBinaryAssignOperator(frame, assignExpression, assignExpression.Operator, assignExpression.Location.Start.Line);
                frame.AddInstruction(InsPop.Default, 0);
            }
            else
            {
                ThrowCompilationError(assignExpression, $"Unimplemented operator assignment {assignExpression.Operator}");
            }
        }

        /// <summary>
        /// Compiles an assignment to a variable.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="expression"></param>
        public void CompileVariableAssignment(AdhocCodeFrame frame, Expression expression)
        {
            if (expression is Identifier ident) // hello = world
            {
                InsertVariablePush(frame, ident, false);
            }
            else if (expression is AttributeMemberExpression attrMember) // Pushing into an object i.e hello.world = "!"
            {
                InsertAttributeMemberAssignmentPush(frame, attrMember);
            }
            else if (expression is ComputedMemberExpression compExpression)
            {
                CompileComputedMemberExpressionAssignment(frame, compExpression);
            }
            else if (expression is UnaryExpression unaryExp && unaryExp.Operator == UnaryOperator.Indirection)
            {
                if (unaryExp.Argument.Type == Nodes.Identifier || unaryExp.Argument is AttributeMemberExpression)
                    CompileExpression(frame, unaryExp.Argument);
                else
                    ThrowCompilationError(expression, "TODO: Ref stuff");
            }
            else if (expression is StaticMemberExpression staticMembExpression) // main::hello = hi
            {
                CompileStaticMemberExpressionAssignment(frame, staticMembExpression);
            }
            else if (expression is ArrayPattern pattern) // var [hi, hello] = variable
            {
                CompileArrayPatternPush(frame, pattern);
                return; // No need for assign pop
            }
            else
                ThrowCompilationError(expression, "Unimplemented");

            InsertAssignPop(frame);
        }

        /// <summary>
        /// test ? consequent : alternate;
        /// </summary>
        /// <param name="condExpression"></param>
        private void CompileConditionalExpression(AdhocCodeFrame frame, ConditionalExpression condExpression)
        {
            // Compile condition
            CompileExpression(frame, condExpression.Test);

            InsJumpIfFalse alternateJump = new InsJumpIfFalse();
            frame.AddInstruction(alternateJump, 0);

            CompileExpression(frame, condExpression.Consequent);

            // This jump will skip the alternate statement if the consequent path is taken
            InsJump altSkipJump = new InsJump();
            frame.AddInstruction(altSkipJump, 0);
            frame.AddInstruction(InsPop.Default, 0);

            // Update alternate jump index now that we've compiled the consequent
            alternateJump.JumpIndex = frame.GetLastInstructionIndex();

            // Proceed to compile alternate/no match statement
            CompileExpression(frame, condExpression.Alternate);

            // Done completely, update alt skip jump to end of condition instruction frame
            altSkipJump.JumpInstructionIndex = frame.GetLastInstructionIndex();
        }

        /// <summary>
        /// Compiles an identifier. var test = otherVariable;
        /// </summary>
        /// <param name="identifier"></param>
        private void CompileIdentifier(AdhocCodeFrame frame, Identifier identifier, bool attribute = false)
        {
            if (attribute)
                InsertAttributeEval(frame, identifier);
            else
                InsertVariableEval(frame, identifier);
        }


        /// <summary>
        /// Compiles array or map access or anything that can be indexed
        /// </summary>
        private void CompileComputedMemberExpression(AdhocCodeFrame frame, ComputedMemberExpression computedMember)
        {
            CompileExpression(frame, computedMember.Object);
            CompileExpression(frame, computedMember.Property);

            InsElementEval eval = new InsElementEval();
            frame.AddInstruction(eval, 0);
        }

        /// <summary>
        /// Compiles array or map element assignment
        /// </summary>
        private void CompileComputedMemberExpressionAssignment(AdhocCodeFrame frame, ComputedMemberExpression computedMember)
        {
            CompileExpression(frame, computedMember.Object);
            CompileExpression(frame, computedMember.Property);

            InsElementPush push = new InsElementPush();
            frame.AddInstruction(push, 0);
        }

        /// <summary>
        /// Compiles an attribute member path.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="staticExp"></param>
        private void CompileAttributeMemberExpression(AdhocCodeFrame frame, AttributeMemberExpression staticExp)
        {
            CompileExpression(frame, staticExp.Object); // ORG

            if (staticExp.Property.Type == Nodes.Identifier)
            {
                CompileIdentifier(frame, staticExp.Property as Identifier, attribute: true); // inSession
            }
            else if (staticExp.Property is StaticMemberExpression)
            {
                CompileStaticMemberExpressionAttributeEval(frame, staticExp.Property as StaticMemberExpression);
            }
            else
                ThrowCompilationError(staticExp, "Expected attribute member to be identifier or static member expression.");
        }

        /// <summary>
        /// Compiles a static member path.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="staticExp"></param>
        private void CompileStaticMemberExpression(AdhocCodeFrame frame, StaticMemberExpression staticExp)
        {
            // Recursively build the namespace path
            List<string> pathParts = new(4);
            BuildStaticPath(staticExp, ref pathParts);

            InsVariableEvaluation eval = new InsVariableEvaluation();
            foreach (string part in pathParts)
            {
                AdhocSymbol symb = SymbolMap.RegisterSymbol(part);
                eval.VariableSymbols.Add(symb);
            }

            string fullPath = string.Join("::", pathParts);
            AdhocSymbol fullPathSymb = SymbolMap.RegisterSymbol(fullPath);
            eval.VariableSymbols.Add(fullPathSymb);

            int idx = frame.AddScopeVariable(fullPathSymb, isAssignment: false, isStatic: true);
            eval.VariableStorageIndex = idx;

            frame.AddInstruction(eval, staticExp.Location.Start.Line);
        }

        /// <summary>
        /// Compiles a static member path assignment.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="staticExp"></param>
        private void CompileStaticMemberExpressionAssignment(AdhocCodeFrame frame, StaticMemberExpression staticExp)
        {
            // Recursively build the namespace path
            List<string> pathParts = new(4);
            BuildStaticPath(staticExp, ref pathParts);

            InsVariablePush push = new InsVariablePush();
            foreach (string part in pathParts)
            {
                AdhocSymbol symb = SymbolMap.RegisterSymbol(part);
                push.VariableSymbols.Add(symb);
            }

            string fullPath = string.Join("::", pathParts);
            AdhocSymbol fullPathSymb = SymbolMap.RegisterSymbol(fullPath);
            push.VariableSymbols.Add(fullPathSymb);

            int idx = frame.AddScopeVariable(fullPathSymb, isAssignment: false, isStatic: true);
            push.VariableStorageIndex = idx;

            frame.AddInstruction(push, staticExp.Location.Start.Line);
        }

        /// <summary>
        /// Compiles a static member path as an attribute evaluation.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="staticExp"></param>
        private void CompileStaticMemberExpressionAttributeEval(AdhocCodeFrame frame, StaticMemberExpression staticExp)
        {
            // Recursively build the namespace path
            List<string> pathParts = new(4);
            BuildStaticPath(staticExp, ref pathParts);

            InsAttributeEvaluation attrEval = new InsAttributeEvaluation();
            foreach (string part in pathParts)
            {
                AdhocSymbol symb = SymbolMap.RegisterSymbol(part);
                attrEval.AttributeSymbols.Add(symb);
            }

            string fullPath = string.Join("::", pathParts);
            AdhocSymbol fullPathSymb = SymbolMap.RegisterSymbol(fullPath);
            attrEval.AttributeSymbols.Add(fullPathSymb);
            frame.AddInstruction(attrEval, staticExp.Location.Start.Line);
        }

        /// <summary>
        /// Compiles a function or method call.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="call"></param>
        private void CompileCall(AdhocCodeFrame frame, CallExpression call, bool popReturnValue = false)
        {
            // HACK
            if (call.Callee is Identifier ident)
            {
                if (ident.Name == "object_selector")
                {
                    if (call.Arguments.Count != 2)
                        ThrowCompilationError(call, "Expected object selector call to have 2 argument.");

                    // Hack to handle object selectors
                    CompileExpression(frame, call.Arguments[0]);
                    CompileExpression(frame, call.Arguments[1]);

                    frame.AddInstruction(InsObjectSelector.Default, 0);
                    frame.AddInstruction(InsEval.Default, 0);
                    return;
                }
            }

            CompileExpression(frame, call.Callee);

            for (int i = 0; i < call.Arguments.Count; i++)
            {
                CompileExpression(frame, call.Arguments[i]);
            }
            
            var callIns = new InsCall(call.Arguments.Count);
            frame.AddInstruction(callIns, call.Location.Start.Line);

            // When calling and not caring about returns
            if (popReturnValue)
                frame.AddInstruction(InsPop.Default, 0);
        }

        /// <summary>
        /// Compiles a binary expression.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="binExp"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void CompileBinaryExpression(AdhocCodeFrame frame, BinaryExpression binExp)
        {
            CompileExpression(frame, binExp.Left);

            // Check for logical operators that checks between both conditions
            if (binExp.Operator == BinaryOperator.LogicalAnd || binExp.Operator == BinaryOperator.LogicalOr)
            {
                if (binExp.Operator == BinaryOperator.LogicalOr)
                {
                    InsLogicalOr orIns = new InsLogicalOr();
                    frame.AddInstruction(orIns, 0);

                    CompileExpression(frame, binExp.Right);
                    orIns.InstructionJumpIndex = frame.GetLastInstructionIndex();
                }
                else if (binExp.Operator == BinaryOperator.LogicalAnd)
                {
                    InsLogicalAnd andIns = new InsLogicalAnd();
                    frame.AddInstruction(andIns, 0);

                    CompileExpression(frame, binExp.Right);
                    andIns.InstructionJumpIndex = frame.GetLastInstructionIndex();
                }
                else
                {
                    throw new InvalidOperationException();
                }
                
            }
            else if (binExp.Operator == BinaryOperator.InstanceOf)
            {
                CompileInstanceOfOperator(frame, binExp);
            }
            else
            {
                
                CompileExpression(frame, binExp.Right);

                string opStr = binExp.Operator switch
                {
                    BinaryOperator.Equal => "==",
                    BinaryOperator.NotEqual => "!=",
                    BinaryOperator.Less => "<",
                    BinaryOperator.Greater => ">",
                    BinaryOperator.LessOrEqual => "<=",
                    BinaryOperator.GreaterOrEqual => ">=",
                    BinaryOperator.Plus => "+",
                    BinaryOperator.Minus => "-",
                    BinaryOperator.Divide => "/",
                    BinaryOperator.Times => "*",
                    BinaryOperator.Modulo => "%",
                    BinaryOperator.BitwiseOr => "|",
                    BinaryOperator.BitwiseXOr => "^",
                    BinaryOperator.BitwiseAnd => "&",
                    BinaryOperator.LeftShift => "<<",
                    BinaryOperator.RightShift => ">>",
                    BinaryOperator.Exponentiation => "**",
                    _ => null
                };

                if (opStr is null)
                    ThrowCompilationError(binExp, $"Binary operator {binExp.Operator} not implemented");

                AdhocSymbol opSymbol = SymbolMap.RegisterSymbol(opStr);
                InsBinaryOperator binOpIns = new InsBinaryOperator(opSymbol);
                frame.AddInstruction(binOpIns, binExp.Location.Start.Line);
            }
        }

        private void CompileInstanceOfOperator(AdhocCodeFrame frame, BinaryExpression binExp)
        {
            CompileExpression(frame, binExp.Left);

            // Object.isInstanceOf - No idea if adhoc supports it, but eh, why not
            InsertAttributeEval(frame, new Identifier("isInstanceOf"));

            // Eval right identifier (if its one)
            CompileExpression(frame, binExp.Right);

            // Call.
            frame.AddInstruction(new InsCall(argumentCount: 1), binExp.Location.Start.Line);
        }

        private void CompileFinalizerStatement(AdhocCodeFrame frame, FinalizerStatement finalizerStatement)
        {
            if (finalizerStatement.Body is FunctionExpression funcExp)
            {
                if (funcExp.Id is not null)
                    ThrowCompilationError(funcExp, "Finalizer function expression must not have a name.");
                if (funcExp.Params.Count > 0)
                    ThrowCompilationError(funcExp, "Finalizer function expression must not have arguments.");
            }
            else if (finalizerStatement.Body is ArrowFunctionExpression arrowFuncExp)
            {
                if (arrowFuncExp.Id is not null)
                    ThrowCompilationError(arrowFuncExp, "Finalizer arrow function expression must not have a name.");
                if (arrowFuncExp.Params.Count > 0)
                    ThrowCompilationError(arrowFuncExp, "Finalizer arrow function expression must not have arguments.");
            }
            else
            {
                ThrowCompilationError(finalizerStatement.Body, "Expected finalizer to be a function expression or function lambda.");
            }

            CompileExpression(frame, finalizerStatement.Body);

            // add temp value & set finally
            InsertVariablePush(frame, new Identifier($"fin#{SymbolMap.TempVariableCounter++}") { Location = finalizerStatement.Body.Location }, true);

            InsAttributePush push = new InsAttributePush();
            push.AttributeSymbols.Add(SymbolMap.RegisterSymbol("finally"));
            frame.AddInstruction(push, finalizerStatement.Body.Location.Start.Line);
            InsertAssignPop(frame);
        }

        /// <summary>
        /// Compiles an unary expression.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="unaryExp"></param>
        private void CompileUnaryExpression(AdhocCodeFrame frame, UnaryExpression unaryExp, bool popResult = false, bool isReference = false)
        {
            if (unaryExp is UpdateExpression upd)
            {
                if (!isReference)
                {
                    // Assigning - we need to push
                    if (unaryExp.Argument.Type == Nodes.Identifier)
                        InsertVariablePush(frame, unaryExp.Argument as Identifier, false);
                    else if (unaryExp.Argument is AttributeMemberExpression attr)
                        InsertAttributeMemberAssignmentPush(frame, attr);
                    else if (unaryExp.Argument is ComputedMemberExpression comp)
                        CompileComputedMemberExpression(frame, comp);
                    else if (unaryExp.Argument.Type == Nodes.Literal) // -1 -> int const + unary op
                        CompileLiteral(frame, unaryExp.Argument as Literal);
                    else if (unaryExp.Argument.Type == Nodes.CallExpression)
                        CompileCall(frame, unaryExp.Argument as CallExpression);
                    else if (unaryExp.Argument is BinaryExpression binaryExp)
                        CompileBinaryExpression(frame, binaryExp);
                    else
                        ThrowCompilationError(unaryExp.Argument, "Unsupported");
                }
                else
                {
                    // Reference objects can just be eval'd
                    CompileExpression(frame, upd.Argument);
                }

                bool preIncrement = unaryExp.Prefix;

                string op = unaryExp.Operator switch
                {
                    UnaryOperator.Increment when !preIncrement => "@++",
                    UnaryOperator.Increment when preIncrement => "++@",
                    UnaryOperator.Decrement when !preIncrement => "@--",
                    UnaryOperator.Decrement when preIncrement => "--@",
                    _ => throw new NotImplementedException("TODO"),
                };

                AdhocSymbol symb = SymbolMap.RegisterSymbol(op);
                InsUnaryAssignOperator unaryIns = new InsUnaryAssignOperator(symb);
                frame.AddInstruction(unaryIns, unaryExp.Location.Start.Line);

                // If we aren't assigning, or not using the return value immediately, pop it
                // Usages: i++;
                //         for (var i = 0; i < 10; [i++])

                if (popResult)
                    frame.AddInstruction(InsPop.Default, 0);
            }
            else
            {
                if (unaryExp.Operator == UnaryOperator.Indirection) // *
                {
                    CompileExpression(frame, unaryExp.Argument);
                    frame.AddInstruction(InsEval.Default, 0);
                }
                else if (unaryExp.Operator == UnaryOperator.ReferenceOf) // &
                {
                    CompileReferenceOfUnaryExpression(frame, unaryExp);
                }
                else
                {
                    CompileExpression(frame, unaryExp.Argument);
                    string op = unaryExp.Operator switch
                    {
                        UnaryOperator.LogicalNot => "!",
                        UnaryOperator.Minus => "-@",
                        UnaryOperator.Plus => "+@",
                        UnaryOperator.BitwiseNot => "~",
                        _ => throw new NotImplementedException("TODO"),
                    };

                    AdhocSymbol symb = SymbolMap.RegisterSymbol(op);
                    InsUnaryOperator unaryIns = new InsUnaryOperator(symb);
                    frame.AddInstruction(unaryIns, unaryExp.Location.Start.Line);
                }
            }
        }

        private void CompileReferenceOfUnaryExpression(AdhocCodeFrame frame, UnaryExpression unaryExp)
        {
            if (unaryExp.Argument is AttributeMemberExpression attrMemberExp)
            {
                // We need to push the object's reference, instead of simply evaluating it
                InsertAttributeMemberAssignmentPush(frame, attrMemberExp);
            }
            else if (unaryExp.Argument is StaticMemberExpression staticExpr)
            {
                CompileStaticMemberExpressionAssignment(frame, staticExpr);
            }
            else if (unaryExp.Argument is UpdateExpression upd)
            {
                CompileUnaryExpression(frame, upd, isReference: true);
            }
            else if (unaryExp.Argument is Identifier identifier)
            {
                InsertVariablePush(frame, identifier, false);
            }
            else
            {
                ThrowCompilationError(unaryExp.Argument, "Not implemented");
            }
        }

        /// <summary>
        /// Compile a literal into a proper constant instruction.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="literal"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CompileLiteral(AdhocCodeFrame frame, Literal literal)
        {
            switch (literal.TokenType)
            {
                case TokenType.NilLiteral:
                    InsNilConst nil = new InsNilConst();
                    frame.AddInstruction(nil, literal.Location.Start.Line);
                    break;

                case TokenType.BooleanLiteral:
                    InsBoolConst boolConst = new InsBoolConst((literal.Value as bool?).Value);
                    frame.AddInstruction(boolConst, literal.Location.Start.Line);
                    break;

                case TokenType.NumericLiteral:
                    InstructionBase ins = literal.NumericTokenType switch
                    {
                        NumericTokenType.Integer => new InsIntConst((int)literal.NumericValue),
                        NumericTokenType.UnsignedInteger => new InsUIntConst((uint)literal.NumericValue),
                        NumericTokenType.Float => new InsFloatConst((float)literal.NumericValue),
                        NumericTokenType.Long => new InsLongConst((long)literal.NumericValue),
                        NumericTokenType.UnsignedLong => new InsULongConst((ulong)literal.NumericValue),
                        _ => throw GetCompilationError(literal, "Unknown numeric literal type"),
                    };
                    frame.AddInstruction(ins, literal.Location.Start.Line);
                    break;

                case TokenType.SymbolLiteral:
                    InsSymbolConst symbConst = new InsSymbolConst(SymbolMap.RegisterSymbol(literal.Value as string));
                    frame.AddInstruction(symbConst, literal.Location.Start.Line);
                    break;

                default:
                    throw new NotImplementedException($"Not implemented literal {literal.TokenType}");
            }
        }

        /// <summary>
        /// Inserts an attribute eval instruction to access an attribute of a certain object.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        private AdhocSymbol InsertAttributeEval(AdhocCodeFrame frame, Identifier identifier)
        {
            AdhocSymbol symb = SymbolMap.RegisterSymbol(identifier.Name);
            var attrEval = new InsAttributeEvaluation();
            attrEval.AttributeSymbols.Add(symb); // Only one
            frame.AddInstruction(attrEval, identifier.Location.Start.Line);

            return symb;
        }

        /// <summary>
        /// Inserts a variable evaluation instruction.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        private void InsertVariableEval(AdhocCodeFrame frame, Identifier identifier)
        {
            if (ProcessStringDefine(frame, identifier))
                return;

            AdhocSymbol symb = SymbolMap.RegisterSymbol(identifier.Name);
            int idx = identifier.Name != "self" ? frame.AddScopeVariable(symb, isAssignment: false) : 0;
            var varEval = new InsVariableEvaluation(idx);
            varEval.VariableSymbols.Add(symb); // Only one
            frame.AddInstruction(varEval, identifier.Location.Start.Line);

            // Static references or pushes always have double their own symbol
            // If its a static reference, do not add it as a declared variable within this scope
            // "self" keyword is ignored
            if (frame.IsStaticVariable(symb))
                varEval.VariableSymbols.Add(symb); // Static, two symbols
        }

        public bool ProcessStringDefine(AdhocCodeFrame frame, Identifier identifier)
        {
            if (identifier.Name == "__LINE__")
            {
                frame.AddInstruction(new InsUIntConst((uint)identifier.Location.Start.Line), identifier.Location.Start.Line);
                return true;
            }
            else if (identifier.Name == "__FILE__")
            {
                frame.AddInstruction(new InsStringConst(frame.SourceFilePath), identifier.Location.Start.Line);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Inserts a variable push instruction to push a variable into the heap.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        private AdhocSymbol InsertVariablePush(AdhocCodeFrame frame, Identifier identifier, bool isVariableDeclaration)
        {
            AdhocSymbol varSymb = SymbolMap.RegisterSymbol(identifier.Name);
            int idx = frame.AddScopeVariable(varSymb, isAssignment: true, isLocalDeclaration: isVariableDeclaration);

            var varPush = new InsVariablePush();
            varPush.VariableSymbols.Add(varSymb);

            // Refer to comment in InsertVariableEval
            if (frame.IsStaticVariable(varSymb))
                varPush.VariableSymbols.Add(varSymb);
                
            varPush.VariableStorageIndex = idx;
            frame.AddInstruction(varPush, identifier.Location.Start.Line);

            return varSymb;
        }

        /// <summary>
        /// Inserts a push to an object attribute
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="attr"></param>
        private void InsertAttributeMemberAssignmentPush(AdhocCodeFrame frame, AttributeMemberExpression attr)
        {
            // Pushing to object attribute
            CompileExpression(frame, attr.Object);
            if (attr.Property is not Identifier)
                ThrowCompilationError(attr.Property, "Expected attribute member property identifier");

            var propIdent = attr.Property as Identifier;

            InsAttributePush attrPush = new InsAttributePush();
            AdhocSymbol attrSymbol = SymbolMap.RegisterSymbol(propIdent.Name);
            attrPush.AttributeSymbols.Add(attrSymbol);
            frame.AddInstruction(attrPush, propIdent.Location.Start.Line);
        }

        /// <summary>
        /// Inserts a binary assign operator.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="parentNode"></param>
        /// <param name="assignOperator"></param>
        /// <param name="lineNumber"></param>
        /// <returns></returns>
        private AdhocSymbol InsertBinaryAssignOperator(AdhocCodeFrame frame, Node parentNode, AssignmentOperator assignOperator, int lineNumber)
        {
            string opStr = AssignOperatorToString(assignOperator);
            if (string.IsNullOrEmpty(opStr))
                ThrowCompilationError(parentNode, "Unrecognized operator");

            var symb = SymbolMap.RegisterSymbol(opStr);
            frame.AddInstruction(new InsBinaryAssignOperator(symb), lineNumber);

            return symb;
        }

        /// <summary>
        /// Inserts an empty return instruction if the frame wasn't explicitly exited with a return statement.
        /// </summary>
        /// <param name="frame"></param>
        private void InsertFrameExitIfNeeded(AdhocCodeFrame frame, Node bodyNode)
        {
            // Was a return explicitly specified?
            if (!frame.HasTopLevelReturnValue)
            {
                // All functions return a value internally, even if they don't in the code.
                // So, add one.
                frame.AddInstruction(InsVoidConst.Empty, bodyNode.Location.End.Line);
                frame.AddInstruction(new InsSetState(AdhocRunState.RETURN), 0);
            }
        }

        private static string AssignOperatorToString(AssignmentOperator op)
        {
            return op switch
            {
                AssignmentOperator.PlusAssign => "+",
                AssignmentOperator.MinusAssign => "-",
                AssignmentOperator.TimesAssign => "*",
                AssignmentOperator.DivideAssign => "/",
                AssignmentOperator.ModuloAssign => "%",
                AssignmentOperator.BitwiseAndAssign => "&",
                AssignmentOperator.BitwiseOrAssign => "|",
                AssignmentOperator.BitwiseXOrAssign => "^",
                AssignmentOperator.ExponentiationAssign => "**",
                AssignmentOperator.RightShiftAssign => ">>",
                AssignmentOperator.LeftShiftAssign => "<<",
                _ => null
            };
        }

        private static string UnaryOperatorToString(UnaryOperator op, bool postIncrement)
        {
            return op switch
            {
                UnaryOperator.Increment when postIncrement => "@++",
                UnaryOperator.Increment when !postIncrement => "++@",
                UnaryOperator.Decrement when postIncrement => "@--",
                UnaryOperator.Decrement when !postIncrement => "--@",
                _ => null,
            };
        }

        private static bool IsAdhocAssignWithOperandOperator(AssignmentOperator op)
        {
            switch (op)
            {
                case AssignmentOperator.PlusAssign:
                case AssignmentOperator.MinusAssign:
                case AssignmentOperator.TimesAssign:
                case AssignmentOperator.DivideAssign:
                case AssignmentOperator.ModuloAssign:
                case AssignmentOperator.BitwiseAndAssign:
                case AssignmentOperator.BitwiseOrAssign:
                case AssignmentOperator.BitwiseXOrAssign:
                case AssignmentOperator.ExponentiationAssign:
                case AssignmentOperator.RightShiftAssign:
                case AssignmentOperator.LeftShiftAssign:
                    return true;
            }

            return false;
        }

        private void ThrowCompilationError(Node node, string message)
        {
            throw GetCompilationError(node, message);
        }

        /// <summary>
        /// Gets a new compilation exception.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private AdhocCompilationException GetCompilationError(Node node, string message)
        {
            return new AdhocCompilationException($"{message}. Line {node.Location.Start.Line}:{node.Location.Start.Column}");
        }

        private LoopContext EnterLoop(AdhocCodeFrame frame, Statement loopStatement)
        {
            LoopContext loopCtx = new LoopContext(loopStatement);
            frame.CurrentLoops.Push(loopCtx);
            frame.CurrentScopes.Push(loopCtx);
            return loopCtx;
        }

        private SwitchContext EnterSwitch(AdhocCodeFrame frame, SwitchStatement node)
        {
            var scope = new SwitchContext(node);
            frame.CurrentScopes.Push(scope);
            return scope;
        }

        private ScopeContext EnterScope(AdhocCodeFrame frame, Node node)
        {
            var scope = new ScopeContext(node);
            frame.CurrentScopes.Push(scope);
            return scope;
        }

        private AdhocModule EnterModuleOrClass(AdhocCodeFrame frame, Node node)
        {
            var scope = new ScopeContext(node);
            frame.CurrentScopes.Push(scope);

            AdhocModule newModule = new AdhocModule();
            Modules.Push(newModule);
            frame.CurrentModule = newModule;

            return newModule;
        }

        /// <summary>
        /// Leaves a loop scope for the frame.
        /// </summary>
        /// <param name="frame"></param>
        private void LeaveLoop(AdhocCodeFrame frame)
        {
            frame.CurrentLoops.Pop();
            LeaveScope(frame);
        }

        /// <summary>
        /// Leaves a scope for the frame, inserts a leave scope instruction.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="insertLeaveInstruction"></param>
        /// <param name="isModuleLeave"></param>
        private void LeaveScope(AdhocCodeFrame frame, bool insertLeaveInstruction = true, bool isModuleLeave = false)
        {
            var lastScope = frame.CurrentScopes.Pop();

            if (!isModuleLeave) // Module leaves don't actually reset the max.
            {
                // Clear up/rewind
                foreach (var variable in lastScope.ScopeVariables)
                    frame.FreeVariable(variable.Value);
            }

            if (insertLeaveInstruction)
            {
                InsLeaveScope leave = new InsLeaveScope();

                if (isModuleLeave)
                {
                    leave.VariableStorageRewindIndex = 1;
                    leave.ModuleOrClassDepth = Modules.Count - 1;
                }
                else
                {
                    leave.VariableStorageRewindIndex = frame.Stack.GetLastVariableStorageIndex();
                }

                frame.AddInstruction(leave, 0);
            }
        }

        private void LeaveModuleOrClass(AdhocCodeFrame frame)
        {
            LeaveScope(frame, isModuleLeave: true);
            Modules.Pop();
            frame.CurrentModule = Modules.Peek();
        }

        /// <summary>
        /// Compiles a statement and opens a new scope (unless it is a continue or break statement.).
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="statement"></param>
        private void CompileStatementWithScope(AdhocCodeFrame frame, Statement statement)
        {
            if (statement is BlockStatement)
            {
                CompileStatement(frame, statement);
            }
            else if (statement is ContinueStatement
                || statement is BreakStatement)
            {
                // continues are not a scope
                CompileStatement(frame, statement);
            }
            else
            {
                EnterScope(frame, statement);
                CompileStatement(frame, statement);
                LeaveScope(frame);
            }
        }

        private void BuildStaticPath(StaticMemberExpression exp, ref List<string> pathParts)
        {
            if (exp.Object is StaticMemberExpression obj)
            {
                BuildStaticPath(obj, ref pathParts);
            }
            else if (exp.Object is Identifier identifier)
            {
                pathParts.Add(identifier.Name);
            }

            if (exp.Property is Identifier propIdentifier)
            {
                pathParts.Add(propIdentifier.Name);
                return;
            }
        }

        private void InsertAssignPop(AdhocCodeFrame frame)
        {
            if (frame.Version >= 11)
            {
                frame.AddInstruction(InsAssignPop.Default, 0);
            }
            else if (frame.Version >= 10)
            {
                frame.AddInstruction(InsAssign.Default, 0);
                frame.AddInstruction(InsPopOld.Default, 0);
            }
            else // Assume under 10 that its the traditional assign + pop old
            {
                frame.AddInstruction(InsAssignOld.Default, 0);
                frame.AddInstruction(InsPopOld.Default, 0);
            }
        }
    }
}
