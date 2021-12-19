
using Esprima;
using Esprima.Ast;

using GTAdhocCompiler.Instructions;

namespace GTAdhocCompiler
{
    public class AdhocScriptCompiler
    {
        public List<InstructionBase> MainInstructions { get; set; } = new();
        public Dictionary<string, AdhocSymbol> Symbols { get; set; } = new();

        public void Compile(Script script)
        {
            CompileStatements(script.Body);

            // Done, finish up Main
            AddInstruction(new InsSetState(1), 0);
            AddInstruction(new InsLeaveScope(), 0); // FIX ME MAYBE

            // Script done.
            AddInstruction(new InsSetState(0), 0);
        }

        public void CompileStatements(Node node)
        {
            foreach (var n in node.ChildNodes)
                CompileStatement(n);
        }

        public void CompileStatements(NodeList<Statement> nodes)
        {
            foreach (var n in nodes)
                CompileStatement(n);
        }

        public void CompileStatement(Node node)
        {
            switch (node)
            {
                case FunctionDeclaration funcDecl:
                    CompileFunction(null, funcDecl);
                    break;
                case VariableDeclaration variableDecl:
                    CompileVariableDeclaration(variableDecl);
                    break;
                case ReturnStatement retStatement:
                    CompileReturnStatement(retStatement);
                    break;
                case ImportDeclaration importDecl:
                    CompileImport(importDecl);
                    break;
                case IfStatement ifStatement:
                    CompileIfStatement(ifStatement);
                    break;
                case BlockStatement blockStatement:
                    CompileStatements(blockStatement.Body);
                    break;
                case ExpressionStatement expStatement:
                    CompileExpressionStatement(expStatement);
                    break;
                default:
                    ThrowCompilationError(node, "Statement not supported");
                    break;
            }
        }

        public void CompileIfStatement(IfStatement ifStatement)
        {
            Expression condition = ifStatement.Test;
            Statement result = ifStatement.Consequent;
            Statement alt = ifStatement.Alternate;

            CompileExpression(condition);

            // Create jump
            InsJumpIfFalse jmp = new InsJumpIfFalse();
            AddInstruction(jmp, 0);

            // Apply block
            CompileStatement(result);

            jmp.JumpIndex = GetLastFunctionInstructionIndex();
        }

        public void CompileFunction(Script script, FunctionDeclaration decl)
        {
            var funcInst = new InsFunctionConst();
            AddInstruction(funcInst, decl.Location.Start.Line);

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
            if (funcBody is BlockStatement block)
            {
                CompileStatements(block);
            }
            else
                ThrowCompilationError(funcBody, "Expected function body to be block statement.");
        }

        public void CompileReturnStatement(ReturnStatement retStatement)
        {
            if (retStatement.Argument is not null) // Return has argument?
                CompileExpression(retStatement.Argument);

            AddInstruction(new InsSetState(1), 0);
        }

        public void CompileVariableDeclaration(VariableDeclaration varDeclaration)
        {
            NodeList<VariableDeclarator> declarators = varDeclaration.Declarations;
            VariableDeclarator declarator = declarators[0];

            Expression? initValue = declarator.Init;
            Expression? id = declarator.Id;

            CompileExpression(initValue);

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

                AddInstruction(varPush, idIdentifier.Location.Start.Line);
            }
            else
            {
                ThrowCompilationError(varDeclaration, "Variable declaration for id is not an identifier.");
            }

            // Perform assignment
            AddInstruction(new InsAssignPop(), 0);
        }

