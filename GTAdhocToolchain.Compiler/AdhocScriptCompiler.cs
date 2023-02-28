
using Esprima;
using Esprima.Ast;

using GTAdhocToolchain.Core;
using GTAdhocToolchain.Core.Instructions;
using GTAdhocToolchain.Core.Variables;

namespace GTAdhocToolchain.Compiler
{
    /// <summary>
    /// Adhoc script compiler.
    /// </summary>
    public class AdhocScriptCompiler : AdhocCodeFrame
    {
        public AdhocSymbolMap SymbolMap { get; set; } = new();

        public Stack<AdhocModule> ModuleStack { get; set; } = new();
        public Dictionary<string, AdhocModule> TopLevelModules { get; set; } = new();

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public HashSet<string> PostCompilationWarnings = new();

        private Script _debugPrintException;
        private Script _debugThrow;

        public AdhocScriptCompiler()
        {
            var topLevelModule = new AdhocModule();
            ModuleStack.Push(topLevelModule); // Top Level Module
            this.CurrentModule = topLevelModule;
        }

        public string BaseIncludeFolder { get; set; }
        public string ProjectDirectory { get; set; }
        public string BaseDirectory { get; set; }

        public void SetBaseIncludeFolder(string dir)
        {
            BaseIncludeFolder = dir;
        }

        public void SetProjectDirectory(string dir)
        {
            ProjectDirectory = dir;
        }

        public void SetVersion(int version)
        {
            Version = version;
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
            InsertSetState(this, AdhocRunState.EXIT);

            PrintPostCompilationWarnings();

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

        /// <summary>
        /// Builds code for printing exceptions to file (used in CompileStatements())
        /// </summary>
        public void BuildTryCatchDebugStatements()
        {
            _debugPrintException = new AdhocAbstractSyntaxTree("__toplevel__::main::pdistd::AppendFile(\"/APP_DATA_RAW/exceptions.txt\", \"%{__ex}\\n\");").ParseScript();
            _debugThrow = new AdhocAbstractSyntaxTree("throw __ex;").ParseScript();
        }

        public void CompileStatements(AdhocCodeFrame frame, Node node)
        {
            foreach (var n in node.ChildNodes)
                CompileStatement(frame, n);
        }

        public void CompileStatements(AdhocCodeFrame frame, NodeList<Statement> nodes)
        {
            if (_debugPrintException != null)
            {
                // This is super hacky. But the intent is a hack anyway.
                var tryCatch = new InsTryCatch();
                frame.AddInstruction(tryCatch, 0);

                foreach (var n in nodes)
                    CompileStatement(frame, n);

                InsertSetState(frame, AdhocRunState.EXIT);
                tryCatch.InstructionIndex = frame.GetLastInstructionIndex();

                InsJump catchClauseSkipper = new InsJump();
                frame.AddInstruction(catchClauseSkipper, 0);

                frame.AddInstruction(new InsIntConst(0), 0);
                InsertVariablePush(frame, new Identifier("__ex"), true);
                frame.AddInstruction(InsAssign.Default, 0);

                string tmpCaseVariable = $"catch#{SymbolMap.TempVariableCounter++}";
                InsertVariablePush(frame, new Identifier(tmpCaseVariable), true);
                InsertAssignPop(frame);

                CompileStatement(frame, _debugPrintException.Body[0]);
                CompileStatement(frame, _debugThrow.Body[0]);

                catchClauseSkipper.JumpInstructionIndex = frame.GetLastInstructionIndex();
            }
            else
            {
                foreach (var n in nodes)
                    CompileStatement(frame, n);
            }
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
                case Nodes.SourceFileStatement:
                    CompileSourceFileStatement(frame, node as SourceFileStatement);
                    break;
                case Nodes.ModuleConstructorStatement:
                    CompileModuleConstructorStatement(frame, node as ModuleConstructorStatement);
                    break;
                case Nodes.PrintStatement:
                    CompilePrintStatement(frame, node as PrintStatement);
                    break;
                case Nodes.EmptyStatement:
                    break;
                default:
                    ThrowCompilationError(node, $"Unsupported statement: {node.Type}");
                    break;
            }
        }

        public void CompilePrintStatement(AdhocCodeFrame frame, PrintStatement printStatement)
        {
            foreach (var exp in printStatement.Expressions)
                CompileExpression(frame, exp);

            frame.AddInstruction(new InsPrint(printStatement.Expressions.Count), printStatement.Location.Start.Line);
            InsertPop(frame);
        }

        public void CompileModuleConstructorStatement(AdhocCodeFrame frame, ModuleConstructorStatement ctorStatement)
        {
            // Compile the target expression
            CompileExpression(frame, ctorStatement.Id);

            // Grab target, define a new ctor scope
            frame.AddInstruction(new InsModuleConstructor(), 0);

            // Build scope
            CompileStatement(frame, ctorStatement.Body);

            // Exit ctor
            InsertSetState(frame, AdhocRunState.EXIT);
        }

        public void CompileSourceFileStatement(AdhocCodeFrame frame, SourceFileStatement srcFileStatement)
        {
            InsSourceFile srcFileIns = new InsSourceFile(SymbolMap.RegisterSymbol(srcFileStatement.Path, false));
            frame.AddInstruction(srcFileIns, 0);
            frame.SetSourcePath(SymbolMap, srcFileStatement.Path);
        }

        public void CompileUndefStatement(AdhocCodeFrame frame, UndefStatement undefStatement)
        {
            InsUndef undefIns = new InsUndef();
            var parts = undefStatement.Symbol.Split("::");

            if (parts.Length > 1)
            {
                foreach (string part in undefStatement.Symbol.Split(AdhocConstants.OPERATOR_STATIC))
                    undefIns.Symbols.Add(SymbolMap.RegisterSymbol(part));
            }
            else
                undefIns.Symbols.Add(SymbolMap.RegisterSymbol(parts[0]));

            undefIns.Symbols.Add(SymbolMap.RegisterSymbol(undefStatement.Symbol)); // full

            frame.AddInstruction(undefIns, undefStatement.Location.Start.Line);
        }

        public void CompileTryStatement(AdhocCodeFrame frame, TryStatement tryStatement)
        {
            InsTryCatch tryCatch = new InsTryCatch();
            frame.AddInstruction(tryCatch, tryStatement.Location.Start.Line);

            if (tryStatement.Block.Type != Nodes.BlockStatement)
                ThrowCompilationError(tryStatement.Block, CompilationMessages.Error_TryClauseNotBody);

            CompileBlockStatement(frame, tryStatement.Block as BlockStatement);
            InsertSetState(frame, AdhocRunState.EXIT);

            tryCatch.InstructionIndex = frame.GetLastInstructionIndex();

            if (tryStatement.Handler is not null)
            {
                InsJump catchClauseSkipper = new InsJump();
                frame.AddInstruction(catchClauseSkipper, 0);

                CompileCatchClause(frame, tryStatement.Handler);
                catchClauseSkipper.JumpInstructionIndex = frame.GetLastInstructionIndex();
            }

            if (tryStatement.Finalizer is not null)
            {
                if (tryStatement.Finalizer.Type != Nodes.BlockStatement)
                    ThrowCompilationError(tryStatement.Block, CompilationMessages.Error_CatchClauseNotBody);

                CompileStatementWithScope(frame, tryStatement.Finalizer);
            }
        }

