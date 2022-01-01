
using Esprima;
using Esprima.Ast;

using GTAdhocCompiler.Instructions;

namespace GTAdhocCompiler
{
    /// <summary>
    /// Adhoc script compiler.
    /// </summary>
    public class AdhocScriptCompiler : AdhocCodeFrame
    {
        public AdhocSymbolMap SymbolMap { get; set; } = new();

        public string ProjectDirectory { get; set; }
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
            EnterScope(this, script);
            CompileScriptBody(this, script);
            LeaveScope(this);

            // Script done.
            this.AddInstruction(new InsSetState(AdhocRunState.EXIT), 0);
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
                case Nodes.ThrowStatement:
                    CompileThrowStatement(frame, node as ThrowStatement);
                    break;
                default:
                    ThrowCompilationError(node, "Statement not supported");
                    break;
            }
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
            string pathToIncludeFile = Path.Combine(ProjectDirectory, include.Path);
            if (!File.Exists(pathToIncludeFile))
                ThrowCompilationError(include, $"Include file does not exist: {pathToIncludeFile}.");

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

        private void CompileNewClass(AdhocCodeFrame frame, Identifier id, Node superClass, ClassBody body, bool isModule = false)
        {
            if (id is null || id is not Identifier)
            {
                ThrowCompilationError(id, "Class or module name must have a valid identifier.");
                return;
            }

            EnterScope(frame, body);

            if (isModule)
            {
                InsModuleDefine mod = new InsModuleDefine();
                mod.Names.Add(SymbolMap.RegisterSymbol(id.Name));
                frame.AddInstruction(mod, id.Location.Start.Line);
            }
            else
            {
                InsClassDefine @class = new InsClassDefine();
                @class.Name = SymbolMap.RegisterSymbol(id.Name);
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

            CompileClassBody(frame, body);

            LeaveScope(frame);

            // Exit class or module scope. Important.
            InsSetState state = new InsSetState(AdhocRunState.EXIT);
            frame.AddInstruction(state, 0);
        }

        public void CompileClassBody(AdhocCodeFrame frame, ClassBody classBody)
        {
            foreach (var prop in classBody.Body)
            {
                if (prop is Expression exp)
                    CompileExpression(frame, exp);
                else if (prop is IncludeStatement includeStatement)
                {
                    CompileIncludeStatement(frame, includeStatement);
                }
                else
                {
                    ThrowCompilationError(prop, "Unsupported class body element");
                }
            }
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

        public void CompileClassProperty(AdhocCodeFrame frame, ClassProperty classProp)
        {
            // For some reason the underlaying function expression has its id null when in a class
            if (classProp is MethodDefinition methodDef)
            {
                (classProp.Value as FunctionExpression).Id = classProp.Key as Identifier;
            }

            CompileExpression(frame, classProp.Value);
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
                CompileExpression(frame, forStatement.Update);

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
            if (whileStatement.Test is not null)
                CompileExpression(frame, whileStatement.Test);

            InsJumpIfFalse jumpIfFalse = new InsJumpIfFalse(); // End loop jumper
            frame.AddInstruction(jumpIfFalse, 0);

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

            InsJumpIfTrue jumpIfTrue = new InsJumpIfTrue(); // Start loop jumper
            jumpIfTrue.JumpIndex = loopStartInsIndex;
            frame.AddInstruction(jumpIfTrue, 0);

            // Process break jumps before doing the final exit
            int loopEndIndex = frame.GetLastInstructionIndex();
            foreach (var breakJmp in loopCtx.BreakJumps)
                breakJmp.JumpInstructionIndex = loopEndIndex;

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
            string itorVariable = $"in#{SymbolMap.SwitchCaseTempValues++}";
            Identifier itorIdentifier = new Identifier(itorVariable)
            {
                Location = foreachStatement.Right.Location,
            };

            InsertVariablePush(frame, itorIdentifier);
            frame.AddInstruction(InsAssignPop.Default, 0);

            // Test - fetch_next returns whether the iterator is done or not
            int testInsIndex = frame.GetLastInstructionIndex();
            InsertVariableEval(frame, itorIdentifier);
            InsAttributeEvaluation fetchNextIns = new InsAttributeEvaluation();
            fetchNextIns.AttributeSymbols.Add(SymbolMap.RegisterSymbol("fetch_next"));
            frame.AddInstruction(fetchNextIns, foreachStatement.Right.Location.Start.Line);

            InsJumpIfFalse exitJump = new InsJumpIfFalse(); // End loop jumper
            frame.AddInstruction(exitJump, 0);

            // Entering body, but we need to unbox the iterator's value into our declared variable
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
            string tmpCaseVariable = $"case#{SymbolMap.SwitchCaseTempValues++}";
            AdhocSymbol labelSymb = InsertVariablePush(frame, new Identifier(tmpCaseVariable));
            frame.AddInstruction(InsAssignPop.Default, 0);

            Dictionary<SwitchCase, InsJumpIfTrue> caseBodyJumps = new();
            InsJump defaultJump = null;

            // Write switch table jumps
            for (int i = 0; i < switchStatement.Cases.Count; i++)
            {
                SwitchCase swCase = switchStatement.Cases[i];
                if (swCase.Test != null) // Actual case
                {
                    // Get temp variable
                    InsVariableEvaluation tempVar = new InsVariableEvaluation();
                    tempVar.VariableSymbols.Add(labelSymb);
                    tempVar.VariableHeapIndex = frame.Stack.LocalVariableStorage.IndexOf(tmpCaseVariable);
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
                    if (defaultJump is not null)
                        ThrowCompilationError(swCase, "Switch already has a default statement.");

                    InsJump jmp = new InsJump();
                    defaultJump = jmp;
                    frame.AddInstruction(defaultJump, swCase.Location.Start.Line);
                }
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
            CompileSubroutine(frame, funcDecl, funcDecl.Body, funcDecl.Id, funcDecl.Params, isMethod: false);
        }

        public void CompileSubroutine(AdhocCodeFrame frame, Node parentNode, Node body, Identifier id, NodeList<Expression> subParams, bool isMethod)
        {
            SubroutineBase subroutine = isMethod ? new InsMethodDefine() : new InsFunctionDefine();
            if (id is not null)
                subroutine.Name = SymbolMap.RegisterSymbol(id.Name);
            subroutine.CodeFrame.SourceFilePath = frame.SourceFilePath;
            subroutine.CodeFrame.ParentFrame = this;

            foreach (Expression param in subParams)
            {
                if (param is Identifier paramIdent)
                {
                    AdhocSymbol paramSymb = SymbolMap.RegisterSymbol(paramIdent.Name);
                    subroutine.CodeFrame.FunctionParameters.Add(paramSymb);
                    subroutine.CodeFrame.DeclaredVariables.Add(paramSymb.Name);

                    // Function param is uninitialized, push nil
                    frame.AddInstruction(InsNilConst.Empty, paramIdent.Location.Start.Line);
                    
                }
                else if (param is AssignmentExpression assignmentExpression)
                {
                    if (assignmentExpression.Left is not Identifier || assignmentExpression.Right is not Literal)
                        ThrowCompilationError(parentNode, "Subroutine parameter assignment must be an identifier to a literal. (value = 0)");

                    AdhocSymbol paramSymb = SymbolMap.RegisterSymbol((assignmentExpression.Left as Identifier).Name);
                    subroutine.CodeFrame.FunctionParameters.Add(paramSymb);
                    subroutine.CodeFrame.DeclaredVariables.Add(paramSymb.Name);

                    // Push default value
                    CompileLiteral(frame, assignmentExpression.Right as Literal);
                }
                else
                    ThrowCompilationError(parentNode, "Subroutine definition parameters must all be identifiers.");
            }

            frame.AddScopeVariable(subroutine.Name, isVariableDeclaration: false);
            frame.AddInstruction(subroutine, parentNode.Location.Start.Line);

            if (body is BlockStatement blockStatement)
            {
                EnterScope(subroutine.CodeFrame, parentNode);
                foreach (var param in subroutine.CodeFrame.FunctionParameters)
                    subroutine.CodeFrame.AddScopeVariable(param);
                
                CompileBlockStatement(subroutine.CodeFrame, blockStatement, openScope: false, insertLeaveInstruction: false);
            }
            else
                ThrowCompilationError(body, "Expected subroutine body to be frame statement.");

            InsertFrameExitIfNeeded(subroutine.CodeFrame, body);
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

            if (initValue != null)
                CompileExpression(frame, initValue);

            // Now write the id
            if (id is null)
                ThrowCompilationError(varDeclaration, "Variable declaration for id is null.");

            if (id is Identifier idIdentifier) // var hello [= world];
            {
                if (initValue != null || pushWhenNoInit)
                {
                    // Variable is being defined with a value.
                    InsertVariablePush(frame, idIdentifier);

                    // Perform assignment
                    frame.AddInstruction(InsAssignPop.Default, 0);
                }
                else
                {
                    // Variable is declared but not assigned to anything yet. Do not add any variable push.
                    AdhocSymbol varSymb = SymbolMap.RegisterSymbol(idIdentifier.Name);
                    frame.AddScopeVariable(varSymb);
                }
            }
            else if (id is ArrayPattern arrayPattern) // var [hello, world] = helloworld; - deconstruct array
            {
                if (arrayPattern.Elements.Count == 0)
                    ThrowCompilationError(arrayPattern, "Array pattern has no elements.");

                foreach (Expression exp in arrayPattern.Elements)
                {
                    if (exp is not Identifier)
                        ThrowCompilationError(exp, "Expected array element to be identifier.");

                    Identifier arrElemIdentifier = exp as Identifier;
                    InsertVariablePush(frame, arrElemIdentifier);
                }

                InsListAssign listAssign = new InsListAssign(arrayPattern.Elements.Count);
                frame.AddInstruction(listAssign, arrayPattern.Location.Start.Line);
                frame.AddInstruction(InsPop.Default, 0); // Pop array from stack 
            }
            else
            {
                ThrowCompilationError(varDeclaration, "Variable declaration for id is not an identifier.");
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
            switch (exp)
            {
                case Identifier initIdentifier:
                    CompileIdentifier(frame, initIdentifier);
                    break;
                case FunctionExpression funcExpression:
                    CompileFunctionExpression(frame, funcExpression);
                    break;
                case MethodDefinition methodDefinition: // May also be a function!
                    CompileMethodDefinition(frame, methodDefinition);
                    break;
                case CallExpression callExp:
                    CompileCall(frame, callExp);
                    break;
                case UnaryExpression unaryExpression:
                    CompileUnaryExpression(frame, unaryExpression);
                    break;
                case AttributeMemberExpression attributeMemberException:
                    CompileAttributeMemberExpression(frame, attributeMemberException);
                    break;
                case StaticMemberExpression staticMemberExpression:
                    CompileStaticMemberExpression(frame, staticMemberExpression);
                    break;
                case BinaryExpression binExpression:
                    CompileBinaryExpression(frame, binExpression);
                    break;
                case Literal literal:
                    CompileLiteral(frame, literal);
                    break;
                case ArrayExpression arr:
                    CompileArrayExpression(frame, arr);
                    break;
                case ComputedMemberExpression comp:
                    CompileComputedMemberExpression(frame, comp);
                    break;
                case AssignmentExpression assignExp:
                    CompileAssignmentExpression(frame, assignExp);
                    break;
                case ConditionalExpression condExp:
                    CompileConditionalExpression(frame, condExp);
                    break;
                case TemplateLiteral templateLiteral:
                    CompileTemplateLiteral(frame, templateLiteral);
                    break;
                case TaggedTemplateExpression taggedTemplateExpression:
                    CompileTaggedTemplateExpression(frame, taggedTemplateExpression);
                    break;
                case PropertyDefinition propDefinition:
                    CompilePropertyDefinition(frame, propDefinition);
                    break;
                case StaticExpression staticExpr:
                    CompileStaticExpression(frame, staticExpr);
                    break;
                case ClassExpression classExpr:
                    CompileClassExpression(frame, classExpr);
                    break;
                case ArrowFunctionExpression arrowFuncExpr:
                    CompileArrowFunctionExpression(frame, arrowFuncExpr);
                    break;
                default:
                    ThrowCompilationError(exp, $"Expression {exp.Type} not supported");
                    break;
            }
        }

        /// <summary>
        /// Compiles: .doThing(e => <statement>)
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="arrowFuncExpr"></param>
        private void CompileArrowFunctionExpression(AdhocCodeFrame frame, ArrowFunctionExpression arrowFuncExpr)
        {
            InsFunctionConst funcConst = new InsFunctionConst();
            funcConst.CodeFrame.ParentFrame = frame;
            funcConst.CodeFrame.SourceFilePath = frame.SourceFilePath;

            /* Unlike JS, adhoc can capture variables from the parent frame
             * Example:
             *    var arr = [0, 1, 2];
             *    var map = Map();               
             *    arr.each(e => {
             *        map[e.toString()] = e * 100; -> Inserts a new key/value pair into map, which is from the parent frame
             *    });
             */
            funcConst.CodeFrame.CanCaptureVariablesFromParentFrame = true;

            EnterScope(funcConst.CodeFrame, arrowFuncExpr);
            foreach (Expression param in arrowFuncExpr.Params)
            {
                if (param is not Identifier)
                    ThrowCompilationError(param, "Expected function parameter to be an identifier.");

                Identifier paramIdent = param as Identifier;
                AdhocSymbol paramSymbol = SymbolMap.RegisterSymbol(paramIdent.Name);
                funcConst.CodeFrame.FunctionParameters.Add(paramSymbol);
                funcConst.CodeFrame.AddScopeVariable(paramSymbol, isVariableDeclaration: true);
            }

            CompileStatement(funcConst.CodeFrame, arrowFuncExpr.Body as BlockStatement);
            LeaveScope(funcConst.CodeFrame, insertLeaveInstruction: false);

            for (int i = 0; i < funcConst.CodeFrame.FunctionParameters.Count; i++)
                frame.AddInstruction(InsNilConst.Empty, 0);

            // "Insert" by evaluating each captured variable
            foreach (var capturedVariable in funcConst.CodeFrame.CapturedCallbackVariables)
                InsertVariableEval(frame, new Identifier(capturedVariable.Name));

            InsertFrameExitIfNeeded(funcConst.CodeFrame, arrowFuncExpr.Body);

            frame.AddInstruction(funcConst, arrowFuncExpr.Location.Start.Line);
        }

        private void CompileClassExpression(AdhocCodeFrame frame, ClassExpression classExpression)
        {
            CompileNewClass(frame, classExpression.Id, classExpression.SuperClass, classExpression.Body, classExpression.IsModule);
        }

        private void CompileStaticExpression(AdhocCodeFrame frame, StaticExpression staticExpression)
        {
            if (staticExpression.VarExpression is not AssignmentExpression)
                ThrowCompilationError(staticExpression, "Expected static keyword to be a variable assignment.");

            AssignmentExpression assignmentExpression = staticExpression.VarExpression as AssignmentExpression;
            if (assignmentExpression.Left is not Identifier)
                ThrowCompilationError(assignmentExpression, "Expected static declaration to be an identifier.");

            Identifier identifier = assignmentExpression.Left as Identifier;
            var idSymb = SymbolMap.RegisterSymbol(identifier.Name);

            InsStaticDefine staticDefine = new InsStaticDefine(idSymb);
            frame.AddInstruction(staticDefine, staticExpression.Location.End.Line);

            if (assignmentExpression.Operator == AssignmentOperator.Assign)
            {
                // Assigning to something new
                CompileExpression(frame, assignmentExpression.Right);
                CompileVariableAssignment(frame, assignmentExpression.Left, staticVariableDoubleSymbol: true);
            }
            else if (IsAdhocAssignWithOperandOperator(assignmentExpression.Operator))
            {
                // Assigning to self (+=)
                InsertVariablePush(frame, assignmentExpression.Left as Identifier, staticVariableDoubleSymbol: true); // Push current value first
                CompileExpression(frame, assignmentExpression.Right);

                InsertBinaryAssignOperator(frame, assignmentExpression, assignmentExpression.Operator, assignmentExpression.Location.Start.Line);
                frame.AddInstruction(InsPop.Default, 0);
            }
            else
            {
                ThrowCompilationError(assignmentExpression, $"Unimplemented operator assignment {assignmentExpression.Operator}");
            }
        }

        private void CompilePropertyDefinition(AdhocCodeFrame frame, PropertyDefinition propDefinition)
        {
            if (propDefinition.Key is not Identifier)
            {
                // Not actually a property definition - esprima hack
                CompileExpression(frame, propDefinition.Value);
                CompileExpression(frame, propDefinition.Key);
            }
            else
            {

                var ident = propDefinition.Key as Identifier;
                var symb = SymbolMap.RegisterSymbol(ident.Name);

                if (propDefinition.Static)
                {
                    InsStaticDefine staticDefine = new InsStaticDefine(symb);
                    frame.AddInstruction(staticDefine, propDefinition.Location.End.Line);

                    CompileExpression(frame, propDefinition.Value);

                    // Assign
                    InsertVariablePush(frame, ident, staticVariableDoubleSymbol: true);
                    frame.AddInstruction(InsAssignPop.Default, 0);
                }
                else
                {
                    CompileExpression(frame, propDefinition.Value);

                    // Declaring a class attribute, so we don't push anything
                    InsAttributeDefine attrDefine = new InsAttributeDefine();
                    attrDefine.AttributeName = SymbolMap.RegisterSymbol(ident.Name);
                    frame.AddInstruction(attrDefine, ident.Location.Start.Line);
                }
            }
        }

        private void CompileArrayExpression(AdhocCodeFrame frame, ArrayExpression arrayExpression)
        {
            frame.AddInstruction(new InsArrayConst((uint)arrayExpression.Elements.Count), arrayExpression.Location.Start.Line);

            foreach (var elem in arrayExpression.Elements)
            {
                if (elem is null)
                    ThrowCompilationError(arrayExpression, "Unsupported empty element in array declaration.");

                CompileExpression(frame, elem);

                frame.AddInstruction(InsArrayPush.Default, elem.Location.Start.Line);
            }
        }

        private void CompileExpressionStatement(AdhocCodeFrame frame, ExpressionStatement expStatement)
        {
            if (expStatement.Expression is CallExpression call)
            {
                // Call expressions need to be directly popped within an expression statement -> 'getThing()' instead of 'var thing = getThing()'.
                // Their return values aren't used.
                CompileCall(frame, call, popReturnValue: true);
            }
            else
            {
                CompileExpression(frame, expStatement.Expression);
            }
        }

        private void CompileMethodDefinition(AdhocCodeFrame frame, MethodDefinition methodDefinition)
        {
            if (methodDefinition.Kind == PropertyKind.Function)
            {
                var funcExp = methodDefinition.Value as FunctionExpression;
                CompileSubroutine(frame, funcExp, funcExp.Body, methodDefinition.Key as Identifier, funcExp.Params, isMethod: false);
            }
            else if (methodDefinition.Kind == PropertyKind.Method)
            {
                if (methodDefinition.Value is not FunctionExpression methodFunc)
                    ThrowCompilationError(methodDefinition, "Unexpected non-function expression value for method");

                if (methodDefinition.Key is not Identifier)
                    ThrowCompilationError(methodDefinition, "Unexpected non-function identifier key for method");

                methodFunc = methodDefinition.Value as FunctionExpression;
                CompileSubroutine(frame, methodDefinition, methodFunc.Body, methodDefinition.Key as Identifier, methodFunc.Params, isMethod: true);
            }
        }

        private void CompileFunctionExpression(AdhocCodeFrame frame, FunctionExpression funcExp)
        {
            CompileSubroutine(frame, funcExp, funcExp.Body, funcExp.Id, funcExp.Params, isMethod: false);
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
                    InsertVariablePush(frame, assignExpression.Left as Identifier); // Push current value first
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
        public void CompileVariableAssignment(AdhocCodeFrame frame, Expression expression, bool staticVariableDoubleSymbol = false)
        {
            if (expression is Identifier ident) // hello = world
            {
                InsertVariablePush(frame, ident, staticVariableDoubleSymbol);
            }
            else if (expression is AttributeMemberExpression attrMember) // Pushing into an object i.e hello.world = "!"
            {
                InsertAttributeMemberAssignmentPush(frame, attrMember);
            }
            else if (expression is ComputedMemberExpression compExpression)
            {
                CompileComputedMemberExpressionAssignment(frame, compExpression);
            }
            else
                ThrowCompilationError(expression, "Unimplemented");

            frame.AddInstruction(InsAssignPop.Default, 0);
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
        /// <param name="compileProperty">Whether to compile the property - used whenever we're compiling the eval call or not</param>
        private void CompileAttributeMemberExpression(AdhocCodeFrame frame, AttributeMemberExpression staticExp, bool compileProperty = true)
        {
            CompileExpression(frame, staticExp.Object); // ORG

            if (staticExp.Property is not Identifier)
                ThrowCompilationError(staticExp, "Expected attribute member to be identifier.");

            if (compileProperty)
                CompileIdentifier(frame, staticExp.Property as Identifier, attribute: true); // inSession
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

            int idx = frame.AddScopeVariable(fullPathSymb, isVariableDeclaration: false);
            eval.VariableHeapIndex = idx;

            frame.AddInstruction(eval, staticExp.Location.Start.Line);
            void BuildStaticPath(StaticMemberExpression exp, ref List<string> pathParts)
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
        }

        /// <summary>
        /// Compiles a function or method call.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="call"></param>
        private void CompileCall(AdhocCodeFrame frame, CallExpression call, bool popReturnValue = false)
        {
            /* NOTE: So adhoc has this "EVAL" instruction which seems to be called as such: myArray[] 
             * Obviously this is weird and Javascript doesnt support it normally
             * So instead we're shortcuting calls to eval of an object property to the actual instruction
             * It's a hack, but when you don't know the actual language syntax (and it may be a pain to implement anyway), this is the best compromise
             * Just don't name your method "eval" i guess  */
            if (call.Callee is AttributeMemberExpression exp && exp.Property is Identifier ident && ident.Name == "eval")
            {
                CompileAttributeMemberExpression(frame, exp, compileProperty: false);
                frame.AddInstruction(InsEval.Default, 0);
                return;
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
                CompileExpression(frame, binExp.Left);

                // Object.isInstanceOf - No idea if adhoc supports it, but eh, why not
                InsertAttributeEval(frame, new Identifier("isInstanceOf"));

                // Eval right identifier (if its one)
                CompileExpression(frame, binExp.Right);

                // Call.
                frame.AddInstruction(new InsCall(argumentCount: 1), binExp.Location.Start.Line);
            }
            else
            {
                
                CompileExpression(frame, binExp.Right);
                        
                AdhocSymbol opSymbol = null;
                switch (binExp.Operator)
                {
                    case BinaryOperator.Equal:
                        opSymbol = SymbolMap.RegisterSymbol("==");
                        break;
                    case BinaryOperator.NotEqual:
                        opSymbol = SymbolMap.RegisterSymbol("!=");
                        break;
                    case BinaryOperator.Less:
                        opSymbol = SymbolMap.RegisterSymbol("<");
                        break;
                    case BinaryOperator.Greater:
                        opSymbol = SymbolMap.RegisterSymbol(">");
                        break;
                    case BinaryOperator.LessOrEqual:
                        opSymbol = SymbolMap.RegisterSymbol("<=");
                        break;
                    case BinaryOperator.GreaterOrEqual:
                        opSymbol = SymbolMap.RegisterSymbol(">=");
                        break;
                    case BinaryOperator.Plus:
                        opSymbol = SymbolMap.RegisterSymbol("+");
                        break;
                    case BinaryOperator.Minus:
                        opSymbol = SymbolMap.RegisterSymbol("-");
                        break;
                    case BinaryOperator.Divide:
                        opSymbol = SymbolMap.RegisterSymbol("/");
                        break;
                    case BinaryOperator.Times:
                        opSymbol = SymbolMap.RegisterSymbol("*");
                        break;
                    case BinaryOperator.Modulo:
                        opSymbol = SymbolMap.RegisterSymbol("%");
                        break;
                    case BinaryOperator.BitwiseOr:
                        opSymbol = SymbolMap.RegisterSymbol("|");
                        break;
                    case BinaryOperator.BitwiseXOr:
                        opSymbol = SymbolMap.RegisterSymbol("^");
                        break;
                    case BinaryOperator.BitwiseAnd:
                        opSymbol = SymbolMap.RegisterSymbol("&");
                        break;
                    default:
                        ThrowCompilationError(binExp, $"Binary operator {binExp.Operator} not implemented");
                        break;
                }

                InsBinaryOperator binOpIns = new InsBinaryOperator(opSymbol);
                frame.AddInstruction(binOpIns, binExp.Location.Start.Line);
            }
        }

        /// <summary>
        /// Compiles an unary expression.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="unaryExp"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CompileUnaryExpression(AdhocCodeFrame frame, UnaryExpression unaryExp)
        {
            if (unaryExp is UpdateExpression upd)
            {
                // Assigning - we need to push
                if (unaryExp.Argument is Identifier leftIdent)
                    InsertVariablePush(frame, leftIdent);
                else if (unaryExp.Argument is AttributeMemberExpression attr)
                    InsertVariablePush(frame, attr.Property as Identifier);
                else if (unaryExp.Argument is ComputedMemberExpression comp)
                    CompileComputedMemberExpression(frame, comp);
                else if (unaryExp.Argument is Literal literal) // -1 -> int const + unary op
                    CompileLiteral(frame, literal);
                else if (unaryExp.Argument is CallExpression call)
                    CompileCall(frame, call);
                else if (unaryExp.Argument is BinaryExpression binaryExp)
                    CompileBinaryExpression(frame, binaryExp);
                else
                    ThrowCompilationError(unaryExp.Argument, "Unsupported");

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
                frame.AddInstruction(InsPop.Default, 0);
            }
            else
            {
                CompileExpression(frame, unaryExp.Argument);

                string op = unaryExp.Operator switch
                {
                    UnaryOperator.LogicalNot => "!",
                    UnaryOperator.Minus => "-@",
                    UnaryOperator.Plus => "+@",
                    _ => throw new NotImplementedException("TODO"),
                };

                AdhocSymbol symb = SymbolMap.RegisterSymbol(op);
                InsUnaryOperator unaryIns = new InsUnaryOperator(symb);
                frame.AddInstruction(unaryIns, unaryExp.Location.Start.Line);
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
                        NumericTokenType.Float => new InsFloatConst((float)literal.NumericValue),
                        _ => throw GetCompilationError(literal, "Unknown numeric literal type"),
                    };
                    frame.AddInstruction(ins, literal.Location.Start.Line);
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
        private AdhocSymbol InsertVariableEval(AdhocCodeFrame frame, Identifier identifier)
        {
            AdhocSymbol symb = SymbolMap.RegisterSymbol(identifier.Name);
            int idx = frame.AddScopeVariable(symb, isVariableDeclaration: false);
            var varEval = new InsVariableEvaluation(idx);
            varEval.VariableSymbols.Add(symb); // Only one
            frame.AddInstruction(varEval, identifier.Location.Start.Line);

            if (!frame.DeclaredVariables.Contains(symb.Name) && identifier.Name != "self") // Ignore self keyword
                varEval.VariableSymbols.Add(symb); // Static, two symbols

            return symb;
        }

        /// <summary>
        /// Inserts a variable push instruction to push a variable into the heap.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        private AdhocSymbol InsertVariablePush(AdhocCodeFrame frame, Identifier identifier, bool staticVariableDoubleSymbol = false)
        {
            AdhocSymbol varSymb = SymbolMap.RegisterSymbol(identifier.Name);
            int idx = frame.AddScopeVariable(varSymb);

            var varPush = new InsVariablePush();
            varPush.VariableSymbols.Add(varSymb);
            if (staticVariableDoubleSymbol)
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
        private void LeaveScope(AdhocCodeFrame frame, bool insertLeaveInstruction = true)
        {
            var lastScope = frame.CurrentScopes.Pop();
            frame.Stack.RewindLocalVariableStorage(lastScope.ScopeVariables.Count);

            foreach (var variable in lastScope.ScopeVariables)
                frame.DeclaredVariables.Remove(variable.Value.Name);

            if (insertLeaveInstruction)
            {
                InsLeaveScope leave = new InsLeaveScope();
                leave.VariableHeapRewindIndex = frame.Stack.LocalVariableStorage.Count;
                frame.AddInstruction(leave, 0);
            }
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
    }
}
