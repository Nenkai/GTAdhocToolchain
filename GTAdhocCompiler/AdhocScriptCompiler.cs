
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
            MainInstructions.Add(new InsSetState(1));
            MainInstructions.Add(new InsLeaveScope()); // FIX ME MAYBE

            // Script done.
            MainInstructions.Add(new InsSetState(0));
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
            }
        }

        public void CompileIfStatement(IfStatement ifStatement)
        {
            Expression condition = ifStatement.Test;
            Statement result = ifStatement.Consequent;
            Statement alt = ifStatement.Alternate;

            CompileExpression(condition);
            ;
        }

        public void CompileFunction(Script script, FunctionDeclaration decl)
        {
            var funcInst = new FunctionConst();
            MainInstructions.Add(funcInst);
            funcInst.LineNumber = decl.Location.Start.Line;

            foreach (Expression param in decl.Params)
            {
                if (param is Identifier paramIdent)
                {
                    AdhocSymbol paramSymb = RegisterSymbol(paramIdent.Name);
                    funcInst.Parameters.Add(paramSymb);
                }
                else
                    ThrowCompilationError("Function definition parameters must all be identifiers.");
            }

            var funcBody = decl.Body;
            if (funcBody is BlockStatement block)
            {
                CompileStatements(block);
            }
            else
                ThrowCompilationError("Expected function body to be block statement.");

            MainInstructions.Add(new InsSetState(1));
        }

        public void CompileReturnStatement(ReturnStatement retStatement)
        {
            if (retStatement.Argument is null) // Return has argument?
                return; // Nothing to do, really

            CompileExpression(retStatement.Argument);
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
                ThrowCompilationError("Variable declaration for id is null.");
            }

            if (id is Identifier idIdentifier)
            {
                AdhocSymbol varSymb = RegisterSymbol(idIdentifier.Name);

                var varPush = new InsVariablePush();
                varPush.VariableSymbol = varSymb;
                // varPush.StackIndex = 

                MainInstructions.Add(varPush);
            }
            else
            {
                ThrowCompilationError("Variable declaration for id is not an identifier.");
            }

            // Perform assignment
            MainInstructions.Add(new InsAssignPop());
        }

        public void CompileImport(ImportDeclaration import)
        {
            if (import.Specifiers.Count == 0)
            {
                ThrowCompilationError("Import declaration is empty.");
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
            MainInstructions.Add(importIns);
        }

        private void CompileExpression(Expression exp)
        {
            if (exp is Identifier initIdentifier) // var test = otherVariable;
            {
                CompileIdentifier(initIdentifier);
            }
            else if (exp is CallExpression callExp)
            {
                CompileCall(callExp);
            }
            else if (exp is BinaryExpression binExpression)
            {

            }
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
            varEval.VariableSymbol = symb;
            // varEval.StackIndex = 
            MainInstructions.Add(varEval);
        }

        private void CompileCall(CallExpression call)
        {
            // Get the function variable
            CompileExpression(call.Callee);

            var callIns = new InsCall(call.Arguments.Count);
            MainInstructions.Add(callIns);

            for (int i = 0; i < call.Arguments.Count; i++)
            {
                CompileExpression(call.Arguments[i]);
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
                "!" => "__not__", // Not

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

                "-@" => "__uminus__", // Variable Minus
                "+@" => "__uplus__", // Variable Plus
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

        private void ThrowCompilationError(string message)
        {

        }
    }
}
