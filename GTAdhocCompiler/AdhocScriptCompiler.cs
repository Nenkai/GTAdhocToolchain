
using Esprima;
using Esprima.Ast;

using GTAdhocCompiler.Instructions;


namespace GTAdhocCompiler
{
    public class AdhocScriptCompiler : AdhocInstructionBlock
    {
        public AdhocSymbolMap SymbolMap { get; set; } = new();

        public void Compile(Script script)
        {
            // "this" is main + declarations
            CompileStatements(this, script.Body);


            // Script done.
            this.AddInstruction(new InsSetState(0), 0);
        }

        public void CompileStatements(AdhocInstructionBlock block, Node node)
        {
            foreach (var n in node.ChildNodes)
                CompileStatement(block, n);
        }

        public void CompileStatements(AdhocInstructionBlock block, NodeList<Statement> nodes)
        {
            int stackStartIndex = block.Stack.StackStorageCounter;
            foreach (var n in nodes)
                CompileStatement(block, n);

            // Exiting scope
            InsLeaveScope leaveIns = new InsLeaveScope();
            leaveIns.TempStackRewindIndex = block.Stack.StackStorageCounter + 1;
            block.AddInstruction(leaveIns, 0);
            block.Stack.StackStorageCounter = stackStartIndex;
        }

        public void CompileStatement(AdhocInstructionBlock block, Node node)
        {
            switch (node)
            {
                case FunctionDeclaration funcDecl:
                    CompileFunction(block, funcDecl);
                    break;
                case ForStatement forStatement:
                    CompileFor(block, forStatement);
                    break;
                case VariableDeclaration variableDecl:
                    CompileVariableDeclaration(block, variableDecl);
                    break;
                case ReturnStatement retStatement:
                    CompileReturnStatement(block, retStatement);
                    break;
                case ImportDeclaration importDecl:
                    CompileImport(block, importDecl);
                    break;
                case IfStatement ifStatement:
                    CompileIfStatement(block, ifStatement);
                    break;
                case BlockStatement blockStatement:
                    CompileStatements(block, blockStatement.Body);
                    break;
                case ExpressionStatement expStatement:
                    CompileExpressionStatement(block, expStatement);
                    break;
                case SwitchStatement switchStatement:
                    CompileSwitch(block, switchStatement);
                    break;
                case BreakStatement breakStatement:
                    break;
                default:
                    ThrowCompilationError(node, "Statement not supported");
                    break;
            }
        }

        public void CompileIfStatement(AdhocInstructionBlock block, IfStatement ifStatement)
        {
            Expression condition = ifStatement.Test;
            Statement result = ifStatement.Consequent;
            Statement alt = ifStatement.Alternate;

            CompileExpression(block, condition);

            // Create jump
            InsJumpIfFalse jmp = new InsJumpIfFalse();
            block.AddInstruction(jmp, 0);

            // Apply block
            CompileStatement(block, result);

            jmp.JumpIndex = block.GetLastInstructionIndex();
        }

        public void CompileFor(AdhocInstructionBlock block, ForStatement forStatement)
        {
            // Initialization
            if (forStatement.Init is VariableDeclaration varDecl)
            {
                CompileVariableDeclaration(block, varDecl);
            }
            else if (forStatement.Init is Identifier ident)
            {
                
            }
            else
            {

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

            // Update Counter
            if (forStatement.Update != null)
                CompileExpression(block, forStatement.Update);

            // Insert jump to go back to the beginning of the loop
            InsJump startJump = new InsJump();
            startJump.JumpInstructionIndex = startIndex;
            block.AddInstruction(startJump, 0);

            // Update jump that exits the loop if it exists
            if (jumpIfFalse != null)
                jumpIfFalse.JumpIndex = block.GetLastInstructionIndex();

            // Insert final leave
            InsLeaveScope leave = new InsLeaveScope();
            block.AddInstruction(leave, 0);
        }

        public void CompileSwitch(AdhocInstructionBlock block, SwitchStatement switchStatement)
        {
            CompileExpression(block, switchStatement.Discriminant); // switch (type)

            // Create a label for the temporary switch variable
            AdhocSymbol labelSymb = SymbolMap.RegisterSymbol("case#0");
            InsVariablePush variablePush = new InsVariablePush();
            variablePush.VariableSymbols.Add(labelSymb);
            block.AddInstruction(variablePush, switchStatement.Discriminant.Location.Start.Line);
            block.AddInstruction(InsAssignPop.Default, 0);

            Dictionary<SwitchCase, InsJumpIfTrue> caseBodyJumps = new();
            Dictionary<SwitchCase, InsJump> caseLeaveJumps = new();
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
                InsJump leaveJump = new InsJump();

                // Update body jump location
                if (swCase.Test != null)
                    caseBodyJumps[swCase].JumpIndex = block.GetLastInstructionIndex();
                else
                    defaultJump.JumpInstructionIndex = block.GetLastInstructionIndex();
                caseLeaveJumps.Add(swCase, leaveJump);

                foreach (var statement in swCase.Consequent)
                    CompileStatement(block, statement);

                block.AddInstruction(leaveJump, 0);
            }

            // Update exit jumps
            for (int i = 0; i < switchStatement.Cases.Count; i++)
            {
                SwitchCase swCase = switchStatement.Cases[i];
                caseLeaveJumps[swCase].JumpInstructionIndex = block.GetLastInstructionIndex();
            }

            // Leave switch block.
            InsLeaveScope leave = new InsLeaveScope();
            block.AddInstruction(leave, 0);
        }