        public void CompileCatchClause(AdhocCodeFrame frame, CatchClause catchClause)
        {
            if (catchClause.Param is not null)
            {
                if (catchClause.Param.Type != Nodes.Identifier)
                    ThrowCompilationError(catchClause.Param, CompilationMessages.Error_CatchClauseParameterNotIdentifier);

                // Create temp variable for the exception
                frame.AddInstruction(new InsIntConst(0), 0);
                InsertVariablePush(frame, catchClause.Param as Identifier, true);
                frame.AddInstruction(InsAssign.Default, 0);

                string tmpCaseVariable = $"catch#{SymbolMap.TempVariableCounter++}";
                InsertVariablePush(frame, new Identifier(tmpCaseVariable), true);
                InsertAssignPop(frame);
            }
            else
            {
                // Discard (pop) exception object as 0
                frame.AddInstruction(new InsIntConst(0), 0);
                frame.AddInstruction(InsPop.Default, 0);

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
            if (string.IsNullOrEmpty(BaseIncludeFolder))
                BaseIncludeFolder = Path.GetDirectoryName(frame.SourceFilePath.Name);

            // Look for the file relative to the provided include path
            string pathToIncludeFile = Path.Combine(BaseIncludeFolder, include.Path);
            if (!File.Exists(pathToIncludeFile))
            {
                // Try project folder
                if (!string.IsNullOrEmpty(ProjectDirectory))
                {
                    pathToIncludeFile = Path.Combine(ProjectDirectory, include.Path);
                    if (!File.Exists(pathToIncludeFile))
                        ThrowCompilationError(include, $"Include file does not exist: '{pathToIncludeFile}'");
                }
                else
                    ThrowCompilationError(include, $"Include file does not exist: '{include.Path}'");
            }
                

            Logger.Info($"Linking include file '{include.Path}' for '{frame.SourceFilePath.Name}'.");

            string file = File.ReadAllText(pathToIncludeFile);

            var parser = new AdhocAbstractSyntaxTree(file);
            parser.SetFileName(include.Path);
            Script includeScript = parser.ParseScript();

            // Set frame file name to our include file's
            string oldPath = frame.SourceFilePath.Name;
            frame.SetSourcePath(SymbolMap, include.Path);

            // Alert interpreter that the current source file has changed for debugging
            InsSourceFile srcFileIns = new InsSourceFile(SymbolMap.RegisterSymbol(include.Path, false));
            frame.AddInstruction(srcFileIns, include.Location.Start.Line);

            // Copy include into current frame
            CompileScriptBody(frame, includeScript);

            // Resume
            InsSourceFile ogSrcFileIns = new InsSourceFile(frame.SourceFilePath);
            frame.AddInstruction(ogSrcFileIns, include.Location.Start.Line);

            frame.SetSourcePath(SymbolMap, oldPath);
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
                ThrowCompilationError(breakStatement, CompilationMessages.Error_BreakWithoutContextualScope);
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
                ThrowCompilationError(id, CompilationMessages.Error_ModuleOrClassNameInvalid);
                return;
            }

            Logger.Debug($"L{id.Location.Start.Line} - Compiling {(isModule ? "module" : "class")} '{id.Name}'");
            AdhocModule moduleOrClass = EnterModuleOrClass(frame, body);
            TopLevelModules.TryAdd(id.Name, moduleOrClass);

            if (isModule)
            {
                InsModuleDefine mod = new InsModuleDefine();

                if (id.Name.Contains(AdhocConstants.OPERATOR_STATIC))
                {
                    foreach (string identifier in id.Name.Split(AdhocConstants.OPERATOR_STATIC))
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
                if (id.Name.Contains(AdhocConstants.OPERATOR_STATIC))
                    ThrowCompilationError(superClass, CompilationMessages.Error_ClassNameIsStatic);

                InsClassDefine @class = new InsClassDefine();
                @class.Name = SymbolMap.RegisterSymbol(id.Name);
                moduleOrClass.Name = id.Name;

                var superClassIdent = superClass as Identifier;
                if (superClass is not null)
                {
                    if (superClassIdent.Name.Contains(AdhocConstants.OPERATOR_STATIC))
                    {
                        foreach (var path in superClassIdent.Name.Split(AdhocConstants.OPERATOR_STATIC))
                            @class.ExtendsFrom.Add(SymbolMap.RegisterSymbol(path));
                    }

                    @class.ExtendsFrom.Add(SymbolMap.RegisterSymbol(superClassIdent.Name));
                }
                else
                {
                    // Not provided, inherits from base object (System::Object, or Object if old)
                    if (frame.Version >= 10)
                    {
                        @class.ExtendsFrom.Add(SymbolMap.RegisterSymbol(AdhocConstants.SYSTEM));
                        @class.ExtendsFrom.Add(SymbolMap.RegisterSymbol(AdhocConstants.OBJECT));
                        @class.ExtendsFrom.Add(SymbolMap.RegisterSymbol($"{AdhocConstants.SYSTEM}{AdhocConstants.OPERATOR_STATIC}{AdhocConstants.OBJECT}"));
                    }
                    else
                        @class.ExtendsFrom.Add(SymbolMap.RegisterSymbol(AdhocConstants.OBJECT));
                }

                frame.AddInstruction(@class, id.Location.Start.Line);
            }

            // Compile statements directly, we don't need a regular leave.
            CompileStatements(frame, body as BlockStatement);

            LeaveModuleOrClass(frame, fromSubroutine: frame is not AdhocScriptCompiler);

            // Exit class or module scope. Important.
            InsertSetState(frame, AdhocRunState.EXIT);
        }

        public void CompileContinue(AdhocCodeFrame frame, ContinueStatement continueStatement)
        {
            if (frame.CurrentLoops.Count == 0)
                ThrowCompilationError(continueStatement, CompilationMessages.Error_ContinueWithoutContextualScope);

            LoopContext loop = frame.GetLastLoop();

            InsJump continueJmp = new InsJump();
            frame.AddInstruction(continueJmp, continueStatement.Location.Start.Line);

            loop.ContinueJumps.Add(continueJmp);
        }

        public void CompileIfStatement(AdhocCodeFrame frame, IfStatement ifStatement)
        {
            EnterScope(frame, ifStatement);

            CompileTestStatement(frame, ifStatement.Test);

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
                // On older versions, an alternate jump exists anyway
                if (frame.Version < 12)
                {
                    InsJump skipAlternateJmp = new InsJump();
                    frame.AddInstruction(skipAlternateJmp, 0);
                    skipAlternateJmp.JumpInstructionIndex = frame.GetLastInstructionIndex();
                }

                endOrNextIfJump.JumpIndex = frame.GetLastInstructionIndex();
            }

            LeaveScope(frame, insertLeaveInstruction: false);
        }

        /// <summary>
        /// Compiles a test statement where the result, or assignment, is not immediately discarded.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="testExpression"></param>
        private void CompileTestStatement(AdhocCodeFrame frame, Expression testExpression)
        {
            if (testExpression.Type == Nodes.AssignmentExpression)
            {
                CompileAssignmentExpression(frame, testExpression as AssignmentExpression, popResult: false); // if (<test>)
            }
            else if (testExpression.Type == Nodes.UpdateExpression)
            {
                CompileUnaryExpression(frame, testExpression as UpdateExpression, popResult: false); // var a = ++b; - Do not discard b
            }
            else
            {
                CompileExpression(frame, testExpression);
            }
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
                        ThrowCompilationError(forStatement.Init, CompilationMessages.Error_ForLoopInitializationType);
                        break;
                }
            }

            int startIndex = frame.GetLastInstructionIndex();

            // Condition
            InsJumpIfFalse jumpIfFalse = null; // will only be inserted if the condition exists, else its essentially a while true loop
            if (forStatement.Test is not null)
            {
                CompileTestStatement(frame, forStatement.Test);

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
            if (forStatement.Update is not null)
                CompileForUpdate(frame, forStatement);

            // Insert jump to go back to the beginning of the loop
            InsJump startJump = new InsJump();
            startJump.JumpInstructionIndex = startIndex;
            frame.AddInstruction(startJump, 0);

            // Update jump that exits the loop if it exists
            int loopExitInsIndex = frame.GetLastInstructionIndex();
            if (jumpIfFalse is not null)
                jumpIfFalse.JumpIndex = loopExitInsIndex;

            // Process break jumps before doing the final exit
            foreach (var breakJmp in loopCtx.BreakJumps)
                breakJmp.JumpInstructionIndex = loopExitInsIndex;

            // Insert final leave
            LeaveLoop(frame);
        }

        private void CompileForUpdate(AdhocCodeFrame frame, ForStatement forStatement)
        {
            if (forStatement.Update.Type == Nodes.UpdateExpression)
            {
                CompileUnaryExpression(frame, forStatement.Update as UpdateExpression, popResult: true);
            }
            else if (forStatement.Update.Type == Nodes.CallExpression)
            {
                CompileCall(frame, forStatement.Update as CallExpression, popReturnValue: true);
            }
            else if (forStatement.Update.Type == Nodes.AssignmentExpression)
            {
                CompileAssignmentExpression(frame, forStatement.Update as AssignmentExpression, popResult: true);
            }
            else if (forStatement.Update.Type == Nodes.SequenceExpression)
            {
                CompileSequenceExpressionAssignmentsOrCall(frame, forStatement.Update as SequenceExpression);
            }
            else
                ThrowCompilationError(forStatement.Update, CompilationMessages.Error_StatementExpressionOnly);
        }

