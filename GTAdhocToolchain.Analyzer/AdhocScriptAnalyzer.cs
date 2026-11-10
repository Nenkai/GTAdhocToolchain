//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

using Esprima;
using Esprima.Ast;

using GTAdhocToolchain.Core;
using GTAdhocToolchain.Core.Instructions;
using GTAdhocToolchain.Core.Variables;

using IntervalTree;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GTAdhocToolchain.Analyzer;

public class AdhocScriptAnalyzer
{
    /// <summary>
    /// Parent modules. Does not include the current module. Use <see cref="CurrentModule"/> to get the current module.<br/>
    /// Only used to determine the current module/class. When leaving modules/classes.
    /// </summary>
    // GT7 1.00: 305E6D0 (ParserModuleListMaybe::Push)
    public List<DeclModule> ParentModules { get; set; } = [];


    /// <summary>
    /// Current module.
    /// </summary>
    public DeclModule CurrentModule { get; set; }

    private readonly List<ScopeContext> _moduleOrClassScopeStack = [];
    private ScopeContext CurrentModuleOrClassScope => _moduleOrClassScopeStack.Count > 0 ? _moduleOrClassScopeStack[^1] : null;

    private readonly List<ScopeContext> _scopeStack = [];
    private ScopeContext CurrentLocalScope => _scopeStack.Count > 0 ? _scopeStack[^1] : null;

    private List<Variable> _scopedVariables = [];

    /// <summary>
    /// Top level module (__toplevel__).
    /// </summary>
    public DeclModule TopLevelModule { get; set; }

    public ScopeContext TopLevelScope { get; set; }

    public LinkedList<bool> StrictStack { get; set; } = new();

    public bool IsStrictMode => StrictStack.Last?.Value ?? false;

    public AdhocSymbolMap SymbolMap { get; set; } = new();

    public IntervalTree<ScopeLocation, ScopeContext> PositionToScope { get; set; } = [];
    public CollectingErrorHandler ErrorHandler { get; set; }

    public AdhocScriptAnalyzer()
    {
        CurrentModule = new DeclModule(parent: null, "__toplevel__", AdhocVariableType.Module, null);
        TopLevelModule = CurrentModule;
    }

    public ScopeContext GetScope(int line, int column)
    {
        IEnumerable<ScopeContext> scopes = PositionToScope.Query(new ScopeLocation(line + 1, column));
        return scopes.LastOrDefault();
    }

    public void ParseScript(Script script)
    {
        ParentModules.Add(CurrentModule);

        AdhocSymbol main = SymbolMap.RegisterSymbol("main");
        SetCurrentModulePath([main, main], AdhocVariableType.Module);

        EnterCodeFrame(script);
        TopLevelScope = CurrentLocalScope;

        ParseStatements(script.Body);

        LeaveCodeFrame();

        ParentModules.Remove(CurrentModule);
    }

    private void ParseStatements(NodeList<Statement> nodes)
    {
        foreach (var n in nodes)
            ParseStatement(n);
    }