        public void CompileFunction(AdhocInstructionBlock block, FunctionDeclaration decl)
        {
            var funcInst = new InsFunctionDefine();
            if (decl.Id is not null)
                funcInst.Name = SymbolMap.RegisterSymbol(decl.Id.Name);

            foreach (Expression param in decl.Params)
            {
                if (param is Identifier paramIdent)
                {
                    AdhocSymbol paramSymb = SymbolMap.RegisterSymbol(paramIdent.Name);
                    funcInst.FunctionBlock.Parameters.Add(paramSymb);
                    funcInst.FunctionBlock.AddSymbolToHeap(paramSymb.Name);
                    funcInst.FunctionBlock.DeclaredVariables.Add(paramSymb.Name);

                    // Function param is uninitialized, push nil
                    block.AddInstruction(InsNilConst.Empty, decl.Location.Start.Line);
                    
                }
                else if (param is AssignmentExpression assignmentExpression)
                {
                    if (assignmentExpression.Left is not Identifier || assignmentExpression.Right is not Literal)
                        ThrowCompilationError(decl, "Function parameter assignment must be an identifier to a literal. (value = 0)");

                    AdhocSymbol paramSymb = SymbolMap.RegisterSymbol((assignmentExpression.Left as Identifier).Name);
                    funcInst.FunctionBlock.Parameters.Add(paramSymb);
                    funcInst.FunctionBlock.AddSymbolToHeap(paramSymb.Name);
                    funcInst.FunctionBlock.DeclaredVariables.Add(paramSymb.Name);

                    // Push default value
                    CompileLiteral(block, assignmentExpression.Right as Literal);
                }
                else
                    ThrowCompilationError(decl, "Function definition parameters must all be identifiers.");
            }

            block.AddSymbolToHeap(decl.Id.Name);
            block.AddInstruction(funcInst, decl.Location.Start.Line);

            var funcBody = decl.Body;
            if (funcBody is BlockStatement blockStatement)
            {
                CompileStatements(funcInst.FunctionBlock, blockStatement);
            }
            else
                ThrowCompilationError(funcBody, "Expected function body to be block statement.");
        }

        public void CompileReturnStatement(AdhocInstructionBlock block, ReturnStatement retStatement)
        {
            if (retStatement.Argument is not null) // Return has argument?
                CompileExpression(block, retStatement.Argument);

            block.AddInstruction(new InsSetState(1), 0);
        }

        public void CompileVariableDeclaration(AdhocInstructionBlock block, VariableDeclaration varDeclaration)
        {
            NodeList<VariableDeclarator> declarators = varDeclaration.Declarations;
            VariableDeclarator declarator = declarators[0];

            Expression? initValue = declarator.Init;
            Expression? id = declarator.Id;

            CompileExpression(block, initValue);

            // Now write the id
            if (id is null)
            {
                ThrowCompilationError(varDeclaration, "Variable declaration for id is null.");
            }

            if (id is Identifier idIdentifier)
            {
                AdhocSymbol varSymb = SymbolMap.RegisterSymbol(idIdentifier.Name);
                int idx = block.AddSymbolToHeap(varSymb.Name);

                var varPush = new InsVariablePush();
                varPush.VariableSymbols.Add(varSymb);
                varPush.VariableStorageIndex = idx;
                block.AddInstruction(varPush, idIdentifier.Location.Start.Line);

                if (!block.DeclaredVariables.Contains(varSymb.Name))
                    block.DeclaredVariables.Add(varSymb.Name);
            }
            else
            {
                ThrowCompilationError(varDeclaration, "Variable declaration for id is not an identifier.");
            }

            // Perform assignment
            block.AddInstruction(InsAssignPop.Default, 0);
        }

