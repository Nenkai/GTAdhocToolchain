
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

        public void CompileScript(Script script)
        {
            EnterScope(this, script);

            // "this" is main + declarations
            CompileStatements(this, script.Body);

            LeaveScope(this);

            // Script done.
            this.AddInstruction(new InsSetState(AdhocRunState.EXIT), 0);
        }

        public void CompileStatements(AdhocCodeFrame block, Node node)
        {
            foreach (var n in node.ChildNodes)
                CompileStatement(block, n);
        }

        public void CompileStatements(AdhocCodeFrame block, NodeList<Statement> nodes)
        {
            foreach (var n in nodes)
                CompileStatement(block, n);
        }

        public void CompileStatement(AdhocCodeFrame block, Node node)
        {
            switch (node.Type)
            {
                case Nodes.ClassDeclaration:
                    CompileClassDeclaration(block, node as ClassDeclaration);
                    break;
                case Nodes.FunctionDeclaration:
                    CompileFunctionDeclaration(block, node as FunctionDeclaration);
                    break;
                case Nodes.ForStatement:
                    CompileFor(block, node as ForStatement);
                    break;
                case Nodes.ForeachStatement:
                    CompileForeach(block, node as ForeachStatement);
                    break;
                case Nodes.WhileStatement:
                    CompileWhile(block, node as WhileStatement);
                    break;
                case Nodes.DoWhileStatement:
                    CompileDoWhile(block, node as DoWhileStatement);
                    break;
                case Nodes.VariableDeclaration:
                    CompileVariableDeclaration(block, node as VariableDeclaration);
                    break;
                case Nodes.ReturnStatement:
                    CompileReturnStatement(block, node as ReturnStatement);
                    break;
                case Nodes.ImportDeclaration:
                    CompileImport(block, node as ImportDeclaration);
                    break;
                case Nodes.IfStatement:
                    CompileIfStatement(block, node as IfStatement);
                    break;
                case Nodes.BlockStatement:
                    CompileBlockStatement(block, node as BlockStatement);
                    break;
                case Nodes.ExpressionStatement:
                    CompileExpressionStatement(block, node as ExpressionStatement);
                    break;
                case Nodes.SwitchStatement:
                    CompileSwitch(block, node as SwitchStatement);
                    break;
                case Nodes.ContinueStatement:
                    CompileContinue(block, node as ContinueStatement);
                    break;
                case Nodes.BreakStatement:
                    CompileBreak(block, node as BreakStatement);
                    break;
                case Nodes.IncludeStatement:
                    CompileIncludeStatement(block, node as IncludeStatement);
                    break;
                case Nodes.ThrowStatement:
                    CompileThrowStatement(block, node as ThrowStatement);
                    break;
                default:
                    ThrowCompilationError(node, "Statement not supported");
                    break;
            }
        }

        /// <summary>
        /// Compiles a new block/scope containing statements.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="blockStatement"></param>
        /// <param name="insertLeaveInstruction">Whether to compile a leave scope, which isnt needed for function returns.</param>
        public void CompileBlockStatement(AdhocCodeFrame block, BlockStatement blockStatement, bool openScope = true, bool insertLeaveInstruction = true)
        {
            if (openScope)
                EnterScope(block, blockStatement);

            CompileStatements(block, blockStatement.Body);

            LeaveScope(block, insertLeaveInstruction && blockStatement.Body.Count > 0);
        }

        public void CompileIncludeStatement(AdhocCodeFrame block, IncludeStatement include)
        {
            string pathToIncludeFile = Path.Combine(ProjectDirectory, include.Path);
            if (!File.Exists(pathToIncludeFile))
                ThrowCompilationError(include, $"Include file does not exist: {pathToIncludeFile}.");

            string file = File.ReadAllText(pathToIncludeFile);

            var parser = new AdhocAbstractSyntaxTree(file);
            Script includeScript = parser.ParseScript();

            InsSourceFile srcFileIns = new InsSourceFile(SymbolMap.RegisterSymbol(include.Path, false));
            block.AddInstruction(srcFileIns, include.Location.Start.Line);

            CompileStatements(this, includeScript.Body);

            // Resume
            InsSourceFile ogSrcFileIns = new InsSourceFile(block.SourceFilePath);
            block.AddInstruction(ogSrcFileIns, include.Location.Start.Line);
        }

        public void CompileThrowStatement(AdhocCodeFrame block, ThrowStatement throwStatement)
        {
            CompileExpression(block, throwStatement.Argument);
            block.AddInstruction(InsThrow.Default, throwStatement.Location.Start.Line);
        }

        public void CompileBreak(AdhocCodeFrame block, BreakStatement breakStatement)
        {
            var scope = block.GetLastBreakControlledScope();
            if (scope is LoopContext loopCtx)
            {
                InsJump breakJmp = new InsJump();
                loopCtx.BreakJumps.Add(breakJmp);
                block.Instructions.Add(breakJmp);
            }
            else if (scope is SwitchContext swContext)
            {
                InsJump breakJmp = new InsJump();
                swContext.BreakJumps.Add(breakJmp);
                block.Instructions.Add(breakJmp);
            }
            else
            {
                ThrowCompilationError(breakStatement, "Expected break statement to be in a loop or switch block.");
            }
        }

        public void CompileClassDeclaration(AdhocCodeFrame block, ClassDeclaration classDecl)
        {
            CompileNewClass(block, classDecl.Id, classDecl.SuperClass, classDecl.Body, classDecl.IsModule);
        }

        private void CompileNewClass(AdhocCodeFrame block, Identifier id, Node superClass, ClassBody body, bool isModule = false)
        {
            if (id is null || id is not Identifier)
            {
                ThrowCompilationError(id, "Class or module name must have a valid identifier.");
                return;
            }

            EnterScope(block, body);

            if (isModule)
            {
                InsModuleDefine mod = new InsModuleDefine();
                mod.Names.Add(SymbolMap.RegisterSymbol(id.Name));
                block.AddInstruction(mod, id.Location.Start.Line);
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
                block.AddInstruction(@class, id.Location.Start.Line);
            }

            CompileClassBody(block, body);

            LeaveScope(block);

            // Exit class or module scope. Important.
            InsSetState state = new InsSetState(AdhocRunState.EXIT);
            block.AddInstruction(state, 0);
        }

        public void CompileClassBody(AdhocCodeFrame block, ClassBody classBody)
        {
            foreach (var prop in classBody.Body)
            {
                if (prop is Expression exp)
                    CompileExpression(block, exp);
                else if (prop is IncludeStatement includeStatement)
                {
                    CompileIncludeStatement(block, includeStatement);
                }
                else
                {
                    ThrowCompilationError(prop, "Unsupported class body element");
                }
            }
        }

        public void CompileContinue(AdhocCodeFrame block, ContinueStatement continueStatement)
        {
            if (block.CurrentLoops.Count == 0)
                ThrowCompilationError(continueStatement, "Got continue keyword without loop.");

            LoopContext loop = block.GetLastLoop();

            InsJump continueJmp = new InsJump();
            block.AddInstruction(continueJmp, continueStatement.Location.Start.Line);

            loop.ContinueJumps.Add(continueJmp);
        }

        public void CompileClassProperty(AdhocCodeFrame block, ClassProperty classProp)
        {
            // For some reason the underlaying function expression has its id null when in a class
            if (classProp is MethodDefinition methodDef)
            {
                (classProp.Value as FunctionExpression).Id = classProp.Key as Identifier;
            }

            CompileExpression(block, classProp.Value);
        }

        public void CompileIfStatement(AdhocCodeFrame block, IfStatement ifStatement)
        {
            EnterScope(block, ifStatement);

            CompileExpression(block, ifStatement.Test); // if (<test>)

            // Create jump
            InsJumpIfFalse endOrNextIfJump = new InsJumpIfFalse();
            block.AddInstruction(endOrNextIfJump, 0);

            // Apply block
            CompileStatementWithScope(block, ifStatement.Consequent); // if body

            endOrNextIfJump.JumpIndex = block.GetLastInstructionIndex();

            // else if's..
            if (ifStatement.Alternate is not null)
            {
                // Jump to skip the else if block if the if was already taken
                InsJump skipAlternateJmp = new InsJump();
                block.AddInstruction(skipAlternateJmp, 0);

                endOrNextIfJump.JumpIndex = block.GetLastInstructionIndex();

                CompileStatementWithScope(block, ifStatement.Alternate);

                skipAlternateJmp.JumpInstructionIndex = block.GetLastInstructionIndex();
            }
            else
            {
                endOrNextIfJump.JumpIndex = block.GetLastInstructionIndex();
            }

            LeaveScope(block, insertLeaveInstruction: false);
        }

        public void CompileFor(AdhocCodeFrame block, ForStatement forStatement)
        {
            LoopContext loopCtx = EnterLoop(block, forStatement);

            // Initialization
            if (forStatement.Init is not null)
            {
                switch (forStatement.Init.Type)
                {
                    case Nodes.VariableDeclaration:
                        CompileVariableDeclaration(block, forStatement.Init as VariableDeclaration); break;
                    case Nodes.AssignmentExpression:
                        CompileAssignmentExpression(block, forStatement.Init as AssignmentExpression); break;
                    case Nodes.Identifier:
                        CompileIdentifier(block, forStatement.Init as Identifier); break;
                    default:
                        ThrowCompilationError(forStatement.Init, "Unsupported for statement init type");
                        break;
                }
            }

            int startIndex = block.GetLastInstructionIndex();

            // Condition
            InsJumpIfFalse jumpIfFalse = null; // will only be inserted if the condition exists, else its essentially a while true loop
            if (forStatement.Test != null)
            {
                CompileExpression(block, forStatement.Test);

                // Insert jump to the end of loop block
                jumpIfFalse = new InsJumpIfFalse();
                block.AddInstruction(jumpIfFalse, 0);
            }

            CompileStatement(block, forStatement.Body);

            // Reached bottom, proceed to do update
            // But first, process continue if any
            int loopUpdInsIndex = block.GetLastInstructionIndex();
            foreach (var continueJmp in loopCtx.ContinueJumps)
                continueJmp.JumpInstructionIndex = loopUpdInsIndex;

            // Update Counter
            if (forStatement.Update != null)
                CompileExpression(block, forStatement.Update);

            // Insert jump to go back to the beginning of the loop
            InsJump startJump = new InsJump();
            startJump.JumpInstructionIndex = startIndex;
            block.AddInstruction(startJump, 0);

            // Update jump that exits the loop if it exists
            int loopExitInsIndex = block.GetLastInstructionIndex();
            if (jumpIfFalse != null)
                jumpIfFalse.JumpIndex = loopExitInsIndex;

            // Process break jumps before doing the final exit
            foreach (var breakJmp in loopCtx.BreakJumps)
                breakJmp.JumpInstructionIndex = loopExitInsIndex;

            // Insert final leave
            LeaveLoop(block);
        }

        public void CompileWhile(AdhocCodeFrame block, WhileStatement whileStatement)
        {
            LoopContext loopCtx = EnterLoop(block, whileStatement);

            int loopStartInsIndex = block.GetLastInstructionIndex();
            if (whileStatement.Test is not null)
                CompileExpression(block, whileStatement.Test);

            InsJumpIfFalse jumpIfFalse = new InsJumpIfFalse(); // End loop jumper
            block.AddInstruction(jumpIfFalse, 0);

            CompileStatementWithScope(block, whileStatement.Body);

            // Insert jump to go back to the beginning of the loop
            InsJump startJump = new InsJump();
            startJump.JumpInstructionIndex = loopStartInsIndex;
            block.AddInstruction(startJump, 0);

            // Reached bottom, proceed to do update
            // But first, process continue if any
            foreach (var continueJmp in loopCtx.ContinueJumps)
                continueJmp.JumpInstructionIndex = loopStartInsIndex;

            // Update jump that exits the loop
            int loopExitInsIndex = block.GetLastInstructionIndex();
            jumpIfFalse.JumpIndex = loopExitInsIndex;

            // Process break jumps before doing the final exit
            foreach (var breakJmp in loopCtx.BreakJumps)
                breakJmp.JumpInstructionIndex = loopExitInsIndex;

            LeaveLoop(block);
        }

        public void CompileDoWhile(AdhocCodeFrame block, DoWhileStatement doWhileStatement)
        {
            LoopContext loopCtx = EnterLoop(block, doWhileStatement);

            int loopStartInsIndex = block.GetLastInstructionIndex();
            CompileStatementWithScope(block, doWhileStatement.Body);

            int testInsIndex = block.GetLastInstructionIndex();
            CompileExpression(block, doWhileStatement.Test);

            // Reached bottom, proceed to do update
            // But first, process continue if any
            foreach (var continueJmp in loopCtx.ContinueJumps)
                continueJmp.JumpInstructionIndex = testInsIndex;

            InsJumpIfTrue jumpIfTrue = new InsJumpIfTrue(); // Start loop jumper
            jumpIfTrue.JumpIndex = loopStartInsIndex;
            block.AddInstruction(jumpIfTrue, 0);

            // Process break jumps before doing the final exit
            int loopEndIndex = block.GetLastInstructionIndex();
            foreach (var breakJmp in loopCtx.BreakJumps)
                breakJmp.JumpInstructionIndex = loopEndIndex;

            LeaveLoop(block);
        }

        public void CompileForeach(AdhocCodeFrame block, ForeachStatement foreachStatement)
        {
            LoopContext loopCtx = EnterLoop(block, foreachStatement);

            CompileExpression(block, foreachStatement.Right);

            // Access object iterator
            InsAttributeEvaluation attrIns = new InsAttributeEvaluation();
            attrIns.AttributeSymbols.Add(SymbolMap.RegisterSymbol("iterator"));
            block.AddInstruction(attrIns, foreachStatement.Right.Location.Start.Line);

            // Assign it to a temporary value for the iteration
            string itorVariable = $"in#{SymbolMap.SwitchCaseTempValues++}";
            Identifier itorIdentifier = new Identifier(itorVariable)
            {
                Location = foreachStatement.Right.Location,
            };

            InsertVariablePush(block, itorIdentifier);
            block.AddInstruction(InsAssignPop.Default, 0);

            // Test - fetch_next returns whether the iterator is done or not
            int testInsIndex = block.GetLastInstructionIndex();
            InsertVariableEval(block, itorIdentifier);
            InsAttributeEvaluation fetchNextIns = new InsAttributeEvaluation();
            fetchNextIns.AttributeSymbols.Add(SymbolMap.RegisterSymbol("fetch_next"));
            block.AddInstruction(fetchNextIns, foreachStatement.Right.Location.Start.Line);

            InsJumpIfFalse exitJump = new InsJumpIfFalse(); // End loop jumper
            block.AddInstruction(exitJump, 0);

            // Entering body, but we need to unbox the iterator's value into our declared variable
            InsertVariableEval(block, itorIdentifier);
            block.AddInstruction(InsEval.Default, 0);

            if (foreachStatement.Left is not VariableDeclaration)
                ThrowCompilationError(foreachStatement, "Expected foreach to have a variable declaration.");

            CompileVariableDeclaration(block, foreachStatement.Left as VariableDeclaration, pushWhenNoInit: true); // We're unboxing, gotta push anyway

            // Compile body.
            CompileStatementWithScope(block, foreachStatement.Body);

            // continue's...
            foreach (var continueJmp in loopCtx.ContinueJumps)
                continueJmp.JumpInstructionIndex = block.GetLastInstructionIndex(); // To the jump that jumps to the test back

            // Add the jump back to the test
            InsJump beginJump = new InsJump();
            beginJump.JumpInstructionIndex = testInsIndex;
            block.AddInstruction(beginJump, 0);

            // Main exit...
            int loopExitIndex = block.GetLastInstructionIndex();
            exitJump.JumpIndex = loopExitIndex;

            // break's...
            foreach (var breakJmp in loopCtx.BreakJumps)
                breakJmp.JumpInstructionIndex = loopExitIndex;

            LeaveLoop(block);
        }

        public void CompileSwitch(AdhocCodeFrame block, SwitchStatement switchStatement)
        {
            CompileExpression(block, switchStatement.Discriminant); // switch (type)
            SwitchContext switchCtx = EnterSwitch(block, switchStatement);

            // Create a label for the temporary switch variable
            string tmpCaseVariable = $"case#{SymbolMap.SwitchCaseTempValues++}";
            AdhocSymbol labelSymb = InsertVariablePush(block, new Identifier(tmpCaseVariable));
            block.AddInstruction(InsAssignPop.Default, 0);

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
                    tempVar.VariableHeapIndex = block.VariableHeap.IndexOf(tmpCaseVariable);
                    block.AddInstruction(tempVar, swCase.Location.Start.Line);

                    // Write what we are comparing to 
                    CompileExpression(block, swCase.Test);

                    // Equal check
                    InsBinaryOperator eqOp = new InsBinaryOperator(SymbolMap.RegisterSymbol("=="));
                    block.AddInstruction(eqOp, swCase.Location.Start.Line);

                    // Write the jump
                    InsJumpIfTrue jit = new InsJumpIfTrue();
                    caseBodyJumps.Add(swCase, jit); // To write the instruction index later on
                    block.AddInstruction(jit, 0);
                }
                else // Default
                {
                    if (defaultJump is not null)
                        ThrowCompilationError(swCase, "Switch already has a default statement.");

                    InsJump jmp = new InsJump();
                    defaultJump = jmp;
                    block.AddInstruction(defaultJump, swCase.Location.Start.Line);
                }
            }

            // Write bodies
            for (int i = 0; i < switchStatement.Cases.Count; i++)
            {
                SwitchCase swCase = switchStatement.Cases[i];

                // Update body jump location
                if (swCase.Test != null)
                    caseBodyJumps[swCase].JumpIndex = block.GetLastInstructionIndex();
                else
                    defaultJump.JumpInstructionIndex = block.GetLastInstructionIndex();

                // Not counting as scopes
                foreach (var statement in swCase.Consequent)
                    CompileStatement(block, statement);
            }

            // Update break case jumps
            for (int i = 0; i < switchCtx.BreakJumps.Count; i++)
            {
                InsJump swCase = switchCtx.BreakJumps[i];
                swCase.JumpInstructionIndex = block.GetLastInstructionIndex();
            }

            // Leave switch block.
            LeaveScope(block);
        }

        public void CompileFunctionDeclaration(AdhocCodeFrame block, FunctionDeclaration funcDecl)
        {
            CompileSubroutine(block, funcDecl, funcDecl.Body, funcDecl.Id, funcDecl.Params, isMethod: false);
        }

        public void CompileSubroutine(AdhocCodeFrame block, Node parentNode, Node body, Identifier id, NodeList<Expression> subParams, bool isMethod)
        {
            SubroutineBase subroutine = isMethod ? new InsMethodDefine() : new InsFunctionDefine();
            if (id is not null)
                subroutine.Name = SymbolMap.RegisterSymbol(id.Name);
            subroutine.CodeFrame.SourceFilePath = block.SourceFilePath;

            foreach (Expression param in subParams)
            {
                if (param is Identifier paramIdent)
                {
                    AdhocSymbol paramSymb = SymbolMap.RegisterSymbol(paramIdent.Name);
                    subroutine.CodeFrame.FunctionParameters.Add(paramSymb);
                    subroutine.CodeFrame.DeclaredVariables.Add(paramSymb.Name);

                    // Function param is uninitialized, push nil
                    block.AddInstruction(InsNilConst.Empty, paramIdent.Location.Start.Line);
                    
                }
                else if (param is AssignmentExpression assignmentExpression)
                {
                    if (assignmentExpression.Left is not Identifier || assignmentExpression.Right is not Literal)
                        ThrowCompilationError(parentNode, "Subroutine parameter assignment must be an identifier to a literal. (value = 0)");

                    AdhocSymbol paramSymb = SymbolMap.RegisterSymbol((assignmentExpression.Left as Identifier).Name);
                    subroutine.CodeFrame.FunctionParameters.Add(paramSymb);
                    subroutine.CodeFrame.DeclaredVariables.Add(paramSymb.Name);

                    // Push default value
                    CompileLiteral(block, assignmentExpression.Right as Literal);
                }
                else
                    ThrowCompilationError(parentNode, "Subroutine definition parameters must all be identifiers.");
            }

            block.AddScopeVariable(subroutine.Name, isVariableDeclaration: false);
            block.AddInstruction(subroutine, parentNode.Location.Start.Line);

            var funcBody = body;
            if (funcBody is BlockStatement blockStatement)
            {
                EnterScope(subroutine.CodeFrame, parentNode);
                foreach (var param in subroutine.CodeFrame.FunctionParameters)
                    subroutine.CodeFrame.AddScopeVariable(param);
                
                CompileBlockStatement(subroutine.CodeFrame, blockStatement, openScope: false, insertLeaveInstruction: false);
            }
            else
                ThrowCompilationError(funcBody, "Expected subroutine body to be block statement.");

            if (!subroutine.CodeFrame.HasTopLevelReturnValue)
            {
                // All functions return a value internally, even if they don't in the code.
                subroutine.CodeFrame.AddInstruction(InsVoidConst.Empty, body.Location.End.Line);
                subroutine.CodeFrame.AddInstruction(new InsSetState(AdhocRunState.RETURN), 0);
            }
        }

        public void CompileReturnStatement(AdhocCodeFrame block, ReturnStatement retStatement)
        {
            if (retStatement.Argument is not null) // Return has argument?
            {
                CompileExpression(block, retStatement.Argument);
                if (retStatement.Argument is AssignmentExpression assignmentExpr)
                    CompileExpression(block, assignmentExpr.Left); // If we are returning an assignment i.e return <variable or path> += "hi", we need to eval str again
            }
            else
            {
                // Void const is returned
                block.AddInstruction(InsVoidConst.Empty, retStatement.Location.Start.Line);
            }

            block.AddInstruction(new InsSetState(AdhocRunState.RETURN), 0);

            // Top level of block?
            if (block.IsTopLevel)
                block.HasTopLevelReturnValue = true;
        }

        public void CompileVariableDeclaration(AdhocCodeFrame block, VariableDeclaration varDeclaration, bool pushWhenNoInit = false)
        {
            NodeList<VariableDeclarator> declarators = varDeclaration.Declarations;
            VariableDeclarator declarator = declarators[0];

            Expression? initValue = declarator.Init;
            Expression? id = declarator.Id;

            if (initValue != null)
                CompileExpression(block, initValue);

            // Now write the id
            if (id is null)
                ThrowCompilationError(varDeclaration, "Variable declaration for id is null.");

            if (id is Identifier idIdentifier) // var hello [= world];
            {
                if (initValue != null || pushWhenNoInit)
                {
                    // Variable is being defined with a value.
                    InsertVariablePush(block, idIdentifier);

                    // Perform assignment
                    block.AddInstruction(InsAssignPop.Default, 0);
                }
                else
                {
                    // Variable is declared but not assigned to anything yet. Do not add any variable push.
                    AdhocSymbol varSymb = SymbolMap.RegisterSymbol(idIdentifier.Name);
                    block.AddScopeVariable(varSymb);
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
                    InsertVariablePush(block, arrElemIdentifier);
                }

                InsListAssign listAssign = new InsListAssign(arrayPattern.Elements.Count);
                block.AddInstruction(listAssign, arrayPattern.Location.Start.Line);
                block.AddInstruction(InsPop.Default, 0); // Pop array from stack 
            }
            else
            {
                ThrowCompilationError(varDeclaration, "Variable declaration for id is not an identifier.");
            }
        }

        public void CompileImport(AdhocCodeFrame block, ImportDeclaration import)
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

            block.AddInstruction(importIns, import.Location.Start.Line);
        }

        private void CompileExpression(AdhocCodeFrame block, Expression exp)
        {
            switch (exp)
            {
                case Identifier initIdentifier:
                    CompileIdentifier(block, initIdentifier);
                    break;
                case FunctionExpression funcExpression:
                    CompileFunctionExpression(block, funcExpression);
                    break;
                case MethodDefinition methodDefinition: // May also be a function!
                    CompileMethodDefinition(block, methodDefinition);
                    break;
                case CallExpression callExp:
                    CompileCall(block, callExp);
                    break;
                case UnaryExpression unaryExpression:
                    CompileUnaryExpression(block, unaryExpression);
                    break;
                case AttributeMemberExpression attributeMemberException:
                    CompileAttributeMemberExpression(block, attributeMemberException);
                    break;
                case StaticMemberExpression staticMemberExpression:
                    CompileStaticMemberExpression(block, staticMemberExpression);
                    break;
                case BinaryExpression binExpression:
                    CompileBinaryExpression(block, binExpression);
                    break;
                case Literal literal:
                    CompileLiteral(block, literal);
                    break;
                case ArrayExpression arr:
                    CompileArrayExpression(block, arr);
                    break;
                case ComputedMemberExpression comp:
                    CompileComputedMemberExpression(block, comp);
                    break;
                case AssignmentExpression assignExp:
                    CompileAssignmentExpression(block, assignExp);
                    break;
                case ConditionalExpression condExp:
                    CompileConditionalExpression(block, condExp);
                    break;
                case TemplateLiteral templateLiteral:
                    CompileTemplateLiteral(block, templateLiteral);
                    break;
                case TaggedTemplateExpression taggedTemplateExpression:
                    CompileTaggedTemplateExpression(block, taggedTemplateExpression);
                    break;
                case PropertyDefinition propDefinition:
                    CompilePropertyDefinition(block, propDefinition);
                    break;
                case StaticExpression staticExpr:
                    CompileStaticExpression(block, staticExpr);
                    break;
                case ClassExpression classExpr:
                    CompileClassExpression(block, classExpr);
                    break;
                default:
                    ThrowCompilationError(exp, $"Expression {exp.Type} not supported");
                    break;
            }
        }

        private void CompileClassExpression(AdhocCodeFrame block, ClassExpression classExpression)
        {
            CompileNewClass(block, classExpression.Id, classExpression.SuperClass, classExpression.Body, classExpression.IsModule);
        }

        private void CompileStaticExpression(AdhocCodeFrame block, StaticExpression staticExpression)
        {
            if (staticExpression.VarExpression is not AssignmentExpression staticAssignment)
                ThrowCompilationError(staticExpression, "Expected static keyword to be a variable assignment.");

            AssignmentExpression assignmentExpression = staticExpression.VarExpression as AssignmentExpression;
            if (assignmentExpression.Left is not Identifier)
                ThrowCompilationError(assignmentExpression, "Expected static declaration to be an identifier.");

            Identifier identifier = assignmentExpression.Left as Identifier;
            var idSymb = SymbolMap.RegisterSymbol(identifier.Name);

            InsStaticDefine staticDefine = new InsStaticDefine(idSymb);
            block.AddInstruction(staticDefine, staticExpression.Location.End.Line);

            if (assignmentExpression.Operator == AssignmentOperator.Assign)
            {
                // Assigning to something new
                CompileExpression(block, assignmentExpression.Right);
                CompileVariableAssignment(block, assignmentExpression.Left, staticVariableDoubleSymbol: true);
            }
            else if (IsAdhocAssignWithOperandOperator(assignmentExpression.Operator))
            {
                // Assigning to self (+=)
                InsertVariablePush(block, assignmentExpression.Left as Identifier, staticVariableDoubleSymbol: true); // Push current value first
                CompileExpression(block, assignmentExpression.Right);

                InsertBinaryAssignOperator(block, assignmentExpression, assignmentExpression.Operator, assignmentExpression.Location.Start.Line);
                block.AddInstruction(InsPop.Default, 0);
            }
            else
            {
                ThrowCompilationError(assignmentExpression, $"Unimplemented operator assignment {assignmentExpression.Operator}");
            }
        }

        private void CompilePropertyDefinition(AdhocCodeFrame block, PropertyDefinition propDefinition)
        {
            if (propDefinition.Key is not Identifier)
            {
                // Not actually a property definition - esprima hack
                CompileExpression(block, propDefinition.Value);
                CompileExpression(block, propDefinition.Key);
            }
            else
            {

                var ident = propDefinition.Key as Identifier;
                var symb = SymbolMap.RegisterSymbol(ident.Name);

                if (propDefinition.Static)
                {
                    InsStaticDefine staticDefine = new InsStaticDefine(symb);
                    block.AddInstruction(staticDefine, propDefinition.Location.End.Line);

                    CompileExpression(block, propDefinition.Value);

                    // Assign
                    InsertVariablePush(block, ident, staticVariableDoubleSymbol: true);
                    block.AddInstruction(InsAssignPop.Default, 0);
                }
                else
                {
                    CompileExpression(block, propDefinition.Value);

                    // Declaring a class attribute, so we don't push anything
                    InsAttributeDefine attrDefine = new InsAttributeDefine();
                    attrDefine.AttributeName = SymbolMap.RegisterSymbol(ident.Name);
                    block.AddInstruction(attrDefine, ident.Location.Start.Line);
                }
            }
        }

        private void CompileArrayExpression(AdhocCodeFrame block, ArrayExpression arrayExpression)
        {
            block.AddInstruction(new InsArrayConst((uint)arrayExpression.Elements.Count), arrayExpression.Location.Start.Line);

            foreach (var elem in arrayExpression.Elements)
            {
                if (elem is null)
                    ThrowCompilationError(arrayExpression, "Unsupported empty element in array declaration.");

                CompileExpression(block, elem);

                block.AddInstruction(InsArrayPush.Default, elem.Location.Start.Line);
            }
        }

        private void CompileExpressionStatement(AdhocCodeFrame block, ExpressionStatement expStatement)
        {
            if (expStatement.Expression is CallExpression call)
            {
                // Call expressions need to be directly popped within an expression statement -> 'getThing()' instead of 'var thing = getThing()'.
                // Their return values aren't used.
                CompileCall(block, call, popReturnValue: true);
            }
            else
            {
                CompileExpression(block, expStatement.Expression);
            }
        }

        private void CompileMethodDefinition(AdhocCodeFrame block, MethodDefinition methodDefinition)
        {
            if (methodDefinition.Kind == PropertyKind.Function)
            {
                var funcExp = methodDefinition.Value as FunctionExpression;
                CompileSubroutine(block, funcExp, funcExp.Body, methodDefinition.Key as Identifier, funcExp.Params, isMethod: false);
            }
            else if (methodDefinition.Kind == PropertyKind.Method)
            {
                if (methodDefinition.Value is not FunctionExpression methodFunc)
                    ThrowCompilationError(methodDefinition, "Unexpected non-function expression value for method");

                if (methodDefinition.Key is not Identifier)
                    ThrowCompilationError(methodDefinition, "Unexpected non-function identifier key for method");

                methodFunc = methodDefinition.Value as FunctionExpression;
                CompileSubroutine(block, methodDefinition, methodFunc.Body, methodDefinition.Key as Identifier, methodFunc.Params, isMethod: true);
            }
        }

        private void CompileFunctionExpression(AdhocCodeFrame block, FunctionExpression funcExp)
        {
            CompileSubroutine(block, funcExp, funcExp.Body, funcExp.Id, funcExp.Params, isMethod: false);
        }

        // Combination of string literals/templates
        private void CompileTaggedTemplateExpression(AdhocCodeFrame block, TaggedTemplateExpression taggedTemplate)
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
                            block.AddInstruction(strConst, element.Location.Start.Line);

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
                                    block.AddInstruction(strConst, n.Location.Start.Line);
                                }
                                else if (n is Expression exp)
                                {
                                    CompileExpression(block, exp);
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
            block.AddInstruction(strPush, taggedTemplate.Location.Start.Line);
        }

        /// <summary>
        /// Compiles a string format literal. i.e "hello %{name}!"
        /// </summary>
        /// <param name="block"></param>
        /// <param name="templateLiteral"></param>
        private void CompileTemplateLiteral(AdhocCodeFrame block, TemplateLiteral templateLiteral)
        {
            if (templateLiteral.Quasis.Count == 1 && templateLiteral.Expressions.Count == 0)
            {
                // Regular string const
                TemplateElement strElement = templateLiteral.Quasis[0];
                if (string.IsNullOrEmpty(strElement.Value.Cooked))
                {
                    // Empty strings are always a string push with 0 args (aka nil)
                    InsStringPush strPush = new InsStringPush(0);
                    block.AddInstruction(strPush, strElement.Location.Start.Line);
                }
                else 
                {
                    AdhocSymbol strSymb = SymbolMap.RegisterSymbol(strElement.Value.Cooked, convertToOperand: false);
                    InsStringConst strConst = new InsStringConst(strSymb);
                    block.AddInstruction(strConst, strElement.Location.Start.Line);
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
                        block.AddInstruction(strConst, tElem.Location.Start.Line);
                    }
                    else if (node is Expression exp)
                    {
                        CompileExpression(block, exp);
                    }
                    else
                        ThrowCompilationError(node, "Unexpected template element type");
                }

                // Link strings together
                InsStringPush strPush = new InsStringPush(literalNodes.Count);
                block.AddInstruction(strPush, templateLiteral.Location.Start.Line);
            }
        }

        private void CompileAssignmentExpression(AdhocCodeFrame block, AssignmentExpression assignExpression)
        {
            if (assignExpression.Operator == AssignmentOperator.Assign)
            {
                // Assigning to something new
                CompileExpression(block, assignExpression.Right);
                CompileVariableAssignment(block, assignExpression.Left);
            }
            else if (IsAdhocAssignWithOperandOperator(assignExpression.Operator))
            {
                // Assigning to self (+=)
                if (assignExpression.Left is Identifier)
                {
                    // Pushing to variable
                    InsertVariablePush(block, assignExpression.Left as Identifier); // Push current value first
                }
                else if (assignExpression.Left is AttributeMemberExpression attr)
                {
                    InsertAttributeMemberAssignmentPush(block, attr);
                }
                else if (assignExpression.Left is ComputedMemberExpression compExpression)
                {
                    CompileComputedMemberExpressionAssignment(block, compExpression);
                }
                else
                    ThrowCompilationError(assignExpression, "Unimplemented");

                CompileExpression(block, assignExpression.Right);

                InsertBinaryAssignOperator(block, assignExpression, assignExpression.Operator, assignExpression.Location.Start.Line);
                block.AddInstruction(InsPop.Default, 0);
            }
            else
            {
                ThrowCompilationError(assignExpression, $"Unimplemented operator assignment {assignExpression.Operator}");
            }
        }

        /// <summary>
        /// Compiles an assignment to a variable.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="expression"></param>
        public void CompileVariableAssignment(AdhocCodeFrame block, Expression expression, bool staticVariableDoubleSymbol = false)
        {
            if (expression is Identifier ident) // hello = world
            {
                InsertVariablePush(block, ident, staticVariableDoubleSymbol);
            }
            else if (expression is AttributeMemberExpression attrMember) // Pushing into an object i.e hello.world = "!"
            {
                InsertAttributeMemberAssignmentPush(block, attrMember);
            }
            else if (expression is ComputedMemberExpression compExpression)
            {
                CompileComputedMemberExpressionAssignment(block, compExpression);
            }
            else
                ThrowCompilationError(expression, "Unimplemented");

            block.AddInstruction(InsAssignPop.Default, 0);
        }

        /// <summary>
        /// test ? consequent : alternate;
        /// </summary>
        /// <param name="condExpression"></param>
        private void CompileConditionalExpression(AdhocCodeFrame block, ConditionalExpression condExpression)
        {
            // Compile condition
            CompileExpression(block, condExpression.Test);

            InsJumpIfFalse alternateJump = new InsJumpIfFalse();
            block.AddInstruction(alternateJump, 0);

            CompileExpression(block, condExpression.Consequent);

            // This jump will skip the alternate statement if the consequent path is taken
            InsJump altSkipJump = new InsJump();
            block.AddInstruction(altSkipJump, 0);
            block.AddInstruction(InsPop.Default, 0);

            // Update alternate jump index now that we've compiled the consequent
            alternateJump.JumpIndex = block.GetLastInstructionIndex();

            // Proceed to compile alternate/no match statement
            CompileExpression(block, condExpression.Alternate);

            // Done completely, update alt skip jump to end of condition instruction block
            altSkipJump.JumpInstructionIndex = block.GetLastInstructionIndex();
        }

        /// <summary>
        /// Compiles an identifier. var test = otherVariable;
        /// </summary>
        /// <param name="identifier"></param>
        private void CompileIdentifier(AdhocCodeFrame block, Identifier identifier, bool attribute = false)
        {
            if (attribute)
                InsertAttributeEval(block, identifier);
            else
                InsertVariableEval(block, identifier);
        }


        /// <summary>
        /// Compiles array or map access or anything that can be indexed
        /// </summary>
        private void CompileComputedMemberExpression(AdhocCodeFrame block, ComputedMemberExpression computedMember)
        {
            CompileExpression(block, computedMember.Object);
            CompileExpression(block, computedMember.Property);

            InsElementEval eval = new InsElementEval();
            block.AddInstruction(eval, 0);
        }

        /// <summary>
        /// Compiles array or map element assignment
        /// </summary>
        private void CompileComputedMemberExpressionAssignment(AdhocCodeFrame block, ComputedMemberExpression computedMember)
        {
            CompileExpression(block, computedMember.Object);
            CompileExpression(block, computedMember.Property);

            InsElementPush push = new InsElementPush();
            block.AddInstruction(push, 0);
        }

        /// <summary>
        /// Compiles an attribute member path.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="staticExp"></param>
        /// <param name="compileProperty">Whether to compile the property - used whenever we're compiling the eval call or not</param>
        private void CompileAttributeMemberExpression(AdhocCodeFrame block, AttributeMemberExpression staticExp, bool compileProperty = true)
        {
            CompileExpression(block, staticExp.Object); // ORG

            if (staticExp.Property is not Identifier)
                ThrowCompilationError(staticExp, "Expected attribute member to be identifier.");

            if (compileProperty)
                CompileIdentifier(block, staticExp.Property as Identifier, attribute: true); // inSession
        }

        /// <summary>
        /// Compiles a static member path.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="staticExp"></param>
        private void CompileStaticMemberExpression(AdhocCodeFrame block, StaticMemberExpression staticExp)
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

            int idx = block.AddScopeVariable(fullPathSymb, isVariableDeclaration: false);
            eval.VariableHeapIndex = idx;

            block.AddInstruction(eval, staticExp.Location.Start.Line);
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
        /// <param name="block"></param>
        /// <param name="call"></param>
        private void CompileCall(AdhocCodeFrame block, CallExpression call, bool popReturnValue = false)
        {
            /* NOTE: So adhoc has this "EVAL" instruction which seems to be called as such: myArray[] 
             * Obviously this is weird and Javascript doesnt support it normally
             * So instead we're shortcuting calls to eval of an object property to the actual instruction
             * It's a hack, but when you don't know the actual language syntax (and it may be a pain to implement anyway), this is the best compromise
             * Just don't name your method "eval" i guess  */
            if (call.Callee is AttributeMemberExpression exp && exp.Property is Identifier ident && ident.Name == "eval")
            {
                CompileAttributeMemberExpression(block, exp, compileProperty: false);
                block.AddInstruction(InsEval.Default, 0);
                return;
            }

            CompileExpression(block, call.Callee);

            for (int i = 0; i < call.Arguments.Count; i++)
            {
                CompileExpression(block, call.Arguments[i]);
            }
            
            var callIns = new InsCall(call.Arguments.Count);
            block.AddInstruction(callIns, call.Location.Start.Line);

            // When calling and not caring about returns
            if (popReturnValue)
                block.AddInstruction(InsPop.Default, 0);
        }

        /// <summary>
        /// Compiles a binary expression.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="binExp"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void CompileBinaryExpression(AdhocCodeFrame block, BinaryExpression binExp)
        {
            CompileExpression(block, binExp.Left);

            // Check for logical operators that checks between both conditions
            if (binExp.Operator == BinaryOperator.LogicalAnd || binExp.Operator == BinaryOperator.LogicalOr)
            {
                if (binExp.Operator == BinaryOperator.LogicalOr)
                {
                    InsLogicalOr orIns = new InsLogicalOr();
                    block.AddInstruction(orIns, 0);

                    CompileExpression(block, binExp.Right);
                    orIns.InstructionJumpIndex = block.GetLastInstructionIndex();
                }
                else if (binExp.Operator == BinaryOperator.LogicalAnd)
                {
                    InsLogicalAnd andIns = new InsLogicalAnd();
                    block.AddInstruction(andIns, 0);

                    CompileExpression(block, binExp.Right);
                    andIns.InstructionJumpIndex = block.GetLastInstructionIndex();
                }
                else
                {
                    throw new InvalidOperationException();
                }
                
            }
            else if (binExp.Operator == BinaryOperator.InstanceOf)
            {
                CompileExpression(block, binExp.Left);

                // Object.isInstanceOf - No idea if adhoc supports it, but eh, why not
                InsertAttributeEval(block, new Identifier("isInstanceOf"));

                // Eval right identifier (if its one)
                CompileExpression(block, binExp.Right);

                // Call.
                block.AddInstruction(new InsCall(argumentCount: 1), binExp.Location.Start.Line);
            }
            else
            {
                
                CompileExpression(block, binExp.Right);
                        
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
                block.AddInstruction(binOpIns, binExp.Location.Start.Line);
            }
        }

        /// <summary>
        /// Compiles an unary expression.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="unaryExp"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CompileUnaryExpression(AdhocCodeFrame block, UnaryExpression unaryExp)
        {
            if (unaryExp is UpdateExpression upd)
            {
                // Assigning - we need to push
                if (unaryExp.Argument is Identifier leftIdent)
                    InsertVariablePush(block, leftIdent);
                else if (unaryExp.Argument is AttributeMemberExpression attr)
                    InsertVariablePush(block, attr.Property as Identifier);
                else if (unaryExp.Argument is ComputedMemberExpression comp)
                    CompileComputedMemberExpression(block, comp);
                else if (unaryExp.Argument is Literal literal) // -1 -> int const + unary op
                    CompileLiteral(block, literal);
                else if (unaryExp.Argument is CallExpression call)
                    CompileCall(block, call);
                else if (unaryExp.Argument is BinaryExpression binaryExp)
                    CompileBinaryExpression(block, binaryExp);
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
                block.AddInstruction(unaryIns, unaryExp.Location.Start.Line);
                block.AddInstruction(InsPop.Default, 0);
            }
            else
            {
                CompileExpression(block, unaryExp.Argument);

                string op = unaryExp.Operator switch
                {
                    UnaryOperator.LogicalNot => "!",
                    UnaryOperator.Minus => "-@",
                    UnaryOperator.Plus => "+@",
                    _ => throw new NotImplementedException("TODO"),
                };

                AdhocSymbol symb = SymbolMap.RegisterSymbol(op);
                InsUnaryOperator unaryIns = new InsUnaryOperator(symb);
                block.AddInstruction(unaryIns, unaryExp.Location.Start.Line);
            }
        }

        /// <summary>
        /// Compile a literal into a proper constant instruction.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="literal"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CompileLiteral(AdhocCodeFrame block, Literal literal)
        {
            switch (literal.TokenType)
            {
                case TokenType.NilLiteral:
                    InsNilConst nil = new InsNilConst();
                    block.AddInstruction(nil, literal.Location.Start.Line);
                    break;
                case TokenType.BooleanLiteral:
                    InsBoolConst boolConst = new InsBoolConst((literal.Value as bool?).Value);
                    block.AddInstruction(boolConst, literal.Location.Start.Line);
                    break;
                case TokenType.NumericLiteral:
                    InstructionBase ins = literal.NumericTokenType switch
                    {
                        NumericTokenType.Integer => new InsIntConst((int)literal.NumericValue),
                        NumericTokenType.Float => new InsFloatConst((float)literal.NumericValue),
                        _ => throw GetCompilationError(literal, "Unknown numeric literal type"),
                    };
                    block.AddInstruction(ins, literal.Location.Start.Line);
                    break;
                default:
                    throw new NotImplementedException($"Not implemented literal {literal.TokenType}");
            }
        }

        /// <summary>
        /// Inserts an attribute eval instruction to access an attribute of a certain object.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        private AdhocSymbol InsertAttributeEval(AdhocCodeFrame block, Identifier identifier)
        {
            AdhocSymbol symb = SymbolMap.RegisterSymbol(identifier.Name);
            var attrEval = new InsAttributeEvaluation();
            attrEval.AttributeSymbols.Add(symb); // Only one
            block.AddInstruction(attrEval, identifier.Location.Start.Line);

            return symb;
        }

        /// <summary>
        /// Inserts a variable evaluation instruction.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        private AdhocSymbol InsertVariableEval(AdhocCodeFrame block, Identifier identifier)
        {
            AdhocSymbol symb = SymbolMap.RegisterSymbol(identifier.Name);
            int idx = block.AddScopeVariable(symb, isVariableDeclaration: false);
            var varEval = new InsVariableEvaluation(idx);
            varEval.VariableSymbols.Add(symb); // Only one
            block.AddInstruction(varEval, identifier.Location.Start.Line);

            if (!block.DeclaredVariables.Contains(symb.Name) && identifier.Name != "self") // Ignore self keyword
                varEval.VariableSymbols.Add(symb); // Static, two symbols

            return symb;
        }

        /// <summary>
        /// Inserts a variable push instruction to push a variable into the heap.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        private AdhocSymbol InsertVariablePush(AdhocCodeFrame block, Identifier identifier, bool staticVariableDoubleSymbol = false)
        {
            AdhocSymbol varSymb = SymbolMap.RegisterSymbol(identifier.Name);
            int idx = block.AddScopeVariable(varSymb);

            var varPush = new InsVariablePush();
            varPush.VariableSymbols.Add(varSymb);
            if (staticVariableDoubleSymbol)
                varPush.VariableSymbols.Add(varSymb);

            varPush.VariableStorageIndex = idx;
            block.AddInstruction(varPush, identifier.Location.Start.Line);

            return varSymb;
        }

        /// <summary>
        /// Inserts a push to an object attribute
        /// </summary>
        /// <param name="block"></param>
        /// <param name="attr"></param>
        private void InsertAttributeMemberAssignmentPush(AdhocCodeFrame block, AttributeMemberExpression attr)
        {
            // Pushing to object attribute
            CompileExpression(block, attr.Object);
            if (attr.Property is not Identifier)
                ThrowCompilationError(attr.Property, "Expected attribute member property identifier");

            var propIdent = attr.Property as Identifier;

            InsAttributePush attrPush = new InsAttributePush();
            AdhocSymbol attrSymbol = SymbolMap.RegisterSymbol(propIdent.Name);
            attrPush.AttributeSymbols.Add(attrSymbol);
            block.AddInstruction(attrPush, propIdent.Location.Start.Line);
        }

        /// <summary>
        /// Inserts a binary assign operator.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="parentNode"></param>
        /// <param name="assignOperator"></param>
        /// <param name="lineNumber"></param>
        /// <returns></returns>
        private AdhocSymbol InsertBinaryAssignOperator(AdhocCodeFrame block, Node parentNode, AssignmentOperator assignOperator, int lineNumber)
        {
            string opStr = AssignOperatorToString(assignOperator);
            if (string.IsNullOrEmpty(opStr))
                ThrowCompilationError(parentNode, "Unrecognized operator");

            var symb = SymbolMap.RegisterSymbol(opStr);
            block.AddInstruction(new InsBinaryAssignOperator(symb), lineNumber);

            return symb;
        }

        /// <summary>
        /// Inserts an unary assign operator.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="parentNode"></param>
        /// <param name="unaryOperator"></param>
        /// <param name="lineNumber"></param>
        /// <returns></returns>
        private AdhocSymbol InsertUnaryAssignOperator(AdhocCodeFrame block, UnaryExpression parentNode, UnaryOperator unaryOperator, int lineNumber)
        {
            bool postIncrement = parentNode.Prefix;
            string op = UnaryOperatorToString(parentNode.Operator, postIncrement);

            AdhocSymbol symb = SymbolMap.RegisterSymbol(op);
            InsUnaryAssignOperator unaryIns = new InsUnaryAssignOperator(symb);
            block.AddInstruction(unaryIns, parentNode.Location.Start.Line);

            return symb;
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

        private AdhocCompilationException GetCompilationError(Node node, string message)
        {
            return new AdhocCompilationException($"{message}. Line {node.Location.Start.Line}:{node.Location.Start.Column}");
        }

        private LoopContext EnterLoop(AdhocCodeFrame block, Statement loopStatement)
        {
            LoopContext loopCtx = new LoopContext(loopStatement);
            block.CurrentLoops.Push(loopCtx);
            block.CurrentScopes.Push(loopCtx);
            return loopCtx;
        }

        private SwitchContext EnterSwitch(AdhocCodeFrame block, SwitchStatement node)
        {
            var scope = new SwitchContext(node);
            block.CurrentScopes.Push(scope);
            return scope;
        }

        private ScopeContext EnterScope(AdhocCodeFrame block, Node node)
        {
            var scope = new ScopeContext(node);
            block.CurrentScopes.Push(scope);
            return scope;
        }

        private void LeaveLoop(AdhocCodeFrame block)
        {
            block.CurrentLoops.Pop();
            LeaveScope(block);
        }

        private void LeaveScope(AdhocCodeFrame block, bool insertLeaveInstruction = true)
        {
            var lastScope = block.CurrentScopes.Pop();
            block.RewindVariableHeap(lastScope.ScopeVariables.Count);

            foreach (var variable in lastScope.ScopeVariables)
                block.DeclaredVariables.Remove(variable.Value.Name);

            if (insertLeaveInstruction)
            {
                InsLeaveScope leave = new InsLeaveScope();
                leave.VariableHeapRewindIndex = block.VariableHeap.Count;
                block.AddInstruction(leave, 0);
            }
        }

        private void CompileStatementWithScope(AdhocCodeFrame block, Statement statement)
        {
            if (statement is BlockStatement)
            {
                CompileStatement(block, statement);
            }
            else if (statement is ContinueStatement
                || statement is BreakStatement)
            {
                // continues are not a scope
                CompileStatement(block, statement);
            }
            else
            {
                EnterScope(block, statement);
                CompileStatement(block, statement);
                LeaveScope(block);
            }
        }
    }
}
