
using Esprima;
using Esprima.Ast;

using GTAdhocToolchain.Core;
using GTAdhocToolchain.Core.Instructions;
using GTAdhocToolchain.Core.Variables;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace GTAdhocToolchain.Compiler;

/// <summary>
/// Adhoc script compiler.
/// </summary>
public class AdhocScriptCompiler
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    ///////////////////////////////////////////////////////////////////
    // The following proeprties are part of the original compiler
    ///////////////////////////////////////////////////////////////////

    /// <summary>
    /// List of all code frames.
    /// </summary>
    public List<AdhocCodeFrame> Frames { get; set; } = [];

    /// <summary>
    /// Current code frame.
    /// </summary>
    public AdhocCodeFrame CurrentFrame => Frames.Count > 0 ? Frames[^1] : null;

    /// <summary>
    /// List of all defined scopes.
    /// </summary>
    public List<ScopeContext> Scopes { get; set; } = [];

    /// <summary>
    /// Current scope.
    /// </summary>
    public ScopeContext CurrentLocalScope => Scopes.Count > 0 ? Scopes[^1] : null;

    /// <summary>
    /// List of all module or class scopes.
    /// </summary>
    public List<ScopeContext> ModuleOrClassScopes { get; set; } = [];

    /// <summary>
    /// Current module or class scope.
    /// </summary>
    public ScopeContext CurrentModuleOrClassScope => ModuleOrClassScopes.Count > 0 ? ModuleOrClassScopes[^1] : null;

    // Original compiler has these two as std::list<std::pair<Symbol, std::vector<int>>>.
    public List<(string Label, List<int> Jumps)> Continues { get; set; } = [];
    public List<(string Label, List<int> Jumps)> Breaks { get; set; } = [];

    // This would be part of hParser

    /// <summary>
    /// Parent modules. Does not include the current module. Use <see cref="CurrentModule"/> to get the current module.<br/>
    /// Only used to determine the current module/class. When leaving modules/classes.
    /// </summary>
    // GT7 1.00: 305E6D0 (ParserModuleListMaybe::Push)
    public List<DeclModule> ParentModules { get; set; } = [];

    /// <summary>
    /// Top level module (__toplevel__).
    /// </summary>
    public DeclModule TopLevel { get; set; }

    /// <summary>
    /// Current module.
    /// </summary>
    public DeclModule CurrentModule { get; set; }

    /// <summary>
    /// Module or class depth per frame.
    /// </summary>
    public LinkedList<int> DepthPerFrame { get; set; } = new();

    ///////////////////////////////////////////////////////////////////
    // ... End of original compiler properties
    ///////////////////////////////////////////////////////////////////

    private AdhocVersion Version { get; }

    // Additional fields relevant to us
    /// <summary>
    /// All the symbols defined for the current compilation unit.
    /// </summary>
    public AdhocSymbolMap SymbolMap { get; set; } = new();

    public HashSet<string> PostCompilationWarnings = [];

    private Script _debugPrintException;
    private Script _debugThrow;

    public AdhocScriptCompiler(uint version)
    {
        Version = new AdhocVersion(version);

        // Normally part of parser
        CurrentModule = new DeclModule(parent: null, "__toplevel__", AdhocVariableType.Module, null);
        TopLevel = CurrentModule;
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

    /// <summary>
    /// Compiles a script.
    /// </summary>
    /// <param name="script"></param>
    public AdhocCodeFrame CompileScript(Script script, string? sourcePath = null)
    {
        Logger.Info("Started script compilation.");

        try
        {
            ParentModules.Add(CurrentModule);
            SetCurrentModulePath(["main", "main"], AdhocVariableType.Module);

            EnterCodeFrame();

            if (!string.IsNullOrEmpty(sourcePath))
                CurrentFrame.SetSourcePath(SymbolMap.RegisterSymbol(sourcePath, false));

            if (CurrentFrame.SourceFilePath is null)
                CurrentFrame.SetSourcePath(SymbolMap.RegisterSymbol("<unnamed file>", false));

            // Always an empty one in old versions (same in subroutines)
            if (CurrentFrame.Version.HasReservedLocalInFrame())
                DefineVariableInCurrentScope(SymbolMap.RegisterSymbol(AdhocConstants.NIL), AdhocVariableType.LocalVariable);

            CompileScriptBody(script);

            // Script done.
            InsertSetState(AdhocRunState.EXIT);
            var mainCodeFrame = LeaveCodeFrame();
            ParentModules.Remove(CurrentModule);

            //////////////////////////////////////////////
            // Done With everything.
            //////////////////////////////////////////////
            PrintPostCompilationWarnings();
            Logger.Info($"Script successfully compiled.");

            return mainCodeFrame;
        }
        catch (StrictCheckException ex)
        {
            throw new AdhocCompilationException(ex);
        }
        catch (NameErrorException ex)
        {
            throw new AdhocCompilationException(ex);
        }
    }

    /// <summary>
    /// Compiles a script body into a frame.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="script"></param>
    public void CompileScriptBody(Script script)
    {
        CompileStatements(script.Body);
    }

    /// <summary>
    /// Builds code for printing exceptions to file (used in CompileStatements())
    /// </summary>
    public void BuildTryCatchDebugStatements()
    {
        _debugPrintException = new AdhocAbstractSyntaxTree("__toplevel__::main::pdistd::AppendFile(\"/APP_DATA_RAW/exceptions.txt\", \"%{__ex}\\n\");").ParseScript();
        _debugThrow = new AdhocAbstractSyntaxTree("throw __ex;").ParseScript();
    }

    public void CompileStatementList(Node node)
    {
        foreach (var n in node.ChildNodes)
            CompileStatement(n);
    }

    public void CompileStatements(NodeList<Statement> nodes)
    {
        if (_debugPrintException != null)
        {
            // This is super hacky. But the intent is a hack anyway.
            var tryCatch = new InsTryCatch();
            AddInstruction(tryCatch);

            foreach (var n in nodes)
                CompileStatement(n);

            InsertSetState(AdhocRunState.EXIT);
            tryCatch.InstructionIndex = CurrentFrame.GetInstructionCount();

            InsJump catchClauseSkipper = new InsJump();
            AddInstruction(catchClauseSkipper);

            AddInstruction(new InsIntConst(0));
            InsertLocalVariablePush("__ex");
            AddInstruction(InsAssign.Default);

            string tmpCaseVariable = $"catch#{SymbolMap.TempVariableCounter++}";
            InsertLocalVariablePush(tmpCaseVariable);
            InsertAssignPop();

            CompileStatement(_debugPrintException.Body[0]);
            CompileStatement(_debugThrow.Body[0]);

            catchClauseSkipper.JumpInstructionIndex = CurrentFrame.GetInstructionCount();
        }
        else
        {
            foreach (var n in nodes)
                CompileStatement(n);
        }
    }

    public void CompileStatement(Node node)
    {
        switch (node.Type)
        {
            case Nodes.ClassDeclaration:
                CompileClassDeclaration(node.As<ClassDeclaration>());
                break;
            case Nodes.ModuleDeclaration:
                CompileModuleDeclaration(node.As<ModuleDeclaration>());
                break;
            case Nodes.FunctionDeclaration:
                CompileFunctionDeclaration(node.As<FunctionDeclaration>());
                break;
            case Nodes.MethodDeclaration:
                CompileMethodDeclaration(node.As<MethodDeclaration>());
                break;
            case Nodes.ForStatement:
                CompileFor(node.As<ForStatement>());
                break;
            case Nodes.ForeachStatement:
                CompileForeach(node.As<ForeachStatement>());
                break;
            case Nodes.WhileStatement:
                CompileWhile(node.As<WhileStatement>());
                break;
            case Nodes.DoWhileStatement:
                CompileDoWhile(node.As<DoWhileStatement>());
                break;
            case Nodes.ListAssignmentStatement:
                CompileListAssignmentStatement(node.As<ListAssignementStatement>());
                break;
            case Nodes.VariableDeclaration:
                CompileVariableDeclaration(node.As<VariableDeclaration>());
                break;
            case Nodes.StaticDeclaration:
                CompileStaticDeclaration(node.As<StaticDeclaration>());
                break;
            case Nodes.AttributeDeclaration:
                CompileAttributeDeclaration(node.As<AttributeDeclaration>());
                break;
            case Nodes.ReturnStatement:
                CompileReturnStatement(node.As<ReturnStatement>());
                break;
            case Nodes.ImportDeclaration:
                CompileImport(node.As<ImportDeclaration>());
                break;
            case Nodes.IfStatement:
                CompileIfStatement(node.As<IfStatement>());
                break;
            case Nodes.BlockStatement:
                CompileBlockStatement(node.As<BlockStatement>());
                break;
            case Nodes.ExpressionStatement:
                CompileExpressionStatement(node.As<ExpressionStatement>());
                break;
            case Nodes.SwitchStatement:
                CompileSwitch(node.As<SwitchStatement>());
                break;
            case Nodes.ContinueStatement:
                CompileContinue(node.As<ContinueStatement>());
                break;
            case Nodes.BreakStatement:
                CompileBreak(node.As<BreakStatement>());
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
            case Nodes.UndefStatement:
                CompileUndefStatement(node.As<UndefStatement>());
                break;
            case Nodes.SourceFileStatement:
                CompileSourceFileStatement(node.As<SourceFileStatement>());
                break;
            case Nodes.ModuleConstructorStatement:
                CompileModuleConstructorStatement(node.As<ModuleConstructorStatement>());
                break;
            case Nodes.PrintStatement:
                CompilePrintStatement(node.As<PrintStatement>());
                break;
            case Nodes.DelegateDeclaration:
                CompileDelegateDefinition(node.As<DelegateDeclaration>());
                break;
            case Nodes.EmptyStatement:
                CompileEmptyStatement(node);
                break;
            case Nodes.LabeledStatement: // NOTE: The original compiler doesn't have this. Instead, the label is part of each loop statement.
                CompileLabeledStatement(node.As<LabeledStatement>());
                break;
            default:
                ThrowCompilationError(node, $"Unsupported statement: {node.Type}");
                break;
        }
    }

    // NOTE: The original compiler doesn't have this. Instead, the label is part of each loop statement.
    private void CompileLabeledStatement(LabeledStatement labeledStatement)
    {
        if (labeledStatement.Body.Type != Nodes.ForStatement &&
            labeledStatement.Body.Type != Nodes.ForeachStatement &&
            labeledStatement.Body.Type != Nodes.DoWhileStatement &&
            labeledStatement.Body.Type != Nodes.WhileStatement)
        {
            ThrowCompilationError(labeledStatement.Body, CompilationMessages.Error_LabeledStatementNotALoop);
            return;
        }

        Identifier label = labeledStatement.Label;
        switch (labeledStatement.Body.Type)
        {
            case Nodes.ForStatement:
                CompileFor(labeledStatement.Body.As<ForStatement>(), label);
                break;
            case Nodes.ForeachStatement:
                CompileForeach(labeledStatement.Body.As<ForeachStatement>(), label);
                break;
            case Nodes.WhileStatement:
                CompileWhile(labeledStatement.Body.As<WhileStatement>(), label);
                break;
            case Nodes.DoWhileStatement:
                CompileDoWhile(labeledStatement.Body.As<DoWhileStatement>(), label);
                break;
        }
    }

    private void CompileEmptyStatement(Node node)
    {
        InsertNop(node.Location);
    }

    public void CompilePrintStatement(PrintStatement printStatement)
    {
        foreach (var exp in printStatement.Expressions)
            CompileExpression(exp);

        AddInstruction(new InsPrint(printStatement.Expressions.Count), printStatement.Location);
        InsertPop();
    }

    public void CompileModuleConstructorStatement(ModuleConstructorStatement ctorStatement)
    {
        // TODO Push current module

        // Compile the target expression
        CompileExpression(ctorStatement.Id);

        // Grab target, define a new ctor scope
        AddInstruction(new InsModuleConstructor());

        // TODO: Push & pop strict?
        EnterModuleOrClassScope();
        CompileStatement(ctorStatement.Body);
        LeaveModuleOrClassScope();

        // Exit ctor
        InsertSetState(AdhocRunState.EXIT);

        CurrentModule = ParentModules[^1];
        ParentModules.Remove(ParentModules[^1]);
    }

    public void CompileSourceFileStatement(SourceFileStatement srcFileStatement)
    {
        // Source file instructions are supported starting in version 7
        if (!CurrentFrame.Version.HasSourceFileInstructionSupport())
            return;

        InsSourceFile srcFileIns = new InsSourceFile(SymbolMap.RegisterSymbol(srcFileStatement.Path, false));
        AddInstruction(srcFileIns);
        CurrentFrame.SetSourcePath(SymbolMap.RegisterSymbol(srcFileStatement.Path, false));
    }

    public void CompileUndefStatement(UndefStatement undefStatement)
    {
        // XX/FIXME: Undef may refer to a local variable aswell, it's not supported though
        // GT5 SoundUtil.ad undefs BootInitialize as a local which is a defined user function

        var parts = undefStatement.Symbol.Split("::");
        List<AdhocSymbol> path = [];
        if (parts.Length > 1)
        {
            foreach (string part in undefStatement.Symbol.Split(AdhocConstants.OPERATOR_STATIC))
                path.Add(SymbolMap.RegisterSymbol(part));
        }
        else
            path.Add(SymbolMap.RegisterSymbol(parts[0]));
        path.Add(SymbolMap.RegisterSymbol(undefStatement.Symbol)); // full

        UndefSymbol(path[^1]);

        InsUndef undefIns = new InsUndef();
        undefIns.Path = path;
        AddInstruction(undefIns, undefStatement.Location);
    }

    // GT7 1.00: 30D5CB0 (mCompiler::UndefSymbol)
    private void UndefSymbol(AdhocSymbol symbol)
    {
        CurrentModuleOrClassScope.Variables.Remove(symbol);
    }

    // GT7 1.00: 30F26D0 (mCompiler::CompileTryCatch)
    public void CompileTryStatement(TryStatement tryStatement)
    {
        DepthPerFrame.Last!.ValueRef++;

        InsTryCatch tryCatch = new InsTryCatch();
        AddInstruction(tryCatch, tryStatement.Location);

        if (tryStatement.Block.Type == Nodes.BlockStatement)
        {
            CompileStatement(tryStatement.Block);
        }
        else
        {
            EnterScope();
            CompileStatement(tryStatement.Block);
            LeaveScope();
        }

        InsertSetState(AdhocRunState.EXIT);
        DepthPerFrame.Last!.ValueRef--;

        InsJump catchClauseSkipper = new InsJump();
        AddInstruction(catchClauseSkipper);
        tryCatch.InstructionIndex = CurrentFrame.GetInstructionCount() - 1;

        CatchClause? catchClause = tryStatement.Handler;
        if (catchClause is not null && catchClause.Type != Nodes.EmptyStatement)
        {
            CompileCatchClause(catchClause);
        }
        else
        {
            // Discard (pop) exception object as 0
            AddInstruction(new InsIntConst(0));
            AddInstruction(InsPop.Default);
        }

        catchClauseSkipper.JumpInstructionIndex = CurrentFrame.GetInstructionCount();
    }

    // GT7 1.00: 30F2AB0 (mCompiler::CompileCatch)
    public void CompileCatchClause(CatchClause catchClause)
    {
        // TODO: Support case keyword (with isInstanceOf)

        EnterScope();
        RegisterLoopLabelForBreak(AdhocConstants.NIL);

        if (catchClause.Param is null)
            ThrowCompilationError(catchClause, CompilationMessages.Error_CatchClauseParameterNotIdentifier);

        if (catchClause.Param.Type != Nodes.Identifier)
            ThrowCompilationError(catchClause.Param, CompilationMessages.Error_CatchClauseParameterNotIdentifier);

        var identifier = catchClause.Param.As<Identifier>();

        // Create temp variable for the exception
        string tmpCaseVariable = $"catch#{SymbolMap.TempVariableCounter++}";
        DefineVariableInCurrentScope(SymbolMap.RegisterSymbol(tmpCaseVariable), AdhocVariableType.LocalVariable);

        AddInstruction(new InsIntConst(0));
        InsertLocalVariablePush(identifier.Name!); // FIXME: as per original compiler, technically it can be any expression here that is pushed to. so a::b would be allowed
        AddInstruction(InsAssign.Default);

        InsertLocalVariablePush(tmpCaseVariable);
        InsertAssignPop();

        // Not actually a block?
        CompileStatementList(catchClause.Body);

        var defaultCaseJump = new InsJump();
        AddInstruction(defaultCaseJump);

        int index = LeaveScope();
        if (/* noDefault */ true)
        {
            defaultCaseJump.JumpInstructionIndex = index;
        }
        else
        {
            // TODO: Index of default body
        }

        ProcessBreaks(index);
    }

    /// <summary>
    /// Compiles a new frame/scope containing statements.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="BlockStatement"></param>
    public void CompileBlockStatement(BlockStatement BlockStatement,
        bool openScope = true,
        bool emitNops = true)
    {
        if (emitNops)
            InsertNop(BlockStatement.Location, useEndLineNumber: false);
        if (openScope)
            EnterScope();

        CompileStatements(BlockStatement.Body);

        if (openScope)
            LeaveScope();
        if (emitNops)
            InsertNop(BlockStatement.Location, useEndLineNumber: true);
    }

    public void CompileIncludeStatement(IncludeStatement include)
    {
        if (string.IsNullOrEmpty(BaseIncludeFolder))
            BaseIncludeFolder = Path.GetDirectoryName(CurrentFrame.SourceFilePath.Name);

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


        Logger.Info($"Linking include file '{include.Path}' for '{CurrentFrame.SourceFilePath.Name}'.");

        string file = File.ReadAllText(pathToIncludeFile);

        var parser = new AdhocAbstractSyntaxTree(file);
        parser.SetFileName(include.Path);
        Script includeScript = parser.ParseScript();

        // Set frame file name to our include file's
        string oldPath = CurrentFrame.SourceFilePath.Name;
        CurrentFrame.SetSourcePath(SymbolMap.RegisterSymbol(include.Path));

        // Alert interpreter that the current source file has changed for debugging
        InsSourceFile srcFileIns = new InsSourceFile(SymbolMap.RegisterSymbol(include.Path, false));
        AddInstruction(srcFileIns, include.Location);

        // Copy include into current frame
        CompileScriptBody(includeScript);

        // Resume
        InsSourceFile ogSrcFileIns = new InsSourceFile(CurrentFrame.SourceFilePath);
        AddInstruction(ogSrcFileIns, include.Location);

        CurrentFrame.SetSourcePath(SymbolMap.RegisterSymbol(oldPath));
    }

    public void CompileRequireStatement(RequireStatement require)
    {
        CompileExpression(require.Path);
        AddInstruction(InsRequire.Default, require.Location);
    }

    public void CompileThrowStatement(ThrowStatement throwStatement)
    {
        CompileExpression(throwStatement.Argument);
        AddInstruction(InsThrow.Default, throwStatement.Location);
    }

    // GT7 1.00: 30DB010 (mCompiler::CompileBreak)
    public void CompileBreak(BreakStatement breakStatement)
    {
        if (breakStatement.Label is null)
        {
            if (Breaks.Count == 0 || Breaks[^1].Label == AdhocConstants.FUNCTION)
                ThrowCompilationError(breakStatement, CompilationMessages.Error_BreakWithoutContextualScope);

            int idx = CurrentFrame.GetInstructionCount();
            InsJump continueJump = new InsJump();
            AddInstruction(continueJump, breakStatement.Location);

            Breaks[^1].Jumps.Add(idx);
        }
        else
        {
            foreach (var labelKv in Breaks)
            {
                if (labelKv.Label == breakStatement.Label.Name)
                {
                    int idx = CurrentFrame.GetInstructionCount();

                    InsJump continueJump = new InsJump();
                    AddInstruction(continueJump, breakStatement.Location);
                    labelKv.Jumps.Add(idx);
                    return;
                }
            }

            ThrowCompilationError(breakStatement.Label, string.Format(CompilationMessages.Error_LoopWithLabelNotFound, breakStatement.Label.Name));
        }
    }

    // GT7 1.00: 30DB530 (mCompiler::CompileContinue)
    public void CompileContinue(ContinueStatement continueStatement)
    {
        if (continueStatement.Label is null)
        {
            if (Continues.Count == 0 || Continues[^1].Label == AdhocConstants.FUNCTION)
                ThrowCompilationError(continueStatement, CompilationMessages.Error_ContinueWithoutContextualScope);

            int idx = CurrentFrame.GetInstructionCount();
            InsJump continueJump = new InsJump();
            AddInstruction(continueJump, continueStatement.Location);

            Continues[^1].Jumps.Add(idx);
        }
        else
        {
            foreach (var labelKv in Continues)
            {
                if (labelKv.Label == continueStatement.Label.Name)
                {
                    int idx = CurrentFrame.GetInstructionCount();

                    InsJump continueJump = new InsJump();
                    AddInstruction(continueJump, continueStatement.Location);
                    labelKv.Jumps.Add(idx);
                    return;
                }
            }

            ThrowCompilationError(continueStatement.Label, string.Format(CompilationMessages.Error_LoopWithLabelNotFound, continueStatement.Label.Name));
        }
    }

    public void CompileClassDeclaration(ClassDeclaration classDecl)
    {
        Identifier name = classDecl.Id.As<Identifier>();
        SetCurrentClass(name);

        InsClassDefine @class = new InsClassDefine();
        @class.Name = SymbolMap.RegisterSymbol(name.Name);

        if (classDecl.SuperClass is not null)
        {
            var superClassIdent = classDecl.SuperClass.As<Identifier>();
            if (superClassIdent.Name!.Contains(AdhocConstants.OPERATOR_STATIC))
            {
                foreach (var path in superClassIdent.Name.Split(AdhocConstants.OPERATOR_STATIC))
                    @class.ExtendsFrom.Add(SymbolMap.RegisterSymbol(path));
            }

            @class.ExtendsFrom.Add(SymbolMap.RegisterSymbol(superClassIdent.Name));
        }
        else
        {
            // Not provided, inherits from base object (System::Object, or Object if old)
            if (CurrentFrame.Version.ObjectInheritsFromSystemObject())
            {
                @class.ExtendsFrom.Add(SymbolMap.RegisterSymbol(AdhocConstants.SYSTEM));
                @class.ExtendsFrom.Add(SymbolMap.RegisterSymbol(AdhocConstants.OBJECT));
                @class.ExtendsFrom.Add(SymbolMap.RegisterSymbol($"{AdhocConstants.SYSTEM}{AdhocConstants.OPERATOR_STATIC}{AdhocConstants.OBJECT}"));
            }
            else
                @class.ExtendsFrom.Add(SymbolMap.RegisterSymbol(AdhocConstants.OBJECT));
        }

        AddInstruction(@class, classDecl.Location);

        EnterModuleOrClassScope();
        CompileStatement(classDecl.Body);
        LeaveModuleOrClassScope();

        // Exit class or module scope. Important.
        InsertSetState(AdhocRunState.EXIT);

        LeaveCurrentModule();
    }

    // GT7 1.00: 305E970 (hParser::LeaveCurrentModule)
    private void LeaveCurrentModule()
    {
        if (CurrentModule != ParentModules[^1])
            CurrentModule = ParentModules[^1];

        if (ParentModules.Count != 0)
            ParentModules.Remove(ParentModules[^1]);
    }

    // GT7 1.00 - 305E8E0 (hParser::SetCurrentClass)
    private void SetCurrentClass(Identifier name)
    {
        ParentModules.Add(CurrentModule);
        SetCurrentModulePath([name.Name!], AdhocVariableType.Class);
    }

    private void CompileModuleDeclaration(ModuleDeclaration moduleDecl)
    {
        List<string> modulePath = [];
        if (moduleDecl.Id is Identifier identifier)
        {
            if (identifier.Name!.Contains("::"))
                modulePath.AddRange(identifier.Name.Split("::"));
            modulePath.Add(identifier.Name);
        }
        else if (moduleDecl.Id is StaticIdentifier staticIdentifier)
        {
             modulePath.AddRange(staticIdentifier.Id.Name!.Split("::"));
             modulePath.Add(staticIdentifier.Id.Name);
        }

        List<AdhocSymbol> modulePathSymbols = [];
        foreach (var str in modulePath)
            modulePathSymbols.Add(SymbolMap.RegisterSymbol(str));

        var moduleDefine = new InsModuleDefine(modulePathSymbols);
        AddInstruction(moduleDefine, moduleDecl.Location);

        ParentModules.Add(CurrentModule);
        SetCurrentModulePath(modulePath, AdhocVariableType.Module);

        EnterModuleOrClassScope();
        CompileBlockStatement(moduleDecl.Body.As<BlockStatement>(), openScope: false);
        LeaveModuleOrClassScope();

        InsertSetState(AdhocRunState.EXIT);

        LeaveCurrentModule();
    }

    // GT7 1.00: 30EECE0 (mCompiler::CompileIf)
    public void CompileIfStatement(IfStatement ifStatement)
    {
        CompileTestStatement(ifStatement.Test);

        // If consequent is empty, and the alternate isn't, optimize by skipping the consequent altogether. 
        if (ifStatement.Consequent.Type == Nodes.EmptyStatement &&
            Version.VersionNumber >= 11) 
        {
            // [if <empty> else ...] path
            if (ifStatement.Alternate is not null && ifStatement.Alternate.Type != Nodes.EmptyStatement)
            {
                InsJumpIfTrue blockJump = new InsJumpIfTrue();
                AddInstruction(blockJump);

                if (ifStatement.Consequent.Type == Nodes.BlockStatement)
                {
                    CompileStatement(ifStatement.Alternate); // else body
                }
                else
                {
                    EnterScope();
                    CompileStatement(ifStatement.Alternate);
                    LeaveScope();
                }

                blockJump.JumpIndex = CurrentFrame.GetInstructionCount();
            }
            else
            {
                // [if <empty> else <empty>] path, we don't even need the result so pop it
                AddInstruction(InsPop.Default);
            }
        }
        else // [if ... else <.../empty>] path
        {
            // Create jump
            InsJumpIfFalse endOrNextIfJump = new InsJumpIfFalse();
            AddInstruction(endOrNextIfJump);

            // Apply frame
            if (ifStatement.Consequent.Type == Nodes.BlockStatement)
            {
                CompileStatement(ifStatement.Consequent); // if body
            }
            else
            {
                EnterScope();
                CompileStatement(ifStatement.Consequent);
                LeaveScope();
            }

            endOrNextIfJump.JumpIndex = CurrentFrame.GetInstructionCount();

            if (ifStatement.Alternate is not null && ifStatement.Alternate.Type != Nodes.EmptyStatement) // [if ... else ...] path
            {
                // Jump to skip the else if frame if the if was already taken
                InsJump skipAlternateJmp = new InsJump();
                AddInstruction(skipAlternateJmp);

                endOrNextIfJump.JumpIndex = CurrentFrame.GetInstructionCount();

                if (ifStatement.Alternate.Type == Nodes.BlockStatement)
                {
                    CompileStatement(ifStatement.Alternate); // else body
                }
                else
                {
                    EnterScope();
                    CompileStatement(ifStatement.Alternate);
                    LeaveScope();
                }

                skipAlternateJmp.JumpInstructionIndex = CurrentFrame.GetInstructionCount();
            }
            else // No else block or it is empty
            {
                if (CurrentFrame.Version.IfConditionAlwaysHasAlternateJump())
                {
                    InsJump skipAlternateJmp = new InsJump();
                    AddInstruction(skipAlternateJmp);
                    skipAlternateJmp.JumpInstructionIndex = CurrentFrame.GetInstructionCount();
                }

                endOrNextIfJump.JumpIndex = CurrentFrame.GetInstructionCount();
            }
        }
    }

    /// <summary>
    /// Compiles a test statement where the result, or assignment, is not immediately discarded.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="testExpression"></param>
    private void CompileTestStatement(Expression testExpression)
    {
        if (testExpression.Type == Nodes.AssignmentExpression)
        {
            CompileAssignmentExpression(testExpression.As<AssignmentExpression>(), popResult: false); // if (<test>)
        }
        else if (testExpression.Type == Nodes.UpdateExpression)
        {
            CompileUnaryExpression(testExpression.As<UpdateExpression>(), popResult: false); // var a = ++b; - Do not discard b
        }
        else
        {
            CompileExpression(testExpression);
        }
    }

    // GT7 1.00: 30F1320 (mCompiler::CompileFor)
    public void CompileFor(ForStatement forStatement, Identifier? label = null)
    {
        EnterScope(shouldCleanupOnExit: true); // True because of init.

        RegisterLoopLabelForContinue(label?.Name ?? AdhocConstants.NIL);
        RegisterLoopLabelForBreak(label?.Name ?? AdhocConstants.NIL);

        // Initialization
        if (forStatement.Init is not null)
        {
            switch (forStatement.Init.Type)
            {
                case Nodes.VariableDeclaration:
                    CompileVariableDeclaration(forStatement.Init.As<VariableDeclaration>()); break;
                case Nodes.AssignmentExpression:
                    CompileAssignmentExpression(forStatement.Init.As<AssignmentExpression>()); break;
                case Nodes.Identifier:
                    CompileIdentifier(forStatement.Init.As<Identifier>()); break;
                case Nodes.CallExpression:
                    CompileCall(forStatement.Init.As<CallExpression>());
                    InsertPop();
                    break;
                default:
                    ThrowCompilationError(forStatement.Init, CompilationMessages.Error_ForLoopInitializationType);
                    break;
            }
        }

        int startIndex = CurrentFrame.GetInstructionCount();

        // Condition
        InsJumpIfFalse? jumpIfFalse = null; // will only be inserted if the condition exists, else its essentially a while true loop
        if (forStatement.Test is not null)
        {
            CompileTestStatement(forStatement.Test);

            // Insert jump to the end of loop frame
            jumpIfFalse = new InsJumpIfFalse();
            AddInstruction(jumpIfFalse);
        }

        int jmpIndex;
        if (forStatement.Body.Type == Nodes.BlockStatement)
        {
            CompileStatement(forStatement.Body);
            jmpIndex = CurrentFrame.GetInstructionCount();
        }
        else
        {
            EnterScope();
            CompileStatement(forStatement.Body);
            jmpIndex = LeaveScope();
        }

        // Reached bottom, proceed to do update
        // But first, process continue if any
        ProcessContinues(jmpIndex);

        // Update Counter
        if (forStatement.Update is not null)
            CompileForUpdate(forStatement);

        // Insert jump to go back to the beginning of the loop
        InsJump startJump = new InsJump();
        startJump.JumpInstructionIndex = startIndex;
        AddInstruction(startJump);

        // Done with it.
        int scopeExitInstIndex = LeaveScope();

        // Update jump that exits the loop if it exists
        if (jumpIfFalse is not null)
            jumpIfFalse.JumpIndex = scopeExitInstIndex;

        ProcessBreaks(scopeExitInstIndex);
    }

    private void CompileForUpdate(ForStatement forStatement)
    {
        ArgumentNullException.ThrowIfNull(forStatement.Update, "forStatement.Update");

        if (forStatement.Update.Type == Nodes.UpdateExpression)
        {
            CompileUnaryExpression(forStatement.Update.As<UpdateExpression>(), popResult: true);
        }
        else if (forStatement.Update.Type == Nodes.CallExpression)
        {
            CompileCall(forStatement.Update.As<CallExpression>(), popReturnValue: true);
        }
        else if (forStatement.Update.Type == Nodes.AssignmentExpression)
        {
            CompileAssignmentExpression(forStatement.Update.As<AssignmentExpression>(), popResult: true);
        }
        else if (forStatement.Update.Type == Nodes.SequenceExpression)
        {
            CompileSequenceExpressionAssignmentsOrCall(forStatement.Update.As<SequenceExpression>());
        }
        else
            ThrowCompilationError(forStatement.Update, CompilationMessages.Error_StatementExpressionOnly);
    }

    /// <summary>
    /// Compiles 'a = b, c = d', assignments or calls only
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="sequenceExpression"></param>
    private void CompileSequenceExpressionAssignmentsOrCall(SequenceExpression sequenceExpression)
    {
        foreach (var exp in sequenceExpression.Expressions)
        {
            if (exp.Type == Nodes.AssignmentExpression)
            {
                CompileAssignmentExpression(exp.As<AssignmentExpression>(), popResult: true);
            }
            else if (exp.Type == Nodes.UpdateExpression)
            {
                CompileUnaryExpression(exp.As<UpdateExpression>(), popResult: true);
            }
            else if (exp.Type == Nodes.CallExpression)
            {
                CompileCall(exp.As<CallExpression>(), popReturnValue: true);
            }
            else
                ThrowCompilationError(exp, CompilationMessages.Error_StatementExpressionOnly);
        }
    }

    // GT7 1.00: 30F0E00 (mCompiler::CompileWhile)
    public void CompileWhile(WhileStatement whileStatement, Identifier? label = null)
    {
        EnterScope();

        RegisterLoopLabelForContinue(label?.Name ?? AdhocConstants.NIL);
        RegisterLoopLabelForBreak(label?.Name ?? AdhocConstants.NIL);

        int loopStartInsIndex = CurrentFrame.GetInstructionCount();

        if (whileStatement.Test is not null)
            CompileTestStatement(whileStatement.Test);

        InsJumpIfFalse jumpIfFalse = new InsJumpIfFalse(); // End loop jumper
        AddInstruction(jumpIfFalse);

        int jumpIdx;
        if (whileStatement.Body.Type == Nodes.BlockStatement)
        {
            CompileStatement(whileStatement.Body);
            jumpIdx = CurrentFrame.GetInstructionCount();
        }
        else
        {
            EnterScope();
            CompileStatement(whileStatement.Body);
            jumpIdx = LeaveScope();
        }

        ProcessContinues(jumpIdx);

        // Insert jump to go back to the beginning of the loop
        InsJump startJump = new InsJump();
        startJump.JumpInstructionIndex = loopStartInsIndex;
        AddInstruction(startJump);

        int loopExitInsIndex = LeaveScope();
        jumpIfFalse.JumpIndex = loopExitInsIndex;

        ProcessBreaks(loopExitInsIndex);
    }

    // GT7 1.00: 30F08E0 (mCompiler::CompileDoWhile)
    public void CompileDoWhile(DoWhileStatement doWhileStatement, Identifier? label = null)
    {
        EnterScope();

        int loopStartInsIndex = CurrentFrame.GetInstructionCount();

        RegisterLoopLabelForContinue(label?.Name ?? AdhocConstants.NIL);
        RegisterLoopLabelForBreak(label?.Name ?? AdhocConstants.NIL);

        int jumpIdx;
        if (doWhileStatement.Body.Type == Nodes.BlockStatement)
        {
            CompileStatement(doWhileStatement.Body);
            jumpIdx = CurrentFrame.GetInstructionCount();
        }
        else
        {
            EnterScope();
            CompileStatement(doWhileStatement.Body);
            jumpIdx = LeaveScope();
        }

        ProcessContinues(jumpIdx);

        CompileTestStatement(doWhileStatement.Test);

        InsJumpIfFalse jumpIfFalse = new InsJumpIfFalse(); // End loop jumper
        AddInstruction(jumpIfFalse);

        InsJump startJmp = new InsJump();
        startJmp.JumpInstructionIndex = loopStartInsIndex;
        AddInstruction(startJmp);

        int loopEndInsIndex = LeaveScope();
        jumpIfFalse.JumpIndex = loopEndInsIndex;

        ProcessBreaks(loopEndInsIndex);
    }

    // GT7 1.00: 30EF440 (mCompiler::CompileForeach)
    public void CompileForeach(ForeachStatement foreachStatement, Identifier? label = null)
    {
        if (!CurrentFrame.Version.HasForeachSupport())
            ThrowCompilationError(foreachStatement, CompilationMessages.Error_ForeachUnsupported);

        EnterScope();

        RegisterLoopLabelForContinue(label?.Name ?? AdhocConstants.NIL);
        RegisterLoopLabelForBreak(label?.Name ?? AdhocConstants.NIL);

        // Define a temporary value to hold the iterator
        AdhocSymbol tempName = SymbolMap.RegisterSymbol($"in#{SymbolMap.TempVariableCounter++}");
        DefineVariableInCurrentScope(tempName, AdhocVariableType.LocalVariable, foreachStatement.Left.Location);
        CompileExpression(foreachStatement.Right);

        // Access object iterator
        InsertAttributeEval(SymbolMap.RegisterSymbol("iterator"), foreachStatement.Left.Location);

        // Push it
        InsertLocalVariablePush(tempName.Name, foreachStatement.Left.Location);
        InsertAssignPop();

        int testInsIndex = CurrentFrame.GetInstructionCount();

        // Evaluate for test
        InsertVariableEval(tempName, foreachStatement.Left.Location);
        InsertAttributeEval(SymbolMap.RegisterSymbol("fetch_next"), foreachStatement.Right.Location);

        InsJumpIfFalse exitJump = new InsJumpIfFalse(); // End loop jumper
        AddInstruction(exitJump);

        
       // Entering body, but we need to get the iterator's value into our declared variable, equivalent to *iterator
       var valueEval = new InsVariableEvaluation();
       valueEval.VariableSymbols.Add(tempName);
       AddInstruction(valueEval, foreachStatement.Left.Location);
       AddInstruction(new InsEval());

        if (foreachStatement.Left.Type == Nodes.VariableDeclaration)
        {
            // foreach (var value in <right>)
            CompileVariableDeclaration(foreachStatement.Left.As<VariableDeclaration>(), pushWhenNoInit: true); // We're unboxing, gotta push anyway
        }
        else if (foreachStatement.Left.Type == Nodes.ListAssignmentExpression)
        {
            // foreach (|var ..| in <right>)
            ListAssignementExpression list = foreachStatement.Left.As<ListAssignementExpression>();
            CompileListAsssignmentExpression(list);
        }
        else if (foreachStatement.Left is Expression)
        {
            // foreach (<expression> in <right>)
            CompileVariableAssignment(foreachStatement.Left.As<Expression>());
        }
        else
        {
            ThrowCompilationError(foreachStatement.Left, CompilationMessages.Error_ForeachDeclarationNotDeclarationOrExpression);
        }
        

        int jumpIdx;
        if (foreachStatement.Body.Type == Nodes.BlockStatement)
        {
            CompileStatement(foreachStatement.Body);
            jumpIdx = CurrentFrame.GetInstructionCount();
        }
        else
        {
            EnterScope();
            CompileStatement(foreachStatement.Body);
            jumpIdx = LeaveScope();
        }

        ProcessContinues(jumpIdx);

        // Add the jump back to the test
        InsJump beginJump = new InsJump();
        beginJump.JumpInstructionIndex = testInsIndex;
        AddInstruction(beginJump);

        int loopEndJumpIdx = LeaveScope();
        exitJump.JumpIndex = loopEndJumpIdx;

        ProcessBreaks(loopEndJumpIdx);
    }

    // GT7 1.00: 30F19B0 (mCompiler::CompileSwitch)
    public void CompileSwitch(SwitchStatement switchStatement)
    {
        EnterScope();
        RegisterLoopLabelForBreak(AdhocConstants.NIL);

        // Create a label for the temporary switch variable
        AdhocSymbol varName = SymbolMap.RegisterSymbol($"case#{SymbolMap.TempVariableCounter++}");
        DefineVariableInCurrentScope(varName, AdhocVariableType.LocalVariable, switchStatement.Discriminant.Location);

        if (Version.ExpressionBeforeEvalOrPush())
        {
            CompileExpression(switchStatement.Discriminant);
            InsertLocalVariablePush(varName.Name, switchStatement.Location);
            InsertAssignPop();
        }
        else
        {
            InsertLocalVariablePush(varName.Name, switchStatement.Location);
            CompileExpression(switchStatement.Discriminant);
            InsertAssignPop();
        }

        Dictionary<SwitchCase, InsJumpIfTrue> caseBodyJumps = [];
        bool hasSpecifiedDefault = false;

        // Write switch table jumps
        for (int i = 0; i < switchStatement.Cases.Count; i++)
        {
            SwitchCase swCase = switchStatement.Cases[i];
            if (swCase.Test is not null) // Actual case
            {
                // Get temp variable
                InsertVariableEval(varName, swCase.Test.Location);

                // Write what we are comparing to 
                CompileExpression(swCase.Test);

                // Equal check
                InsBinaryOperator eqOp = new InsBinaryOperator(SymbolMap.RegisterSymbol(AdhocConstants.OPERATOR_EQUAL, convertToOperand: CurrentFrame.Version.ShouldUseInternalOperatorNames()));
                AddInstruction(eqOp, swCase.Location);

                // Write the jump
                InsJumpIfTrue jit = new InsJumpIfTrue();
                caseBodyJumps.Add(swCase, jit); // To write the instruction index later on
                AddInstruction(jit);
            }
            else // Default
            {
                if (hasSpecifiedDefault)
                    ThrowCompilationError(swCase, CompilationMessages.Error_SwitchAlreadyHasDefault);

                hasSpecifiedDefault = true;
            }
        }

        // Default is always at the end of all the tests regardless of where the default statement is.
        InsJump defaultJump = new InsJump();
        AddInstruction(defaultJump);

        // Write bodies
        for (int i = 0; i < switchStatement.Cases.Count; i++)
        {
            SwitchCase swCase = switchStatement.Cases[i];

            // Update body jump location
            if (swCase.Test is not null)
                caseBodyJumps[swCase].JumpIndex = CurrentFrame.GetInstructionCount();
            else
                defaultJump.JumpInstructionIndex = CurrentFrame.GetInstructionCount();

            // Not counting as scopes
            foreach (var statement in swCase.Consequent)
                CompileStatement(statement);
        }

        // Leave switch frame.
        int switchEndInsIndex = LeaveScope();

        // Update non explicit default case to jump to end
        if (!hasSpecifiedDefault)
            defaultJump.JumpInstructionIndex = switchEndInsIndex;

        // Update break case jumps
        ProcessBreaks(switchEndInsIndex);
    }

    /// <summary>
    /// Compiles a function declaration.
    /// </summary>
    /// <param name="funcDecl"></param>
    public void CompileFunctionDeclaration(FunctionDeclaration funcDecl)
    {
        ArgumentException.ThrowIfNullOrEmpty(funcDecl.Id?.Name, "funcDecl.Id?.Name");

        AdhocSymbol nameSymbol = SymbolMap.RegisterSymbol(funcDecl.Id.Name);
        DefineAttributeForCurrentModule(funcDecl.Id.Name, AdhocVariableType.Function, funcDecl.Id.Location);
        DefineVariableInCurrentScope(SymbolMap.RegisterSymbol(funcDecl.Id.Name), AdhocVariableType.Function, funcDecl.Id.Location);
        
        var functionFrame = CompileSubroutine(funcDecl, funcDecl.Body, funcDecl.Id, funcDecl.Params);

        var functionDefine = new InsFunctionDefine(functionFrame);
        functionDefine.Name = nameSymbol;
        functionDefine.CodeFrame = functionFrame;
        AddInstruction(functionDefine, funcDecl.Location);
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
    // GT7 1.00: 30E1D40 (mCompiler::CompileSubroutine)
    public AdhocCodeFrame CompileSubroutine(Node parentNode, Node body, Identifier? id, NodeList<Expression> subParams)
    {
        Logger.Debug($"L{parentNode.Location.Start.Line} - Compiling subroutine '{id?.Name ?? "<no name>"}'");

        // Default values
        foreach (Expression param in subParams)
            CompileSubroutineParameterDefaultValue(parentNode, param);

        var oldFrame = CurrentFrame;
        EnterCodeFrame();
        CurrentFrame.SetSourcePath(oldFrame.SourceFilePath);


        // Declare the arguments into our new frame
        foreach (var param in subParams)
        {
            if (param.Type == Nodes.Identifier)
            {
                var paramIdent = param.As<Identifier>();
                AdhocSymbol paramSymb = SymbolMap.RegisterSymbol(paramIdent.Name!);
                DefineVariableInCurrentScope(paramSymb, AdhocVariableType.LocalVariable, param.Location);
                CurrentFrame.FunctionParameters.Add(paramSymb);
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
                    CurrentFrame.FunctionParameters.Add(paramSymb);
                    DefineVariableInCurrentScope(paramSymb, AdhocVariableType.LocalVariable, assignmentExpression.Left.Location);
                }
                else if (param.Type == Nodes.AssignmentPattern)
                {
                    var pattern = param.As<AssignmentPattern>();

                    AdhocSymbol paramSymb = SymbolMap.RegisterSymbol(pattern.Left.As<Identifier>().Name!);
                    CurrentFrame.FunctionParameters.Add(paramSymb);
                    DefineVariableInCurrentScope(paramSymb, AdhocVariableType.LocalVariable, pattern.Left.Location);
                }
                else if (param.Type == Nodes.RestElement) // Rest element function(args...)
                {
                    if (CurrentFrame.HasRestElement)
                        ThrowCompilationError(parentNode, "Subroutine already has a rest parameter");

                    CurrentFrame.HasRestElement = true;

                    Identifier paramIdent = param.As<RestElement>().Argument.As<Identifier>();
                    AdhocSymbol paramSymb = SymbolMap.RegisterSymbol(paramIdent.Name!);
                    CurrentFrame.FunctionParameters.Add(paramSymb);
                    DefineVariableInCurrentScope(paramSymb, AdhocVariableType.LocalVariable, paramIdent.Location);
                }
                else
                    throw new NotSupportedException();
            }
            
        }

        // V10 and before allocate a variable after all arguments in functions.
        if (CurrentFrame.Version.HasReservedLocalInFrame())
            DefineVariableInCurrentScope(SymbolMap.RegisterSymbol(AdhocConstants.SELF), AdhocVariableType.LocalVariable);

        // In older versions the subroutines don't count towards the local storage (until referenced)
        // Just keep track of it instead
        // TODO!

        if (body.Type == Nodes.BlockStatement)
            CompileStatementList(body.As<BlockStatement>());
        else if (body.Type == Nodes.CallExpression)
            CompileExpression(body.As<CallExpression>());
        else
            ThrowCompilationError(body, "Expected subroutine body to be frame statement.");

        InsertFrameExitIfNeeded(body);
        var subroutineFrame = LeaveCodeFrame();

        foreach (var (StackIndex, Symbol) in subroutineFrame.CapturedCallbackVariables)
        {
            AddInstruction(new InsVariableEvaluation() { VariableSymbols = [Symbol] });
        }

        // TODO
        //Logger.Debug($"Subroutine '{id.Name}' compiled ({subroutineFrame.Instructions.Count} ins, " +
        //    $"Stack Size: {subroutineFrame.Stack.GetStackSize()}, Variable Storage Size: {subroutineFrame.MaxStaticIndex})");

        return subroutineFrame;
    }

    /// <summary>
    /// Compiles a subroutine parameter and it's default values if provided.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="parentNode"></param>
    /// <param name="subroutine"></param>
    /// <param name="param"></param>
    private void CompileSubroutineParameterDefaultValue(Node parentNode, Expression param)
    {
        // Parameter with no default value
        if (param.Type == Nodes.Identifier)
        {
            var paramIdent = param.As<Identifier>();

            if (CurrentFrame.Version.HasSupportForFunctionParametersDefaultValues())
            {
                // Subroutine param does not have a default parameter, push nil into current frame
                AddInstruction(new InsNilConst(), paramIdent.Location);
            }
        }
        else if (param.Type == Nodes.ListAssignmentExpression)
        {
            // Adhoc allows deconstructing an array directly into variables into a function. i.e:
            /* function sum({a, b})
             * {
             *    return a + b;
             * }
             * sum([1, 2]);
             */
            var listExpr = param.As<ListAssignementExpression>();

            // This isn't ever used by any of the scripts. What the compiler does is generate a temporary 'arg#{number}' variable in the
            // subroutine body itself.

            string tempArgName = $"arg#{SymbolMap.TempVariableCounter++}";
            AdhocSymbol paramSymb = SymbolMap.RegisterSymbol(tempArgName);
            DefineVariableInCurrentScope(paramSymb, AdhocVariableType.LocalVariable, listExpr.Location);

            // We create a new list with identifiers remapped as variable declarations.
            // Any other expression type is not supported, so this doubles as an argument verifier.
            var listClone = CreateAndVerifyListAssignmentForFunctionParameter(listExpr);

            ListAssignementStatement assignmentExpr = new ListAssignementStatement(listClone, new Identifier(tempArgName));
            CompileListAssignmentStatement(assignmentExpr);
        }
        else
        {
            if (!CurrentFrame.Version.HasSupportForFunctionParametersDefaultValues())
                ThrowCompilationError(param, CompilationMessages.Error_DefaultParameterValuesUnsupported);

            if (param is AssignmentExpression assignmentExpression) // Parameter default value set to another variable or static value
            {
                if (assignmentExpression.Left.Type != Nodes.Identifier || assignmentExpression.Right.Type != Nodes.Literal)
                    ThrowCompilationError(parentNode, CompilationMessages.Error_InvalidParameterValueAssignment);

                // Push default value
                CompileLiteral(assignmentExpression.Right.As<Literal>());
            }
            else if (param.Type == Nodes.AssignmentPattern)
            {
                var pattern = param.As<AssignmentPattern>();

                if (pattern.Right.Type != Nodes.Literal &&
                    (pattern.Right.Type == Nodes.UnaryExpression && pattern.Right.As<UnaryExpression>().Argument.Type != Nodes.Literal) && // Stuff like -1
                    pattern.Right.Type != Nodes.Identifier &&
                    pattern.Right.Type != Nodes.MemberExpression &&
                    pattern.Right.Type != Nodes.ArrayExpression &&
                    pattern.Right.Type != Nodes.MapExpression)
                    ThrowCompilationError(parentNode, "Subroutine default parameter value must be an identifier to a literal or other identifier.");

                // Push default value
                CompileExpression(pattern.Right);
            }
            else if (param.Type == Nodes.RestElement) // Rest element function(args...)
            {
                Identifier paramIdent = param.As<RestElement>().Argument.As<Identifier>();
                AddInstruction(new InsNilConst(), paramIdent.Location);
            }
            else
                ThrowCompilationError(parentNode, "Subroutine definition parameters must all be identifier or assignment to a literal.");
        }
    }

    /// <summary>
    /// Creates a list assignment expression for subroutine argument declaration.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    private ListAssignementExpression CreateAndVerifyListAssignmentForFunctionParameter(ListAssignementExpression list)
    {
        List<Node> nodes = [];
        foreach (Node elem in list.Elements)
        {
            if (elem.Type == Nodes.Identifier)
            {
                nodes.Add(new VariableDeclarator(elem.As<Identifier>(), init: null));
            }
            else if (elem.Type == Nodes.ListAssignmentExpression)
            {
                ListAssignementExpression nestedList = CreateAndVerifyListAssignmentForFunctionParameter(elem.As<ListAssignementExpression>());
                nodes.Add(nestedList);
            }
            else
            {
                ThrowCompilationError(elem, CompilationMessages.Error_InvalidListAssignmentArgumentInSubroutine);
            }
        }

        return new ListAssignementExpression(NodeList.Create(nodes), list.HasRestElement);
    }

    /// <summary>
    /// Compiles: 'return <expression>;' .
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="retStatement"></param>
    public void CompileReturnStatement(ReturnStatement retStatement)
    {
        if (retStatement.Argument is not null) // Return has argument?
        {
            if (retStatement.Argument.Type == Nodes.AssignmentExpression)
            {
                var assignmentExpression = (AssignmentExpression)retStatement.Argument;
                if (assignmentExpression.Operator == AssignmentOperator.Assign)
                {
                    // return a = b;
                    CompileAssignmentExpression(retStatement.Argument.As<AssignmentExpression>(), popResult: false);
                }
                else
                {
                    // return a += b;
                    CompileExpression(assignmentExpression);
                    CompileExpression(assignmentExpression.Left); // If we are returning an assignment i.e return <variable or path> += "hi", we need to eval str again
                }
            }
            else
            {
                // return <non assignment expr>
                CompileExpression(retStatement.Argument);
            }

            // Initial return indicates a return value in older versions
            if (CurrentFrame.Version.ShouldPopOnReturnStatementWithValue())
                InsertPop();
        }
        else
        {
            if (CurrentFrame.Version.ShouldReturnVoidForEmptyFunctionReturn())
                InsertVoid(); // Void const is returned
        }

        InsertSetState(AdhocRunState.RETURN);
    }

    /// <summary>
    /// Compiles "var a = 0, ...;"
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="varDeclaration"></param>
    /// <param name="pushWhenNoInit"></param>
    public void CompileVariableDeclaration(VariableDeclaration varDeclaration, bool pushWhenNoInit = false)
    {
        foreach (VariableDeclarator declarator in varDeclaration.Declarations)
        {
            Expression? initValue = declarator.Init;
            Expression id = declarator.Id;

            if (id is null)
                ThrowCompilationError(varDeclaration, CompilationMessages.Error_VariableDeclarationIsNull);

            // Declare the identifier before anything
            Identifier? idIdentifier = id as Identifier;
            if (idIdentifier is null)
                ThrowCompilationError(varDeclaration, "Variable declaration for id is not an identifier.");
            
            if (idIdentifier.Name == "nil")
                ThrowCompilationError(varDeclaration, CompilationMessages.Error_NilNotValidVarialbleName);

            AdhocSymbol varSymb = SymbolMap.RegisterSymbol(idIdentifier.Name!);
            DefineVariableInCurrentScope(varSymb, AdhocVariableType.LocalVariable, idIdentifier.Location);
            
            // Then, we actually compile what they are.
            // In later versions, we first compile the RHS
            if (CurrentFrame.Version.ExpressionBeforeEvalOrPush())
            {
                if (initValue is not null)
                {
                    if (initValue.Type == Nodes.UpdateExpression)
                    {
                        CompileUnaryExpression(initValue.As<UpdateExpression>(), popResult: false); // var a = ++b; - Do not discard b
                    }
                    else if (IsUnaryReferenceOfExpression(initValue))
                    {
                        CompileUnaryExpression(initValue.As<UnaryExpression>(), popResult: false, asReference: true); // var a = &b;
                    }
                    else if (initValue.Type == Nodes.AssignmentExpression)
                    {
                        CompileAssignmentExpression(initValue.As<AssignmentExpression>(), popResult: false); // var a = b = c; - Do not discard b
                    }
                    else
                    {
                        CompileExpression(initValue);
                    }
                }

                // Set variable value if any
                if (initValue is not null || pushWhenNoInit)
                {
                    // Variable is being defined with a value.
                    InsertLocalVariablePush(idIdentifier.Name!, idIdentifier.Location);

                    // Perform assignment
                    InsertAssignPop();
                }
                
            }
            else
            {
                // Set variable value if any
                if (initValue is not null || pushWhenNoInit)
                {
                    InsertLocalVariablePush(varSymb.Name, idIdentifier.Location);
                }

                // THEN Compile RHS
                if (initValue is not null)
                {
                    if (initValue.Type == Nodes.UpdateExpression)
                    {
                        CompileUnaryExpression(initValue.As<UpdateExpression>(), popResult: false); // var a = ++b; - Do not discard b
                    }
                    else if (IsUnaryReferenceOfExpression(initValue))
                    {
                        CompileUnaryExpression(initValue.As<UnaryExpression>(), popResult: false, asReference: true); // var a = &b;
                    }
                    else if (initValue.Type == Nodes.AssignmentExpression)
                    {
                        CompileAssignmentExpression(initValue.As<AssignmentExpression>(), popResult: false); // var a = b = c; - Do not discard b
                    }
                    else
                    {
                        CompileExpression(initValue);
                    }


                    InsertAssignPop();
                }
            }
        }
    }

    /// <summary>
    /// Compiles trivial "|a, b, c.d, e::f| = g" statement
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="listAssignment"></param>
    /// <param name="pushWhenNoInit"></param>
    public void CompileListAssignmentStatement(ListAssignementStatement listAssignment)
    {
        if (CurrentFrame.Version.ExpressionBeforeEvalOrPush()) // Must be before in late versions
            CompileExpression(listAssignment.Right);

        var before = !CurrentFrame.Version.ExpressionBeforeEvalOrPush() ? listAssignment.Right : null;
        CompileListAsssignmentExpression(listAssignment.Left, init: before);
    }

    /// <summary>
    /// Compiles trivial "|a, b, c.d, e::f| = g" statement with specified init, mainly due to foreach having a different init
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="list"></param>
    /// <param name="init"></param>
    private void CompileListAsssignmentExpression(ListAssignementExpression list, Expression? init = null, bool popResult = true)
    {
        if (list.HasRestElement && !CurrentFrame.Version.SupportsRestElement())
            ThrowCompilationError(list, CompilationMessages.Error_ListAssignementRestElementUnsupported);

        Dictionary<ListAssignementExpression, AdhocSymbol> nestedLists = [];
        foreach (var elem in list.Elements)
        {
            if (elem.Type == Nodes.Identifier)
            {
                Identifier variableIdentifier = elem.As<Identifier>();
                InsertLocalVariablePush(variableIdentifier.Name!, variableIdentifier.Location);
            }
            else if (elem.Type == Nodes.VariableDeclarator)
            {
                VariableDeclarator variableDeclarator = elem.As<VariableDeclarator>();
                Identifier variableIdentifier = variableDeclarator.Id.As<Identifier>();
                DefineVariableInCurrentScope(SymbolMap.RegisterSymbol(variableIdentifier.Name!), AdhocVariableType.LocalVariable, variableIdentifier.Location);

                InsertLocalVariablePush(variableIdentifier.Name!, variableIdentifier.Location);
            }
            else if (elem is AttributeMemberExpression)
            {
                CompileAttributeMemberAssignmentPush(elem.As<AttributeMemberExpression>());
            }
            else if (elem is StaticMemberExpression)
            {
                CompileStaticMemberExpressionPush(elem.As<StaticMemberExpression>());
            }
            else if (elem is ListAssignementExpression nestedList)
            {
                string tmpTaskVariable = $"tmp#{SymbolMap.TempVariableCounter++}";
                AdhocSymbol tempVariable = InsertLocalVariablePush(tmpTaskVariable);

                // Keep track of these
                nestedLists.Add(nestedList, tempVariable);
            }
            else
                ThrowCompilationError(elem, "Expected list assignment element to be an identifier, variable declaration, attribute path, static path, or nested list assignment.");
        }

        // FIXME: Logic can probably be improved
        if (!CurrentFrame.Version.ExpressionBeforeEvalOrPush() && init is not null)
        {
            if (init.Type == Nodes.AssignmentExpression)
            {
                InsertListAssign(list.Elements.Count, list.HasRestElement, list.Location);
                CompileExpression(init);
            }
            else
            {
                CompileExpression(init);
                InsertListAssign(list.Elements.Count, list.HasRestElement, list.Location);
            }
        }
        else
        {
            InsertListAssign(list.Elements.Count, list.HasRestElement, list.Location);
        }

        if (popResult)
            InsertPop();

        foreach (KeyValuePair<ListAssignementExpression, AdhocSymbol> nestedListAndTmpVarPair in nestedLists)
        {
            ListAssignementExpression nestedList = nestedListAndTmpVarPair.Key;
            AdhocSymbol tempListVariable = nestedListAndTmpVarPair.Value;

            if (CurrentFrame.Version.ExpressionBeforeEvalOrPush())
                AddInstruction(new InsVariableEvaluation() { VariableSymbols = [tempListVariable] });

            CompileListAsssignmentExpression(nestedList, new Identifier(tempListVariable.Name), popResult: true); // In a nested list, we are not reusing the result. pop it.
        }
    }

    /// <summary>
    /// Compiles an import declaration. 'import main::*'
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="import"></param>
    public void CompileImport(ImportDeclaration import)
    {
        if (import.Alias is not null) // Alias is defined as a static
        {
            if (!CurrentFrame.Version.SupportsImportAlias())
                ThrowCompilationError(import, CompilationMessages.Error_ImportAliasNotSupported);

            if (import.Target.Name == AdhocConstants.OPERATOR_IMPORT_ALL) // Should be caught by parser, but worth having anyway
                ThrowCompilationError(import, CompilationMessages.Error_ImportWildcardWithAlias);
        }

        string modulePath = "";

        List<AdhocSymbol> path = [];
        AdhocSymbol target = SymbolMap.RegisterSymbol(import.Target.Name, convertToOperand: CurrentFrame.Version.ShouldUseInternalOperatorNames());
        AdhocSymbol? alias = import.Alias is not null ? SymbolMap.RegisterSymbol(import.Alias.Name!) : null;

        if (import.Specifiers.Count > 0)
        {
            for (int i = 0; i < import.Specifiers.Count; i++)
            {
                ImportDeclarationSpecifier specifier = import.Specifiers[i];
                AdhocSymbol part = SymbolMap.RegisterSymbol(specifier.Local.Name);
                path.Add(part);
                modulePath += specifier.Local.Name;

                if (i < import.Specifiers.Count - 1)
                    modulePath += AdhocConstants.OPERATOR_STATIC;

                if (i == import.Specifiers.Count - 1)
                    path.Add(SymbolMap.RegisterSymbol(modulePath));
            }
        }
        else
        {
            // Static
            path.Add(SymbolMap.RegisterSymbol(import.Target.Name));
            path.Add(SymbolMap.RegisterSymbol(import.Target.Name));
        }

        AddModuleVariablesFromImport(path, target, alias);
        DefineScopeVariablesFromImport(path, target, alias);

        InsImport importIns = new InsImport();
        importIns.ModulePath = path;
        importIns.ModuleValue = target;
        importIns.ImportAs = alias ?? SymbolMap.RegisterSymbol(AdhocConstants.NIL);
        AddInstruction(importIns, import.Location);
    }

    private void CompileExpression(Expression exp)
    {
        switch (exp.Type)
        {

            case Nodes.Identifier:
                CompileIdentifier(exp.As<Identifier>());
                break;
            case Nodes.StaticIdentifier:
                CompileStaticIdentifier(exp.As<StaticIdentifier>());
                break;
            case Nodes.FunctionExpression:
                CompileFunctionExpression(exp.As<FunctionExpression>());
                break;
            case Nodes.MethodExpression:
                CompileMethodExpression(exp.As<MethodExpression>());
                break;
            case Nodes.CallExpression:
                CompileCall(exp.As<CallExpression>());
                break;
            case Nodes.UnaryExpression:
            case Nodes.UpdateExpression:
                CompileUnaryExpression(exp.As<UnaryExpression>());
                break;
            case Nodes.BinaryExpression:
            case Nodes.LogicalExpression:
                CompileBinaryExpression(exp.As<BinaryExpression>());
                break;
            case Nodes.Literal:
                CompileLiteral(exp.As<Literal>());
                break;
            case Nodes.ArrayExpression:
                CompileArrayExpression(exp.As<ArrayExpression>());
                break;
            case Nodes.MapExpression:
                CompileMapExpression(exp.As<MapExpression>());
                break;
            case Nodes.MemberExpression when exp is ComputedMemberExpression:
                CompileComputedMemberExpression(exp.As<ComputedMemberExpression>());
                break;
            case Nodes.MemberExpression when exp is StaticMemberExpression:
                CompileStaticMemberExpression(exp.As<StaticMemberExpression>());
                break;
            case Nodes.MemberExpression when exp is AttributeMemberExpression:
                CompileAttributeMemberExpression(exp.As<AttributeMemberExpression>());
                break;
            case Nodes.MemberExpression when exp is ObjectSelectorMemberExpression:
                CompileObjectSelectorExpression(exp.As<ObjectSelectorMemberExpression>());
                break;
            case Nodes.AssignmentExpression:
                CompileAssignmentExpression(exp.As<AssignmentExpression>());
                break;
            case Nodes.ConditionalExpression:
                CompileConditionalExpression(exp.As<ConditionalExpression>());
                break;
            case Nodes.TemplateLiteral:
                CompileTemplateLiteral(exp.As<TemplateLiteral>());
                break;
            case Nodes.TaggedTemplateExpression:
                CompileTaggedTemplateExpression(exp.As<TaggedTemplateExpression>());
                break;
            case Nodes.ImportDeclaration:
                CompileImport(exp.As<ImportExpression>().Declaration);
                break;
            case Nodes.YieldExpression:
                CompileYield(exp.As<YieldExpression>());
                break;
            case Nodes.AwaitExpression:
                CompileAwait(exp.As<AwaitExpression>());
                break;
            case Nodes.SpreadElement:
                CompileSpreadElement(exp.As<SpreadElement>());
                break;
            case Nodes.SelfExpression:
                CompileSelfExpression(exp.As<SelfExpression>());
                break;
            case Nodes.ChainExpression:
                CompileChainExpression(exp.As<ChainExpression>());
                break;
            case Nodes.SelfFinalizerExpression:
                CompileSelfFinalizerExpression(exp.As<SelfFinalizerExpression>());
                break;
            default:
                ThrowCompilationError(exp, $"Expression {exp.Type} not supported");
                break;
        }
    }

    /// <summary>
    /// Compile 'delegate myDelegate'
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="chainExpression"></param>
    private void CompileDelegateDefinition(DelegateDeclaration delegateDefinition)
    {
        if (CurrentFrame.Version.IsMinimumVersionForDelegateSupport())
            ThrowCompilationError(delegateDefinition, CompilationMessages.Error_DelegatesUnsupported);

        var idSymb = SymbolMap.RegisterSymbol(delegateDefinition.Identifier.Name!);
        DefineAttributeForCurrentModule(idSymb.Name, AdhocVariableType.Delegate, delegateDefinition.Identifier.Location);
        DefineVariableInCurrentScope(idSymb, AdhocVariableType.Delegate, delegateDefinition.Identifier.Location);

        InsDelegateDefine ins = new InsDelegateDefine(idSymb);
        AddInstruction(ins, delegateDefinition.Location);

        if (CurrentFrame.Version.IsMinimumVersionForDelegateSupport())
            AddPostCompilationWarning(CompilationMessages.Warning_UsingDelegateCode);
    }

    /// <summary>
    /// Compile 'identifier?.attr or identifier?["attr"]'
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="chainExpression"></param>
    private void CompileChainExpression(ChainExpression chainExpression)
    {
        CompileExpression(chainExpression.Expression);
    }

    /// <summary>
    /// Compiles <self>
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="spreadElement"></param>
    private void CompileSelfExpression(SelfExpression selfExpression)
    {
        if (!CurrentFrame.Version.HasSelfSupport())
            ThrowCompilationError(selfExpression, CompilationMessages.Error_SelfUnsupported);

        AdhocSymbol symb = SymbolMap.RegisterSymbol(AdhocConstants.SELF);
        int idx = 0; // Always 0 when refering to self
        var varEval = new InsVariableEvaluation(idx);
        varEval.VariableSymbols.Add(symb); // Self is always considered as a local. Just one
        AddInstruction(varEval, selfExpression.Location);
    }

    /// <summary>
    /// Compiles <function>.(...args)
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="spreadElement"></param>
    private void CompileSpreadElement(SpreadElement spreadElement)
    {
        CompileExpression(spreadElement.Argument);
    }

    private void CompileYield(YieldExpression yield)
    {
        if (yield.Argument is not null)
        {
            CompileExpression(yield.Argument);
            if (yield.Argument is AssignmentExpression)
            {
                ThrowCompilationError(yield.Argument, $"Assignment expressions are not yet supported in yield statements.");
            }
        }
        else
        {
            AddInstruction(new InsVoidConst(), yield.Location);
        }

        InsertSetState(AdhocRunState.YIELD);
    }

    private void CompileAwait(AwaitExpression awaitExpr, bool isReturning = true)
    {
        AdhocSymbol taskSymbol = SymbolMap.RegisterSymbol($"task#{SymbolMap.TempVariableCounter++}");
        DefineVariableInCurrentScope(taskSymbol, AdhocVariableType.LocalVariable, awaitExpr.Location);

        // Awaiting bare call?
        if (awaitExpr.Argument is CallExpression call)
        {
            var awaitStart = new StaticMemberExpression(new Identifier(AdhocConstants.SYSTEM), new Identifier("AwaitTaskStart"), false);
            CompileExpression(awaitStart);
            bool isAwaitTask = IsNewTaskCall(call);

            if (!isAwaitTask)
            {
                // Wrap it into a subroutine (maybe move this to an util at the bottom) 
                var subroutine = new FunctionExpression(null,
                    new NodeList<Expression>(), // No parameters
                    new BlockStatement(NodeList.Create(new Statement[] { isReturning ? new ReturnStatement(call) : new ExpressionStatement(call) })),
                    generator: false,
                    strict: true,
                    async: false);
                subroutine.Location = new Location(call.Location.Start, call.Location.End, call.Location.Source);

                CompileFunctionExpression(subroutine);
            }
            else
            {
                if (call.Arguments.Count != 1)
                    ThrowCompilationError(call, "AwaitTask expects 1 argument");

                if (call.Arguments[0].Type != Nodes.FunctionExpression && call.Arguments[0].Type != Nodes.ArrowFunctionExpression)
                    ThrowCompilationError(call, "AwaitTask expects a function as argument");

                CompileExpression(call.Arguments[0]);
            }
        }
        else
        {
            var awaitStart = new StaticMemberExpression(new Identifier(AdhocConstants.SYSTEM), new Identifier("AwaitTaskStart"), false);
            CompileExpression(awaitStart);
            CompileExpression(awaitExpr.Argument);
        }


        // Get task - <task> = System::AwaitTaskStart(<func>);
        AddInstruction(new InsCall(1));
        AddInstruction(new InsVariablePush() { VariableSymbols = [taskSymbol] }, awaitExpr.Location);
        AddInstruction(InsAssignPop.Default);

        // Get result of task - <result> = System::AwaitTaskResult(<task>);
        var awaitResult = new StaticMemberExpression(new Identifier(AdhocConstants.SYSTEM), new Identifier("AwaitTaskResult"), false);
        CompileExpression(awaitResult);
        AddInstruction(new InsVariableEvaluation() { VariableSymbols = [taskSymbol] });
        AddInstruction(new InsCall(1));

        AddPostCompilationWarning(CompilationMessages.Warning_UsingAwait_Code);
    }

    private static bool IsNewTaskCall(CallExpression call)
    {
        if (call.Callee is StaticMemberExpression staticMemberExp)
        {
            if (staticMemberExp.Object is Identifier objIdentifier && staticMemberExp.Property is Identifier propIdent)
            {
                if (objIdentifier.Name == AdhocConstants.SYSTEM && propIdent.Name == "AwaitTaskStart")
                    return true;
            }
        }
        else if (call.Callee is Identifier ident)
        {
            if (ident.Name == "AwaitTask")
                return true;
        }

        return false;

    }


    private void CompileStaticDeclaration(StaticDeclaration staticDeclaration)
    {
        Identifier ident = staticDeclaration.Declaration.Id.As<Identifier>();
        string name = ident.Name!;

        // static definition with no value
        var idSymb = SymbolMap.RegisterSymbol(name);
        DefineAttributeForCurrentModule(name, AdhocVariableType.Static, ident.Location);
        DefineVariableInCurrentScope(idSymb, AdhocVariableType.Static, ident.Location);

        if (staticDeclaration.Declaration.Init is null)
        {
            InsStaticDefine staticDefine = new InsStaticDefine(idSymb);
            AddInstruction(staticDefine, staticDeclaration.Location);

            // Statics starting V7 until V10 are always set to a nil if not explicitly set to a value
            if (CurrentFrame.Version.ShouldInsertNilForStaticDefinition())
            {
                InsertLocalVariablePush(name, ident.Location);
                AddInstruction(new InsNilConst(), ident.Location);
                InsertAssignPop();
            }
        }
        else
        {
            if (staticDeclaration.Declaration.Id.Type != Nodes.Identifier)
                ThrowCompilationError(staticDeclaration.Declaration.Id, "Expected static declaration to be an identifier.");

            var declValue = staticDeclaration.Declaration.Init;

            InsStaticDefine staticDefine = new InsStaticDefine(idSymb);
            AddInstruction(staticDefine, staticDeclaration.Location);

            // Assigning to something new
            if (CurrentFrame.Version.ExpressionBeforeEvalOrPush())
            {
                CompileExpression(declValue);
                CompileVariableAssignment(staticDeclaration.Declaration.Id);
            }
            else
            {
                Identifier id = staticDeclaration.Declaration.Id.As<Identifier>();
                InsertLocalVariablePush(id.Name!, id.Location);
                CompileExpression(declValue);

                InsertAssignPop();
            }
        }
    }


    private void CompileAttributeDeclaration(AttributeDeclaration attrVariableDefinition)
    {
        if (attrVariableDefinition.VarExpression.Type == Nodes.Identifier)
        {
            Identifier ident = attrVariableDefinition.VarExpression.As<Identifier>();

            // attribute definition with no value

            // defaults to nil (when default values are supported)
            if (CurrentFrame.Version.HasSupportForAttributeDefinitionDefaultValues())
                AddInstruction(new InsNilConst(), ident.Location);

            var idSymb = SymbolMap.RegisterSymbol(ident.Name!);
            DefineAttributeForCurrentModule(idSymb.Name, AdhocVariableType.Attribute, ident.Location);
            DefineVariableInCurrentScope(idSymb, AdhocVariableType.Attribute, ident.Location);

            InsAttributeDefine staticDefine = new InsAttributeDefine(idSymb);
            AddInstruction(staticDefine, attrVariableDefinition.Location);
        }
        else
        {
            if (!CurrentFrame.Version.HasSupportForFunctionParametersDefaultValues())
                ThrowCompilationError(attrVariableDefinition.VarExpression, CompilationMessages.Error_DefaultAttributeValuesUnsupported);

            if (attrVariableDefinition.VarExpression is not AssignmentExpression)
                ThrowCompilationError(attrVariableDefinition, "Expected attribute keyword to be a variable assignment.");

            AssignmentExpression assignmentExpression = attrVariableDefinition.VarExpression.As<AssignmentExpression>();
            if (assignmentExpression.Left is not Identifier)
                ThrowCompilationError(assignmentExpression, "Expected attribute declaration to be an identifier.");

            Identifier identifier = assignmentExpression.Left.As<Identifier>();
            var idSymb = SymbolMap.RegisterSymbol(identifier.Name!);
            DefineAttributeForCurrentModule(idSymb.Name, AdhocVariableType.Attribute, identifier.Location);
            DefineVariableInCurrentScope(idSymb, AdhocVariableType.Attribute, identifier.Location);

            // Value if any
            CompileExpression(assignmentExpression.Right);

            // Declaring a class attribute, so we don't push anything
            InsAttributeDefine attrDefine = new InsAttributeDefine(idSymb);
            AddInstruction(attrDefine, identifier.Location);
        }
    }

    /// <summary>
    /// Compiles: [] or [<expr>,<expr>,...]
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="arrayExpression"></param>
    private void CompileArrayExpression(ArrayExpression arrayExpression)
    {
        if (CurrentFrame.Version.HasNewArrayConstSupport())
        {
            // Version 11 and above - array is defined
            AddInstruction(new InsArrayConst((uint)arrayExpression.Elements.Count), arrayExpression.Location);

            // Then all items are pushed to it, one by one
            foreach (var elem in arrayExpression.Elements)
            {
                if (elem is null)
                    ThrowCompilationError(arrayExpression, "Unsupported empty element in array declaration.");

                CompileExpression(elem);

                AddInstruction(InsArrayPush.Default);
            }
        }
        else
        {
            // Version 10 and below - items are all pushed into the stack at once
            foreach (var elem in arrayExpression.Elements)
            {
                if (elem is null)
                    ThrowCompilationError(arrayExpression, "Unsupported empty element in array declaration.");

                CompileExpression(elem);
            }

            // Then the array is defined
            AddInstruction(new InsArrayConstOld(arrayExpression.Elements.Count), arrayExpression.Location);
        }
    }

    /// <summary>
    /// Compiles: [:] or [k:v, k:v, ...]
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="mapExpression"></param>
    private void CompileMapExpression(MapExpression mapExpression)
    {
        if (!CurrentFrame.Version.HasMapSupport())
            ThrowCompilationError(mapExpression, CompilationMessages.Error_MapUnsupported);

        AddInstruction(new InsMapConst(), mapExpression.Location);

        foreach (var (key, value) in mapExpression.Elements)
        {
            CompileExpression(key);
            CompileExpression(value);
            AddInstruction(InsMapInsert.Default);
        }
    }

    /// <summary>
    /// Compiles expression statements
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="expStatement"></param>
    private void CompileExpressionStatement(ExpressionStatement expStatement)
    {
        if (expStatement.Expression.Type == Nodes.AwaitExpression)
            CompileAwait(expStatement.Expression.As<AwaitExpression>(), isReturning: false);
        else
            CompileExpression(expStatement.Expression);

        if (expStatement.Expression.Type != Nodes.AssignmentExpression
            && expStatement.Expression.Type != Nodes.StaticDeclaration
            && expStatement.Expression.Type != Nodes.AttributeDeclaration
            && expStatement.Expression.Type != Nodes.YieldExpression
            && expStatement.Expression.Type != Nodes.SelfFinalizerExpression)
            InsertPop();
    }

    private void CompileMethodDeclaration(MethodDeclaration methodDecl)
    {
        Identifier id = methodDecl.Id!.As<Identifier>();
        string name = id.Name!;

        var symbol = SymbolMap.RegisterSymbol(name);
        DefineAttributeForCurrentModule(name, AdhocVariableType.Method, id.Location);
        DefineVariableInCurrentScope(symbol, AdhocVariableType.Method, id.Location);
        AdhocCodeFrame methodFrame = CompileSubroutine(methodDecl, methodDecl.Body, id, methodDecl.Params);

        var methodDefine = new InsMethodDefine();
        methodDefine.CodeFrame = methodFrame;
        methodDefine.Name = symbol;
        AddInstruction(methodDefine, methodDecl.Location);
    }

    private void CompileFunctionExpression(FunctionExpression funcExp)
    {
        var functionConstFrame = CompileSubroutine(funcExp, funcExp.Body, funcExp.Id, funcExp.Params);

        var functionConst = new InsFunctionConst();
        functionConst.CodeFrame = functionConstFrame;
        AddInstruction(functionConst, funcExp.Location);
    }

    private void CompileMethodExpression(MethodExpression methodExpr)
    {
        var methodConstFrame = CompileSubroutine(methodExpr, methodExpr.Body, methodExpr.Id, methodExpr.Params);

        var methodConst = new InsMethodConst();
        methodConst.CodeFrame = methodConstFrame;
        AddInstruction(methodConst, methodExpr.Location);
    }

    // Combination of string literals/templates
    private void CompileTaggedTemplateExpression(TaggedTemplateExpression taggedTemplate)
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
                        AddInstruction(strConst, element.Location);

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
                                AddInstruction(strConst, n.Location);
                            }
                            else if (n is Expression exp)
                            {
                                CompileExpression(exp);
                            }
                            else
                                ThrowCompilationError(node, "Unexpected template element type");

                            elemCount++;
                        }
                    }
                }
                else
                    ThrowCompilationError(node, "Unsupported Tagged Template Node");
            }
        }

        InsStringPush strPush = new InsStringPush(elemCount);
        AddInstruction(strPush, taggedTemplate.Location);
    }

    /// <summary>
    /// Compiles a string format literal. i.e "hello %{name}!"
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="templateLiteral"></param>
    private void CompileTemplateLiteral(TemplateLiteral templateLiteral)
    {
        if (templateLiteral.Quasis.Count == 1 && templateLiteral.Expressions.Count == 0)
        {
            // Regular string const
            TemplateElement strElement = templateLiteral.Quasis[0];

            // On later versions, empty strings are always a string push with 0 args, which is a short hand for a static empty string
            // It also works on earlier versions, but that's just how they compiled it
            if (string.IsNullOrEmpty(strElement.Value.Cooked) && CurrentFrame.Version.ShouldUseStringPushForEmptyStrings())
            {
                InsStringPush strPush = new InsStringPush(0);
                AddInstruction(strPush, strElement.Location);
            }
            else
            {
                AdhocSymbol strSymb = SymbolMap.RegisterSymbol(strElement.Value.Cooked, convertToOperand: false, strElement.Value.HasHexEscape);
                InsStringConst strConst = new InsStringConst(strSymb);
                AddInstruction(strConst, strElement.Location);
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
                    AddInstruction(strConst, tElem.Location);
                }
                else if (node is Expression exp)
                {
                    CompileExpression(exp);
                }
                else
                    ThrowCompilationError(node, "Unexpected template element type");
            }

            // Link strings together
            if (literalNodes.Count > 0)
            {
                InsStringPush strPush = new InsStringPush(literalNodes.Count);
                AddInstruction(strPush, templateLiteral.Location);
            }
            else
            {
                if (CurrentFrame.Version.ShouldUseStringPushForEmptyStrings())
                {
                    InsStringPush strPush = new InsStringPush(0);
                    AddInstruction(strPush, templateLiteral.Location);
                }
                else
                {
                    AdhocSymbol valSymb = SymbolMap.RegisterSymbol("");
                    InsStringConst strConst = new InsStringConst(valSymb);
                    AddInstruction(strConst, templateLiteral.Location);
                }
            }
        }
    }

    // TODO: split this into seperate functions for each version
    private void CompileAssignmentExpression(AssignmentExpression assignExpression, bool popResult = true)
    {
        // Assigning to a variable or literal directly?
        if (assignExpression.Operator == AssignmentOperator.Assign)
        {
            if (CurrentFrame.Version.ExpressionBeforeEvalOrPush())
            {
                // a = b = c?
                if (assignExpression.Right.Type == Nodes.AssignmentExpression)
                {
                    // We are reusing the result (b in this case) - we do not pop it.
                    CompileAssignmentExpression(assignExpression.Right.As<AssignmentExpression>(), popResult: false);
                }
                else
                {
                    // Regular assignment
                    CompileExpression(assignExpression.Right);
                }

                // |a, b| = |c, d| = ...?
                if (assignExpression.Left.Type == Nodes.ListAssignmentExpression)
                {
                    CompileListAsssignmentExpression(assignExpression.Left.As<ListAssignementExpression>(), popResult: false);
                }
                else
                {
                    CompileVariableAssignment(assignExpression.Left, popResult);
                }
            }
            else // Target first in old versions
            {
                // Regular update of left-hand side
                // Left-hand side needs to be pushed first
                if (IsUnaryIndirection(assignExpression.Left)) // Assigning to a reference variable (*var = 1)
                {
                    // Trivially compile left reference
                    CompileUnaryExpression(assignExpression.Left.As<UnaryExpression>());
                }
                else if (assignExpression.Left.Type == Nodes.StaticIdentifier)
                {
                    StaticIdentifier staticIdentifier = assignExpression.Left.As<StaticIdentifier>();
                    CompileStaticIdentifierPush(staticIdentifier);
                }
                else if (assignExpression.Left.Type == Nodes.ListAssignmentExpression) // |a, b| = |c, d| = e;
                {
                    var listAssignExpr = assignExpression.Left.As<ListAssignementExpression>();
                    CompileListAsssignmentExpression(listAssignExpr, null, popResult: false);
                    return; // No actual assignment, covered by list assign.
                }
                else
                {
                    if (assignExpression.Left.Type == Nodes.Identifier)
                    {
                        Identifier id = assignExpression.Left.As<Identifier>();
                        InsertLocalVariablePush(id.Name!, id.Location);
                    }
                    else if (assignExpression.Left is AttributeMemberExpression attr)
                    {
                        CompileAttributeMemberAssignmentPush(attr);
                    }
                    else if (assignExpression.Left is ComputedMemberExpression compExpression)
                    {
                        CompileComputedMemberExpressionAssignmentPush(compExpression);
                    }
                    else if (assignExpression.Left is StaticMemberExpression staticMember)
                    {
                        CompileStaticMemberExpressionPush(staticMember);
                    }
                    else if (assignExpression.Left is ObjectSelectorMemberExpression objectSelector)
                    {
                        ThrowCompilationError(assignExpression, "Unimplemented object selector assignment expression");
                    }
                    else
                    {
                        ThrowCompilationError(assignExpression, "Unimplemented or Invalid");
                    }
                }

                if (IsUnaryReferenceOfExpression(assignExpression.Right))
                {
                    CompileUnaryExpression(assignExpression.Right.As<UnaryExpression>(), popResult: false, asReference: true); // a = &b;
                }
                else if (assignExpression.Right.Type == Nodes.AssignmentExpression)
                {
                    // We are reusing the result (b in this case) - we do not pop it.
                    CompileAssignmentExpression(assignExpression.Right.As<AssignmentExpression>(), popResult: false);
                }
                else
                {
                    CompileExpression(assignExpression.Right);
                }

                if (popResult)
                    InsertAssignPop();
                else
                    InsertAssign();
            }
        }
        else if (IsAdhocAssignWithOperandOperator(assignExpression.Operator)) // += -= /= etc..
        {
            // Assigning to a reference variable? (*a += b)
            if (IsUnaryIndirection(assignExpression.Left))
            {
                // No need to push, eval
                CompileUnaryExpression(assignExpression.Left.As<UnaryExpression>(), asReference: false, isIndirectionBinaryAssignment: true);
            }
            else
            {
                // Regular update of left-hand side
                // Left-hand side needs to be pushed first
                if (assignExpression.Left.Type == Nodes.Identifier)
                {
                    Identifier id = assignExpression.Left.As<Identifier>();
                    InsertLocalVariablePush(id.Name!, id.Location);
                }
                else if (assignExpression.Left is AttributeMemberExpression attr)
                {
                    CompileAttributeMemberAssignmentPush(attr);
                }
                else if (assignExpression.Left is ComputedMemberExpression compExpression)
                {
                    CompileComputedMemberExpressionAssignmentPush(compExpression);
                }
                else if (assignExpression.Left is ObjectSelectorMemberExpression objectSelector)
                {
                    ThrowCompilationError(assignExpression, "Unimplemented object selector assignment expression");
                }
                else
                {
                    ThrowCompilationError(assignExpression, "Unimplemented or Invalid");
                }
            }

            if (assignExpression.Right.Type == Nodes.AssignmentExpression) // a += b += c?
            {
                // Then do not immediately discard result for the next inline operation
                CompileAssignmentExpression(assignExpression.Right.As<AssignmentExpression>(), popResult: false);
            }
            else
                CompileExpression(assignExpression.Right);

            InsertBinaryAssignOperator(assignExpression, assignExpression.Operator, assignExpression.Location);
            if (popResult)
                InsertPop();
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
    public void CompileVariableAssignment(Expression expression, bool popValue = true)
    {
        if (expression.Type == Nodes.Identifier) // hello = world
        {
            Identifier id = expression.As<Identifier>();
            InsertLocalVariablePush(id.Name!, id.Location);
        }
        else if (expression.Type == Nodes.StaticIdentifier)
        {
            StaticIdentifier staticIdentifier = expression.As<StaticIdentifier>();
            CompileStaticIdentifierPush(staticIdentifier);
        }
        else if (expression.Type == Nodes.MemberExpression)
        {
            if (expression is AttributeMemberExpression attrMember) // Pushing into an object i.e hello.world = "!"
            {
                CompileAttributeMemberAssignmentPush(attrMember);
            }
            else if (expression is ComputedMemberExpression compExpression) // hello[world] = "foo"
            {
                CompileComputedMemberExpressionAssignmentPush(compExpression);
            }
            else if (expression is ObjectSelectorMemberExpression objSelectExpression)
            {
                CompileObjectSelectorExpressionAssignmentPush(objSelectExpression);
            }
            else if (expression is StaticMemberExpression staticMembExpression) // main::hello = hi
            {
                CompileStaticMemberExpressionPush(staticMembExpression);
            }
            else
                ThrowCompilationError(expression, $"Unimplemented member expression assignment type: '{expression.Type}'");
        }
        else if (expression is UnaryExpression unaryExp && unaryExp.Operator == UnaryOperator.Indirection) // (*/&)hello = world
        {
            if (unaryExp.Argument.Type == Nodes.Identifier ||
                unaryExp.Argument is AttributeMemberExpression ||
                unaryExp.Argument is StaticMemberExpression ||
                unaryExp.Argument is ComputedMemberExpression)
                CompileExpression(unaryExp.Argument);
            else
                ThrowCompilationError(expression, "Unexpected assignment to unary argument. Only Indirection (*) is allowed.");
        }
        else
        {
            ThrowCompilationError(expression, $"Unimplemented or invalid variable assignment type: '{expression.Type}'");
        }

        if (popValue)
            InsertAssignPop();
        else
            InsertAssign();
    }

    // TODO: Maybe remove this. StaticIdentifier doesn't feel right as an expression, it should be unary probably
    private void CompileStaticIdentifierPush(StaticIdentifier staticIdentifier)
    {
        AdhocSymbol varSymb = SymbolMap.RegisterSymbol(staticIdentifier.Id.Name!);

        var varPush = new InsVariablePush();
        varPush.VariableSymbols.Add(varSymb);
        varPush.VariableSymbols.Add(varSymb);
        AddInstruction(varPush, staticIdentifier.Location);
    }

    /// <summary>
    /// Or ternary for short - test ? consequent : alternate;
    /// </summary>
    /// <param name="condExpression"></param>
    private void CompileConditionalExpression(ConditionalExpression condExpression)
    {
        // Compile condition
        CompileExpression(condExpression.Test);

        InsJumpIfFalse alternateJump = new InsJumpIfFalse();
        AddInstruction(alternateJump);

        if (IsUnaryReferenceOfExpression(condExpression.Consequent))
            CompileUnaryExpression(condExpression.Consequent.As<UnaryExpression>(), popResult: false, asReference: true);
        else
            CompileExpression(condExpression.Consequent);

        // This jump will skip the alternate statement if the consequent path is taken
        InsJump altSkipJump = new InsJump();
        AddInstruction(altSkipJump);

        if (CurrentFrame.Version.ShouldPopInTernary())
            InsertPop();

        // Update alternate jump index now that we've compiled the consequent
        alternateJump.JumpIndex = CurrentFrame.GetInstructionCount();

        // Proceed to compile alternate/no match statement
        if (IsUnaryReferenceOfExpression(condExpression.Alternate))
            CompileUnaryExpression(condExpression.Alternate.As<UnaryExpression>(), popResult: false, asReference: true);
        else
            CompileExpression(condExpression.Alternate);

        // Done completely, update alt skip jump to end of condition instruction frame
        altSkipJump.JumpInstructionIndex = CurrentFrame.GetInstructionCount();
    }

    /// <summary>
    /// Compiles an identifier. var test = otherVariable;
    /// </summary>
    /// <param name="identifier"></param>
    private void CompileIdentifier(Identifier identifier, bool attribute = false)
    {
        if (attribute)
            InsertAttributeEval(SymbolMap.RegisterSymbol(identifier.Name!), identifier.Location);
        else
            InsertVariableEval(SymbolMap.RegisterSymbol(identifier.Name!), identifier.Location);
    }

    /// <summary>
    /// Compiles an identifier. var test = otherVariable;
    /// </summary>
    /// <param name="identifier"></param>
    private void CompileStaticIdentifier(StaticIdentifier identifier)
    {
        InsertVariableEval(SymbolMap.RegisterSymbol(identifier.Id.Name!), identifier.Id.Location);
    }


    /// <summary>
    /// Compiles array or map access or anything that can be indexed
    /// </summary>
    private void CompileComputedMemberExpression(ComputedMemberExpression computedMember)
    {
        CompileExpression(computedMember.Object);

        if (computedMember.Optional)
        {
            if (!CurrentFrame.Version.IsMinimumVersionForOptionalSupport())
                ThrowCompilationError(computedMember, CompilationMessages.Error_OptionalComputedMemberUnsupported);
            else
                AddPostCompilationWarning(CompilationMessages.Warning_UsingOptional_Code);

            AddInstruction(new InsLogicalOptional(), computedMember.Location);
        }

        CompileExpression(computedMember.Property);

        if (CurrentFrame.Version.HasElementEvalSupport())
            AddInstruction(InsElementEval.Default);
        else
        {
            // Below, including 11 uses direct symbols
            var indexerIns = new InsBinaryOperator(SymbolMap.RegisterSymbol(AdhocConstants.OPERATOR_SUBSCRIPT, convertToOperand: CurrentFrame.Version.ShouldUseInternalOperatorNames()));

            AddInstruction(indexerIns, computedMember.Location);
            AddInstruction(new InsEval());
        }
    }

    /// <summary>
    /// Compiles array or map element assignment (ELEMENT_PUSH)
    /// </summary>
    private void CompileComputedMemberExpressionAssignmentPush(ComputedMemberExpression computedMember)
    {
        CompileExpression(computedMember.Object);
        CompileExpression(computedMember.Property);

        if (CurrentFrame.Version.HasElementPushSupport())
            AddInstruction(InsElementPush.Default);
        else
        {
            // Below, including 11 uses direct symbols
            var indexerIns = new InsBinaryOperator(SymbolMap.RegisterSymbol(AdhocConstants.OPERATOR_SUBSCRIPT, convertToOperand: CurrentFrame.Version.ShouldUseInternalOperatorNames()));

            AddInstruction(indexerIns, computedMember.Location);
        }
    }

    private void CompileObjectSelectorExpressionAssignmentPush(ObjectSelectorMemberExpression objSelector)
    {
        CompileExpression(objSelector.Object);
        CompileExpression(objSelector.Property);

        AddInstruction(InsObjectSelector.Default);
    }

    /// <summary>
    /// Compiles an attribute member path.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="attrExp"></param>
    private void CompileAttributeMemberExpression(AttributeMemberExpression attrExp)
    {
        CompileExpression(attrExp.Object); // ORG

        if (attrExp.Optional)
        {
            if (CurrentFrame.Version.IsMinimumVersionForOptionalSupport())
                ThrowCompilationError(attrExp, CompilationMessages.Error_OptionalMemberUnsupported);
            else
                AddPostCompilationWarning(CompilationMessages.Warning_UsingOptional_Code);

            AddInstruction(new InsLogicalOptional(), attrExp.Location);
        }

        if (attrExp.Property.Type == Nodes.Identifier)
        {
            CompileIdentifier(attrExp.Property.As<Identifier>(), attribute: true);
        }
        else if (attrExp.Property is StaticMemberExpression)
        {
            CompileStaticMemberExpressionAttributeEval(attrExp.Property.As<StaticMemberExpression>());
        }
        else
            ThrowCompilationError(attrExp, "Expected attribute member to be identifier or static member expression.");
    }

    private void CompileObjectSelectorExpression(ObjectSelectorMemberExpression objSelectExpr)
    {
        CompileExpression(objSelectExpr.Object);
        CompileExpression(objSelectExpr.Property);
        AddInstruction(InsObjectSelector.Default);
        AddInstruction(new InsEval());
    }

    /// <summary>
    /// Compiles a static member path.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="staticExp"></param>
    private void CompileStaticMemberExpression(StaticMemberExpression staticExp)
    {
        // Recursively build the namespace path
        List<string> pathParts = new(4);
        BuildStaticPath(staticExp, ref pathParts);

        string fullPath = string.Join(AdhocConstants.OPERATOR_STATIC, pathParts);
        AdhocSymbol fullPathSymb = SymbolMap.RegisterSymbol(fullPath);

        if (CurrentFrame.Version.HasVariableEvalSupport())
        {
            InsVariableEvaluation eval = new InsVariableEvaluation();
            foreach (string part in pathParts)
            {
                AdhocSymbol symb = SymbolMap.RegisterSymbol(part);
                eval.VariableSymbols.Add(symb);
            }

            eval.VariableSymbols.Add(fullPathSymb);

            AddInstruction(eval, staticExp.Location);
        }
        else
        {
            InsVariablePush push = new InsVariablePush();
            foreach (string part in pathParts)
            {
                AdhocSymbol symb = SymbolMap.RegisterSymbol(part);
                push.VariableSymbols.Add(symb);
            }

            push.VariableSymbols.Add(fullPathSymb);

            AddInstruction(push, staticExp.Location);
            AddInstruction(new InsEval(), staticExp.Location);
        }
    }

    /// <summary>
    /// Compiles a static member path assignment.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="staticExp"></param>
    private void CompileStaticMemberExpressionPush(StaticMemberExpression staticExp)
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

        AddInstruction(push, staticExp.Location);
    }

    /// <summary>
    /// Compiles a static member path as an attribute evaluation.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="staticExp"></param>
    private void CompileStaticMemberExpressionAttributeEval(StaticMemberExpression staticExp)
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
        AddInstruction(attrEval, staticExp.Location);
    }

    /// <summary>
    /// Compiles a function or method call.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="call"></param>
    private void CompileCall(CallExpression call, bool popReturnValue = false)
    {
        // Handle special types first
        if (call.Callee is Identifier ident && ident.Name == "call") // VA_CALL
        {
            if (call.Arguments.Count < 1)
                ThrowCompilationError(call, CompilationMessages.Error_VaCall_MissingFunctionTarget);

            if (call.Arguments.Count < 2)
                ThrowCompilationError(call, CompilationMessages.Error_VaCall_MissingArguments);

            foreach (var arg in call.Arguments)
                CompileExpression(arg);

            var vaCallIns = new InsVaCall() { PopObjectCount = call.Arguments.Count };
            AddInstruction(vaCallIns, call.Location);
            AddPostCompilationWarning(CompilationMessages.Warning_UsingVaCall_Code);

            if (CurrentFrame.Version.ShouldEvalOnCall())
                AddInstruction(new InsEval(), call.Location);
        }
        else if (IsNumberTypeIdentifier(call.Callee) && call.Arguments.Count == 1 && call.Arguments[0].Type == Nodes.Literal &&
            ((Literal)call.Arguments[0]).TokenType == TokenType.NumericLiteral) // UInt(1) => U_INT_CONST
        {
            CompileNumberConstructor(call);
        }
        else
        {
            // Regular calls
            if (call.Callee is Identifier awaitTaskIdentifier && awaitTaskIdentifier.Name == "AwaitTask")
            {
                if (call.Arguments.Count != 1)
                    ThrowCompilationError(call, "AwaitTask expects 1 argument");

                if (call.Arguments[0].Type != Nodes.FunctionExpression && call.Arguments[0].Type != Nodes.ArrowFunctionExpression)
                    ThrowCompilationError(call, "AwaitTask expects a function as argument");

                var awaitStart = new StaticMemberExpression(new Identifier(AdhocConstants.SYSTEM), new Identifier("AwaitTaskStart"), false);
                CompileExpression(awaitStart);
            }
            else
                CompileExpression(call.Callee);

            for (int i = 0; i < call.Arguments.Count; i++)
            {
                if (call.Arguments[i].Type == Nodes.SpreadElement) // Has more than 1
                    ThrowCompilationError(call.Arguments[i], "Only a spread element as an argument is allowed in a Variable function call (VA_CALL). There must not be more than one argument.");

                if (IsUnaryReferenceOfExpression(call.Arguments[i]))
                    CompileUnaryExpression(call.Arguments[i].As<UnaryExpression>(), asReference: true); // We may be pushing it
                else
                    CompileExpression(call.Arguments[i]);
            }

            var callIns = new InsCall(call.Arguments.Count);
            AddInstruction(callIns, call.Location);

            if (CurrentFrame.Version.ShouldEvalOnCall())
                AddInstruction(new InsEval(), call.Location);
        }

        // When calling and not caring about returns
        if (popReturnValue)
            InsertPop();
    }

    /// <summary>
    /// Compiles 'UInt(1), Float(5)' etc.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="call"></param>
    // NOTE: This was added after GT6
    private void CompileNumberConstructor(CallExpression call)
    {
        Identifier calleeIdentifier = call.Callee.As<Identifier>();
        if (call.Arguments.Count < 1)
            ThrowCompilationError(call, CompilationMessages.Error_NumberConstructorMissingArgument);

        if (call.Arguments[0].Type != Nodes.Literal)
            ThrowCompilationError(call, CompilationMessages.Error_NumberConstructorArgumentNotNumericLiteral);

        var literal = (Literal)call.Arguments[0];
        if (literal.TokenType != TokenType.NumericLiteral)
            ThrowCompilationError(call, CompilationMessages.Error_NumberConstructorArgumentNotNumericLiteral);

        try
        {
            switch (calleeIdentifier.Name)
            {
                case "Byte":
                    if (!CurrentFrame.Version.HasByteSupport())
                        ThrowCompilationError(calleeIdentifier, CompilationMessages.Error_V13ByteLiteralsUnsupported);

                    var byteConst = new InsByteConst(Convert.ToSByte(literal.NumericValue));
                    AddInstruction(byteConst, literal.Location);
                    break;
                case "UByte":
                    if (!CurrentFrame.Version.HasUByteSupport())
                        ThrowCompilationError(calleeIdentifier, CompilationMessages.Error_V13UByteLiteralsUnsupported);

                    var ubyteConst = new InsUByteConst(Convert.ToByte(literal.NumericValue));
                    AddInstruction(ubyteConst, literal.Location);
                    break;
                case "Short":
                    if (!CurrentFrame.Version.HasShortSupport())
                        ThrowCompilationError(calleeIdentifier, CompilationMessages.Error_V13ShortLiteralsUnsupported);

                    var shortConst = new InsShortConst(Convert.ToInt16(literal.NumericValue));
                    AddInstruction(shortConst, literal.Location);
                    break;
                case "UShort":
                    if (!CurrentFrame.Version.HasUShortSupport())
                        ThrowCompilationError(calleeIdentifier, CompilationMessages.Error_V13UShortLiteralsUnsupported);

                    var ushortConst = new InsUShortConst(Convert.ToUInt16(literal.NumericValue));
                    AddInstruction(ushortConst, literal.Location);
                    break;
                case "Int":
                    var intConst = new InsIntConst(Convert.ToInt32(literal.NumericValue));
                    AddInstruction(intConst, literal.Location);
                    break;
                case "UInt":
                    if (!CurrentFrame.Version.HasUIntSupport())
                        ThrowCompilationError(literal, CompilationMessages.Error_V12UIntLiteralUnsupported);

                    var uintConst = new InsUIntConst(Convert.ToUInt32(literal.NumericValue));
                    AddInstruction(uintConst, literal.Location);
                    break;
                case "Long":
                    var longConst = new InsLongConst(Convert.ToInt64(literal.NumericValue));
                    AddInstruction(longConst, literal.Location);
                    break;
                case "ULong":
                    if (!CurrentFrame.Version.HasULongSupport())
                        ThrowCompilationError(literal, CompilationMessages.Error_V12ULongLiteralUnsupported);

                    var ulongConst = new InsULongConst(Convert.ToUInt64(literal.NumericValue));
                    AddInstruction(ulongConst, literal.Location);
                    break;
                case "Float":
                    var singleConst = new InsFloatConst(Convert.ToSingle(literal.NumericValue));
                    AddInstruction(singleConst, literal.Location);
                    break;
                case "Double":
                    if (!CurrentFrame.Version.HasDoubleSupport())
                        ThrowCompilationError(literal, CompilationMessages.Error_V12DoubleLiteralUnsupported);

                    var doubleConst = new InsDoubleConst(Convert.ToDouble(literal.NumericValue));
                    AddInstruction(doubleConst, literal.Location);
                    break;
            }
        }
        catch (OverflowException overflowEx)
        {
            ThrowCompilationError(literal, overflowEx.Message);
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Compiles a binary expression.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="binExp"></param>
    /// <exception cref="InvalidOperationException"></exception>
    private void CompileBinaryExpression(BinaryExpression binExp)
    {
        if (binExp.Left.Type == Nodes.AssignmentExpression)
        {
            // (r = x % y) != 0 - reuse result
            CompileAssignmentExpression(binExp.Left.As<AssignmentExpression>(), false);
        }
        else
        {
            CompileExpression(binExp.Left);
        }

        // Check for logical operators that checks between both conditions
        if (binExp.Operator == BinaryOperator.LogicalAnd ||
            binExp.Operator == BinaryOperator.LogicalOr ||
            binExp.Operator == BinaryOperator.NullishCoalescing)
        {
            if (binExp.Operator == BinaryOperator.LogicalOr)
            {
                InsLogicalBase orIns = CurrentFrame.Version.UsesNewLogicalInstructions() ? new InsLogicalOr() : new InsLogicalOrOld();
                AddInstruction(orIns);
                if (!CurrentFrame.Version.UsesNewLogicalInstructions())
                    InsertPop();

                CompileExpression(binExp.Right);
                orIns.InstructionJumpIndex = CurrentFrame.GetInstructionCount();
            }
            else if (binExp.Operator == BinaryOperator.LogicalAnd)
            {
                InsLogicalBase andIns = CurrentFrame.Version.UsesNewLogicalInstructions() ? new InsLogicalAnd() : new InsLogicalAndOld();
                AddInstruction(andIns);
                if (!CurrentFrame.Version.UsesNewLogicalInstructions())
                    InsertPop();

                CompileExpression(binExp.Right);
                andIns.InstructionJumpIndex = CurrentFrame.GetInstructionCount();
            }
            else if (binExp.Operator == BinaryOperator.NullishCoalescing)
            {
                if (CurrentFrame.Version.IsMinimumVersionForOptionalSupport())
                    ThrowCompilationError(binExp, CompilationMessages.Error_NullCoalescingUnsupported);
                else
                    AddPostCompilationWarning(CompilationMessages.Warning_UsingOptional_Code);

                var jumpIfNotNil = new InsJumpIfNil();
                AddInstruction(jumpIfNotNil, binExp.Location);
                CompileExpression(binExp.Right);
                jumpIfNotNil.InstructionJumpIndex = CurrentFrame.GetInstructionCount();
            }
            else
            {
                throw new InvalidOperationException();
            }

        }
        else if (binExp.Operator == BinaryOperator.InstanceOf)
        {
            ThrowCompilationError(binExp, "isInstanceOf is not valid");
        }
        else
        {

            CompileExpression(binExp.Right);

            string? opStr = binExp.Operator switch
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

            AdhocSymbol opSymbol = SymbolMap.RegisterSymbol(opStr, convertToOperand: CurrentFrame.Version.ShouldUseInternalOperatorNames());
            InsBinaryOperator binOpIns = new InsBinaryOperator(opSymbol);
            AddInstruction(binOpIns, binExp.Location);
        }
    }

    private void CompileFinalizerStatement(FinalizerStatement finalizer)
    {
        string finVariable = $"fin#{SymbolMap.FinalizerTempVariableCounter++}";
        var finSymbol = SymbolMap.RegisterSymbol(finVariable);
        DefineVariableInCurrentScope(finSymbol, AdhocVariableType.LocalVariable);

        var asSubroutine = new FunctionDeclaration(null,
            new NodeList<Expression>(), // No parameters
            new BlockStatement(NodeList.Create(new Statement[] { finalizer })),
            generator: false,
            strict: true,
            async: false);
        asSubroutine.Location = new Location(finalizer.Location.Start, finalizer.Location.End, finalizer.Location.Source);

        var frame = CompileSubroutine(asSubroutine, finalizer.Body, null, new NodeList<Expression>());

        var functionConst = new InsFunctionConst();
        functionConst.CodeFrame = frame;
        AddInstruction(functionConst, finalizer.Location);

        // Assign <temp var>.finally
        InsertLocalVariablePush(finVariable, finalizer.Location);
        InsAttributePush push = new InsAttributePush();
        push.AttributeSymbols.Add(SymbolMap.RegisterSymbol("finally"));
        AddInstruction(push, finalizer.Body.Location);
        InsertAssignPop();
    }

    // NOTE: This doesn't seem to exist past GT5 (no traces/support of it in GT7's compiler). But we gotta support it.
    private void CompileSelfFinalizerExpression(SelfFinalizerExpression finalizer)
    {
        // Example:
        ////////////////////////////////////////////////////////////////////////
        //     VARIABLE_EVAL: self, Local:0
        //     VARIABLE_EVAL: project, Local:2
        //     METHOD_CONST - ()[project]
        //           ATTRIBUTE_EVAL: unloadProject
        //           VARIABLE_EVAL: project, Local:-1
        //           CALL: ArgCount=1
        //           POP
        //           VOID_CONST
        //           SET_STATE: State=RETURN(1)
        //     
        //     OBJECT_SELECTOR
        //     EVAL
        //     VARIABLE_PUSH: fin#2, Local:3
        //     ATTRIBUTE_PUSH: finally
        ////////////////////////////////////////////////////////////////////////
        // Original compiler (GT7) doesn't handle it that way. Was this ^ the original way a simple finally statement was compiled in GT5?
        // Before they switched to to setting self with an object selector? and method const?

        // Push self
        CompileSelfExpression(new SelfExpression());

        string finVariable = $"fin#{SymbolMap.FinalizerTempVariableCounter++}";
        var finSymbol = SymbolMap.RegisterSymbol(finVariable);
        DefineVariableInCurrentScope(finSymbol, AdhocVariableType.LocalVariable);

        var asSubroutine = new FunctionDeclaration(null,
            new NodeList<Expression>(), // No parameters
            new BlockStatement(NodeList.Create(new Statement[] { finalizer.Body })),
            generator: false,
            strict: true,
            async: false);
        asSubroutine.Location = new Location(finalizer.Location.Start, finalizer.Location.End, finalizer.Location.Source);

        var frame = CompileSubroutine(asSubroutine, finalizer.Body, null, new NodeList<Expression>());

        var methodConst = new InsMethodConst();
        methodConst.CodeFrame = frame;
        AddInstruction(methodConst, finalizer.Location);

        // .* finalizer
        AddInstruction(new InsObjectSelector());
        AddInstruction(new InsEval());

        // Assign <temp var>.finally
        InsertLocalVariablePush(finVariable, finalizer.Location);
        InsAttributePush push = new InsAttributePush();
        push.AttributeSymbols.Add(SymbolMap.RegisterSymbol("finally"));
        AddInstruction(push, finalizer.Body.Location);
        InsertAssignPop();
    }

    /// <summary>
    /// Compiles an unary expression.
    /// </summary>
    /// <param name="frame">Current frame.</param>
    /// <param name="unaryExp">Target expression.</param>
    /// <param name="popResult">Whether to pop after the expression to not reuse the result.</param>
    /// <param name="asReference">Whether to treat the expression as a reference, result may or may not be pushed to the reference variable.</param>
    /// <exception cref="NotImplementedException"></exception>
    private void CompileUnaryExpression(UnaryExpression unaryExp, bool popResult = false, bool asReference = false, bool isIndirectionBinaryAssignment = false)
    {
        if (unaryExp is UpdateExpression upd) // ++var / --var etc
        {
            if (!asReference)
            {
                // Assigning to a variable - we need to push
                PushUnaryExpressionArgument(unaryExp.Argument);
            }
            else
            {
                // Reference objects can just be eval'd when doing something like '&myObj--'
                CompileExpression(upd.Argument);
            }

            bool preIncrement = unaryExp.Prefix;

            string op = unaryExp.Operator switch
            {
                UnaryOperator.Increment when !preIncrement => AdhocConstants.UNARY_OPERATOR_POST_INCREMENT,
                UnaryOperator.Increment when preIncrement => AdhocConstants.UNARY_OPERATOR_PRE_INCREMENT,
                UnaryOperator.Decrement when !preIncrement => AdhocConstants.UNARY_OPERATOR_POST_DECREMENT,
                UnaryOperator.Decrement when preIncrement => AdhocConstants.UNARY_OPERATOR_PRE_DECREMENT,
                _ => throw new UnreachableException("Invalid unary operator"),
            };

            bool opToSymbol = CurrentFrame.Version.ShouldUseInternalOperatorNames();
            AdhocSymbol symb = SymbolMap.RegisterSymbol(op, opToSymbol);
            InsUnaryAssignOperator unaryIns = new InsUnaryAssignOperator(symb);
            AddInstruction(unaryIns, unaryExp.Location);
        }
        else if (unaryExp.Operator == UnaryOperator.Indirection) // *var - eval variable
        {
            if (asReference)
                PushUnaryExpressionArgument(unaryExp.Argument);
            else if (unaryExp.Argument is UpdateExpression updArg)
                CompileUnaryExpression(updArg, asReference: true);
            else
            {
                CompileExpression(unaryExp.Argument);

                if (!isIndirectionBinaryAssignment && CurrentFrame.Version.ShouldEvalInIndirection())
                    AddInstruction(new InsEval());
            }

        }
        else if (unaryExp.Operator == UnaryOperator.ReferenceOf) // &var - get reference of variable
        {
            PushUnaryExpressionArgument(unaryExp.Argument);
        }
        else // -var / +var / ~var
        {
            if (unaryExp.Argument is Literal literal)
            {
                if (unaryExp.Operator == UnaryOperator.Minus)
                {
                    if (literal.NumericTokenType == NumericTokenType.UnsignedLong)
                        ThrowCompilationError(unaryExp, "Unary operator '-' cannot be applied to ULong");

                    if (literal.NumericTokenType == NumericTokenType.UnsignedInteger)
                        ThrowCompilationError(unaryExp, "Unary operator '-' cannot be applied to UInt");
                }
            }

            CompileExpression(unaryExp.Argument);
            string op;

            switch (unaryExp.Operator)
            {
                case UnaryOperator.LogicalNot:
                    op = AdhocConstants.UNARY_OPERATOR_LOGICAL_NOT;
                    break;

                case UnaryOperator.Minus:
                    op = AdhocConstants.UNARY_OPERATOR_MINUS;
                    break;

                case UnaryOperator.Plus:
                    op = AdhocConstants.UNARY_OPERATOR_PLUS;
                    break;

                case UnaryOperator.BitwiseNot:
                    op = AdhocConstants.UNARY_OPERATOR_BITWISE_INVERT;
                    break;

                default:
                    ThrowCompilationError(unaryExp, "Invalid operator");
                    return;
            }

            bool opToSymbol = CurrentFrame.Version.ShouldUseInternalOperatorNames();
            AdhocSymbol symb = SymbolMap.RegisterSymbol(op, opToSymbol);
            InsUnaryOperator unaryIns = new InsUnaryOperator(symb);
            AddInstruction(unaryIns, unaryExp.Location);
        }

        // If we aren't assigning, or not using the return value immediately, pop it
        // Usages: i++;
        //         for (var i = 0; i < 10; [i++])

        if (popResult)
            InsertPop();
    }

    private void PushUnaryExpressionArgument(Expression expression)
    {
        if (expression.Type == Nodes.Identifier)
        {
            Identifier id = expression.As<Identifier>();
            InsertLocalVariablePush(id.Name!, id.Location);
        }
        else if (expression.Type == Nodes.MemberExpression)
        {
            if (expression is AttributeMemberExpression attr)
            {
                // ++myObj.property
                CompileAttributeMemberAssignmentPush(attr);
            }
            else if (expression is ComputedMemberExpression comp)
            {
                // --hello["world"];
                CompileComputedMemberExpressionAssignmentPush(comp);
            }
            else if (expression is StaticMemberExpression staticMemberExpression)
            {
                // ++GameParameterUtil::loaded_time;
                CompileStaticMemberExpressionPush(staticMemberExpression);
            }
            else
                ThrowCompilationError(expression, CompilationMessages.Error_UnsupportedUnaryOprationOnMemberExpression);
        }
        else if (expression.Type == Nodes.Literal)
        {
            // Special case: -1 -> int const + unary op
            CompileLiteral(expression.As<Literal>());
        }
        else if (expression.Type == Nodes.CallExpression)
        {
            // --doThing();
            CompileCall(expression.As<CallExpression>());
        }
        else if (expression.Type == Nodes.BinaryExpression)
        {
            // ++(1 + 1)
            CompileBinaryExpression(expression.As<BinaryExpression>());
        }
        else if (expression.Type == Nodes.StaticIdentifier)
        {
            // --::test
            CompileStaticIdentifierPush(expression.As<StaticIdentifier>());
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
    private void CompileLiteral(Literal literal)
    {
        switch (literal.TokenType)
        {
            case TokenType.NilLiteral:
                AddInstruction(new InsNilConst(), literal.Location);
                break;

            case TokenType.BooleanLiteral:
                if (CurrentFrame.Version.HasBoolSupport())
                {
                    // On later versions, a specialized bool instruction was added
                    InsBoolConst boolConst = new InsBoolConst(((bool?)literal.Value!).Value);
                    AddInstruction(boolConst, literal.Location);
                }
                else
                {
                    // Translate bool to int
                    InsIntConst intConst = new InsIntConst(((bool?)literal.Value!).Value ? 1 : 0);
                    AddInstruction(intConst, literal.Location);
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
                        if (!CurrentFrame.Version.HasUIntSupport())
                            ThrowCompilationError(literal, CompilationMessages.Error_V12UIntLiteralUnsupported);
                        ins = new InsUIntConst((uint)literal.NumericValue);
                        break;

                    case NumericTokenType.Long:
                        ins = new InsLongConst((long)literal.NumericValue);
                        break;

                    case NumericTokenType.UnsignedLong:
                        if (!CurrentFrame.Version.HasULongSupport())
                            ThrowCompilationError(literal, CompilationMessages.Error_V12ULongLiteralUnsupported);
                        ins = new InsULongConst((ulong)literal.NumericValue);
                        break;

                    case NumericTokenType.Double:
                        if (!CurrentFrame.Version.HasDoubleSupport())
                            ThrowCompilationError(literal, CompilationMessages.Error_V12DoubleLiteralUnsupported);
                        ins = new InsDoubleConst((double)literal.NumericValue);
                        break;

                    default:
                        throw GetCompilationError(literal, "Unknown numeric literal type");
                }

                AddInstruction(ins, literal.Location);
                break;

            case TokenType.SymbolLiteral:
                InsSymbolConst symbConst = new InsSymbolConst(SymbolMap.RegisterSymbol((string)literal.Value!));
                AddInstruction(symbConst, literal.Location);
                break;

            default:
                ThrowCompilationError(literal, $"Not implemented literal {literal.TokenType}");
                break;
        }
    }

    /// <summary>
    /// Inserts a variable push instruction to push a variable.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="identifier"></param>
    /// <returns></returns>
    private AdhocSymbol InsertLocalVariablePush(string identifier, Location? location = null)
    {
        AdhocSymbol varSymb = SymbolMap.RegisterSymbol(identifier);

        var varPush = new InsVariablePush();
        varPush.VariableSymbols.Add(varSymb);
        AddInstruction(varPush, location);

        return varSymb;
    }

    /// <summary>
    /// Inserts a push to an object attribute
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="attr"></param>
    private void CompileAttributeMemberAssignmentPush(AttributeMemberExpression attr)
    {
        // Pushing to object attribute
        CompileExpression(attr.Object);
        if (attr.Property is not Identifier)
            ThrowCompilationError(attr.Property, "Expected attribute member property identifier.");

        var propIdent = attr.Property.As<Identifier>();

        InsAttributePush attrPush = new InsAttributePush();
        AdhocSymbol attrSymbol = SymbolMap.RegisterSymbol(propIdent.Name!);
        attrPush.AttributeSymbols.Add(attrSymbol);
        AddInstruction(attrPush, propIdent.Location);
    }


    #region Scope Handling

    /// <summary>
    /// Enters a new scope.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="node"></param>
    /// <returns></returns>
    private ScopeContext EnterScope(bool shouldCleanupOnExit = false)
    {
        var lastScope = CurrentLocalScope;

        var scope = new ScopeContext()
        {
            CleanupOnExit = shouldCleanupOnExit,
            NumLocals = lastScope.NumLocals,
            StackCounter = lastScope.StackCounter,
        };

        Scopes.Add(scope);
        return scope;
    }

    /// <summary>
    /// Leaves a scope for the frame, inserts a leave scope instruction (if supported).
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="insertLeaveInstruction"></param>
    /// <param name="isModuleLeave"></param>
    // GT7 1.00: 30D8FC0 (mCompiler::LeaveScope)
    private int LeaveScope()
    {
        var scope = Scopes[^1];
        Scopes.Remove(scope);

        if (scope.CleanupOnExit && CurrentFrame.Version.HasLeaveSupport())
        {
            // We return the index of the instruction before the leave.
            int instructionIndex = CurrentFrame.GetInstructionCount();

            var insLeave = new InsLeaveScope();
            insLeave.VariableStorageRewindIndex = CurrentLocalScope.NumLocals;
            insLeave.ModuleOrClassDepthRewindIndex = DepthPerFrame.Last!.Value;
            AddInstruction(insLeave);

            return instructionIndex;
        }

        return CurrentFrame.GetInstructionCount();
    }

    /// <summary>
    /// Setups the compiler's version and the stack along with it.
    /// </summary>
    /// <param name="version"></param>
    // GT7 1.00: 30D95A0 (mCompiler::CreateCodeFrame)
    private void EnterCodeFrame()
    {
        var frame = new AdhocCodeFrame(Version);
        Frames.Add(frame);

        var scope = new ScopeContext() { Type = AdhocScopeType.TopLevel };
        Scopes.Add(scope);
        ModuleOrClassScopes.Add(scope);
        DepthPerFrame.AddLast(0);

        if (Version.ShouldAllocateVariableForSubroutines())
            DefineVariableInCurrentScope(SymbolMap.RegisterSymbol(AdhocConstants.SELF), AdhocVariableType.LocalVariable);

        RegisterLoopLabelForBreak(AdhocConstants.FUNCTION);
        RegisterLoopLabelForContinue(AdhocConstants.FUNCTION);
    }

    private AdhocCodeFrame LeaveCodeFrame()
    {
        ProcessContinues(0);
        ProcessBreaks(0);

        var frame = Frames[^1];
        DepthPerFrame.RemoveLast();
        Scopes.Remove(CurrentLocalScope);
        ModuleOrClassScopes.Remove(CurrentModuleOrClassScope);
        Frames.Remove(frame);

        return frame;
    }

    // GT7 1.00: 30D9340 (mCompiler::EnterModuleOrClass)
    private void EnterModuleOrClassScope()
    {
        var lastScope = CurrentLocalScope;

        var scope = new ScopeContext()
        {
            Type = AdhocScopeType.ModuleOrClass,
            NumLocals = lastScope.NumLocals,
            StackCounter = lastScope.StackCounter,
        };

        Scopes.Add(scope);
        ModuleOrClassScopes.Add(scope);
        DepthPerFrame.Last!.ValueRef++;
    }

    private void LeaveModuleOrClassScope()
    {
        LeaveScope();
        ModuleOrClassScopes.Remove(ModuleOrClassScopes[^1]);
        DepthPerFrame.Last!.ValueRef--;
    }

    #endregion

    /// <summary>
    /// Compiles a statement and opens a new scope (unless it is a continue or break statement.).
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="statement"></param>
    private void CompileStatementWithScope(Statement statement)
    {
        EnterScope(false);
        CompileStatement(statement);
        LeaveScope();
    }


    #region Instruction insert methods
    /// <summary>
    /// Inserts an attribute eval instruction to access an attribute of a certain object.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="identifier"></param>
    /// <returns></returns>
    private void InsertAttributeEval(AdhocSymbol symbol, Location location)
    {
        if (CurrentFrame.Version.HasAttributeEvalSupport())
        {
            var attrEval = new InsAttributeEvaluation();
            attrEval.AttributeSymbols.Add(symbol); // Only one
            AddInstruction(attrEval, location);
        }
        else
        {
            var attrPush = new InsAttributePush();
            attrPush.AttributeSymbols.Add(symbol); // Only one
            AddInstruction(attrPush, location);
            AddInstruction(new InsEval(), location);
        }
    }

    /// <summary>
    /// Inserts a variable evaluation instruction.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="identifier"></param>
    /// <returns></returns>
    private void InsertVariableEval(AdhocSymbol symbol, Location location)
    {
        if (CurrentFrame.Version.HasVariableEvalSupport())
        {
            var varEval = new InsVariableEvaluation();
            varEval.VariableSymbols.Add(symbol); // Only one
            AddInstruction(varEval, location);
        }
        else
        {
            var varPush = new InsVariablePush();
            varPush.VariableSymbols.Add(symbol); // Only one
            AddInstruction(varPush, location);
            AddInstruction(new InsEval(), location);
        }
    }

    /// <summary>
    /// Inserts a version-aware assign pop
    /// </summary>
    /// <param name="frame"></param>
    private void InsertAssignPop()
    {
        if (CurrentFrame.Version.HasAssignPopSupport())
        {
            AddInstruction(InsAssignPop.Default);
        }
        else
        {
            InsertAssign();
            InsertPop();
        }
    }

    private void InsertAssign()
    {
        if (CurrentFrame.Version.HasNewAssignSupport())
            AddInstruction(InsAssign.Default);
        else // Assume under 10 that its the traditional assign + pop old
            AddInstruction(InsAssignOld.Default);
    }

    /// <summary>
    /// Inserts a version-aware list_assign
    /// </summary>
    /// <param name="frame"></param>
    private void InsertListAssign(int numElements, bool restElement, Location? location = null)
    {
        if (CurrentFrame.Version.HasNewListAssignSupport())
            AddInstruction(new InsListAssign() { VariableCount = numElements, HasRestElement = restElement }, location);
        else
            AddInstruction(new InsListAssignOld() { VariableCount = numElements }, location);
    }

    /// <summary>
    /// Inserts a version-aware pop
    /// </summary>
    /// <param name="frame"></param>
    private void InsertPop()
    {
        if (CurrentFrame.Version.HasNewPopSupport())
            AddInstruction(InsPop.Default);
        else // Assume under 10 that its the traditional assign + pop old
            AddInstruction(InsPopOld.Default);
    }

    /// <summary>
    /// Inserts a version-aware set state
    /// </summary>
    /// <param name="frame"></param>
    private void InsertSetState(AdhocRunState state)
    {
        if (CurrentFrame.Version.HasNewSetStateSupport())
            AddInstruction(new InsSetState(state));
        else
            AddInstruction(new InsSetStateOld(state));
    }

    /// <summary>
    /// Inserts a version-aware void
    /// </summary>
    /// <param name="frame"></param>
    private void InsertVoid()
    {
        if (CurrentFrame.Version.UseVoidInsteadOfNop())
            AddInstruction(new InsVoidConst());
        else
            AddInstruction(new InsNop());
    }

    /// <summary>
    /// Inserts a binary assign operator.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="parentNode"></param>
    /// <param name="assignOperator"></param>
    /// <param name="lineNumber"></param>
    /// <returns></returns>
    private AdhocSymbol InsertBinaryAssignOperator(Node parentNode, AssignmentOperator assignOperator, Location? location = null)
    {
        string? opStr = AssignOperatorToString(assignOperator);
        if (string.IsNullOrWhiteSpace(opStr))
            ThrowCompilationError(parentNode, $"Unrecognized operator '{opStr}'");

        bool opToSymbol = CurrentFrame.Version.ShouldUseInternalOperatorNames();
        var symb = SymbolMap.RegisterSymbol(opStr, opToSymbol);

        if (CurrentFrame.Version.HasBinaryAssignSupport())
        {
            AddInstruction(new InsBinaryAssignOperator(symb), location);
        }
        else
        {
            // FIXME: Not sure about this, and the version
            AddInstruction(new InsBinaryOperator(symb), location);
            AddInstruction(new InsAssign(), location);
        }


        return symb;
    }

    /// <summary>
    /// Inserts an empty return instruction if the frame wasn't explicitly exited with a return statement.
    /// </summary>
    /// <param name="frame"></param>
    private void InsertFrameExitIfNeeded(Node bodyNode)
    {
        // Older versions's compilers don't check if a return at the top level with an argument was already specified
        // A return instruction is added anyway
        if (CurrentFrame.Version.ShouldAlwaysEmitSetStateInFunctions())
        {
            InsertSetState(AdhocRunState.RETURN);
            return;
        }

        // Check whether the frame was terminated with an explicit return, if not, insert one
        if (bodyNode is BlockStatement blockStatement && (blockStatement.ChildNodes.Count == 0 || blockStatement.ChildNodes[^1].Type != Nodes.ReturnStatement))
        {
            if (CurrentFrame.Version.ShouldReturnVoidForEmptyFunctionReturn())
            {
                // All functions return a value internally in newer adhoc, even if they don't in the code.
                // So, add one.
                InsertVoid();
            }

            InsertSetState(AdhocRunState.RETURN);
        }
    }

    /// <summary>
    /// For debugging, inserts a nop for scope start/end
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="bodyNode"></param>
    private void InsertNop(Location? location = null, bool useEndLineNumber = false)
    {
        // This was used for debugging on their end (breakpoint on scope tokens with an adhoc debugger)
        // Older versions (< 7) and release scripts just had it not stripped

        // It is known to be emitted for any { } block except function start/end

        // TODO: Have some sort of system to compile as DEBUG, which would emit these anyway
        // It'd then be useful to make a debugger

        if (CurrentFrame.Version.IsNopAlwaysEmitted())
            AddInstruction(new InsNop(), location, useEndLineNumber);
    }

    #endregion

    #region Utils

    private static string? AssignOperatorToString(AssignmentOperator op)
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
    /// Returns whether an expression is an identifier mapping to base numeric types.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    private static bool IsNumberTypeIdentifier(Expression expression)
    {
        if (expression is Identifier identifier)
        {
            switch (identifier.Name)
            {
                case "Byte":
                case "UByte":
                case "Short":
                case "UShort":
                case "Int":
                case "UInt":
                case "Long":
                case "ULong":
                case "Float":
                case "Double":
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns whether the provided expression is an unary indirection of expression (i.e *myObj).
    /// </summary>
    /// <param name="exp"></param>
    /// <returns></returns>
    private static bool IsUnaryIndirection(Expression exp)
    {
        return exp is UnaryExpression unaryExp && unaryExp.Operator == UnaryOperator.Indirection;
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

    private static void BuildStaticPath(StaticMemberExpression exp, ref List<string> pathParts)
    {
        if (exp.Object is StaticMemberExpression obj)
        {
            BuildStaticPath(obj, ref pathParts);
        }
        else if (exp.Object is Identifier identifier)
        {
            pathParts.Add(identifier.Name!);
        }

        if (exp.Property is Identifier propIdentifier)
        {
            pathParts.Add(propIdentifier.Name!);
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
        PostCompilationWarnings.Add(warningCode);
    }

    private void PrintCompilationWarning(Node node, string message)
    {
        Logger.Warn(GetSourceNodeString(node, message));
    }

    [DoesNotReturn]
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

    private static string GetSourceNodeString(Node node, string message)
    {
        return $"{message} at {node.Location.Source}:{node.Location.Start.Line}";
    }

    /// <summary>
    /// Gets or define a new attribute within a module.
    /// </summary>
    /// <param name="module"></param>
    /// <param name="attributeName"></param>
    /// <param name="newVariableType"></param>
    /// <returns></returns>
    // GT7 1.00: 305CB40 (hParser::GetOrDefineModuleAttribute)
    private DeclValue GetOrDefineModuleAttribute(DeclModule module, string attributeName, AdhocVariableType newVariableType, Location? location = null)
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
            newModule = new DeclModule(parent: module, attributeName, newVariableType, location);
        else
            newModule = new DeclValue(parent: module, attributeName, newVariableType, location);

        module.AddVariable(attributeName, newModule);
        return newModule;
    }

    /// <summary>
    /// Returns the last module value from a path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    // GT7 1.00: 305C090 (hParser::GetLastModuleFromPath)
    private DeclValue? GetLastModuleFromPath(List<AdhocSymbol> path)
    {
        // Determine root.
        DeclValue? targetModuleValue = null;

        var pathStart = path[0];
        if (path.Count == 0)
        {
            if (TopLevel is not null)
                targetModuleValue = TopLevel;
            else
                targetModuleValue = CurrentModule;
        }
        else if (pathStart.Name == "__toplevel__")
        {
            targetModuleValue = TopLevel;
        }
        else if (pathStart.Name == "__module__")
        {
            targetModuleValue = CurrentModule;
        }
        else
        {
            // Climb current module to top level for potential module start matches
            for (var module = CurrentModule; module != null; module = module.ParentModule)
            {
                if (module.Variables.TryGetValue(pathStart.Name, out DeclValue? moduleValue))
                {
                    targetModuleValue = moduleValue;
                    break;
                }
            }
        }

        // No valid root found in existing scopes.
        if (targetModuleValue is null)
            return null;

        // Navigate from base module to specific module.
        for (int i = 1; i < path.Count - 1; i++)
        {
            if (targetModuleValue.Type == AdhocVariableType.Unknown)
                break;

            if (targetModuleValue is DeclModule module)
            {
                if (module.Variables.TryGetValue(path[i].Name, out DeclValue? moduleValue))
                    targetModuleValue = moduleValue;
                else
                    return null; // Sub-module not found.
            }
            else
                throw new Exception($"{targetModuleValue.Name} is not a module name. (in {path[^1].Name})");
        }

        return targetModuleValue;
    }

    // GT7 1.00: 30D76D0 (mCompiler::DefineScopeVariablesFromImport)
    private void DefineScopeVariablesFromImport(List<AdhocSymbol> path, AdhocSymbol target, AdhocSymbol? alias = null)
    {
        alias ??= target;

        DeclValue? moduleValue = GetLastModuleFromPath(path);
        if (moduleValue is not null)
        {
            if (moduleValue is DeclModule module)
            {
                if (target.Name == "__mul__")
                {
                    foreach (var variable in module.Variables)
                        DefineVariableInCurrentScope(SymbolMap.RegisterSymbol(variable.Value.Name), variable.Value.Type);
                }
                else
                {
                    if (module.Variables.TryGetValue(target.Name, out DeclValue? targetValue))
                        DefineVariableInCurrentScope(alias, targetValue.Type);
                    else
                        DefineVariableInCurrentScope(alias, AdhocVariableType.Unknown);
                }
            }
        }
        else if (target.Name != "__mul__")
        {
            DefineVariableInCurrentScope(alias, AdhocVariableType.Unknown);
        }
    }

    // GT7 1.00: 305F0C0 (mParser::AddModuleVariablesFromImport)
    public void AddModuleVariablesFromImport(List<AdhocSymbol> path, AdhocSymbol target, AdhocSymbol? alias = null)
    {
        alias ??= target;

        DeclValue? moduleValue = GetLastModuleFromPath(path);
        if (moduleValue is not null)
        {
            if (moduleValue is DeclModule module)
            {
                if (target.Name == "__mul__")
                {
                    foreach (var variable in module.Variables)
                    {
                        CurrentModule.AddVariable(variable.Key, variable.Value);
                    }
                }
                else
                {
                    if (module.Variables.TryGetValue(alias.Name, out DeclValue? targetValue))
                    {
                        CurrentModule.AddVariable(alias.Name, targetValue);
                    }
                    else
                    {
                        if (false /* TODO: Strict mode */)
                            ThrowStrictCheckError($"undefined variable {path[^1].Name}::{target.Name}.");
                    }
                }
            }
            else
            {
                if (false /* TODO: Strict mode */)
                    ThrowStrictCheckError($"{moduleValue.Name} is not a module name. (in {path[^1].Name}).");
            }
        }
        else
        {
            if (false /* TODO: Strict mode */)
                ThrowStrictCheckError($"undefined module variable '{path[^1]}'.");
        }
    }

    /// <summary>
    /// Sets the current module to the specified path.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="variableType"></param>
    /// <exception cref="Exception"></exception>
    // GT7 1.00: 305D320 (hParser::SetCurrentModule)
    private void SetCurrentModulePath(List<string> path, AdhocVariableType variableType)
    {
        // Try finding an existing starting scope path based on our input path
        DeclValue? baseModule = null;
        if (path[0] == "__toplevel__")
        {
            baseModule = TopLevel;
        }
        else if (path[0] == "__module__")
        {
            baseModule = CurrentModule;
        }
        else
        {
            // Go up in module scopes until we find a matching starting path
            for (var currentModule = CurrentModule; currentModule != null; currentModule = currentModule.ParentModule)
            {
                if (currentModule.Variables.TryGetValue(path[0], out DeclValue? moduleVariable))
                {
                    // Found a module scope that starts with what we expect
                    if (moduleVariable.Type != AdhocVariableType.Unknown)
                        baseModule = moduleVariable;
                    else
                        baseModule = (DeclModule)GetOrDefineModuleAttribute(currentModule, path[0], variableType); // Redefine if there's any ambiguity.
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
                baseModule = GetOrDefineModuleAttribute((DeclModule)baseModule, path[i], variableType);
            }
        }
        else
        {
            // We did, so simply define the path starting from the starting module.
            for (int i = 1; i < path.Count - 1; i++)
            {
                if (baseModule is not DeclModule declModule)
                    throw new Exception($"[NameError] '{baseModule.Name}' is not a module name (in {path[^1]}"); // [NameError] '%s' is not a module name. (in %s)

                if (declModule.Variables.TryGetValue(path[i], out DeclValue? moduleVariable))
                {
                    if (moduleVariable.Type != AdhocVariableType.Unknown)
                        baseModule = moduleVariable;
                    else
                        baseModule = GetOrDefineModuleAttribute((DeclModule)baseModule, path[i], variableType); // Redefine if there's any ambiguity.
                }
                else
                {
                    baseModule = GetOrDefineModuleAttribute((DeclModule)baseModule, path[i], variableType);
                }
            }
        }

        if (baseModule is not null)
        {
            CurrentModule = (DeclModule)baseModule;
        }
        else
            throw new Exception($"NameError: cannot set current module {path[^1]}"); // NameError: cannot set current module %s
    }

    /// <summary>
    /// Defines a variable within the current scope.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="variableType"></param>
    /// <param name="location"></param>
    // GT7 1.00: 30D5320 (mCompiler::DefineVariableInCurrentScope)
    private void DefineVariableInCurrentScope(AdhocSymbol name, AdhocVariableType variableType, Location? location = null)
    {
        if (variableType == AdhocVariableType.Undef)
        {
            UndefSymbol(name);
            return;
        }

        // TODO strict mode

        if (false /* TODO strict mode */ && variableType <= AdhocVariableType.Static)
        {
            if (variableType != AdhocVariableType.LocalVariable)
            {
                // Look up the symbol in the parser's declaration table
                DeclValue value = null;

                if (value is not null)
                {
                    if (value.Type == AdhocVariableType.Delegate || value.Type == variableType)
                    {
                        //value->byte54 = 1;
                        value.Location = location.Value;
                    }
                    else
                    {
                        ThrowStrictCheckError($"{variableType} '{value.Name}' is already defined as {value.Type} at {value.Location.Value.Source}:{value.Location.Value.Start.Line}.");
                    }
                }
                else
                {
                    ThrowStrictCheckError($"'{name.Name}' is not declared in this scope.");
                }
            }
        }

        switch (variableType)
        {
            case AdhocVariableType.LocalVariable:
                if (!CurrentLocalScope.Variables.TryGetValue(name, out var varEntry))
                {
                    DefineLocalVariable(name, location);
                    return;
                }
                break;

            default:
                if (!CurrentModuleOrClassScope.Variables.TryGetValue(name, out var _))
                {
                    DefineStaticVariable(variableType, name, location);
                    return;
                }
                break;
        }
    }

    /// <summary>
    /// Defines a static variable within the current scope.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="location"></param>
    /// <returns></returns>
    private int DefineStaticVariable(AdhocVariableType type, AdhocSymbol name, Location? location)
    {
        int staticIndex;
        if (CurrentFrame.Version.UsesNewSplitStack())
        {
            if (CurrentModuleOrClassScope.Variables.TryGetValue(name, out Variable? definedVariable))
            {
                return definedVariable.StackIndex;
            }
            
            staticIndex = CurrentFrame.StaticCount++;

            CurrentModuleOrClassScope.Variables.Add(name, new Variable(name, type, staticIndex, location));
            return staticIndex;
        }
        else
        {
            // GT4/TT and earlier

            // Functions don't count as a variable.
            if (type == AdhocVariableType.Function || type == AdhocVariableType.Method)
                return 0;

            if (CurrentModuleOrClassScope.Variables.TryGetValue(name, out Variable? definedVariable))
            {
                return definedVariable.StackIndex;
            }

            staticIndex = CurrentFrame.LocalCount++;
            CurrentModuleOrClassScope.Variables.Add(name, new Variable(name, type, staticIndex, location));
        }

        return staticIndex;
    }


    /// <summary>
    /// Defines a local variable within the current scope.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="location"></param>
    /// <returns></returns>
    // GT7 1.00: 30D5DC0 (mCompiler::DefineLocalVariable)
    private int DefineLocalVariable(AdhocSymbol name, Location? location = null)
    {
        int localIndex;
        if (CurrentFrame.Version.UsesNewSplitStack())
        {
            if (CurrentLocalScope.Variables.TryGetValue(name, out Variable? definedVariable))
            {
                return definedVariable.StackIndex;
            }

            localIndex = CurrentLocalScope.NumLocals;
            CurrentLocalScope.NumLocals++;
            if (CurrentLocalScope.NumLocals > CurrentFrame.LocalCount)
                CurrentFrame.LocalCount = CurrentLocalScope.NumLocals;

            CurrentLocalScope.CleanupOnExit = true;
            CurrentLocalScope.Variables.Add(name, new Variable(name, AdhocVariableType.LocalVariable, localIndex, location));
        }
        else
        {
            // GT4/TT and earlier
            if (CurrentLocalScope.Variables.TryGetValue(name, out Variable? definedVariable))
            {
                return definedVariable.StackIndex;
            }

            localIndex = CurrentFrame.LocalCount++;
            CurrentLocalScope.Variables.Add(name, new Variable(name, AdhocVariableType.LocalVariable, localIndex, location));
        }
        
        return localIndex;
    }

    private void AddInstruction(InstructionBase instruction, Location? location = null, bool useEndLineNumber = false)
    {
        // Set up some stack utilities
        void IncrementStackCounter()
        {
            CurrentLocalScope.StackCounter++;
            if (CurrentLocalScope.StackCounter > CurrentFrame.MaxStackSize)
                CurrentFrame.MaxStackSize = CurrentLocalScope.StackCounter;
        }

        void DecrementStackCounter()
        {
            CurrentLocalScope.StackCounter--;
        }

        void DecreaseStackCounter(int count)
        {
            CurrentLocalScope.StackCounter -= count;
        }

        instruction.LineNumber = useEndLineNumber ? (uint)(location?.End.Line ?? 0) : (uint)(location?.Start.Line ?? 0);

        var instType = instruction.InstructionType;
        switch (instType)
        {
            case AdhocInstructionType.ARRAY_CONST_OLD:
                {
                    var arrayConstOld = (InsArrayConstOld)instruction;
                    DecreaseStackCounter(arrayConstOld.ArraySize);
                    IncrementStackCounter(); // Return Array

                    CurrentLocalScope.CleanupOnExit = true;
                }
                break;
            case AdhocInstructionType.PRINT:
                {
                    var print = (InsPrint)instruction;
                    DecreaseStackCounter(print.ArgCount);
                    IncrementStackCounter(); // Return
                }
                break;
            case AdhocInstructionType.STRING_PUSH:
                {
                    InsStringPush push = (InsStringPush)instruction;
                    DecreaseStackCounter(push.StringCount);
                    IncrementStackCounter(); // Return concatenation
                    break;
                }
            case AdhocInstructionType.VA_CALL:
                {
                    InsVaCall vaCall = (InsVaCall)instruction;
                    DecreaseStackCounter(vaCall.PopObjectCount);
                    IncrementStackCounter(); // Return vacall value
                    break;
                }

            case AdhocInstructionType.ASSIGN_OLD:
                DecrementStackCounter();
                break;
            case AdhocInstructionType.ATTRIBUTE_DEFINE:
                if (CurrentFrame.Version.HasSupportForAttributeDefinitionDefaultValues())
                    DecrementStackCounter();
                break;
            case AdhocInstructionType.BINARY_ASSIGN_OPERATOR:
                DecrementStackCounter();
                break;
            case AdhocInstructionType.BINARY_OPERATOR:
                DecrementStackCounter();
                break;
            case AdhocInstructionType.JUMP_IF_TRUE:
                DecrementStackCounter();
                break;
            case AdhocInstructionType.JUMP_IF_FALSE:
                DecrementStackCounter();
                break;
            case AdhocInstructionType.POP_OLD:
                DecrementStackCounter();
                break;
            case AdhocInstructionType.REQUIRE:
                DecrementStackCounter();
                break;
            case AdhocInstructionType.THROW:
                DecrementStackCounter();
                break;
            case AdhocInstructionType.ASSIGN:
                DecrementStackCounter();
                break;
            case AdhocInstructionType.ARRAY_PUSH:
                DecrementStackCounter();
                break;
            case AdhocInstructionType.OBJECT_SELECTOR:
                DecrementStackCounter();
                break;
            case AdhocInstructionType.POP:
                DecrementStackCounter();
                break;
            case AdhocInstructionType.ELEMENT_PUSH:
                DecrementStackCounter();
                break;
            case AdhocInstructionType.ELEMENT_EVAL:
                DecrementStackCounter();
                break;
            case AdhocInstructionType.LOGICAL_AND:
                DecrementStackCounter();
                break;
            case AdhocInstructionType.LOGICAL_OR:
                DecrementStackCounter();
                break;
            case AdhocInstructionType.MODULE_CONSTRUCTOR:
                DecrementStackCounter();
                break;
            case AdhocInstructionType.JUMP_IF_NIL:
                DecrementStackCounter();
                break;

            case AdhocInstructionType.ATTRIBUTE_PUSH:
            case AdhocInstructionType.CLASS_DEFINE:
            case AdhocInstructionType.EVAL:
            case AdhocInstructionType.IMPORT:
            case AdhocInstructionType.JUMP:
            case AdhocInstructionType.LOGICAL_AND_OLD:
            case AdhocInstructionType.LOGICAL_OR_OLD:
            case AdhocInstructionType.MODULE_DEFINE:
            case AdhocInstructionType.NOP:
            case AdhocInstructionType.STATIC_DEFINE:
            case AdhocInstructionType.TRY_CATCH:
            case AdhocInstructionType.UNARY_ASSIGN_OPERATOR:
            case AdhocInstructionType.UNARY_OPERATOR:
            case AdhocInstructionType.ATTRIBUTE_EVAL:
            case AdhocInstructionType.SOURCE_FILE:
            case AdhocInstructionType.LEAVE:
            case AdhocInstructionType.CODE_EVAL:
            case AdhocInstructionType.DELEGATE_DEFINE:
            case AdhocInstructionType.LOGICAL_OPTIONAL:
                break;

            case AdhocInstructionType.CALL:
                {
                    InsCall call = (InsCall)instruction;
                    DecreaseStackCounter(call.ArgumentCount);

                    if (call.ArgumentCount > 0)
                        CurrentLocalScope.CleanupOnExit = true;
                }
                break;
            case AdhocInstructionType.LIST_ASSIGN_OLD:
                {
                    var listAssignOld = (InsListAssignOld)instruction;
                    DecreaseStackCounter(listAssignOld.VariableCount);

                    if (listAssignOld.VariableCount > 0)
                        CurrentLocalScope.CleanupOnExit = true;
                }
                break;
            case AdhocInstructionType.LIST_ASSIGN:
                {
                    var listAssign = (InsListAssign)instruction;
                    DecreaseStackCounter(listAssign.VariableCount);

                    if (listAssign.VariableCount > 0)
                        CurrentLocalScope.CleanupOnExit = true;
                }
                break;
            case AdhocInstructionType.CALL_OLD:
                {
                    InsCallOld callOld = (InsCallOld)instruction;
                    DecreaseStackCounter(callOld.ArgumentCount);

                    if (callOld.ArgumentCount > 0)
                        CurrentLocalScope.CleanupOnExit = true;
                }
                break;

            case AdhocInstructionType.FLOAT_CONST:
            case AdhocInstructionType.INT_CONST:
            case AdhocInstructionType.NIL_CONST:
            case AdhocInstructionType.STRING_CONST:
            case AdhocInstructionType.LONG_CONST:
            case AdhocInstructionType.ARRAY_CONST:
            case AdhocInstructionType.MAP_CONST:
            case AdhocInstructionType.VOID_CONST:
            case AdhocInstructionType.U_INT_CONST:
            case AdhocInstructionType.U_LONG_CONST:
            case AdhocInstructionType.DOUBLE_CONST:
            case AdhocInstructionType.BOOL_CONST:
            case AdhocInstructionType.BYTE_CONST:
            case AdhocInstructionType.U_BYTE_CONST:
            case AdhocInstructionType.SHORT_CONST:
            case AdhocInstructionType.U_SHORT_CONST:
            case AdhocInstructionType.SYMBOL_CONST:
                IncrementStackCounter();
                CurrentLocalScope.CleanupOnExit = true;
                break;

            case AdhocInstructionType.FUNCTION_DEFINE:
                {
                    InsFunctionDefine functionDefine = (InsFunctionDefine)instruction;

                    int popCount = 0;
                    if (CurrentFrame.Version.HasSupportForFunctionParametersDefaultValues())
                    {
                        DecreaseStackCounter(functionDefine.CodeFrame.FunctionParameters.Count);
                        popCount += functionDefine.CodeFrame.FunctionParameters.Count;
                    }

                    if (CurrentFrame.Version.VersionNumber >= 8)
                    {
                        DecreaseStackCounter(functionDefine.CodeFrame.CapturedCallbackVariables.Count);
                        popCount += functionDefine.CodeFrame.CapturedCallbackVariables.Count;
                    }

                    if (popCount > 0)
                        CurrentLocalScope.CleanupOnExit = true;
                }
                break;
            case AdhocInstructionType.METHOD_DEFINE:
                {
                    InsMethodDefine methodDefine = (InsMethodDefine)instruction;
                    int popCount = 0;
                    if (CurrentFrame.Version.HasSupportForFunctionParametersDefaultValues())
                    {
                        DecreaseStackCounter(methodDefine.CodeFrame.FunctionParameters.Count);
                        popCount += methodDefine.CodeFrame.FunctionParameters.Count;
                    }

                    if (CurrentFrame.Version.VersionNumber >= 8)
                    {
                        DecreaseStackCounter(methodDefine.CodeFrame.CapturedCallbackVariables.Count);
                        popCount += methodDefine.CodeFrame.CapturedCallbackVariables.Count;
                    }

                    if (popCount > 0)
                        CurrentLocalScope.CleanupOnExit = true;
                }
                break;

            case AdhocInstructionType.UNDEF:
                {
                    InsUndef undef = (InsUndef)instruction;
                    UndefSymbol(undef.Path[^1]);
                }
                break;

            // Conversion to static paths is handled here at the instruction level.
            case AdhocInstructionType.VARIABLE_EVAL:
                {
                    IncrementStackCounter();

                    var variableEval = (InsVariableEvaluation)instruction;
                    variableEval.VariableStorageIndex = GetVariableIndex(variableEval.VariableSymbols, isOnlyEval: true, location);

                    CurrentLocalScope.CleanupOnExit = true;
                }
                break;
            case AdhocInstructionType.VARIABLE_PUSH:
                {
                    IncrementStackCounter();

                    var variablePush = (InsVariablePush)instruction;
                    variablePush.VariableStorageIndex = GetVariableIndex(variablePush.VariableSymbols, isOnlyEval: false, location);

                    CurrentLocalScope.CleanupOnExit = true;
                }
                break;

            case AdhocInstructionType.FUNCTION_CONST:
                {
                    InsFunctionConst functionConst = (InsFunctionConst)instruction;
                    if (CurrentFrame.Version.VersionNumber >= 8)
                        DecreaseStackCounter(functionConst.CodeFrame.FunctionParameters.Count); // Ver > 8

                    if (CurrentFrame.Version.VersionNumber >= 8)
                        DecreaseStackCounter(functionConst.CodeFrame.CapturedCallbackVariables.Count);

                    IncrementStackCounter(); // New function return

                    if (functionConst.CodeFrame.FunctionParameters.Count + functionConst.CodeFrame.CapturedCallbackVariables.Count > 0)
                        CurrentLocalScope.CleanupOnExit = true;
                }
                break;
            case AdhocInstructionType.METHOD_CONST:
                {
                    InsMethodConst methodConst = (InsMethodConst)instruction;
                    if (CurrentFrame.Version.VersionNumber >= 8)
                        DecreaseStackCounter(methodConst.CodeFrame.FunctionParameters.Count); // Ver > 8

                    if (CurrentFrame.Version.VersionNumber >= 8)
                        DecreaseStackCounter(methodConst.CodeFrame.CapturedCallbackVariables.Count);

                    IncrementStackCounter(); // New method return

                    if (methodConst.CodeFrame.FunctionParameters.Count + methodConst.CodeFrame.CapturedCallbackVariables.Count > 0)
                        CurrentLocalScope.CleanupOnExit = true;
                }
                break;

            case AdhocInstructionType.MAP_CONST_OLD:
                {
                    var mapConstOld = (InsMapConstOld)instruction;
                    DecreaseStackCounter(mapConstOld.Value * 2);
                    IncrementStackCounter(); // returned map

                    if (mapConstOld.Value > 0)
                        CurrentLocalScope.CleanupOnExit = true;
                }
                break;

            case AdhocInstructionType.ASSIGN_POP:
            case AdhocInstructionType.MAP_INSERT:
                DecreaseStackCounter(2);
                break;

            case AdhocInstructionType.SET_STATE:
                {
                    InsSetState state = (InsSetState)instruction;
                    if (state.State == AdhocRunState.RETURN || state.State == AdhocRunState.YIELD || state.State == AdhocRunState.CALL)
                        DecrementStackCounter();
                }
                break;
            case AdhocInstructionType.LOCAL_DEFINE:
                break;
            case AdhocInstructionType.SET_STATE_OLD:
                break;

            default:
                throw new ArgumentException($"{nameof(AddInstruction)}: Unsupported instruction {instType}");
        }

        CurrentFrame.Instructions.Add(instruction);
    }

    private void RegisterLoopLabelForContinue(string symbol)
    {
        Continues.Add((symbol, []));
    }

    private void RegisterLoopLabelForBreak(string symbol)
    {
        Breaks.Add((symbol, []));
    }

    // GT7 1.00: 30DAB90 (mCompiler::ProcessContinues)
    private void ProcessContinues(int index)
    {
        var list = Continues[^1];
        for (int i = 0; i < list.Jumps.Count; i++)
        {
            var prevJump = (InsJump)CurrentFrame.Instructions[list.Jumps[i]];
            prevJump.JumpInstructionIndex = index;
        }

        Continues.Remove(list);
    }

    private void ProcessBreaks(int index)
    {
        var list = Breaks[^1];
        for (int i = 0; i < list.Jumps.Count; i++)
        {
            var prevJump = (InsJump)CurrentFrame.Instructions[list.Jumps[i]];
            prevJump.JumpInstructionIndex = index;
        }

        Breaks.Remove(list);
    }

    /// <summary>
    /// Gets the stack/local index of the specified variable path.<br/>
    /// If it does not already exist, it may be created.<br/>
    /// <br/>
    /// <b>The specified path may be amended to form a static path, if a local variable is not found within any scope.</b>
    /// </summary>
    /// <param name="pathParts"></param>
    /// <param name="isOnlyEval"></param>
    /// <param name="location"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    // GT7 1.00: 30D7CA0 (mCompiler::GetVariableIndex)
    private int GetVariableIndex(List<AdhocSymbol> pathParts, bool isOnlyEval, Location? location = null)
    {
        AdhocSymbol fullSymbolPath = pathParts[^1];

        bool crossedFunction = false;
        bool crossedModule = false;
        for (int i = Scopes.Count - 1; i >= 0; i--) // Iterate from current scope to top level
        {
            if (Scopes[i].Variables.TryGetValue(fullSymbolPath, out Variable? variable)) // Is there a variable with that name in this scope?
            {
                if (pathParts.Count > 1) // Static path
                {
                    if (crossedFunction) // Is top level?
                    {
                        return DefineStaticVariable(AdhocVariableType.Unknown, fullSymbolPath, location);
                    }
                    else if (variable.Type != AdhocVariableType.LocalVariable)
                    {
                        return variable.StackIndex;
                    }
                    else
                    {
                        // [NameError] "'%s' is already used as a local variable at %s:%d."
                        ThrowNameError($"{location?.Source}:{location?.Start.Line}: '{fullSymbolPath}' is already used as a local variable at {variable.DeclarationSourceFileName}:{variable.DeclarationLineNumber}.");
                    }
                }
                else // Local path aka local variable
                {
                    if (!crossedFunction)
                    {
                        if (variable.Type != AdhocVariableType.LocalVariable)
                        {
                            if (pathParts.Count == 1)
                                pathParts.Add(pathParts[0]); // Add again for static resolution

                            if (crossedModule)
                                return DefineStaticVariable(AdhocVariableType.Unknown, fullSymbolPath, location);
                        }

                        return variable.StackIndex;
                    }

                    if (variable.Type != AdhocVariableType.LocalVariable)
                    {
                        if (pathParts.Count == 1)
                            pathParts.Add(pathParts[0]); // Add again for static resolution

                        return DefineStaticVariable(AdhocVariableType.Unknown, fullSymbolPath, location);
                    }

                    // Captured variable from other frame.
                    int stackIndex = -(CurrentFrame.CapturedCallbackVariables.Count + 1);
                    CurrentFrame.CapturedCallbackVariables.Add((stackIndex, fullSymbolPath));

                    var capturedVariable = new Variable(fullSymbolPath, AdhocVariableType.LocalVariable, stackIndex, variable.DeclarationLineNumber, variable.DeclarationSourceFileName);
                    CurrentLocalScope.Variables.TryAdd(fullSymbolPath, capturedVariable);

                    return stackIndex;
                }
            }

            if (Scopes[i].Type == AdhocScopeType.TopLevel)
                crossedFunction = i != 0;
            if (Scopes[i].Type == AdhocScopeType.ModuleOrClass)
                crossedModule = i != 0;
        }

        // Did not find anything.

        // Are we evaluating a static (with only one part?)
        // (note: old versions always expand on push)
        if (pathParts.Count == 1 && (isOnlyEval || !Version.HasVariableEvalSupport()))
        {
            // Yes. Define as static and make a path.
            pathParts.Add(pathParts[0]);
        }

        // TODO strict mode. we shouldn't be allowed to define anything that's not yet defined.
        var varType = pathParts.Count > 1 ? AdhocVariableType.Unknown : AdhocVariableType.LocalVariable;
        if (false /* TODO: strict mode */)
        {
            if (pathParts.Count > 1)
            {
                DeclValue moduleValue = GetLastModuleFromPath(pathParts);
                if (moduleValue is not null)
                {
                    varType = moduleValue.Type;
                }
                else
                    ThrowStrictCheckError($"undefined variable '{pathParts[^1].Name}'.");
            }
            else
                ThrowStrictCheckError($"undefined variable '%s'.");
        }
        else
        {
            if (varType == AdhocVariableType.LocalVariable)
                return DefineLocalVariable(fullSymbolPath, location);
            else
                return DefineStaticVariable(varType, fullSymbolPath, location);
        }
    }

    [DoesNotReturn]
    private void ThrowNameError(string message)
    {
        throw new NameErrorException(message);
    }

    [DoesNotReturn]
    private void ThrowStrictCheckError(string message)
    {
        throw new StrictCheckException(message);
    }

    // GT7 1.00: 305D2C0 hParser::DefineAttributeForCurrentModule
    private void DefineAttributeForCurrentModule(string name, AdhocVariableType type, Location location)
    {
        GetOrDefineModuleAttribute(CurrentModule, name, type, location);
    }

    public class NameErrorException : Exception
    {
        public NameErrorException(string message)
            : base(message) { }
    }

    public class StrictCheckException : Exception 
    {
        public StrictCheckException(string message)
            : base(message) { }
    }

    #endregion
}