        public void CompileImport(AdhocInstructionBlock block, ImportDeclaration import)
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

        private void CompileExpression(AdhocInstructionBlock block, Expression exp)
        {
            switch (exp)
            {
                case Identifier initIdentifier:
                    CompileIdentifier(block, initIdentifier);
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
                default:
                    ThrowCompilationError(exp, $"Expression {exp.Type} not supported");
                    break;
            }
        }

        private void CompileExpressionStatement(AdhocInstructionBlock block, ExpressionStatement expStatement)
        {
            CompileExpression(block, expStatement.Expression);
        }

        // Combination of string literals/templates
        private void CompileTaggedTemplateExpression(AdhocInstructionBlock block, TaggedTemplateExpression taggedTemplate)
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


        private void CompileTemplateLiteral(AdhocInstructionBlock block, TemplateLiteral templateLiteral)
        {
            if (templateLiteral.Quasis.Count == 1 && templateLiteral.Expressions.Count == 0)
            {
                // Regular string const
                TemplateElement strElement = templateLiteral.Quasis[0];
                if (string.IsNullOrEmpty(strElement.Value.Cooked))
                {
                    // Empty strings are always a string push with 0 args (aka nil)
                    InsStringPush strPush = new InsStringPush(0);
                    block.AddInstruction(strPush, 0);
                }
                else 
                {
                    AdhocSymbol strSymb = SymbolMap.RegisterSymbol(strElement.Value.Cooked);
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
                        AdhocSymbol valSymb = SymbolMap.RegisterSymbol(tElem.Value.Cooked);
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

        private void CompileAssignmentExpression(AdhocInstructionBlock block, AssignmentExpression assignExpression)
        {
            if (assignExpression.Operator == AssignmentOperator.Assign)
            {
                // Assigning to something new
                CompileExpression(block, assignExpression.Right);
                CompileVariableAssignment(block, assignExpression.Left);
            }
            else if (assignExpression.Operator == AssignmentOperator.PlusAssign)
            {
                // Assigning to self (+=)
                CompileExpression(block, assignExpression.Left); // Push current value first
                CompileExpression(block, assignExpression.Right);

                var symb = SymbolMap.RegisterSymbol("+");
                block.AddInstruction(new InsBinaryAssignOperator(symb), assignExpression.Location.Start.Line);
                block.AddInstruction(InsPop.Default, 0);
            }
            else
            {
                ThrowCompilationError(assignExpression, $"Unimplemented operator assignment {assignExpression.Operator}");
            }
        }

        public void CompileVariableAssignment(AdhocInstructionBlock block, Expression expression)
        {
            if (expression is Identifier ident)
            {
                AdhocSymbol varSymb = SymbolMap.RegisterSymbol(ident.Name);
                int idx = block.AddSymbolToHeap(varSymb.Name);

                var varPush = new InsVariablePush();
                varPush.VariableSymbols.Add(varSymb);
                varPush.VariableStorageIndex = idx;
                block.AddInstruction(varPush, ident.Location.Start.Line);
            }
            else
                ThrowCompilationError(expression, "Implement this");

            block.AddInstruction(InsAssignPop.Default, 0);
        }

        /// <summary>
        /// test ? consequent : alternate;
        /// </summary>
        /// <param name="condExpression"></param>
        private void CompileConditionalExpression(AdhocInstructionBlock block, ConditionalExpression condExpression)
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
        private void CompileIdentifier(AdhocInstructionBlock block, Identifier identifier, bool attribute = false)
        {
            AdhocSymbol symb = SymbolMap.RegisterSymbol(identifier.Name);

            if (attribute)
            {
                var attrEval = new InsAttributeEvaluation();
                attrEval.AttributeSymbols.Add(symb); // Only one
                block.AddInstruction(attrEval, identifier.Location.Start.Line);
            }
            else
            {
                int idx = block.AddSymbolToHeap(symb.Name);
                var varEval = new InsVariableEvaluation(idx);
                varEval.VariableSymbols.Add(symb); // Only one
                block.AddInstruction(varEval, identifier.Location.Start.Line);

                if (!block.DeclaredVariables.Contains(symb.Name))
                    varEval.VariableSymbols.Add(symb); // Static, two symbols
            }
        }

        /// <summary>
        /// Compiles array or map access or anything that can be indexed
        /// </summary>
        private void CompileComputedMemberExpression(AdhocInstructionBlock block, ComputedMemberExpression computedMember)
        {
            CompileExpression(block, computedMember.Object);
            CompileExpression(block, computedMember.Property);

            InsElementEval eval = new InsElementEval();
            block.AddInstruction(eval, 0);
        }

        // ORG.inSession();
        private void CompileAttributeMemberExpression(AdhocInstructionBlock block, AttributeMemberExpression staticExp)
        {
            CompileExpression(block, staticExp.Object); // ORG

            if (staticExp.Property is not Identifier)
                ThrowCompilationError(staticExp, "Expected attribute member to be identifier.");

            CompileIdentifier(block, staticExp.Property as Identifier, attribute: true); // inSession
        }

        // pdistd::MPjson::Encode
        private void CompileStaticMemberExpression(AdhocInstructionBlock block, StaticMemberExpression staticExp)
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

            int idx = block.AddSymbolToHeap(fullPathSymb.Name);
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

        private void CompileCall(AdhocInstructionBlock block, CallExpression call)
        {
            // Get the function variable
            CompileExpression(block, call.Callee);

            for (int i = 0; i < call.Arguments.Count; i++)
            {
                CompileExpression(block, call.Arguments[i]);
            }
            
            var callIns = new InsCall(call.Arguments.Count);
            block.AddInstruction(callIns, call.Location.Start.Line);
        }

        private void CompileCallCallee(AdhocInstructionBlock block, Expression callee)
        {

        }

        private void CompileBinaryExpression(AdhocInstructionBlock block, BinaryExpression binExp)
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
                    case BinaryOperator.Times:
                        opSymbol = SymbolMap.RegisterSymbol("*");
                        break;
                    default:
                        ThrowCompilationError(binExp, $"Binary operator {binExp.Operator} not implemented");
                        break;
                }

                InsBinaryOperator binOpIns = new InsBinaryOperator(opSymbol);
                block.AddInstruction(binOpIns, binExp.Location.Start.Line);
            }
        }