    private void ParseStatement(Node statement)
    {
        switch (statement.Type)
        {
            // Scoped statements
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
            case Nodes.DoWhileStatement:
                ParseDoWhileStatement(statement.As<DoWhileStatement>());
                break;
            case Nodes.SwitchStatement:
                ParseSwitchStatement(statement.As<SwitchStatement>());
                break;

            // Declarations
            case Nodes.ClassDeclaration:
                ParseClassDeclaration(statement.As<ClassDeclaration>());
                break;
            case Nodes.FunctionDeclaration:
                ParseFunctionDeclaration(statement.As<FunctionDeclaration>());
                break;
            case Nodes.ModuleDeclaration:
                ParseModuleDeclaration(statement.As<ModuleDeclaration>());
                break;
            case Nodes.MethodDeclaration:
                ParseMethodDeclaration(statement.As<MethodDeclaration>());
                break;
            case Nodes.DelegateDeclaration:
                ParseDelegateDeclaration(statement.As<DelegateDeclaration>());
                break;
            case Nodes.VariableDeclaration:
                ParseVariableDeclaration(statement.As<VariableDeclaration>());
                break;
            case Nodes.VariableDeclarator:
                ParseVariableDeclarator(statement.As<VariableDeclarator>());
                break;
            case Nodes.StaticDeclaration:
                ParseStaticDeclaration(statement.As<StaticDeclaration>());
                break;
            case Nodes.AttributeDeclaration:
                ParseAttributeDeclaration(statement.As<AttributeDeclaration>());
                break;

            case Nodes.UndefStatement:
                break; // TODO

            // Useless
            case Nodes.ReturnStatement:
            case Nodes.ContinueStatement:
            case Nodes.BreakStatement:
            case Nodes.PrintStatement:
            case Nodes.EmptyStatement:
                break;

            case Nodes.ExpressionStatement:
                ParseExpressionStatement(statement.As<ExpressionStatement>());
                break;

            /*
            case Nodes.ListAssignmentStatement:
                CompileListAssignmentStatement(node.As<ListAssignementStatement>());
                break;
            case Nodes.ImportDeclaration:
                CompileImport(node.As<ImportDeclaration>());
                break;
            case Nodes.IncludeStatement:
                CompileIncludeStatement(node.As<IncludeStatement>());
                break;
            case Nodes.RequireStatement:
                CompileRequireStatement(node.As<RequireStatement>());
                break;
            case Nodes.ThrowStatement:
                CompileThrowStatement(node.As<ThrowStatement>());
                break;
            case Nodes.FinalizerStatement:
                CompileFinalizerStatement(node.As<FinalizerStatement>());
                break;
            case Nodes.TryStatement:
                CompileTryStatement(node.As<TryStatement>());
                break;
            case Nodes.SourceFileStatement:
                CompileSourceFileStatement(node.As<SourceFileStatement>());
                break;
            case Nodes.ModuleConstructorStatement:
                CompileModuleConstructorStatement(node.As<ModuleConstructorStatement>());
                break;
            case Nodes.LabeledStatement: // NOTE: The original compiler doesn't have this. Instead, the label is part of each loop statement.
                CompileLabeledStatement(node.As<LabeledStatement>());
                break;

            // Pragmas
            case Nodes.PragmaDumpStatement:
                CompilePragmaDumpStatement(node.As<PragmaDumpStatement>());
                break;
            case Nodes.PragmaExecStatement:
                CompilePragmaExecStatement(node.As<PragmaExecStatement>());
                break;
            case Nodes.PragmaCurrentModuleStatement:
                CompilePragmaCurrentModuleStatement(node.As<PragmaCurrentModuleStatement>());
                break;
            case Nodes.PragmaVarStatement:
                CompilePragmaVarStatement(node.As<PragmaVarStatement>());
                break;
            case Nodes.PragmaUseStrictStatement:
                CompilePragmaUseStrictStatement(node.As<PragmaUseStrictStatement>());
                break;
            case Nodes.PragmaNoStrictStatement:
                CompilePragmaNoStrictStatement(node.As<PragmaNoStrictStatement>());
                break;
            case Nodes.PragmaPushStrictStatement:
                CompilePragmaPushStrictStatement(node.As<PragmaPushStrictStatement>());
                break;
            case Nodes.PragmaPopStrictStatement:
                CompilePragmaPopStrictStatement(node.As<PragmaPopStrictStatement>());
                break;
            case Nodes.PragmaIncludeStatement:
                CompilePragmaIncludeStatement(node.As<PragmaIncludeStatement>());
                break;

            default:
                ThrowCompilationError(node, $"Unsupported statement: {node.Type}");
                break;
            */


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
        EnterScope(statement);

        if (statement.Test is not null)
            ParseStatement(statement.Test);

        if (statement.Body.Type == Nodes.BlockStatement)
        {
            ParseStatement(statement.Body);
        }
        else
        {
            EnterScope(statement.Body);
            ParseStatement(statement.Body);
            LeaveScope();
        }

        LeaveScope();
    }

    private void ParseDoWhileStatement(DoWhileStatement statement)
    {
        EnterScope(statement);

        if (statement.Body.Type == Nodes.BlockStatement)
        {
            ParseStatement(statement.Body);
        }
        else
        {
            EnterScope(statement.Body);
            ParseStatement(statement.Body);
            LeaveScope();
        }

        ParseStatement(statement.Test);

        LeaveScope();
    }

    private void ParseSwitchStatement(SwitchStatement switchStatement)
    {
        EnterScope(switchStatement);

        ParseExpression(switchStatement.Discriminant);

        // Write switch table jumps
        for (int i = 0; i < switchStatement.Cases.Count; i++)
        {
            SwitchCase swCase = switchStatement.Cases[i];
            if (swCase.Test is not null)
            {
                ParseExpression(swCase.Test);
                foreach (var statement in swCase.Consequent)
                    ParseStatement(statement);
            }
        }

        LeaveScope();
    }

    private void ParseIfStatement(IfStatement statement)
    {
        if (statement.Test is not null)
            ParseStatement(statement.Test);

        if (statement.Consequent is not null)
        {
            if (statement.Consequent.Type == Nodes.BlockStatement)
            {
                ParseStatement(statement.Consequent);
            }
            else
            {
                EnterScope(statement.Consequent);
                ParseStatement(statement.Consequent);
                LeaveScope();
            }
        }

        if (statement.Alternate is not null)
        {
            if (statement.Alternate.Type == Nodes.BlockStatement)
            {
                ParseStatement(statement.Alternate); // else body
            }
            else
            {
                EnterScope(statement.Alternate);
                ParseStatement(statement.Alternate);
                LeaveScope();
            }
        }
    }

    private void ParseExpressionStatement(ExpressionStatement expStatement)
    {
        ParseExpression(expStatement.Expression);
    }

    private void ParseStaticDeclaration(StaticDeclaration staticDecl)
    {
        Identifier ident = staticDecl.Declaration.Id.As<Identifier>();
        string name = ident.Name!;

        // static definition with no value
        var idSymb = SymbolMap.RegisterSymbol(name);
        DefineVariableForCurrentModule(name, AdhocVariableType.Static, ident.Location, staticDecl.Location);
        DefineVariableInCurrentScope(idSymb, AdhocVariableType.Static, ident.Location, staticDecl.Location);
    }

    private void ParseAttributeDeclaration(AttributeDeclaration attributeDecl)
    {
        var node = attributeDecl.VarExpression;

        Identifier ident;
        if (attributeDecl.VarExpression.Type == Nodes.AssignmentExpression)
        {
            ident = attributeDecl.VarExpression.As<AssignmentExpression>().Left.As<Identifier>();
        }
        else if (attributeDecl.VarExpression.Type == Nodes.Identifier)
        {
            ident = attributeDecl.VarExpression.As<Identifier>();
        }
        else
            return; // TODO log error?

        var idSymb = SymbolMap.RegisterSymbol(ident.Name!);

        DefineVariableForCurrentModule(idSymb.Name, AdhocVariableType.Attribute, ident.Location, attributeDecl.Location);
        DefineVariableInCurrentScope(idSymb, AdhocVariableType.Attribute, ident.Location, attributeDecl.Location);
    }

    private void ParseFunctionDeclaration(FunctionDeclaration funcDecl)
    {
        AdhocSymbol nameSymbol = SymbolMap.RegisterSymbol(funcDecl.Id.Name);
        DefineVariableForCurrentModule(funcDecl.Id.Name, AdhocVariableType.Function, funcDecl.Id.Location, funcDecl.Location);
        DefineVariableInCurrentScope(SymbolMap.RegisterSymbol(funcDecl.Id.Name), AdhocVariableType.Function, funcDecl.Id.Location, funcDecl.Location);

        ParseSubroutine(funcDecl, funcDecl.Body, funcDecl.Id, funcDecl.Params);

        _scopedVariables.Remove(_scopedVariables[^1]);
    }

    private void ParseModuleDeclaration(ModuleDeclaration moduleDecl)
    {
        List<string> modulePath = [];
        if (moduleDecl.Id is Identifier identifier)
        {
            if (identifier.Name!.Contains(AdhocConstants.OPERATOR_STATIC))
                modulePath.AddRange(identifier.Name.Split(AdhocConstants.OPERATOR_STATIC));
            modulePath.Add(identifier.Name);
        }
        else if (moduleDecl.Id is StaticIdentifier staticIdentifier)
        {
            modulePath.AddRange(staticIdentifier.Id.Name!.Split(AdhocConstants.OPERATOR_STATIC));
            modulePath.Add(staticIdentifier.Id.Name);
        }

        List<AdhocSymbol> modulePathSymbols = [];
        foreach (var str in modulePath)
            modulePathSymbols.Add(SymbolMap.RegisterSymbol(str));

        ParentModules.Add(CurrentModule);
        SetCurrentModulePath(modulePathSymbols, AdhocVariableType.Module);
        DefineVariableInCurrentScope(modulePathSymbols[^1], AdhocVariableType.Module, moduleDecl.Id.Location, moduleDecl.Location);

        EnterModuleOrClassScope();
        ParseBlockStatement(moduleDecl.Body.As<BlockStatement>(), createNewScope: false);
        LeaveModuleOrClassScope();

        LeaveCurrentModule();

        _scopedVariables.Remove(_scopedVariables[^1]);
    }

    private void ParseSubroutine(Node parentNode, Node body, Identifier? id, NodeList<Expression> subParams)
    {
        EnterCodeFrame(body);

        if (parentNode is MethodDeclaration)
            DefineVariableInCurrentScope(SymbolMap.RegisterSymbol(AdhocConstants.SELF), AdhocVariableType.LocalVariable, id.Location, id.Location);

        foreach (var param in subParams)
        {
            if (param.Type == Nodes.Identifier)
            {
                var paramIdent = param.As<Identifier>();
                AdhocSymbol paramSymb = SymbolMap.RegisterSymbol(paramIdent.Name!);
                DefineVariableInCurrentScope(paramSymb, AdhocVariableType.LocalVariable, param.Location, param.Location);
            }
            else if (param.Type == Nodes.ListAssignmentExpression)
            {
                throw new NotImplementedException(); // TODO move scope stuff here
            }
            else
            {
                if (param is AssignmentExpression assignmentExpression) // Parameter default value set to another variable or static value
                {
                    AdhocSymbol paramSymb = SymbolMap.RegisterSymbol(assignmentExpression.Left.As<Identifier>().Name!);
                    DefineVariableInCurrentScope(paramSymb, AdhocVariableType.LocalVariable, assignmentExpression.Left.Location, assignmentExpression.Left.Location);
                }
                else if (param.Type == Nodes.AssignmentPattern)
                {
                    var pattern = param.As<AssignmentPattern>();

                    AdhocSymbol paramSymb = SymbolMap.RegisterSymbol(pattern.Left.As<Identifier>().Name!);
                    DefineVariableInCurrentScope(paramSymb, AdhocVariableType.LocalVariable, pattern.Left.Location, pattern.Left.Location);
                }
                else if (param.Type == Nodes.RestElement) // Rest element function(args...)
                {
                    Identifier paramIdent = param.As<RestElement>().Argument.As<Identifier>();
                    AdhocSymbol paramSymb = SymbolMap.RegisterSymbol(paramIdent.Name!);
                    DefineVariableInCurrentScope(paramSymb, AdhocVariableType.LocalVariable, paramIdent.Location, paramIdent.Location);
                }
                else
                    throw new NotSupportedException();
            }

        }

        if (body.Type == Nodes.BlockStatement)
            ParseStatement(body.As<BlockStatement>()); // TODO: ParseStatementList
        else if (body.Type == Nodes.CallExpression)
            ParseStatement(body.As<CallExpression>());

        LeaveCodeFrame();
    }

    private void ParseMethodDeclaration(MethodDeclaration methodDecl)
    {
        Identifier id = methodDecl.Id!.As<Identifier>();
        string name = id.Name!;

        var symbol = SymbolMap.RegisterSymbol(name);
        DefineVariableForCurrentModule(name, AdhocVariableType.Method, id.Location, methodDecl.Location);
        DefineVariableInCurrentScope(symbol, AdhocVariableType.Method, id.Location, methodDecl.Location);
        ParseSubroutine(methodDecl, methodDecl.Body, id, methodDecl.Params);

        _scopedVariables.Remove(_scopedVariables[^1]);
    }

    private void ParseDelegateDeclaration(DelegateDeclaration delegateDefinition)
    {
        var idSymb = SymbolMap.RegisterSymbol(delegateDefinition.Identifier.Name!);
        DefineVariableForCurrentModule(idSymb.Name, AdhocVariableType.Delegate, delegateDefinition.Identifier.Location, delegateDefinition.Location);
        DefineVariableInCurrentScope(idSymb, AdhocVariableType.Delegate, delegateDefinition.Identifier.Location, delegateDefinition.Location);
    }

    private void ParseBlockStatement(BlockStatement blockStatement, bool createNewScope = true)
    {
        if (createNewScope)
            EnterScope(blockStatement);

        ParseStatements(blockStatement.Body);

        if (createNewScope)
            LeaveScope();
    }

    private void ParseClassDeclaration(ClassDeclaration classDecl)
    {
        Identifier name = classDecl.Id.As<Identifier>();
        AdhocSymbol nameSymbol = SymbolMap.RegisterSymbol(name.Name);
        SetCurrentClass(nameSymbol);
        DefineVariableInCurrentScope(nameSymbol, AdhocVariableType.Class, classDecl.Id.Location, classDecl.Location);

        EnterModuleOrClassScope();
        ParseStatement(classDecl.Body);
        LeaveModuleOrClassScope();

        LeaveCurrentModule();
    }

    private void ParseForStatement(ForStatement forStatement)
    {
        EnterScope(forStatement);

        if (forStatement.Init is not null)
            ParseStatement(forStatement.Init);

        if (forStatement.Test is not null)
            ParseStatement(forStatement.Test);

        if (forStatement.Body.Type == Nodes.BlockStatement)
        {
            ParseStatement(forStatement.Body);
        }
        else
        {
            EnterScope(forStatement.Body);
            ParseStatement(forStatement.Body);
            LeaveScope();
        }

        if (forStatement.Update is not null)
            ParseStatement(forStatement.Update);

        LeaveScope();
    }

    private void ParseForeachStatement(ForeachStatement foreachStatement)
    {
        EnterScope(foreachStatement);

        ParseExpression(foreachStatement.Right);
        ParseStatement(foreachStatement.Left);

        if (foreachStatement.Body.Type == Nodes.BlockStatement)
        {
            ParseStatement(foreachStatement.Body);
        }
        else
        {
            EnterScope(foreachStatement.Body);
            ParseStatement(foreachStatement.Body);
            LeaveScope();
        }

        LeaveScope();
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
        if (declarator.Id is Identifier id && !string.IsNullOrWhiteSpace(id.Name))
        {
            DefineLocalVariable(SymbolMap.RegisterSymbol(id.Name), id.Location, declarator.Location);
        }
    }

    private ScopeContext EnterScope(Node sourceNode)
    {
        var lastScope = CurrentLocalScope;

        var scope = new ScopeContext()
        {
            Parent = lastScope,
            Location = sourceNode.Location,
        };
        lastScope?.Children?.Add(scope);

        _scopeStack.Add(scope);
        PositionToScope.Add(
            new ScopeLocation(scope.Location.Start.Line, scope.Location.Start.Column),
            new ScopeLocation(scope.Location.End.Line, scope.Location.End.Column),
            scope);

        return scope;
    }

    private void LeaveScope()
    {
        var scope = _scopeStack[^1];
        _scopeStack.Remove(scope);
    }

    private void EnterCodeFrame(Node sourceNode)
    {
        var lastScope = CurrentLocalScope;
        var scope = new ScopeContext() 
        { 
            Type = AdhocScopeType.TopLevel, 
            Parent = lastScope,
            Location = sourceNode.Location,
        };
        lastScope?.Children?.Add(scope);

        _scopeStack.Add(scope);
        _moduleOrClassScopeStack.Add(scope);

        PositionToScope.Add(
           new ScopeLocation(scope.Location.Start.Line, scope.Location.Start.Column),
           new ScopeLocation(scope.Location.End.Line, scope.Location.End.Column),
           scope);
    }

    private void LeaveCodeFrame()
    {
        _scopeStack.Remove(CurrentLocalScope);
        _moduleOrClassScopeStack.Remove(CurrentModuleOrClassScope);
    }

    private void LeaveCurrentModule()
    {
        if (CurrentModule != ParentModules[^1])
            CurrentModule = ParentModules[^1];

        if (ParentModules.Count != 0)
            ParentModules.Remove(ParentModules[^1]);
    }

    private void EnterModuleOrClassScope()
    {
        var lastScope = CurrentLocalScope;
        var scope = new ScopeContext()
        {
            Type = AdhocScopeType.ModuleOrClass,
            Parent = lastScope,
        };
        lastScope?.Children?.Add(scope);

        _scopeStack.Add(scope);
        _moduleOrClassScopeStack.Add(scope);
    }

    private void LeaveModuleOrClassScope()
    {
        LeaveScope();
        _moduleOrClassScopeStack.Remove(_moduleOrClassScopeStack[^1]);
    }

    private void SetCurrentClass(AdhocSymbol symbol)
    {
        ParentModules.Add(CurrentModule);
        SetCurrentModulePath([symbol], AdhocVariableType.Class);
    }

    private void SetCurrentModulePath(List<AdhocSymbol> path, AdhocVariableType variableType)
    {
        // Try finding an existing starting scope path based on our input path
        DeclValue? baseModule = null;
        if (path[0].Name == "__toplevel__")
        {
            baseModule = TopLevelModule;
        }
        else if (path[0].Name == "__module__")
        {
            baseModule = CurrentModule;
        }
        else
        {
            // Go up in module scopes until we find a matching starting path
            for (var currentModule = CurrentModule; currentModule != null; currentModule = currentModule.ParentModule)
            {
                if (currentModule.Variables.TryGetValue(path[0].Name, out DeclValue? moduleVariable))
                {
                    // Found a module scope that starts with what we expect
                    if (moduleVariable.Type != AdhocVariableType.Unknown)
                        baseModule = moduleVariable;
                    else
                        baseModule = (DeclModule)GetOrDefineModuleVariable(currentModule, path[0].Name, variableType); // Redefine if there's any ambiguity.
                    break;
                }

                if (path.Count <= 1)
                    break;
            }
        }

        // Did we find a matching starting module that matches our path?
        if (baseModule is null)
        {
            // Nope, so declare the path entirely.
            baseModule = CurrentModule;
            for (int i = 0; i < Math.Max(path.Count - 1, 1); i++)
            {
                baseModule = GetOrDefineModuleVariable((DeclModule)baseModule, path[i].Name, variableType);
            }
        }
        else
        {
            // We did, so simply define the path starting from the starting module.
            for (int i = 1; i < path.Count - 1; i++)
            {
                if (baseModule is not DeclModule declModule)
                {
                    //ThrowNameError($"'{baseModule.Name}' is not a module name (in {path[^1]}"); // '%s' is not a module name. (in %s)
                    return;
                }

                if (declModule.Variables.TryGetValue(path[i].Name, out DeclValue? moduleVariable))
                {
                    if (moduleVariable.Type != AdhocVariableType.Unknown)
                        baseModule = moduleVariable;
                    else
                        baseModule = GetOrDefineModuleVariable((DeclModule)baseModule, path[i].Name, variableType); // Redefine if there's any ambiguity.
                }
                else
                {
                    baseModule = GetOrDefineModuleVariable((DeclModule)baseModule, path[i].Name, variableType);
                }
            }
        }

        if (baseModule is not null)
        {
            CurrentModule = (DeclModule)baseModule;
        }
        else
            ; //ThrowNameError($"cannot set current module {path[^1]}"); // cannot set current module %s
    }

    private void DefineVariableForCurrentModule(string name, AdhocVariableType type, Location idLocation, Location bodyLocation)
    {
        GetOrDefineModuleVariable(CurrentModule, name, type, idLocation);
    }

    private DeclValue GetOrDefineModuleVariable(DeclModule module, string attributeName, AdhocVariableType newVariableType, Location? idLocation = null)
    {
        if (module.Variables.TryGetValue(attributeName, out DeclValue? value))
        {
            // Found something.

            // If our expected type, or the value type is unknown, it is removed
            if (newVariableType == AdhocVariableType.Unknown || value.Type == AdhocVariableType.Unknown)
                module.Variables.Remove(attributeName); // Undef, just to make sure.
            else
                return value; // This will be returned regardless of expected type
        }

        // Define new one
        DeclValue newModule;
        if (newVariableType == AdhocVariableType.Module || newVariableType == AdhocVariableType.Class)
            newModule = new DeclModule(parent: module, attributeName, newVariableType, idLocation);
        else
            newModule = new DeclValue(parent: module, attributeName, newVariableType, idLocation);

        module.AddVariable(attributeName, newModule);
        return newModule;
    }

    private void DefineVariableInCurrentScope(AdhocSymbol name, AdhocVariableType variableType, Location? idLocation = null, Location? bodyLocation = null)
    {
        if (variableType == AdhocVariableType.Undef)
        {
            UndefSymbol(name);
            return;
        }

        if (IsStrictMode && IsDeclarationType(variableType))
        {
            if (variableType != AdhocVariableType.LocalVariable)
            {
                DeclValue? value = FindVariableFromSymbol(name);
                if (value is not null)
                {
                    if (value.Type == AdhocVariableType.Delegate || value.Type == variableType)
                    {
                        //value->byte54 = 1;
                        value.Location = idLocation.Value;
                    }
                    else
                    {
                        string fullPath = value.GetFullPath();
                        //ThrowStrictCheckError($"{variableType} '{fullPath}' is already defined as {value.Type} at {value.Location.Value.Source}:{value.Location.Value.Start.Line}.");
                    }
                }
                else
                {
                    //ThrowStrictCheckError($"'{name.Name}' is not declared in this scope.");
                }
            }
        }

        Variable? variable;
        switch (variableType)
        {
            case AdhocVariableType.LocalVariable:
                if (!CurrentLocalScope.Variables.TryGetValue(name, out variable))
                {
                    DefineLocalVariable(name, idLocation, bodyLocation);
                    return;
                }
                break;

            default:
                if (!CurrentModuleOrClassScope.Variables.TryGetValue(name, out variable))
                {
                    DefineStaticVariable(variableType, name, idLocation, bodyLocation);
                    return;
                }
                break;
        }

        // Check redefinition types
        switch (variable.Type)
        {
            case AdhocVariableType.Attribute:
            case AdhocVariableType.Function:
            case AdhocVariableType.Method:
            case AdhocVariableType.Static:
                if (variable.Type == variableType)
                    return; // Redeclaring to same type, no problems
                break;

            case AdhocVariableType.Class:
                if (variableType == AdhocVariableType.Class || variableType == AdhocVariableType.Module)
                    return; // Redeclaring to already module or class type, no problems
                break;

            case AdhocVariableType.Module:
                if (variableType == AdhocVariableType.Module)
                    return; // Redeclaring to already module type, no problems
                break;

            case AdhocVariableType.LocalVariable:
                break; // Local is being redeclared, not good.

            default:
                return;
        }

        // %s '%s' is already defined as %s at %s:%d.
        //ThrowNameError($"{location?.Source}:{location?.Start.Line ?? 0}: {variableType} '{name.Name}' is already defined as {variable.Type} at {variable.DeclarationSourceFileName}:{variable.DeclarationLineNumber}.");
    }

    private DeclValue? FindVariableFromSymbol(AdhocSymbol path)
    {
        if (path.Name == "__toplevel__")
        {
            return TopLevelModule;
        }
        else if (path.Name == "__module__")
        {
            return CurrentModule;
        }

        if (CurrentModule.Variables.TryGetValue(path.Name, out DeclValue? value))
            return value;

        return null;
    }

    private static bool IsDeclarationType(AdhocVariableType variableType)
    {
        return variableType <= AdhocVariableType.Static;
    }

    private void UndefSymbol(AdhocSymbol symbol)
    {
        CurrentModuleOrClassScope.Variables.Remove(symbol);
    }

    private void DefineLocalVariable(AdhocSymbol name, Location? idLocation = null, Location? bodyLocation = null)
    {
       if (CurrentLocalScope.Variables.TryGetValue(name, out Variable? definedVariable))
           return;

        var variable = new Variable(name, AdhocVariableType.LocalVariable, 0, idLocation, bodyLocation);
        CurrentLocalScope.Variables.Add(name, variable);

        if (_scopedVariables.Count > 0)
        {
            _scopedVariables[^1].Children.Add(variable);
        }
    }

    private void DefineStaticVariable(AdhocVariableType type, AdhocSymbol name, Location? idLocation, Location? bodyLocation)
    {
        if (CurrentModuleOrClassScope.Variables.TryGetValue(name, out Variable? definedVariable))
            return;

        var variable = new Variable(name, type, 0, idLocation, bodyLocation);
        CurrentModuleOrClassScope.Variables.Add(name, variable);

        if (_scopedVariables.Count > 0)
        {
            _scopedVariables[^1].Children.Add(variable);
        }

        if (type == AdhocVariableType.Function || type == AdhocVariableType.Method ||
            type == AdhocVariableType.Module || type == AdhocVariableType.Class)
        {
            _scopedVariables.Add(variable);
        }
    }
}

public record ScopeLocation(int Line, int Column) : IComparable<ScopeLocation>
{
    public int CompareTo(ScopeLocation? other)
    {
        if (other is null)
            return 1;

        int lineComparison = Line.CompareTo(other.Line);
        return lineComparison != 0 ? lineComparison : Column.CompareTo(other.Column);
    }
}