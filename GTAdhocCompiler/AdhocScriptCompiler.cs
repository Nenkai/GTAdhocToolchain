
using Esprima;
using Esprima.Ast;

using GTAdhocCompiler.Instructions;


namespace GTAdhocCompiler
{
    public class AdhocScriptCompiler : AdhocInstructionBlock
    {
        public Dictionary<string, AdhocSymbol> Symbols { get; set; } = new();

        public void Compile(Script script)
        {
            // "this" is main + declarations
            CompileStatements(this, script.Body);

            // Done, finish up Main
            this.AddInstruction(new InsSetState(1), 0);
            this.AddInstruction(new InsLeaveScope(), 0); // FIX ME MAYBE

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
            foreach (var n in nodes)
                CompileStatement(block, n);
        }

        public void CompileStatement(AdhocInstructionBlock block, Node node)
        {
            switch (node)
            {
                case FunctionDeclaration funcDecl:
                    CompileFunction(block, funcDecl);
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

        public void CompileFunction(AdhocInstructionBlock block, FunctionDeclaration decl)
        {
            var funcInst = new InsFunctionConst();
            if (decl.Id is not null)
                funcInst.Name = RegisterSymbol(decl.Id.Name);

            block.AddInstruction(funcInst, decl.Location.Start.Line);

            foreach (Expression param in decl.Params)
            {
                if (param is Identifier paramIdent)
                {
                    AdhocSymbol paramSymb = RegisterSymbol(paramIdent.Name);
                    funcInst.Parameters.Add(paramSymb);
                }
                else
                    ThrowCompilationError(decl, "Function definition parameters must all be identifiers.");
            }

            var funcBody = decl.Body;
            if (funcBody is BlockStatement blockStatement)
            {
                AdhocInstructionBlock functionBlock = new AdhocInstructionBlock();
                funcInst.Instructions = functionBlock.Instructions;
                CompileStatements(functionBlock, blockStatement);
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
                AdhocSymbol varSymb = RegisterSymbol(idIdentifier.Name);

                var varPush = new InsVariablePush();
                varPush.VariableSymbol = varSymb;
                // varPush.StackIndex = 

                block.AddInstruction(varPush, idIdentifier.Location.Start.Line);
            }
            else
            {
                ThrowCompilationError(varDeclaration, "Variable declaration for id is not an identifier.");
            }

            // Perform assignment
            block.AddInstruction(new InsAssignPop(), 0);
        }

        public void CompileImport(AdhocInstructionBlock block, ImportDeclaration import)
        {
            if (import.Specifiers.Count == 0)
            {
                ThrowCompilationError(import, "Import declaration is empty.");
            }

            string importNamespace = "";
            string target = "";
            for (int i = 0; i < import.Specifiers.Count; i++)
            {
                ImportDeclarationSpecifier specifier = import.Specifiers[i];
                if (i == import.Specifiers.Count - 1)
                {
                    target = specifier.Local.Name;
                    break;
                }

                importNamespace += specifier.Local.Name;
                if (i < import.Specifiers.Count - 1 && import.Specifiers[i + 1].Local.Name != "*")
                    importNamespace += "::";
            }

            AdhocSymbol namespaceSymbol = RegisterSymbol(importNamespace);
            AdhocSymbol targetNamespace = RegisterSymbol(target);
            AdhocSymbol nilSymbol = RegisterSymbol("nil");

            var importIns = new InsImport(namespaceSymbol, targetNamespace);
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
                default:
                    ThrowCompilationError(exp, "Expression not supported");
                    break;
            }
        }

        private void CompileExpressionStatement(AdhocInstructionBlock block, ExpressionStatement expStatement)
        {
            CompileExpression(block, expStatement.Expression);
        }

       
        private void CompileAssignmentExpression(AdhocInstructionBlock block, AssignmentExpression assignExpression)
        {
            CompileExpression(block, assignExpression.Right);
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
            InsAssignPop pop = new InsAssignPop(); // Also needed
            block.AddInstruction(pop, 0);

            // Update alternate jump index now that we've compiled the consequent
            alternateJump.JumpIndex = block.GetLastInstructionIndex();

            // Proceed to compile alternate/no match statement
            CompileExpression(block, condExpression.Alternate);

            // Done completely, update alt skip jump to end of condition instruction block
            altSkipJump.InstructionIndex = block.GetLastInstructionIndex();
        }

        /// <summary>
        /// Compiles an identifier. var test = otherVariable;
        /// </summary>
        /// <param name="identifier"></param>
        private void CompileIdentifier(AdhocInstructionBlock block, Identifier identifier)
        {
            if (identifier.ChildNodes.Count == 0) // Direct assignment to something?
            {

            }

            AdhocSymbol symb = RegisterSymbol(identifier.Name);

            var varEval = new InsVariableEvaluation();
            varEval.VariableSymbols.Add(symb); // Only one
            // varEval.StackIndex = 
            block.AddInstruction(varEval, identifier.Location.Start.Line);
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
            CompileExpression(block, staticExp.Property); // inSession
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
                AdhocSymbol symb = RegisterSymbol(part);
                eval.VariableSymbols.Add(symb);
            }

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
                        opSymbol = RegisterSymbol("==");
                        break;
                    case BinaryOperator.NotEqual:
                        opSymbol = RegisterSymbol("!=");
                        break;
                    default:
                        ThrowCompilationError(binExp, "Binary operator not implemented");
                        break;
                }