        private void CompileUnaryExpression(AdhocInstructionBlock block, UnaryExpression unaryExp)
        {
            CompileExpression(block, unaryExp.Argument);

            if (unaryExp is UpdateExpression upd)
            {
                bool postIncrement = unaryExp.Prefix;

                string op = unaryExp.Operator switch
                {
                    UnaryOperator.Increment when postIncrement => "@++",
                    UnaryOperator.Increment when !postIncrement => "++@",
                    UnaryOperator.Decrement when postIncrement => "@--",
                    UnaryOperator.Decrement when !postIncrement => "--@",
                    _ => throw new NotImplementedException("TODO"),
                };

                AdhocSymbol symb = SymbolMap.RegisterSymbol(op);
                InsUnaryAssignOperator unaryIns = new InsUnaryAssignOperator(symb);
                block.AddInstruction(unaryIns, unaryExp.Location.Start.Line);
                block.AddInstruction(InsPop.Default, 0);
            }
            else
            {
                string op = unaryExp.Operator switch
                {
                    UnaryOperator.LogicalNot => "!",
                    _ => throw new NotImplementedException("TODO"),
                };

                AdhocSymbol symb = SymbolMap.RegisterSymbol(op);
                InsUnaryOperator unaryIns = new InsUnaryOperator(symb);
                block.AddInstruction(unaryIns, unaryExp.Location.Start.Line);
            }
        }

        private void CompileLiteral(AdhocInstructionBlock block, Literal literal)
        {
            switch (literal.TokenType)
            {
                case TokenType.NilLiteral:
                    InsNilConst nil = new InsNilConst();
                    block.AddInstruction(nil, literal.Location.Start.Line);
                    break;
                case TokenType.NumericLiteral:
                    InstructionBase ins = literal.NumericTokenType switch
                    {
                        NumericTokenType.Integer => new InsIntConst((int)literal.NumericValue),
                        _ => throw GetCompilationError(literal, "Unknown numeric literal type"),
                    };
                    block.AddInstruction(ins, literal.Location.Start.Line);
                    break;
                default:
                    throw new NotImplementedException($"Not implemented literal {literal.TokenType}");
            }
        }


        private void ThrowCompilationError(Node node, string message)
        {
            throw GetCompilationError(node, message);
        }

        private AdhocCompilationException GetCompilationError(Node node, string message)
        {
            return new AdhocCompilationException($"{message}. Line {node.Location.Start.Line}:{node.Location.Start.Column}");
        }
    }
}