        public void CompileImport(ImportDeclaration import)
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
            AddInstruction(importIns, import.Location.Start.Line);
        }

        private void CompileExpression(Expression exp)
        {
            switch (exp)
            {
                case Identifier initIdentifier:
                    CompileIdentifier(initIdentifier);
                    break;
                case CallExpression callExp:
                    CompileCall(callExp);
                    break;
                case UnaryExpression unaryExpression:
                    CompileUnaryExpression(unaryExpression);
                    break;
                case AttributeMemberExpression attributeMemberException:
                    CompileAttributeMemberExpression(attributeMemberException);
                    break;
                case StaticMemberExpression staticMemberExpression:
                    CompileStaticMemberExpression(staticMemberExpression);
                    break;
                case BinaryExpression binExpression:
                    CompileBinaryExpression(binExpression);
                    break;
                case Literal literal:
                    CompileLiteral(literal);
                    break;
                case ComputedMemberExpression comp:
                    CompileComputedMemberExpression(comp);
                    break;
                case AssignmentExpression assignExp:
                    CompileAssignmentExpression(assignExp);
                    break;
                case ConditionalExpression condExp:
                    CompileConditionalExpression(condExp);
                    break;
                default:
                    ThrowCompilationError(exp, "Expression not supported");
                    break;
            }
        }

        private void CompileExpressionStatement(ExpressionStatement expStatement)
        {
            CompileExpression(expStatement.Expression);
        }

       
        private void CompileAssignmentExpression(AssignmentExpression assignExpression)
        {
            CompileExpression(assignExpression.Right);
        }

        /// <summary>
        /// test ? consequent : alternate;
        /// </summary>
        /// <param name="condExpression"></param>
        private void CompileConditionalExpression(ConditionalExpression condExpression)
        {
            // Compile condition
            CompileExpression(condExpression.Test);

            InsJumpIfFalse alternateJump = new InsJumpIfFalse();
            AddInstruction(alternateJump, 0);

            CompileExpression(condExpression.Consequent);

            // This jump will skip the alternate statement if the consequent path is taken
            InsJump altSkipJump = new InsJump();
            AddInstruction(altSkipJump, 0);
            InsAssignPop pop = new InsAssignPop(); // Also needed
            AddInstruction(pop, 0);

            // Update alternate jump index now that we've compiled the consequent
            alternateJump.JumpIndex = GetLastFunctionInstructionIndex();

            // Proceed to compile alternate/no match statement
            CompileExpression(condExpression.Alternate);

            // Done completely, update alt skip jump to end of condition instruction block
            altSkipJump.InstructionIndex = GetLastFunctionInstructionIndex();
        }

        /// <summary>
        /// Compiles an identifier. var test = otherVariable;
        /// </summary>
        /// <param name="identifier"></param>
        private void CompileIdentifier(Identifier identifier)
        {
            if (identifier.ChildNodes.Count == 0) // Direct assignment to something?
            {

            }

            AdhocSymbol symb = RegisterSymbol(identifier.Name);

            var varEval = new InsVariableEvaluation();
            varEval.VariableSymbols.Add(symb); // Only one
            // varEval.StackIndex = 
            AddInstruction(varEval, identifier.Location.Start.Line);
        }

        /// <summary>
        /// Compiles array or map access or anything that can be indexed
        /// </summary>
        private void CompileComputedMemberExpression(ComputedMemberExpression computedMember)
        {
            CompileExpression(computedMember.Object);
            CompileExpression(computedMember.Property);

            InsElementEval eval = new InsElementEval();
            AddInstruction(eval, 0);
        }

        // ORG.inSession();
        private void CompileAttributeMemberExpression(AttributeMemberExpression staticExp)
        {
            CompileExpression(staticExp.Object); // ORG
            CompileExpression(staticExp.Property); // inSession
        }

        // pdistd::MPjson::Encode
        private void CompileStaticMemberExpression(StaticMemberExpression staticExp)
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

            AddInstruction(eval, staticExp.Location.Start.Line);
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

        private void CompileCall(CallExpression call)
        {
            // Get the function variable
            CompileExpression(call.Callee);

            for (int i = 0; i < call.Arguments.Count; i++)
            {
                CompileExpression(call.Arguments[i]);
            }
            
            var callIns = new InsCall(call.Arguments.Count);
            AddInstruction(callIns, call.Location.Start.Line);
        }

        private void CompileBinaryExpression(BinaryExpression binExp)
        {
            CompileExpression(binExp.Left);
            CompileExpression(binExp.Right);

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
            AddInstruction(binOpIns, binExp.Location.Start.Line);
        }

        private void CompileUnaryExpression(UnaryExpression unaryExp)
        {
            CompileExpression(unaryExp.Argument);

            string op = unaryExp.Operator switch
            {
                UnaryOperator.LogicalNot => "!",
                _ => throw new NotImplementedException("TODO"),
            };

            AdhocSymbol symb = RegisterSymbol(op);
            InsUnaryOperator unaryIns = new InsUnaryOperator(symb);
            AddInstruction(unaryIns, unaryExp.Location.Start.Line);
        }

        private void CompileLiteral(Literal literal)
        {
            switch (literal.TokenType)
            {
                case TokenType.StringLiteral:
                    AdhocSymbol str = RegisterSymbol(literal.StringValue);
                    InsStringConst strIns = new InsStringConst(str);
                    AddInstruction(strIns, literal.Location.Start.Line);
                    break;
                case TokenType.NilLiteral:
                    InsNilConst nil = new InsNilConst();
                    AddInstruction(nil, literal.Location.Start.Line);
                    break;
                default:
                    throw new NotImplementedException("Not implemented literal");
            }
        }

        private int GetLastFunctionInstructionIndex()
        {
            return MainInstructions.Count;
        }

        private void AddInstruction(InstructionBase ins, int lineNumber)
        {
            ins.LineNumber = lineNumber;
            MainInstructions.Add(ins);
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