                InsBinaryOperator binOpIns = new InsBinaryOperator(opSymbol);
                block.AddInstruction(binOpIns, binExp.Location.Start.Line);
            }
        }

        private void CompileUnaryExpression(AdhocInstructionBlock block, UnaryExpression unaryExp)
        {
            CompileExpression(block, unaryExp.Argument);

            string op = unaryExp.Operator switch
            {
                UnaryOperator.LogicalNot => "!",
                _ => throw new NotImplementedException("TODO"),
            };

            AdhocSymbol symb = RegisterSymbol(op);
            InsUnaryOperator unaryIns = new InsUnaryOperator(symb);
            block.AddInstruction(unaryIns, unaryExp.Location.Start.Line);
        }

        private void CompileLiteral(AdhocInstructionBlock block, Literal literal)
        {
            switch (literal.TokenType)
            {
                case TokenType.StringLiteral:
                    AdhocSymbol str = RegisterSymbol(literal.StringValue);
                    InsStringConst strIns = new InsStringConst(str);
                    block.AddInstruction(strIns, literal.Location.Start.Line);
                    break;
                case TokenType.NilLiteral:
                    InsNilConst nil = new InsNilConst();
                    block.AddInstruction(nil, literal.Location.Start.Line);
                    break;
                default:
                    throw new NotImplementedException("Not implemented literal");
            }
        }

        private AdhocSymbol RegisterSymbol(string symbolName)
        {
            string identifier = symbolName switch
            {
                "==" => "__eq__", // Equals
                "!=" => "__ne__", // Not Equal
                ">=" => "__ge__", // Greater Equal
                ">" => "__gt__", // Greater Than
                "<=" => "__le__", // Lesser Equal
                "<" => "__lt__", // Lesser Than
                "!" => "__not__", // Logical Not

                // __minus__
                "+" => "__add__", // Add,
                "-" => "__min__", // Minus,
                "*" => "__mul__", // Multiply or Import Wildcard
                "/" => "__div__", // Division
                "^" => "__xor__", // Xor,
                "%" => "__mod__", // Modulo
                "**" => "__pow__", // Pow
                "<<" => "__lshift__", // Left Shift
                ">>" => "__rshift__", // Right Shift
                "~" => "__invert__", // Invert
                "|" => "__or__", // Or

                "-@" => "__uminus__", // Unary Minus
                "+@" => "__uplus__", // Unary Plus
                "--@" => "__pre_decr__", // Pre Decrementation,
                "++@" => "__pre_incr__", // Pre Incrementation
                "@--" => "__post_decr__", // Post Decrementation,
                "@++" => "__post_incr__", // Post Incrementation

                _ => symbolName,
            };


            if (!Symbols.TryGetValue(identifier, out var symbol))
            {
                symbol = new AdhocSymbol(Symbols.Count, identifier);
                Symbols.Add(identifier, symbol);
            }

            return symbol;
        }

        private void ThrowCompilationError(Node node, string message)
        {
            throw new AdhocCompilationException($"{message}. Line {node.Location.Start.Line}:{node.Location.Start.Column}");
        }
    }
}
