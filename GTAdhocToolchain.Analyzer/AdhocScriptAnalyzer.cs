using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Esprima.Ast;
using Esprima;

using IntervalTree;

namespace GTAdhocToolchain.Analyzer
{
    public class AdhocScriptAnalyzer
    {
        public List<int> Symbols { get; set; } // This should be a position range to symbol pointer

        public Scope MainScope { get; set; }
        public Scope CurrentScope { get; set; } 

        public IntervalTree<int, Scope> PositionToScope { get; set; } = new IntervalTree<int, Scope>();

        public AdhocAbstractSyntaxTree Tree { get; set; }
        public AdhocErrorHandler ErrorHandler { get; set; }

        public AdhocScriptAnalyzer(AdhocAbstractSyntaxTree tree)
        {
            MainScope = new Scope();
            CurrentScope = MainScope;

            Tree = tree;

        }

        public void ParseScript(Script script)
        {
            MainScope.Range = script.Range;
            PositionToScope.Add(MainScope.Range.Start, MainScope.Range.End, MainScope);

            foreach (var n in script.ChildNodes)
                ParseStatement(n);
        }

        private void ParseStatement(Node statement)
        {
            switch (statement.Type)
            {
                case Nodes.BlockStatement:
                    ParseBlockStatement(statement.As<BlockStatement>());
                    break;

                case Nodes.IfStatement:
                    ParseIfStatement(statement.As<IfStatement>());
                    break;

                case Nodes.ForStatement:
                    ParseForStatement(statement.As<ForStatement>());
                    break;

                case Nodes.ForeachStatement:
                    ParseForeachStatement(statement.As<ForeachStatement>());
                    break;

                case Nodes.WhileStatement:
                    ParseWhileStatement(statement.As<WhileStatement>());
                    break;

                case Nodes.ExpressionStatement:
                    ParseExpressionStatement(statement.As<ExpressionStatement>());
                    break;

                case Nodes.ClassDeclaration:
                    ParseClassDeclaration(statement.As<ClassDeclaration>());
                    break;

                case Nodes.FunctionDeclaration:
                    ParseFunctionDeclaration(statement.As<FunctionDeclaration>());
                    break;

                case Nodes.MethodDeclaration:
                    ParseMethodDeclaration(statement.As<MethodDeclaration>());
                    break;

                case Nodes.UndefStatement:
                case Nodes.ReturnStatement:
                case Nodes.BreakStatement:
                    break;

                case Nodes.VariableDeclaration:
                    ParseVariableDeclaration(statement.As<VariableDeclaration>());
                    break;

                case Nodes.StaticDeclaration:
                    ParseStaticDeclaration(statement.As<StaticDeclaration>());
                    break;

                case Nodes.AttributeDeclaration:
                    ParseAttributeDeclaration(statement.As<AttributeDeclaration>());
                    break;

                case Nodes.VariableDeclarator:
                    ParseVariableDeclarator(statement.As<VariableDeclarator>());
                    break;

                default:
                    break;
            }
        }

        private void ParseExpression(Expression expression)
        {
            switch (expression.Type)
            {
                case Nodes.CallExpression:
                case Nodes.AssignmentExpression:
                    break;

                default:
                    break;
            }
        }

        private void ParseWhileStatement(WhileStatement statement)
        {
            var parentScope = CurrentScope;
            CurrentScope = NewScope(statement.Range, CurrentScope);
            ParseStatementWithoutNewScope(statement.Body);
            CurrentScope = parentScope;
        }

        private void ParseIfStatement(IfStatement statement)
        {
            var parentScope = CurrentScope;
            CurrentScope = NewScope(statement.Range, CurrentScope);

            // If is already a new scope on its own
            ParseStatementWithoutNewScope(statement.Consequent);

            CurrentScope = parentScope;

            if (statement.Alternate != null)
                ParseStatement(statement.Alternate);
        }

        private void ParseStatementWithoutNewScope(Node statement)
        {
            if (statement.Type == Nodes.BlockStatement)
                ParseBlockStatement(statement.As<BlockStatement>(), createNewScope: false);
            else
                ParseStatement(statement);
        }

        private void ParseExpressionStatement(ExpressionStatement expStatement)
        {
            ParseExpression(expStatement.Expression);
        }

        private void ParseStaticDeclaration(StaticDeclaration staticDecl)
        {
            var node = staticDecl.Expression;
            if (node is AssignmentExpression assignment)
            {
                var identifier = assignment.Left.As<Identifier>();
                CurrentScope.DefinedStatics.Add(new Symbol(identifier, identifier.Name));
            }
            else if (node.Type == Nodes.Identifier)
            {
                var identifier = node.As<Identifier>();
                CurrentScope.DefinedStatics.Add(new Symbol(identifier, identifier.Name));
            }
        }

        private void ParseAttributeDeclaration(AttributeDeclaration attributeDecl)
        {
            var node = attributeDecl.VarExpression;
            if (node is AssignmentExpression assignment)
            {
                var identifier = assignment.Left.As<Identifier>();
                CurrentScope.DefinedAttributes.Add(new Symbol(identifier, identifier.Name));
            }
            else if (node.Type == Nodes.Identifier)
            {
                var identifier = node.As<Identifier>();
                CurrentScope.DefinedAttributes.Add(new Symbol(identifier, identifier.Name));
            }
        }