        /// <summary>
        /// Compiles 'a = b, c = d', assignments or calls only
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="sequenceExpression"></param>
        private void CompileSequenceExpressionAssignmentsOrCall(AdhocCodeFrame frame, SequenceExpression sequenceExpression)
        {
            foreach (var exp in sequenceExpression.Expressions)
            {
                if (exp.Type == Nodes.AssignmentExpression)
                {
                    CompileAssignmentExpression(frame, exp as AssignmentExpression, popResult: true);
                }
                else if (exp.Type == Nodes.UpdateExpression)
                {
                    CompileUnaryExpression(frame, exp as UpdateExpression, popResult: true);
                }
                else if (exp.Type == Nodes.CallExpression)
                {
                    CompileCall(frame, exp as CallExpression, popReturnValue: true);
                }
                else
                    ThrowCompilationError(exp, CompilationMessages.Error_StatementExpressionOnly);
            }
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
                    CompileTestStatement(frame, whileStatement.Test);
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
            CompileTestStatement(frame, doWhileStatement.Test);

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
            if (frame.Version < 12)
                ThrowCompilationError(foreachStatement, CompilationMessages.Error_ForeachUnsupported);

            LoopContext loopCtx = EnterLoop(frame, foreachStatement);

            CompileExpression(frame, foreachStatement.Right);

            // Access object iterator
            InsAttributeEvaluation attrIns = new InsAttributeEvaluation();
            attrIns.AttributeSymbols.Add(SymbolMap.RegisterSymbol(AdhocConstants.ITERATOR));
            frame.AddInstruction(attrIns, foreachStatement.Right.Location.Start.Line);

            // Assign it to a temporary value for the iteration
            AdhocSymbol itorIdentifier = InsertNewLocalVariable(frame, null, $"in#{SymbolMap.TempVariableCounter++}", foreachStatement.Right.Location);

            // Test - fetch_next returns whether the iterator is done or not
            int testInsIndex = frame.GetLastInstructionIndex();
            InsertVariableEvalFromSymbol(frame, itorIdentifier, foreachStatement.Right.Location);
            InsAttributeEvaluation fetchNextIns = new InsAttributeEvaluation();
            fetchNextIns.AttributeSymbols.Add(SymbolMap.RegisterSymbol("fetch_next"));
            frame.AddInstruction(fetchNextIns, foreachStatement.Right.Location.Start.Line);

            InsJumpIfFalse exitJump = new InsJumpIfFalse(); // End loop jumper
            frame.AddInstruction(exitJump, 0);

            // Entering body, but we need to get the iterator's value into our declared variable, equivalent to *iterator
            InsertVariableEvalFromSymbol(frame, itorIdentifier);
            frame.AddInstruction(InsEval.Default, 0);

            if (foreachStatement.Left is not VariableDeclaration)
                ThrowCompilationError(foreachStatement, CompilationMessages.Error_ForeachDeclarationNotVariable);

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
            SwitchContext switchCtx = EnterSwitch(frame, switchStatement);

            // Create a label for the temporary switch variable
            AdhocSymbol caseSymb = InsertNewLocalVariable(frame, switchStatement.Discriminant, $"case#{SymbolMap.TempVariableCounter++}");

            Dictionary<SwitchCase, InsJumpIfTrue> caseBodyJumps = new();
            InsJump defaultJump = null;
            bool hasDefault = false;

            // Write switch table jumps
            for (int i = 0; i < switchStatement.Cases.Count; i++)
            {
                SwitchCase swCase = switchStatement.Cases[i];
                if (swCase.Test is not null) // Actual case
                {
                    // Get temp variable
                    InsertVariableEvalFromSymbol(frame, caseSymb);

                    // Write what we are comparing to 
                    CompileExpression(frame, swCase.Test);

                    // Equal check
                    InsBinaryOperator eqOp = new InsBinaryOperator(SymbolMap.RegisterSymbol("==", convertToOperand: frame.Version > 10));
                    frame.AddInstruction(eqOp, swCase.Location.Start.Line);

                    // Write the jump
                    InsJumpIfTrue jit = new InsJumpIfTrue();
                    caseBodyJumps.Add(swCase, jit); // To write the instruction index later on
                    frame.AddInstruction(jit, 0);
                }
                else // Default
                {
                    if (hasDefault)
                        ThrowCompilationError(swCase, CompilationMessages.Error_SwitchAlreadyHasDefault);

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
                if (swCase.Test is not null)
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

        /// <summary>
        /// Compiles a function/method.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="parentNode"></param>
        /// <param name="body"></param>
        /// <param name="id"></param>
        /// <param name="subParams"></param>
        /// <param name="isMethod"></param>
        /// <param name="isAsync"></param>
        public void CompileSubroutine(AdhocCodeFrame frame, Node parentNode, Node body, Identifier id, NodeList<Expression> subParams, bool isMethod = false, bool isAsync = false)
        {
            if (id is null)
                ThrowCompilationError(parentNode, CompilationMessages.Error_SubroutineWithoutIdentifier);

            Logger.Debug($"L{parentNode.Location.Start.Line} - Compiling subroutine '{id.Name}'");

            SubroutineBase subroutine = isMethod ? new InsMethodDefine() : new InsFunctionDefine();
            if (id is not null)
                subroutine.Name = SymbolMap.RegisterSymbol(id.Name);
            subroutine.CodeFrame.SourceFilePath = frame.SourceFilePath;
            subroutine.CodeFrame.ParentFrame = this;
            subroutine.CodeFrame.CurrentModule = frame.CurrentModule;
            subroutine.CodeFrame.Version = this.Version;
            subroutine.CodeFrame.SetupStack();

            if (isMethod)
            {
                if (!frame.CurrentModule.DefineMethod(subroutine.Name))
                    ThrowCompilationError(id, $"Method name '{subroutine.Name}' already defined in this scope.");
            }

            EnterScope(subroutine.CodeFrame, parentNode);
            foreach (Expression param in subParams)
                CompileSubroutineParameter(frame, parentNode, subroutine, param);

            if (isMethod && frame.Version <= 10)
            {
                // Methods always reserve a variable slot for 'self' (the keyword doesn't exist) in older versions
                // It's also after parameters
                subroutine.CodeFrame.Stack.AddLocalVariable(null);
            }

            if (frame.CurrentScope.StaticScopeVariables.ContainsKey(subroutine.Name.Name))
                ThrowCompilationError(parentNode, $"Static subroutine name '{subroutine.Name.Name}' is already defined in this scope.");

            frame.AddAttributeOrStaticMemberVariable(subroutine.Name);
            frame.AddInstruction(subroutine, parentNode.Location.Start.Line);


            if (body is BlockStatement blockStatement)
                CompileBlockStatement(subroutine.CodeFrame, blockStatement, openScope: false, insertLeaveInstruction: false);
            else
                ThrowCompilationError(body, "Expected subroutine body to be frame statement.");

            InsertFrameExitIfNeeded(subroutine.CodeFrame, body);

            Logger.Debug($"Subroutine '{id.Name}' compiled ({subroutine.CodeFrame.Instructions.Count} ins, " +
                $"Stack Size: {subroutine.CodeFrame.Stack.GetStackSize()}, Variable Storage Size: {subroutine.CodeFrame.Stack.GetLocalVariableStorageSize()})");
        }

        /// <summary>
        /// Compiles a subroutine parameter and it's default values if provided.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="parentNode"></param>
        /// <param name="subroutine"></param>
        /// <param name="param"></param>
        private void CompileSubroutineParameter(AdhocCodeFrame frame, Node parentNode, SubroutineBase subroutine, Expression param)
        {
            // Parameter with no default value
            if (param.Type == Nodes.Identifier)
            {
                var paramIdent = param as Identifier;

                AdhocSymbol paramSymb = SymbolMap.RegisterSymbol(paramIdent.Name);
                subroutine.CodeFrame.FunctionParameters.Add(paramSymb);
                subroutine.CodeFrame.AddScopeVariable(paramSymb, isAssignment: true, isLocalDeclaration: true);

                // Subroutine param is uninitialized, push nil into current frame
                frame.AddInstruction(new InsNilConst(), paramIdent.Location.Start.Line);
            }
            else if (param is AssignmentExpression assignmentExpression) // Parameter default value set to another variable or static value
            {
                if (assignmentExpression.Left.Type != Nodes.Identifier || assignmentExpression.Right.Type != Nodes.Literal)
                    ThrowCompilationError(parentNode, CompilationMessages.Error_InvalidParameterValueAssignment);

                AdhocSymbol paramSymb = SymbolMap.RegisterSymbol((assignmentExpression.Left as Identifier).Name);
                subroutine.CodeFrame.FunctionParameters.Add(paramSymb);
                subroutine.CodeFrame.AddScopeVariable(paramSymb, isAssignment: true, isLocalDeclaration: true);

                // Push default value
                CompileLiteral(frame, assignmentExpression.Right as Literal);
            }
            else if (param.Type == Nodes.AssignmentPattern)
            {
                var pattern = param as AssignmentPattern;

                if (pattern.Right.Type != Nodes.Literal &&
                    (pattern.Right.Type == Nodes.UnaryExpression && (pattern.Right as UnaryExpression).Argument.Type != Nodes.Literal) && // Stuff like -1
                    pattern.Right.Type != Nodes.Identifier &&
                    pattern.Right.Type != Nodes.MemberExpression &&
                    pattern.Right.Type != Nodes.ArrayExpression &&
                    pattern.Right.Type != Nodes.MapExpression)
                    ThrowCompilationError(parentNode, "Subroutine default parameter value must be an identifier to a literal or other identifier.");

                AdhocSymbol paramSymb = SymbolMap.RegisterSymbol((pattern.Left as Identifier).Name);
                subroutine.CodeFrame.FunctionParameters.Add(paramSymb);
                subroutine.CodeFrame.AddScopeVariable(paramSymb, isAssignment: true, isLocalDeclaration: true);

                // Push default value
                CompileExpression(frame, pattern.Right);
            }
            else if (param.Type == Nodes.RestElement) // Rest element (...params)
            {
                subroutine.CodeFrame.HasRestElement = true;

                Identifier paramIdent = (param as RestElement).Argument as Identifier;
                AdhocSymbol paramSymb = SymbolMap.RegisterSymbol(paramIdent.Name);
                subroutine.CodeFrame.FunctionParameters.Add(paramSymb);
                subroutine.CodeFrame.AddScopeVariable(paramSymb, isAssignment: true, isLocalDeclaration: true);

                frame.AddInstruction(new InsNilConst(), paramIdent.Location.Start.Line);
            }
            else
                ThrowCompilationError(parentNode, "Subroutine definition parameters must all be identifier or assignment to a literal.");
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

                if (frame.Version < 12)
                {
                    // Initial return indicates a return value in older versions
                    InsertPop(frame);
                    InsertSetState(frame, AdhocRunState.RETURN);
                }
            }
            else
            {
                
                if (frame.Version >= 12)
                    InsertVoid(frame); // Void const is returned
            }

            InsertSetState(frame, AdhocRunState.RETURN);

            // Top level of frame?
            if (frame.IsTopLevel)
                frame.HasTopLevelReturnValue = true;
        }

        /// <summary>
        /// Compiles "var a = 0, [b = 1] ...;"
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="varDeclaration"></param>
        /// <param name="pushWhenNoInit"></param>
        public void CompileVariableDeclaration(AdhocCodeFrame frame, VariableDeclaration varDeclaration, bool pushWhenNoInit = false)
        {
            foreach (VariableDeclarator declarator in varDeclaration.Declarations)
            {
                Expression? initValue = declarator.Init;
                Expression? id = declarator.Id;

                // In later versions, we first compile the call
                if (frame.Version > 10)
                {
                    if (initValue is not null)
                    {
                        if (initValue.Type == Nodes.UpdateExpression)
                        {
                            CompileUnaryExpression(frame, initValue as UpdateExpression, popResult: false); // var a = ++b; - Do not discard b
                        }
                        else if (IsUnaryReferenceOfExpression(initValue))
                        {
                            CompileUnaryExpression(frame, initValue as UnaryExpression, popResult: false, asReference: true); // var a = &b;
                        }
                        else if (initValue.Type == Nodes.AssignmentExpression)
                        {
                            CompileAssignmentExpression(frame, initValue as AssignmentExpression, popResult: false); // var a = b = c; - Do not discard b
                        }
                        else
                        {
                            CompileExpression(frame, initValue);
                        }
                    }


                    // Now write the id
                    if (id is null)
                        ThrowCompilationError(varDeclaration, CompilationMessages.Error_VariableDeclarationIsNull);

                    if (id is Identifier idIdentifier) // var hello [= world];
                    {
                        if (initValue is not null || pushWhenNoInit)
                        {
                            // Variable is being defined with a value.
                            InsertVariablePush(frame, idIdentifier, isVariableDeclaration: true);

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
                        CompileArrayPatternPush(frame, arrayPattern, isDeclaration: true);
                    }
                    else
                    {
                        ThrowCompilationError(varDeclaration, "Variable declaration for id is not an identifier.");
                    }
                }
                else
                {
                    if (id is Identifier idIdentifier) // var hello [= world];
                    {
                        if (initValue is not null || pushWhenNoInit)
                        {
                            // Variable is being defined with a value.
                            InsertVariablePush(frame, idIdentifier, isVariableDeclaration: true);
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
                        CompileArrayPatternPush(frame, arrayPattern, isDeclaration: true);
                    }
                    else
                    {
                        ThrowCompilationError(varDeclaration, "Variable declaration for id is not an identifier.");
                    }

                    if (initValue is not null)
                    {
                        if (initValue.Type == Nodes.UpdateExpression)
                        {
                            CompileUnaryExpression(frame, initValue as UpdateExpression, popResult: false); // var a = ++b; - Do not discard b
                        }
                        else if (IsUnaryReferenceOfExpression(initValue))
                        {
                            CompileUnaryExpression(frame, initValue as UnaryExpression, popResult: false, asReference: true); // var a = &b;
                        }
                        else if (initValue.Type == Nodes.AssignmentExpression)
                        {
                            CompileAssignmentExpression(frame, initValue as AssignmentExpression, popResult: false); // var a = b = c; - Do not discard b
                        }
                        else
                        {
                            CompileExpression(frame, initValue);
                        }

                        // Perform assignment
                        if (id is ArrayPattern arrayPattern)
                        {
                            InsListAssignOld listAssign = new InsListAssignOld(arrayPattern.Elements.Count);
                            frame.AddInstruction(listAssign, arrayPattern.Location.Start.Line);
                            InsertPop(frame);
                        }
                        else
                        {
                            InsertAssignPop(frame);
                        }
                    }
                }
                
            }
        }

        private void CompileArrayPatternPush(AdhocCodeFrame frame, ArrayPattern arrayPattern, bool isDeclaration = false)
        {
            if (arrayPattern.Elements.Count == 0)
                ThrowCompilationError(arrayPattern, CompilationMessages.Error_ArrayPatternNoElements);

            foreach (Expression exp in arrayPattern.Elements)
            {
                if (exp.Type == Nodes.Identifier)
                {
                    Identifier arrElemIdentifier = exp as Identifier;
                    InsertVariablePush(frame, arrElemIdentifier, isDeclaration);
                }
                else if (exp is AttributeMemberExpression)
                {
                    CompileAttributeMemberAssignmentPush(frame, exp as AttributeMemberExpression);
                }
                else
                    ThrowCompilationError(exp, "Expected array pattern element to be an identifier or attribute member expression.");
            }

            if (frame.Version >= 12)
            {
                InsListAssign listAssign = new InsListAssign(arrayPattern.Elements.Count);
                frame.AddInstruction(listAssign, arrayPattern.Location.Start.Line);
                InsertPop(frame);
            }
            else
            {
                return; // Needs to be after the value evaluation
            }
        }

        /// <summary>
        /// Compiles an import declaration. 'import main::*'
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="import"></param>
        public void CompileImport(AdhocCodeFrame frame, ImportDeclaration import)
        {
            if (import.Specifiers.Count == 0)
                ThrowCompilationError(import, CompilationMessages.Error_ImportDeclarationEmpty);

            string fullImportNamespace = "";

            InsImport importIns = new InsImport();

            for (int i = 0; i < import.Specifiers.Count; i++)
            {
                ImportDeclarationSpecifier specifier = import.Specifiers[i];
                AdhocSymbol part = SymbolMap.RegisterSymbol(specifier.Local.Name);
                importIns.ImportNamespaceParts.Add(part);
                fullImportNamespace += specifier.Local.Name;

                if (i < import.Specifiers.Count - 1)
                    fullImportNamespace += AdhocConstants.OPERATOR_STATIC;
            }

            AdhocSymbol namespaceSymbol = SymbolMap.RegisterSymbol(fullImportNamespace);
            AdhocSymbol value = SymbolMap.RegisterSymbol(import.Target.Name);
            AdhocSymbol nilSymbol = SymbolMap.RegisterSymbol(AdhocConstants.NIL);

            importIns.ImportNamespaceParts.Add(namespaceSymbol);
            importIns.ModuleValue = value;
            importIns.ImportAs = nilSymbol;

            /* Imports actually copies the static members from the target */
            if (import.Target.Name == AdhocConstants.OPERATOR_IMPORT_ALL)
            {
                if (TopLevelModules.TryGetValue(fullImportNamespace, out AdhocModule mod))
                {
                    foreach (var memberSymbol in mod.GetAllMembers())
                        frame.Stack.AddStaticVariable(new StaticVariable() { Symbol = memberSymbol });
                }
                else
                {
                    // TODO
                    frame.Stack.AddStaticVariable(null);
                }
            }
            else
            {
                frame.Stack.AddStaticVariable(new StaticVariable() { Symbol = value });
            }

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
                case Nodes.MemberExpression when exp is ObjectSelectorMemberExpression:
                    CompileObjectSelectorExpression(frame, exp as ObjectSelectorMemberExpression);
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
                case Nodes.AwaitExpression:
                    CompileAwait(frame, exp as AwaitExpression);
                    break;
                case Nodes.SpreadElement:
                    CompileSpreadElement(frame, exp as SpreadElement);
                    break;
                case Nodes.SelfExpression:
                    CompileSelfExpression(frame, exp as SelfExpression);
                    break;
                default:
                    ThrowCompilationError(exp, $"Expression {exp.Type} not supported");
                    break;
            }
        }

        /// <summary>
        /// Compiles <self>
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="spreadElement"></param>
        private void CompileSelfExpression(AdhocCodeFrame frame, SelfExpression selfExpression)
        {
            if (frame.Version < 10)
                ThrowCompilationError(selfExpression, CompilationMessages.Error_SelfUnsupported);

            AdhocSymbol symb = SymbolMap.RegisterSymbol(AdhocConstants.SELF);
            int idx = 0; // Always 0 when refering to self
            var varEval = new InsVariableEvaluation(idx);
            varEval.VariableSymbols.Add(symb); // Self is always considered as a local. Just one
            frame.AddInstruction(varEval, selfExpression.Location.Start.Line);
        }

        /// <summary>
        /// Compiles <function>.(...args)
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="spreadElement"></param>
        private void CompileSpreadElement(AdhocCodeFrame frame, SpreadElement spreadElement)
        {
            CompileExpression(frame, spreadElement.Argument);
        }

        private void CompileArrowFunctionExpression(AdhocCodeFrame frame, ArrowFunctionExpression arrowFuncExpr)
        {
            CompileAnonymousSubroutine(frame, arrowFuncExpr, arrowFuncExpr.Body, arrowFuncExpr.Params);
        }

        private void CompileYield(AdhocCodeFrame frame, YieldExpression yield)
        {
            frame.AddInstruction(new InsVoidConst(), yield.Location.Start.Line);
            InsertSetState(frame, AdhocRunState.YIELD);
        }

        private void CompileAwait(AdhocCodeFrame frame, AwaitExpression awaitExpr)
        {
            var awaitStart = new StaticMemberExpression(new Identifier(AdhocConstants.SYSTEM), new Identifier("AwaitTaskStart"), false);
            CompileExpression(frame, awaitStart);

            // Awaiting bare call?
            if (awaitExpr.Argument is CallExpression call)
            {
                // Wrap it into a subroutine (maybe move this to an util at the bottom) 
                var subroutine = new FunctionDeclaration(null, 
                    new NodeList<Expression>(), // No parameters
                    new BlockStatement(NodeList.Create(new Statement[] { new ExpressionStatement(call) })),
                    generator: false,
                    strict: true,
                    async: false);
                subroutine.Location = new Location(call.Location.Start, call.Location.End, call.Location.Source);

                CompileAnonymousSubroutine(frame, subroutine, call, new NodeList<Expression>());
            }
            else
            {
                CompileExpression(frame, awaitExpr.Argument);
            }

            // Get task - <task> = System::AwaitTaskStart(<func>);
            frame.AddInstruction(new InsCall(1), 0);
            string tmpTaskVariable = $"task#{SymbolMap.TempVariableCounter++}";
            AdhocSymbol taskSymb = InsertVariablePush(frame, new Identifier(tmpTaskVariable), true);
            frame.AddInstruction(InsAssignPop.Default, 0);

            // Get result of task - <result> = System::AwaitTaskResult(<task>);
            var awaitResult = new StaticMemberExpression(new Identifier(AdhocConstants.SYSTEM), new Identifier("AwaitTaskResult"), false);
            CompileExpression(frame, awaitResult);
            InsertVariableEvalFromSymbol(frame, taskSymb);
            frame.AddInstruction(new InsCall(1), 0);

            AddPostCompilationWarning(CompilationMessages.Warning_UsingAwait_Code);
        }

        /// <summary>
        /// Compiles: .doThing(e => <statement>)
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="arrowFuncExpr"></param>
        private void CompileAnonymousSubroutine(AdhocCodeFrame frame, Node parentNode, Node body, NodeList<Expression> funcParams, bool isMethod = false, bool isAsync = false)
        {

            

            SubroutineBase subroutine = isMethod ? new InsMethodConst() : new InsFunctionConst();
            subroutine.CodeFrame.ParentFrame = frame;
            subroutine.CodeFrame.SourceFilePath = frame.SourceFilePath;
            subroutine.CodeFrame.CurrentModule = frame.CurrentModule;
            subroutine.CodeFrame.Version = this.Version;
            subroutine.CodeFrame.SetupStack();

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
                CompileSubroutineParameter(frame, parentNode, subroutine, param);

            if (body.Type == Nodes.BlockStatement)
            {
                CompileStatement(subroutine.CodeFrame, body as BlockStatement);
                InsertFrameExitIfNeeded(subroutine.CodeFrame, body);
            }
            else
            {
                CompileExpression(subroutine.CodeFrame, body as Expression);

                // Add implicit return
                InsertSetState(subroutine.CodeFrame, AdhocRunState.RETURN);
            }

            LeaveScope(subroutine.CodeFrame, insertLeaveInstruction: false);

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
                frame.AddInstruction(staticDefine, staticExpression.Location.Start.Line);

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
                frame.AddInstruction(staticDefine, staticExpression.Location.Start.Line);

                // This was previously an error
                // But it turns out that you can define statics within function scopes, so they actually can be redefined (GT6 garage project)
                // Will leave it as a warning from now on
                if (!frame.CurrentModule.DefineStatic(idSymb))
                    PrintCompilationWarning(staticExpression, $"Static member '{idSymb.Name}' was already declared in this module.");

                frame.AddAttributeOrStaticMemberVariable(idSymb);

                if (assignmentExpression.Operator == AssignmentOperator.Assign)
                {
                    // Assigning to something new
                    if (frame.Version > 10)
                    {
                        CompileExpression(frame, assignmentExpression.Right);
                        CompileVariableAssignment(frame, assignmentExpression.Left);
                    }
                    else
                    {
                        InsertVariablePush(frame, assignmentExpression.Left as Identifier, isVariableDeclaration: false);
                        CompileExpression(frame, assignmentExpression.Right);

                        InsertAssignPop(frame);
                    }
                }
                else if (IsAdhocAssignWithOperandOperator(assignmentExpression.Operator))
                {
                    // Assigning to self (+=)
                    InsertVariablePush(frame, assignmentExpression.Left as Identifier, false); // Push current value first
                    CompileExpression(frame, assignmentExpression.Right);

                    InsertBinaryAssignOperator(frame, assignmentExpression, assignmentExpression.Operator, assignmentExpression.Location.Start.Line);
                    InsertPop(frame);
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
                frame.AddInstruction(new InsNilConst(), ident.Location.Start.Line);

                var idSymb = SymbolMap.RegisterSymbol(ident.Name);
                InsAttributeDefine staticDefine = new InsAttributeDefine(idSymb);
                frame.AddInstruction(staticDefine, attrVariableDefinition.Location.Start.Line);

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
            if (frame.Version >= 11)
            {
                // Version 11 and above - array is defined
                frame.AddInstruction(new InsArrayConst((uint)arrayExpression.Elements.Count), arrayExpression.Location.Start.Line);

                // Then all items are pushed to it, one by one
                foreach (var elem in arrayExpression.Elements)
                {
                    if (elem is null)
                        ThrowCompilationError(arrayExpression, "Unsupported empty element in array declaration.");

                    CompileExpression(frame, elem);

                    frame.AddInstruction(InsArrayPush.Default, 0);
                }
            }
            else
            {
                // Version 10 and below - items are all pushed into the stack at once
                foreach (var elem in arrayExpression.Elements)
                {
                    if (elem is null)
                        ThrowCompilationError(arrayExpression, "Unsupported empty element in array declaration.");

                    CompileExpression(frame, elem);
                }

                // Then the array is defined
                frame.AddInstruction(new InsArrayConstOld((uint)arrayExpression.Elements.Count), arrayExpression.Location.Start.Line);
            }
        }

        /// <summary>
        /// Compiles: [:] or [k:v, k:v, ...]
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="mapExpression"></param>
        private void CompileMapExpression(AdhocCodeFrame frame, MapExpression mapExpression)
        {
            if (frame.Version < 11)
                ThrowCompilationError(mapExpression, CompilationMessages.Error_MapUnsupported);

            frame.AddInstruction(InsMapConst.Default, mapExpression.Location.Start.Line);

            foreach (var (key, value) in mapExpression.Elements)
            {
                CompileExpression(frame, key);
                CompileExpression(frame, value);
                frame.AddInstruction(InsMapInsert.Default, 0);
            }
        }

        /// <summary>
        /// Compiles expression statements
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="expStatement"></param>
        private void CompileExpressionStatement(AdhocCodeFrame frame, ExpressionStatement expStatement)
        {
            CompileExpression(frame, expStatement.Expression);

            if (expStatement.Expression.Type != Nodes.AssignmentExpression
                && expStatement.Expression.Type != Nodes.StaticDeclaration
                && expStatement.Expression.Type != Nodes.AttributeDeclaration
                && expStatement.Expression.Type != Nodes.YieldExpression)
                InsertPop(frame);
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
                            AdhocSymbol strSymb = SymbolMap.RegisterSymbol(element.Value.Cooked, convertToOperand: false);
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
                                    AdhocSymbol valSymb = SymbolMap.RegisterSymbol(tElem.Value.Cooked, convertToOperand: false);
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
                        throw new Exception("Unsupported Tagged Template Node");
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

                // On later versions, empty strings are always a string push with 0 args, which is a short hand for a static empty string
                if (string.IsNullOrEmpty(strElement.Value.Cooked) && frame.Version > 10)
                {
                    
                    InsStringPush strPush = new InsStringPush(0);
                    frame.AddInstruction(strPush, strElement.Location.Start.Line);
                }
                else 
                {
                    AdhocSymbol strSymb = SymbolMap.RegisterSymbol(strElement.Value.Cooked, convertToOperand: false, strElement.Value.HasHexEscape);
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
                        AdhocSymbol valSymb = SymbolMap.RegisterSymbol(tElem.Value.Cooked, convertToOperand: false, tElem.Value.HasHexEscape);
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

        // TODO: split this into seperate functions for each version
        private void CompileAssignmentExpression(AdhocCodeFrame frame, AssignmentExpression assignExpression, bool popResult = true)
        {
            // Assigning to a variable or literal directly?
            if (assignExpression.Operator == AssignmentOperator.Assign)
            {
                if (frame.Version > 10)
                {
                    // a = b = c?
                    if (assignExpression.Right.Type == Nodes.AssignmentExpression)
                    {
                        // We are reusing the result (b in this case) - we do not pop it.
                        CompileAssignmentExpression(frame, assignExpression.Right as AssignmentExpression, popResult: false);
                    }
                    else
                    {
                        // Regular assignment
                        CompileExpression(frame, assignExpression.Right);
                    }

                    CompileVariableAssignment(frame, assignExpression.Left, popResult);
                }
                else // Target first in old versions
                {
                    // Regular update of left-hand side
                    // Left-hand side needs to be pushed first

                    // Assigning to a reference
                    if (IsUnaryReferenceOfExpression(assignExpression.Left))
                    {
                        // No need to push, eval
                        CompileUnaryExpression(frame, assignExpression.Left as UnaryExpression, asReference: false);
                    }
                    else
                    {
                        if (assignExpression.Left is Identifier)
                        {
                            InsertVariablePush(frame, assignExpression.Left as Identifier, isVariableDeclaration: false);
                        }
                        else if (assignExpression.Left is AttributeMemberExpression attr)
                        {
                            CompileAttributeMemberAssignmentPush(frame, attr);
                        }
                        else if (assignExpression.Left is ComputedMemberExpression compExpression)
                        {
                            CompileComputedMemberExpressionAssignmentPush(frame, compExpression);
                        }
                        else if (assignExpression.Left is ObjectSelectorMemberExpression objectSelector)
                        {
                            throw new NotImplementedException("Unimplemented object selector assignment expression");
                        }
                        else
                        {
                            ThrowCompilationError(assignExpression, "Unimplemented or Invalid");
                        }
                    }

                    if (IsUnaryReferenceOfExpression(assignExpression.Right))
                    {
                        CompileUnaryExpression(frame, assignExpression.Right as UnaryExpression, popResult: false, asReference: true); // a = &b;
                    }
                    else
                    {
                        CompileExpression(frame, assignExpression.Right);
                    }

                    if (popResult)
                        InsertAssignPop(frame);
                    else
                        ; // throw new NotImplementedException("aa");
                }
            }
            else if (IsAdhocAssignWithOperandOperator(assignExpression.Operator)) // += -= /= etc..
            {
                // Assigning to a reference
                if (IsUnaryReferenceOfExpression(assignExpression.Left))
                {
                    // No need to push, eval
                    CompileUnaryExpression(frame, assignExpression.Left as UnaryExpression, asReference: false);
                }
                else
                {
                    // Regular update of left-hand side
                    // Left-hand side needs to be pushed first
                    if (assignExpression.Left is Identifier)
                    {
                        InsertVariablePush(frame, assignExpression.Left as Identifier, isVariableDeclaration: false);
                    }
                    else if (assignExpression.Left is AttributeMemberExpression attr)
                    {
                        CompileAttributeMemberAssignmentPush(frame, attr);
                    }
                    else if (assignExpression.Left is ComputedMemberExpression compExpression)
                    {
                        CompileComputedMemberExpressionAssignmentPush(frame, compExpression);
                    }
                    else if (assignExpression.Left is ObjectSelectorMemberExpression objectSelector)
                    {
                        throw new NotImplementedException("Unimplemented object selector assignment expression");
                    }
                    else
                    {
                        ThrowCompilationError(assignExpression, "Unimplemented or Invalid");
                    }
                }

                if (assignExpression.Right.Type == Nodes.AssignmentExpression) // a += b += c?
                {
                    // Then do not immediately discard result for the next inline operation
                    CompileAssignmentExpression(frame, assignExpression.Right as AssignmentExpression, popResult: false);
                }
                else 
                    CompileExpression(frame, assignExpression.Right);

                InsertBinaryAssignOperator(frame, assignExpression, assignExpression.Operator, assignExpression.Location.Start.Line);
                if (popResult)
                    InsertPop(frame);
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
        public void CompileVariableAssignment(AdhocCodeFrame frame, Expression expression, bool popValue = true)
        {

            if (expression.Type == Nodes.Identifier) // hello = world
            {
                InsertVariablePush(frame, expression as Identifier, false);
            }
            else if (expression.Type == Nodes.MemberExpression)
            {
                if (expression is AttributeMemberExpression attrMember) // Pushing into an object i.e hello.world = "!"
                {
                    CompileAttributeMemberAssignmentPush(frame, attrMember);
                }
                else if (expression is ComputedMemberExpression compExpression) // hello[world] = "foo"
                {
                    CompileComputedMemberExpressionAssignmentPush(frame, compExpression);
                }
                else if (expression is ObjectSelectorMemberExpression objSelectExpression)
                {
                    CompileObjectSelectorExpressionAssignmentPush(frame, objSelectExpression);
                }
                else if (expression is StaticMemberExpression staticMembExpression) // main::hello = hi
                {
                    CompileStaticMemberExpressionAssignment(frame, staticMembExpression);
                }
                else
                    ThrowCompilationError(expression, $"Unimplemented member expression assignment type: '{expression.Type}'");

            }
            else if (expression.Type == Nodes.ArrayPattern) // var [hi, hello] = args
            {
                CompileArrayPatternPush(frame, expression as ArrayPattern, isDeclaration: false);
                return; // No need for assign pop
            }
            else if (expression is UnaryExpression unaryExp && (unaryExp.Operator == UnaryOperator.Indirection || unaryExp.Operator == UnaryOperator.ReferenceOf)) // (*/&)hello = world
            {
                if (unaryExp.Argument.Type == Nodes.Identifier ||
                    unaryExp.Argument is AttributeMemberExpression ||
                    unaryExp.Argument is StaticMemberExpression ||
                    unaryExp.Argument is ComputedMemberExpression)
                    CompileExpression(frame, unaryExp.Argument);
                else
                    ThrowCompilationError(expression, "Unexpected assignment to unary argument. Only Indirection (*) or Reference (&) is allowed.");
            }
            else
            {
                ThrowCompilationError(expression, $"Unimplemented variable assignment type: '{expression.Type}'");
            }

            if (popValue)
                InsertAssignPop(frame);
            else
                frame.AddInstruction(InsAssign.Default, 0);
        }

        /// <summary>
        /// Or ternary for short - test ? consequent : alternate;
        /// </summary>
        /// <param name="condExpression"></param>
        private void CompileConditionalExpression(AdhocCodeFrame frame, ConditionalExpression condExpression)
        {
            // Compile condition
            CompileExpression(frame, condExpression.Test);

            InsJumpIfFalse alternateJump = new InsJumpIfFalse();
            frame.AddInstruction(alternateJump, 0);

            if (IsUnaryReferenceOfExpression(condExpression.Consequent))
                CompileUnaryExpression(frame, condExpression.Consequent as UnaryExpression, popResult: false, asReference: true);
            else
                CompileExpression(frame, condExpression.Consequent);

            // This jump will skip the alternate statement if the consequent path is taken
            InsJump altSkipJump = new InsJump();
            frame.AddInstruction(altSkipJump, 0);

            if (frame.Version >= 12)
                InsertPop(frame);

            // Update alternate jump index now that we've compiled the consequent
            alternateJump.JumpIndex = frame.GetLastInstructionIndex();

            // Proceed to compile alternate/no match statement
            if (IsUnaryReferenceOfExpression(condExpression.Alternate))
                CompileUnaryExpression(frame, condExpression.Alternate as UnaryExpression, popResult: false, asReference: true);
            else
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

            if (frame.Version >= 12)
                frame.AddInstruction(InsElementEval.Default, 0);
            else
            {
                // Below, including 11 uses direct symbols
                var indexerIns = new InsBinaryOperator(SymbolMap.RegisterSymbol(AdhocConstants.OPERATOR_SUBSCRIPT, convertToOperand: frame.Version > 10));

                frame.AddInstruction(indexerIns, computedMember.Location.Start.Line);
                frame.AddInstruction(InsEval.Default, 0);
            }
        }

        /// <summary>
        /// Compiles array or map element assignment (ELEMENT_PUSH)
        /// </summary>
        private void CompileComputedMemberExpressionAssignmentPush(AdhocCodeFrame frame, ComputedMemberExpression computedMember)
        {
            CompileExpression(frame, computedMember.Object);
            CompileExpression(frame, computedMember.Property);

            if (frame.Version >= 12)
                frame.AddInstruction(InsElementPush.Default, 0);
            else
            {
                // Below, including 11 uses direct symbols
                var indexerIns = new InsBinaryOperator(SymbolMap.RegisterSymbol(AdhocConstants.OPERATOR_SUBSCRIPT, convertToOperand: frame.Version > 10));

                frame.AddInstruction(indexerIns, computedMember.Location.Start.Line);
            }
        }

        private void CompileObjectSelectorExpressionAssignmentPush(AdhocCodeFrame frame, ObjectSelectorMemberExpression objSelector)
        {
            CompileExpression(frame, objSelector.Object);
            CompileExpression(frame, objSelector.Property);

            frame.AddInstruction(InsObjectSelector.Default, 0);
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

        private void CompileObjectSelectorExpression(AdhocCodeFrame frame, ObjectSelectorMemberExpression objSelectExpr)
        {
            CompileExpression(frame, objSelectExpr.Object);
            CompileExpression(frame, objSelectExpr.Property);
            frame.AddInstruction(InsObjectSelector.Default, 0);
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

            string fullPath = string.Join(AdhocConstants.OPERATOR_STATIC, pathParts);
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

            string fullPath = string.Join(AdhocConstants.OPERATOR_STATIC, pathParts);
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

            string fullPath = string.Join(AdhocConstants.OPERATOR_STATIC, pathParts);
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
            bool isVariadicCall = false;
            if (call.Callee is Identifier ident && ident.Name == "call")
            {
                if (call.Arguments.Count < 1)
                    ThrowCompilationError(call, CompilationMessages.Error_VaCall_MissingFunctionTarget);

                if (call.Arguments.Count < 2)
                    ThrowCompilationError(call, CompilationMessages.Error_VaCall_MissingArguments);
                
                foreach (var arg in call.Arguments)
                    CompileExpression(frame, arg);

                isVariadicCall = true;
            }
            else
            {
                CompileExpression(frame, call.Callee);

                for (int i = 0; i < call.Arguments.Count; i++)
                {
                    if (call.Arguments[i].Type == Nodes.SpreadElement) // Has more than 1
                        ThrowCompilationError(call.Arguments[i], "Only a spread element as an argument is allowed in a Variable function call (VA_CALL). There must not be more than one argument.");

                    if (IsUnaryReferenceOfExpression(call.Arguments[i]))
                        CompileUnaryExpression(frame, call.Arguments[i] as UnaryExpression, asReference: true); // We may be pushing it
                    else
                        CompileExpression(frame, call.Arguments[i]);
                }
            }

            if (isVariadicCall)
            {
                var vaCallIns = new InsVaCall() { PopObjectCount = (uint)call.Arguments.Count };
                frame.AddInstruction(vaCallIns, call.Location.Start.Line);
                AddPostCompilationWarning(CompilationMessages.Warning_UsingVaCall_Code);
            }
            else
            {
                var callIns = new InsCall(call.Arguments.Count);
                frame.AddInstruction(callIns, call.Location.Start.Line);
            }

            if (frame.Version < 12)
                frame.AddInstruction(new InsEval(), call.Location.Start.Line);

            // When calling and not caring about returns
            if (popReturnValue)
                InsertPop(frame);
        }

        /// <summary>
        /// Compiles a binary expression.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="binExp"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void CompileBinaryExpression(AdhocCodeFrame frame, BinaryExpression binExp)
        {
            if (binExp.Left.Type == Nodes.AssignmentExpression)
            {
                // (r = x % y) != 0 - reuse result
                CompileAssignmentExpression(frame, binExp.Left as AssignmentExpression, false);
            }
            else
            {
                CompileExpression(frame, binExp.Left);
            }

            // Check for logical operators that checks between both conditions
            if (binExp.Operator == BinaryOperator.LogicalAnd || binExp.Operator == BinaryOperator.LogicalOr)
            {
                if (binExp.Operator == BinaryOperator.LogicalOr)
                {
                    if (frame.Version > 10)
                    {
                        var orIns = new InsLogicalOr();
                        frame.AddInstruction(orIns, 0);
                        CompileExpression(frame, binExp.Right);
                        orIns.InstructionJumpIndex = frame.GetLastInstructionIndex();
                    }
                    else
                    {
                        var orIns = new InsLogicalOrOld();
                        frame.AddInstruction(orIns, 0);
                        InsertPop(frame);
                        CompileExpression(frame, binExp.Right);
                        orIns.InstructionJumpIndex = frame.GetLastInstructionIndex();
                    }
                }
                else if (binExp.Operator == BinaryOperator.LogicalAnd)
                {
                    if (frame.Version > 10)
                    {
                        var andIns = new InsLogicalAnd();
                        frame.AddInstruction(andIns, 0);
                        CompileExpression(frame, binExp.Right);
                        andIns.InstructionJumpIndex = frame.GetLastInstructionIndex();
                    }
                    else
                    {
                        var andIns = new InsLogicalAndOld();
                        frame.AddInstruction(andIns, 0);
                        InsertPop(frame);
                        CompileExpression(frame, binExp.Right);
                        andIns.InstructionJumpIndex = frame.GetLastInstructionIndex();
                    }
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
                    BinaryOperator.Equal => AdhocConstants.OPERATOR_EQUAL,
                    BinaryOperator.NotEqual => AdhocConstants.OPERATOR_NOT_EQUAL,
                    BinaryOperator.Less => AdhocConstants.OPERATOR_LESS_THAN,
                    BinaryOperator.Greater => AdhocConstants.OPERATOR_GREATER_THAN,
                    BinaryOperator.LessOrEqual => AdhocConstants.OPERATOR_LESS_OR_EQUAL,
                    BinaryOperator.GreaterOrEqual => AdhocConstants.OPERATOR_GREATER_OR_EQUAL,
                    BinaryOperator.Plus => AdhocConstants.OPERATOR_ADD,
                    BinaryOperator.Minus => AdhocConstants.OPERATOR_SUBTRACT,
                    BinaryOperator.Divide => AdhocConstants.OPERATOR_DIVIDE,
                    BinaryOperator.Times => AdhocConstants.OPERATOR_MULTIPLY,
                    BinaryOperator.Modulo => AdhocConstants.OPERATOR_MODULO,
                    BinaryOperator.BitwiseOr => AdhocConstants.OPERATOR_BITWISE_OR,
                    BinaryOperator.BitwiseXOr => AdhocConstants.OPERATOR_BITWISE_XOR,
                    BinaryOperator.BitwiseAnd => AdhocConstants.OPERATOR_BITWISE_AND,
                    BinaryOperator.LeftShift => AdhocConstants.OPERATOR_LEFT_SHIFT,
                    BinaryOperator.RightShift => AdhocConstants.OPERATOR_RIGHT_SHIFT,
                    BinaryOperator.Exponentiation => AdhocConstants.OPERATOR_POWER,
                    _ => null
                };

                if (opStr is null)
                    ThrowCompilationError(binExp, $"Binary operator {binExp.Operator} not implemented");

                AdhocSymbol opSymbol = SymbolMap.RegisterSymbol(opStr, convertToOperand: frame.Version > 10);
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
        /// <param name="frame">Current frame.</param>
        /// <param name="unaryExp">Target expression.</param>
        /// <param name="popResult">Whether to pop after the expression to not reuse the result.</param>
        /// <param name="asReference">Whether to treat the expression as a reference, result may or may not be pushed to the reference variable.</param>
        /// <exception cref="NotImplementedException"></exception>
        private void CompileUnaryExpression(AdhocCodeFrame frame, UnaryExpression unaryExp, bool popResult = false, bool asReference = false)
        {
            if (unaryExp is UpdateExpression upd) // ++var / --var etc
            {
                if (!asReference)
                {
                    // Assigning to a variable - we need to push
                    PushUnaryExpressionArgument(frame, unaryExp.Argument);
                }
                else
                {
                    // Reference objects can just be eval'd when doing something like '&myObj--'
                    CompileExpression(frame, upd.Argument);
                }

                bool preIncrement = unaryExp.Prefix;

                string op = unaryExp.Operator switch
                {
                    UnaryOperator.Increment when !preIncrement => AdhocConstants.UNARY_OPERATOR_POST_INCREMENT,
                    UnaryOperator.Increment when preIncrement => AdhocConstants.UNARY_OPERATOR_PRE_INCREMENT,
                    UnaryOperator.Decrement when !preIncrement => AdhocConstants.UNARY_OPERATOR_POST_DECREMENT,
                    UnaryOperator.Decrement when preIncrement => AdhocConstants.UNARY_OPERATOR_PRE_DECREMENT,
                    _ => throw new NotImplementedException("TODO"),
                };

                bool opToSymbol = frame.Version >= 12;
                AdhocSymbol symb = SymbolMap.RegisterSymbol(op, opToSymbol);
                InsUnaryAssignOperator unaryIns = new InsUnaryAssignOperator(symb);
                frame.AddInstruction(unaryIns, unaryExp.Location.Start.Line);
            }
            else if (unaryExp.Operator == UnaryOperator.Indirection) // *var - eval variable
            {
                CompileExpression(frame, unaryExp.Argument);

                // Eval said local
                frame.AddInstruction(InsEval.Default, 0);
            }
            else if (unaryExp.Operator == UnaryOperator.ReferenceOf) // &var - get reference of variable
            {
                if (asReference)
                    PushUnaryExpressionArgument(frame, unaryExp.Argument);
                else if (unaryExp.Argument is UpdateExpression updArg)
                    CompileUnaryExpression(frame, updArg, asReference: true);
                else
                    CompileExpression(frame, unaryExp.Argument);
            }
            else // -var / +var / ~var
            {
                CompileExpression(frame, unaryExp.Argument);
                string op = unaryExp.Operator switch
                {
                    UnaryOperator.LogicalNot => AdhocConstants.UNARY_OPERATOR_LOGICAL_NOT,
                    UnaryOperator.Minus => AdhocConstants.UNARY_OPERATOR_MINUS,
                    UnaryOperator.Plus => AdhocConstants.UNARY_OPERATOR_PLUS,
                    UnaryOperator.BitwiseNot => AdhocConstants.UNARY_OPERATOR_BITWISE_INVERT,
                    _ => throw new NotImplementedException("TODO"),
                };

                bool opToSymbol = frame.Version >= 12;
                AdhocSymbol symb = SymbolMap.RegisterSymbol(op, opToSymbol);
                InsUnaryOperator unaryIns = new InsUnaryOperator(symb);
                frame.AddInstruction(unaryIns, unaryExp.Location.Start.Line);
            }

            // If we aren't assigning, or not using the return value immediately, pop it
            // Usages: i++;
            //         for (var i = 0; i < 10; [i++])

            if (popResult)
                InsertPop(frame);
        }

        private void PushUnaryExpressionArgument(AdhocCodeFrame frame, Expression expression)
        {
            if (expression.Type == Nodes.Identifier)
            {
                InsertVariablePush(frame, expression as Identifier, isVariableDeclaration: false);
            }
            else if (expression.Type == Nodes.MemberExpression)
            {
                if (expression is AttributeMemberExpression attr)
                {
                    // ++myObj.property
                    CompileAttributeMemberAssignmentPush(frame, attr);
                }
                else if (expression is ComputedMemberExpression comp)
                {
                    // --hello["world"];
                    CompileComputedMemberExpressionAssignmentPush(frame, comp);
                }
                else if (expression is StaticMemberExpression staticMemberExpression)
                {
                    // ++GameParameterUtil::loaded_time;
                    CompileStaticMemberExpressionAssignment(frame, staticMemberExpression);
                }
                else
                    ThrowCompilationError(expression, CompilationMessages.Error_UnsupportedUnaryOprationOnMemberExpression);
            }
            else if (expression.Type == Nodes.Literal)
            {
                // Special case: -1 -> int const + unary op
                CompileLiteral(frame, expression as Literal);
            }
            else if (expression.Type == Nodes.CallExpression)
            {
                // --doThing();
                CompileCall(frame, expression as CallExpression);
            }
            else if (expression.Type == Nodes.BinaryExpression)
            {
                // ++(1 + 1)
                CompileBinaryExpression(frame, expression as BinaryExpression);
            }
            else
                ThrowCompilationError(expression, $"Unsupported unary operation on type: {expression.Type}");
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
                    frame.AddInstruction(new InsNilConst(), literal.Location.Start.Line);
                    break;

                case TokenType.BooleanLiteral:
                    if (frame.Version >= 12)
                    {
                        // On later versions, a specialized bool instruction was added
                        InsBoolConst boolConst = new InsBoolConst((literal.Value as bool?).Value);
                        frame.AddInstruction(boolConst, literal.Location.Start.Line);
                    }
                    else
                    {
                        // Translate bool to int
                        InsIntConst intConst = new InsIntConst((literal.Value as bool?).Value ? 1 : 0);
                        frame.AddInstruction(intConst, literal.Location.Start.Line);
                    }
                    break;

                case TokenType.NumericLiteral:

                    InstructionBase ins;
                    switch (literal.NumericTokenType)
                    {
                        case NumericTokenType.Integer:
                            ins = new InsIntConst((int)literal.NumericValue);
                            break;

                        case NumericTokenType.Float:
                            ins = new InsFloatConst((float)literal.NumericValue);
                            break;

                        case NumericTokenType.UnsignedInteger:
                            if (frame.Version < 12)
                                ThrowCompilationError(literal, "Unsigned integer literals are only available in Adhoc version 12 and above.");
                            ins = new InsUIntConst((uint)literal.NumericValue);
                            break;

                        case NumericTokenType.Long:
                            ins = new InsLongConst((long)literal.NumericValue);
                            break;

                        case NumericTokenType.UnsignedLong:
                            if (frame.Version < 12)
                                ThrowCompilationError(literal, "Unsigned long literals are only available in Adhoc version 12 and above.");
                            ins = new InsULongConst((ulong)literal.NumericValue);
                            break;

                        case NumericTokenType.Double:
                            if (frame.Version < 12)
                                ThrowCompilationError(literal, "Double literals are only available in Adhoc version 12 and above.");
                            ins = new InsDoubleConst((double)literal.NumericValue);
                            break;

                        default:
                            throw GetCompilationError(literal, "Unknown numeric literal type");
                    }

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
            else if (AdhocDefineConstants.CompilerProvidedConstants.ContainsKey(identifier.Name))
            {
                var define = AdhocDefineConstants.CompilerProvidedConstants[identifier.Name];
                if (define is InsStringConst str)
                    str.String = SymbolMap.RegisterSymbol(str.String.Name);

                frame.AddInstruction(define, identifier.Location.Start.Line);
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
        private void CompileAttributeMemberAssignmentPush(AdhocCodeFrame frame, AttributeMemberExpression attr)
        {
            // Pushing to object attribute
            CompileExpression(frame, attr.Object);
            if (attr.Property is not Identifier)
                ThrowCompilationError(attr.Property, "Expected attribute member property identifier.");

            var propIdent = attr.Property as Identifier;

            InsAttributePush attrPush = new InsAttributePush();
            AdhocSymbol attrSymbol = SymbolMap.RegisterSymbol(propIdent.Name);
            attrPush.AttributeSymbols.Add(attrSymbol);
            frame.AddInstruction(attrPush, propIdent.Location.Start.Line);
        }


        #region Scope/Loop Handling
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
            ModuleStack.Push(newModule);

            newModule.ParentModule = frame.CurrentModule;
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
        private void LeaveScope(AdhocCodeFrame frame, 
            bool insertLeaveInstruction = true, 
            bool isModuleLeave = false, 
            bool isModuleExitFromSubroutine = false)
        {
            var lastScope = frame.CurrentScopes.Pop();

            // No such thing as cleanup in older versions
            if (frame.Version >= 11)
            {
                if (!isModuleLeave) // Module leaves don't actually reset the max.
                {
                    // Clear up/rewind
                    foreach (var variable in lastScope.LocalScopeVariables)
                        frame.FreeLocalVariable(variable.Value);
                }
                else
                {
                    foreach (var variable in lastScope.StaticScopeVariables)
                        frame.FreeStaticVariable(variable.Value);
                }
            }

            if (insertLeaveInstruction && frame.Version >= 11) // Leave only available >= 11
            {
                InsLeaveScope leave = new InsLeaveScope();

                if (isModuleLeave)
                {
                    leave.ModuleOrClassDepthRewindIndex = ModuleStack.Count - 1;

                    if (isModuleExitFromSubroutine)
                        leave.VariableStorageRewindIndex = frame.Stack.GetLastLocalVariableIndex();
                    else
                        leave.VariableStorageRewindIndex = 1;
                }
                else
                {
                    leave.VariableStorageRewindIndex = frame.Stack.GetLastLocalVariableIndex();
                }

                frame.AddInstruction(leave, 0);
            }
        }

        private void LeaveModuleOrClass(AdhocCodeFrame frame, bool fromSubroutine = false)
        {
            LeaveScope(frame, isModuleLeave: true, isModuleExitFromSubroutine: fromSubroutine);
            ModuleStack.Pop();
            frame.CurrentModule = ModuleStack.Peek();
        }
        #endregion

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


        #region Instruction insert methods
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
            int idx = frame.AddScopeVariable(symb, isAssignment: false);
            var varEval = new InsVariableEvaluation(idx);
            varEval.VariableSymbols.Add(symb); // Only one
            frame.AddInstruction(varEval, identifier.Location.Start.Line);

            // Static references or pushes always have double their own symbol
            // If its a static reference, do not add it as a declared variable within this scope
            if (frame.IsStaticVariable(symb))
                varEval.VariableSymbols.Add(symb); // Static, two symbols
        }

        /// <summary>
        /// Inserts a new variable.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="variable"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        // Mostly used for temp variables produced by the compiler
        private AdhocSymbol InsertNewLocalVariable(AdhocCodeFrame frame, Expression exprValue, string variable, Location location = default)
        {
            if (frame.Version > 10)
            {
                if (exprValue is not null)
                    CompileExpression(frame, exprValue);

                AdhocSymbol symb = InsertVariablePush(frame, new Identifier(variable) { Location = location }, true);
                InsertAssignPop(frame);
                return symb;
            }
            else
            {
                AdhocSymbol symb = InsertVariablePush(frame, new Identifier(variable) { Location = location }, true);
                CompileExpression(frame, exprValue);
                InsertAssignPop(frame);
                return symb;
            }
        }

        /// <summary>
        /// Inserts a new variable eval from a symbol.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        // Mostly used for temp variables produced by the compiler
        private void InsertVariableEvalFromSymbol(AdhocCodeFrame frame, AdhocSymbol symbol, Location location = default)
        {
            LocalVariable taskVariable = frame.Stack.GetLocalVariableBySymbol(symbol);
            int taskVariableStoreIdx = frame.Stack.GetLocalVariableIndex(taskVariable);

            var insVarEval = new InsVariableEvaluation();
            insVarEval.VariableSymbols.Add(symbol);
            insVarEval.VariableStorageIndex = taskVariableStoreIdx;
            frame.AddInstruction(insVarEval, location.Start.Line);
        }

        /// <summary>
        /// Inserts a version-aware assign pop
        /// </summary>
        /// <param name="frame"></param>
        private void InsertAssignPop(AdhocCodeFrame frame)
        {
            if (frame.Version >= 11)
            {
                frame.AddInstruction(InsAssignPop.Default, 0);
            }
            else
            {
                if (frame.Version >= 10)
                    frame.AddInstruction(InsAssign.Default, 0);
                else // Assume under 10 that its the traditional assign + pop old
                    frame.AddInstruction(InsAssignOld.Default, 0);

                InsertPop(frame);
            }
        }

        /// <summary>
        /// Inserts a version-aware pop
        /// </summary>
        /// <param name="frame"></param>
        private void InsertPop(AdhocCodeFrame frame)
        {
            if (frame.Version >= 10)
                frame.AddInstruction(InsPop.Default, 0);
            else // Assume under 10 that its the traditional assign + pop old
                frame.AddInstruction(InsPopOld.Default, 0);
        }

        /// <summary>
        /// Inserts a version-aware set state
        /// </summary>
        /// <param name="frame"></param>
        private void InsertSetState(AdhocCodeFrame frame, AdhocRunState state)
        {
            if (frame.Version >= 10)
                frame.AddInstruction(new InsSetState(state), 0);
            else
                frame.AddInstruction(new InsSetStateOld(state), 0);
        }

        /// <summary>
        /// Inserts a version-aware void
        /// </summary>
        /// <param name="frame"></param>
        private void InsertVoid(AdhocCodeFrame frame)
        {
            if (frame.Version > 10)
                frame.AddInstruction(new InsVoidConst(), 0);
            else
                frame.AddInstruction(InsNop.Empty, 0);
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
                ThrowCompilationError(parentNode, $"Unrecognized operator '{opStr}'");

            bool opToSymbol = frame.Version >= 12;
            var symb = SymbolMap.RegisterSymbol(opStr, opToSymbol);

            if (frame.Version >= 5)
            {
                frame.AddInstruction(new InsBinaryAssignOperator(symb), lineNumber);
            }
            else
            {
                // FIXME: Not sure about this, and the version
                frame.AddInstruction(new InsBinaryOperator(symb), lineNumber);
                frame.AddInstruction(new InsAssign(), lineNumber);
            }


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
                if (frame.Version >= 12)
                {
                    // All functions return a value internally in newer adhoc, even if they don't in the code.
                    // So, add one.
                    InsertVoid(frame);
                }

                InsertSetState(frame, AdhocRunState.RETURN);
            }
        }
        #endregion

        #region Utils

        private static string AssignOperatorToString(AssignmentOperator op)
        {
            return op switch
            {
                AssignmentOperator.PlusAssign => AdhocConstants.OPERATOR_ADD,
                AssignmentOperator.MinusAssign => AdhocConstants.OPERATOR_SUBTRACT,
                AssignmentOperator.TimesAssign => AdhocConstants.OPERATOR_MULTIPLY,
                AssignmentOperator.DivideAssign => AdhocConstants.OPERATOR_DIVIDE,
                AssignmentOperator.ModuloAssign => AdhocConstants.OPERATOR_MODULO,
                AssignmentOperator.BitwiseAndAssign => AdhocConstants.OPERATOR_BITWISE_AND,
                AssignmentOperator.BitwiseOrAssign => AdhocConstants.OPERATOR_BITWISE_OR,
                AssignmentOperator.BitwiseXOrAssign => AdhocConstants.OPERATOR_BITWISE_XOR,
                AssignmentOperator.ExponentiationAssign => AdhocConstants.OPERATOR_POWER,
                AssignmentOperator.RightShiftAssign => AdhocConstants.OPERATOR_RIGHT_SHIFT,
                AssignmentOperator.LeftShiftAssign => AdhocConstants.OPERATOR_LEFT_SHIFT,
                _ => null
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

        /// <summary>
        /// Returns whether the provided expression is an unary reference of expression (i.e &myObj).
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        private static bool IsUnaryReferenceOfExpression(Expression exp)
        {
            return exp is UnaryExpression unaryExp && unaryExp.Operator == UnaryOperator.ReferenceOf;
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
        #endregion

        #region Warning/Error Handling Methods
        private void PrintPostCompilationWarnings()
        {
            foreach (var warn in PostCompilationWarnings)
                Logger.Warn($"Feature Warning: {CompilationMessages.Warnings[warn]}. This may crash older game builds.");
        }

        private void AddPostCompilationWarning(string warningCode)
        {
            if (!PostCompilationWarnings.Contains(warningCode))
                PostCompilationWarnings.Add(warningCode);
        }

        private void PrintCompilationWarning(Node node, string message)
        {
            Logger.Warn(GetSourceNodeString(node, message));
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
            return new AdhocCompilationException(GetSourceNodeString(node, message));
        }

        private string GetSourceNodeString(Node node, string message)
        {
            return $"{message} at {node.Location.Source}:{node.Location.Start.Line}";
        }
        #endregion
    }
}