        private void ParseFunctionDeclaration(FunctionDeclaration funcDeclaration)
        {
            CurrentScope.DefinedFunctions.Add(new Symbol(funcDeclaration, funcDeclaration.Id.Name));

            var parentScope = CurrentScope;
            CurrentScope = NewScope(funcDeclaration.Range, CurrentScope);

            foreach (var funcParameter in funcDeclaration.Params)
            {
                if (funcParameter.Type == Nodes.Identifier)
                    ParseIdentifierAsNewVariable(funcParameter.As<Identifier>());
            }

            ParseStatementWithoutNewScope(funcDeclaration.Body);

            CurrentScope = parentScope;
        }

        private void ParseMethodDeclaration(MethodDeclaration methodDeclaration)
        {
            CurrentScope.DefinedMethods.Add(new Symbol(methodDeclaration, methodDeclaration.Id.Name));

            var parentScope = CurrentScope;
            CurrentScope = NewScope(methodDeclaration.Range, CurrentScope);

            foreach (var funcParameter in methodDeclaration.Params)
            {
                if (funcParameter.Type == Nodes.Identifier)
                    ParseIdentifierAsNewVariable(funcParameter.As<Identifier>());
            }

            ParseStatementWithoutNewScope(methodDeclaration.Body);

            CurrentScope = parentScope;
        }

        private void ParseBlockStatement(BlockStatement blockStatement, bool createNewScope = true)
        {
            Scope parentScope = CurrentScope;
            if (createNewScope)
                CurrentScope = NewScope(blockStatement.Range, CurrentScope);

            foreach (var statement in blockStatement.Body)
                ParseStatement(statement);

            if (createNewScope)
                CurrentScope = parentScope;
        }

        private void ParseClassDeclaration(ClassDeclaration classDecl)
        {
            CurrentScope.DefinedModules.Add(new Symbol(classDecl, classDecl.Id.Name));

            ParseStatement(classDecl.Body);
        }

        private void ParseForStatement(ForStatement forStatement)
        {
            var parentScope = CurrentScope;
            CurrentScope = NewScope(forStatement.Range, CurrentScope);

            if (forStatement.Init != null)
            {
                foreach (Node init in forStatement.Init.ChildNodes)
                {
                    ParseStatement(init);
                }
            }

            // For is already a new scope on its own
            ParseStatementWithoutNewScope(forStatement.Body);

            CurrentScope = parentScope;
        }

        private void ParseForeachStatement(ForeachStatement foreachStatement)
        {
            var parentScope = CurrentScope;
            CurrentScope = NewScope(foreachStatement.Range, CurrentScope);

            var n = foreachStatement.Left;

            ParseVariableDeclaration(n.As<VariableDeclaration>());
            ParseStatementWithoutNewScope(foreachStatement.Body);

            CurrentScope = parentScope;
        }

        private void ParseVariableDeclaration(VariableDeclaration varDeclaration)
        {
            foreach (var declaration in varDeclaration.Declarations)
            {
                ParseStatement(declaration);
            }
        }

        private void ParseVariableDeclarator(VariableDeclarator declarator)
        {
            if (declarator.Id.Type == Nodes.ArrayPattern)
            {
                var pattern = declarator.Id.As<ArrayPattern>();
                foreach (var n in pattern.Elements)
                {
                    if (n.Type == Nodes.Identifier)
                        ParseIdentifierAsNewVariable(n.As<Identifier>());
                }
                    
            }
            else if (declarator.Id.Type == Nodes.Identifier)
            {
                ParseIdentifierAsNewVariable(declarator.Id.As<Identifier>());
            }
        }

        private void ParseIdentifierAsNewVariable(Identifier identifier)
        {
            if (string.IsNullOrEmpty(identifier.Name))
                return;

            var varName = identifier.Name;
            if (varName.Contains("::"))
                return; // Static reference, do not track

            var symbol = new Symbol(identifier, varName);
            RegisterVariable(symbol);
        }


        private void RegisterVariable(Symbol symbol)
        {
            CurrentScope.DefinedVariables.Add(symbol);
        }

        private Scope NewScope(Esprima.Ast.Range range, Scope parent)
        {
            var scope = new Scope();
            scope.Range = range;
            scope.Parent = parent;
            parent.Child.Add(scope);

            PositionToScope.Add(range.Start, range.End, scope);
            return scope;
        }

        public AdhocAnalysisResult GetScopesFromPosition(int codePosition)
        {
            var scopes = PositionToScope.Query(codePosition);
            var result = new AdhocAnalysisResult();
            result.Scopes = scopes;
            return result;
        }
    }

    public class Scope
    {
        public Esprima.Ast.Range Range { get; set; }
        public Scope Parent { get; set; }
        public List<Scope> Child { get; set; } = new List<Scope>();

        public List<Symbol> DefinedVariables { get; set; } = new List<Symbol>();
        public List<Symbol> DefinedStatics { get; set; } = new List<Symbol>();
        public List<Symbol> DefinedAttributes { get; set; } = new List<Symbol>();
        public List<Symbol> DefinedFunctions { get; set; } = new List<Symbol>();
        public List<Symbol> DefinedMethods { get; set; } = new List<Symbol>();
        public List<Symbol> DefinedModules { get; set; } = new List<Symbol>();
    }

    public class Symbol
    {
        public Node Node { get; set; }
        public string Name { get; set; }

        public Symbol(Node baseNode, string name)
        {
            Node = baseNode;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
