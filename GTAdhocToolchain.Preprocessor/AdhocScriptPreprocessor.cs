using Esprima;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using Esprima.Ast;

namespace GTAdhocToolchain.Preprocessor;

/* This preprocessor is a bit hacky at times, but it works. It's pretty much intended to work like a C preprocessor.
 * 
 * It does not output text 1:1 as esprima's scanner only fetches the tokens but does not provide
 * a way to have a callback for each character that is read
 * 
 * So that means that we have to find an alternate method to write whitespaces, newlines etc.
 * What we do is copy the entirety of the characters between two tokens using their locations
 * into the output, it may unnecessarily copy whitespaces and comments so kind of wasteful, but it still works nonetheless
 * 
 * Some of the subroutines are duplicated when not operating on the source tokens but macro tokens, could be optimized
 */

/// <summary>
/// Script preprocessor.
/// </summary>
public class AdhocScriptPreprocessor
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    private Dictionary<string, Macro> _definedMacros { get; set; } = new Dictionary<string, Macro>();

    // Current preprocessing state. This will change when we are preprocessing includes or built-in macros
    // Not the most pretty to always refer to it, ideally we should pass it across ALL functions (or make it do stuff)
    public AdhocPreprocessorUnit _state = new AdhocPreprocessorUnit();

    private StringWriter _writer;
    private StringBuilder _sb = new();

    /// <summary>
    /// For the __COUNTER__ directive
    /// </summary>
    private int _counter;
    private DateTime _time;

    private int _includeDepth = 0;

    public AdhocScriptPreprocessor()
    {
        _writer = new StringWriter(_sb);
        _time = DateTime.Now;

        // NOTE: For now special macros are expected to return only one token.
        AddMacro("__LINE__", isBuiltIn: true, isSpecial: true);
        AddMacro("__FILE__", isBuiltIn: true, isSpecial: true);
        AddMacro("__DATE__", isBuiltIn: true, isSpecial: true);
        AddMacro("__TIME__", isBuiltIn: true, isSpecial: true);
        AddMacro("__COUNTER__",  isBuiltIn: true, isSpecial: true);
        AddMacro("__TIMESTAMP__", isBuiltIn: true, isSpecial: true);

        foreach (var def in BuiltinMacros.CompilerProvidedConstants)
        {
            AddMacro(def, isBuiltIn: true);
        }
    }

    public void AddMacros(List<string> macros)
    {
        foreach (var define in macros)
            AddMacro(define);
    }

    public void AddMacro(string code, bool isBuiltIn = false, bool isSpecial = false)
    {
        var old = _state;
        _state = new AdhocPreprocessorUnit();
        _state.SetCode(code);
        _state.Writing = false;

        NextToken();

        Macro define = ParseMacro();
        define.IsBuiltin = isBuiltIn;
        define.IsSpecialMacro = true;
        _definedMacros.TryAdd(define.Name, define);

        _state = old;
    }

    public void SetBaseDirectory(string baseDirectory)
    {
        _state.BaseDirectory = baseDirectory;
    }

    public void SetCurrentFileName(string fileName)
    {
        _state.CurrentFileName = fileName;
        _state?.TokenScanner?.SetFileName(fileName);
    }

    public void SetCurrentFileTimestamp(DateTime timestamp)
    {
        _state.FileTimeStamp = timestamp;
    }

    public string Preprocess(string code)
    {
        _state.SetCode(code);

        NextToken();
        _Preprocess(false);

        return _sb.ToString();
    }

    private void _Preprocess(bool inConditional)
    {
        while (true)
        {
            if (_state.Lookahead.Type == TokenType.EOF)
                break;

            if (_state.Lookahead.Type == TokenType.Punctuator && (string)_state.Lookahead.Value == "#")
            {
                NextToken();

                switch (_state.Lookahead.Value)
                {
                    case "include":
                        DoInclude(); break;

                    case "define":
                        DoDefine(); break;
                    case "undef":
                        DoUndef(); break;

                    case "if":
                        DoIf(); break;
                    case "ifdef":
                        DoIfDef(); break;
                    case "ifndef":
                        DoIfNotDef(); break;

                    case "error":
                        DoError(); break; 

                    case "endif":
                        if (!inConditional)
                            ThrowPreprocessorError(_state.Lookahead, "#endif without #if");
                        return;

                    case "elif":
                        if (!inConditional)
                            ThrowPreprocessorError(_state.Lookahead, "#elif without #if");
                        return;

                    case "else":
                        if (!inConditional)
                            ThrowPreprocessorError(_state.Lookahead, "#else without #if");
                        return;

                    default:
                        ThrowPreprocessorError(_state.Lookahead, $"invalid preprocessing directive #{_state.Lookahead.Value as string}");
                        break;
                }

                continue;
            }
            else if (_state.Lookahead.Type == TokenType.Identifier)
            {
                ProcessSourceIdentifier(_state.Lookahead);
            }
            else
            {
                Write(_state.Lookahead.Value as string);
            }

            NextToken();
        }
    }

    /// <summary>
    /// Parses #define and adds it to the macro table
    /// </summary>
    private void DoDefine()
    {
        NextToken();

        Macro macro = ParseMacro();

        if (!_definedMacros.TryAdd(macro.Name, macro))
        {
            if (_definedMacros.TryGetValue(macro.Name, out Macro original) && original.IsBuiltin)
            {
                ThrowPreprocessorError(macro.NameToken, "Redefining builtin macro");
            }

            Warn(macro.NameToken, $"redefinition of macro '{macro.Name}'");
            _definedMacros[macro.Name] = macro;
        }
    }

    private Macro ParseMacro()
    {
        Token name = _state.Lookahead;

        var define = new Macro();
        define.Name = name.Value as string;
        define.NameToken = name;

        int startLine = name.Location.Start.Line;

        NextToken();

        int count = 0;
        while (true)
        {
            if (_state.Lookahead.Type == TokenType.EOF)
                break;

            if (count == 0 &&
                (string)_state.Lookahead.Value == "(" && _state.Lookahead.Location.Start == name.Location.End) // Function macro arguments's open parenthesis must be right next to macro name
            {
                ParseMacroFunctionParameters(define);
            }
            else if (_state.Lookahead.Value as string == "\\")
            {
                NextToken();
                startLine = _state.Lookahead.Location.Start.Line;
                continue;
            }
            else
            {
                if (_state.Lookahead.Location.Start.Line != startLine)
                    break;

                // If the next token is separated by a whitespace, insert one as a token
                if (define.Content.Count > 0)
                {
                    if (define.Content[^1].Location.End != _state.Lookahead.Location.Start)
                        define.Content.Add(new Token() { Value = " " });
                }

                define.Content.Add(_state.Lookahead);
            }

            count++;
            NextToken();
        }

        return define;
    }

    /// <summary>
    /// Parses #undef
    /// </summary>
    private void DoUndef()
    {
        NextToken();
        Token name = _state.Lookahead;

        if (name.Type != TokenType.Identifier)
            ThrowPreprocessorError(name, "macro names must be identifiers");

        if (!_definedMacros.TryGetValue(name.Value as string, out Macro define))
            Warn(name, $"cannot undef '{name.Value as string}', not defined");
        else if (define.IsBuiltin)
            Warn(name, $"undefining builtin define '{name.Value as string}'");

        _definedMacros.Remove(name.Value as string);

        NextToken();
    }

    private void DoIfDef()
    {
        NextToken();
        Token name = _state.Lookahead;

        if (name.Type != TokenType.Identifier)
            ThrowPreprocessorError(name, "#ifdef: macro names must be identifiers");

        var res = _definedMacros.ContainsKey(name.Value as string);

        NextToken();
        DoConditional(res);
    }

    private void DoIfNotDef()
    {
        NextToken();
        Token name = _state.Lookahead;

        if (name.Type != TokenType.Identifier)
            ThrowPreprocessorError(name, "#ifndef: macro names must be identifiers");

        var res = _definedMacros.ContainsKey(name.Value as string);

        NextToken();
        DoConditional(!res);
    }

    private void DoError()
    {
        Expect("error");

        Token name = _state.Lookahead;
        NextToken();

        Token msg = _state.Lookahead;
        NextToken();

        if (msg.Type != TokenType.Template)
            ThrowPreprocessorError(msg, "#error: error message must be a string");

        ThrowPreprocessorError(name, $"#error was called: {(msg.Value as string).Trim('\"')}");
    }

    private void DoIf()
    {
        int line = _state.Lookahead.Location.Start.Line;
        Token ifToken = _state.Lookahead;
        NextToken();

        var cond = new List<Token>();
        
        while (true)
        {
            if (_state.Lookahead.Type == TokenType.EOF)
                ThrowPreprocessorError(ifToken, "unexpected end of file after #if");

            if (_state.Lookahead.Value as string == "\\")
            {
                NextToken();
                line = _state.Lookahead.Location.Start.Line;
                continue;
            }

            if (line != _state.Lookahead.Location.Start.Line)
                break;

            cond.Add(_state.Lookahead);
            NextToken();
        }

        if (cond.Count == 0)
            ThrowPreprocessorError(ifToken, "#if with no expression");

        var expanded = ExpandTokens(cond);

        var evaluator = new AdhocExpressionEvaluator(expanded);
        int res = evaluator.Evaluate();
        DoConditional(res != 0);
    }

    private void DoInclude()
    {
        if (_includeDepth >= 200)
            ThrowPreprocessorError(_state.Lookahead, "max #include depth reached");

        NextToken();

        if (_state.Lookahead.Type != TokenType.Template)
            ThrowPreprocessorError(_state.Lookahead, "#include expects a file name");

        string file = (_state.Lookahead.Value as string).Trim('\"');
        string pathToInclude = Path.Combine(_state.BaseDirectory, file);
        if (!File.Exists(pathToInclude))
        {
            // Try in same directory as current file
            string dir = Path.GetDirectoryName(Path.Combine(_state.BaseDirectory, _state.CurrentFileName));
            string potentialFile = Path.Combine(dir, file);
            if (!File.Exists(potentialFile))
            {
                // Try alternative based on current (include) file
                if (string.IsNullOrEmpty(_state.CurrentIncludePath))
                    ThrowPreprocessorError(_state.Lookahead, $"#include '{file}': No such file or directory");
                
                potentialFile = Path.Combine(Path.GetDirectoryName(_state.CurrentIncludePath), file);
                if (!File.Exists(potentialFile))
                    ThrowPreprocessorError(_state.Lookahead, $"#include '{file}': No such file or directory");
            }
                
            pathToInclude = potentialFile;
        }

        NextToken();

        var content = File.ReadAllText(pathToInclude);

        // Save state
        var oldState = _state;

        _state = new AdhocPreprocessorUnit();
        _state.BaseDirectory = oldState.BaseDirectory;
        _state.CurrentIncludePath = pathToInclude;
        _state.FileTimeStamp = new FileInfo(pathToInclude).LastWriteTime;
        SetCurrentFileName(file);

        WriteLine($"# 1 \"{file}\"");

        _includeDepth++;
        Preprocess(content);
        _includeDepth--;

        WriteLine();

        // Restore state
        _state = oldState;

        WriteLine($"# {_state.Lookahead.Location.Start.Line} \"{_state.CurrentFileName}\"");
    }

    private void DoConditional(bool res)
    {
        if (res)
        {
            // Do lines until endif
            _Preprocess(true);
        }
        else
        {
            SkipUntilNextScopedConditionalDirective();
        }

        while (true)
        {
            if (_state.Lookahead.Type == TokenType.EOF)
                ThrowPreprocessorError(_state.Lookahead, "Unexpected EOF in conditional preprocessor directive");

            if (_state.Lookahead.Value as string == "else")
            {
                NextToken();
                if (res)
                    SkipUntilNextScopedConditionalDirective();
                else
                    _Preprocess(true);

                if (_state.Lookahead.Type == TokenType.EOF)
                    ThrowPreprocessorError(_state.Lookahead, "Unterminated #else");
            }
            else if (_state.Lookahead.Value as string == "elif")
            {
                if (!res)
                {
                    var cond = new List<Token>();
                    int line = _state.Lookahead.Location.Start.Line;

                    NextToken();
                    while (true)
                    {
                        if (_state.Lookahead.Type == TokenType.EOF)
                            ThrowPreprocessorError(_state.Lookahead, "unexpected end of file after #elif");

                        if (_state.Lookahead.Value as string == "\\")
                        {
                            NextToken();
                            line = _state.Lookahead.Location.Start.Line;
                            continue;
                        }

                        if (line != _state.Lookahead.Location.Start.Line)
                            break;

                        cond.Add(_state.Lookahead);
                        NextToken();
                    }

                    if (cond.Count == 0)
                        ThrowPreprocessorError(_state.Lookahead, "#elif with no expression");

                    List<Token> expanded = ExpandTokens(cond);
                    var evaluator = new AdhocExpressionEvaluator(expanded);

                    var elif_res = evaluator.Evaluate() != 0;
                    if (elif_res)
                        _Preprocess(elif_res);
                    else
                        SkipUntilNextScopedConditionalDirective();

                    if (elif_res && !res)
                        res = true;
                }
                else
                {
                    SkipUntilNextScopedConditionalDirective();
                }
            }
            else if (_state.Lookahead.Value as string == "endif")
            {
                NextToken();
                break;
            }
        }
    }

    private void SkipUntilNextScopedConditionalDirective()
    {
        int depth = 1;
        while (true)
        {
            if (_state.Lookahead.Type == TokenType.EOF)
                ThrowPreprocessorError(_state.Lookahead, "unterminated #ifdef");

            if (_state.Lookahead.Value as string == "#")
            {
                NextToken();

                string dir = _state.Lookahead.Value as string;
                if (dir == "if" || dir == "ifdef" || dir == "ifndef")
                {
                    depth++;
                }
                else if (dir == "elif" || dir == "else")
                {
                    if (depth == 1)
                        break;
                }
                else if (dir == "endif")
                {
                    depth--;
                }
            }

            if (depth < 1)
                break;

            NextToken();
        }
    }

    /// <summary>
    /// Parses a macro define's parameters
    /// </summary>
    /// <param name="define"></param>
    private void ParseMacroFunctionParameters(Macro define)
    {
        Expect("(");
        NextToken();

        define.IsFunction = true;

        while ((string)_state.Lookahead.Value != ")")
        {
            var arg = new MacroArgument();
            string name = _state.Lookahead.Value as string;

            if (define.Arguments.Find(e => e.Name == name) is not null)
                ThrowPreprocessorError(_state.Lookahead, $"duplicate macro parameter \"{name}\"");

            arg.Name = name;
            define.Arguments.Add(arg);

            NextToken();

            if ((string)_state.Lookahead.Value == ",")
                NextToken();
        }

        Expect(")");
    }

    /// <summary>
    /// Processes a source code identifier
    /// </summary>
    /// <param name="token"></param>
    /// <exception cref="Exception"></exception>
    private void ProcessSourceIdentifier(Token token)
    {
        if (_definedMacros.TryGetValue(token.Value as string, out Macro define))
        {
            if (define.IsFunction)
            {
                // Parse arguments
                NextToken();

                if (_state.Lookahead.Value as string != "(")
                    ThrowPreprocessorError(token, $"Expected arguments for macro function '{define.Name}'");

                var args = CollectArguments(define);
                if (args.Count < define.Arguments.Count)
                    ThrowPreprocessorError(token, $"macro \"{define.Name}\" requires {define.Arguments.Count} arguments, but only {args.Count} given");

                List<Token> expanded = args.Count > 0 ? ExpandMacroFunction(define, args) : ExpandTokens(define.Content);
                WriteTokens(expanded);
            }
            else if (define.IsSpecialMacro)
            {
                var expanded = ExpandSpecialMacro(define, token);
                WriteTokens([expanded]);
            }
            else
            {
                var expanded = ExpandTokens(define.Content);
                WriteTokens(expanded);
            }
        }
        else
        {
            Write(token.Value as string);
        }
    }
    
    private Token ExpandSpecialMacro(Macro define, Token token)
    {
        switch (define.Name)
        {
            case "__LINE__":
                return new Token() { Value = $"{token.Location.Start.Line}u" };

            case "__FILE__":
                return new Token() { Value = $"\"{_state.CurrentFileName.Replace("\\", "\\\\")}\"" }; // Make sure to escape it

            case "__COUNTER__":
                {
                    var tokenValue = new Token() { Value = _counter.ToString() };
                    _counter++;
                    return tokenValue;
                }

            case "__DATE__":
                return new Token() { Value = $"\"{_time:MMM dd yyyy}\"" };

            case "__TIME__":
                return new Token() { Value = $"\"{_time:HH:mm:ss}\"" };

            case "__TIMESTAMP__":
                return new Token() { Value = $"\"{_state.FileTimeStamp:MMM dd yyyy HH:mm:ss yyyy}\"" };
        }

        return null;
    }

    private List<Token> ExpandMacroFunction(Macro define, Dictionary<string, List<Token>> callArgs)
    {
        var list = new List<Token>();

        for (int i = 0; i < define.Content.Count; i++)
        {
            Token? token = define.Content[i];
            if (token.Type != TokenType.Identifier)
            {
                list.Add(token);
                continue;
            }

            if (callArgs.TryGetValue(token.Value as string, out List<Token> replacements))
            {
                list.AddRange(ExpandTokens(replacements));
            }
            else if (_definedMacros.TryGetValue(token.Value as string, out Macro def))
            {
                if (def.IsFunction)
                {
                    // We are possibly passing an argument from parent to child macro function
                    i++;
                    var args = EvalCollectArguments(define.Content, ref i, def);

                    // Translate parent to child args
                    foreach (var arg in args)
                    {
                        var argTokens = arg.Value;

                        for (var j = 0; j < argTokens.Count; j++)
                        {
                            var tken = argTokens[j];
                            if (tken.Type != TokenType.Identifier)
                                continue;

                            string tokenStr = tken.Value as string;
                            if (callArgs.TryGetValue(tokenStr, out List<Token> callTokens))
                            {
                                // Remove arg identifier
                                argTokens.Remove(tken);

                                // Insert what we are replacing with
                                argTokens.InsertRange(j, callTokens);
                                j += callTokens.Count;
                            }
                        }
                    }

                    List<Token> expanded = ExpandMacroFunction(def, args);

                    list.AddRange(expanded);
                }
                else if (define.IsSpecialMacro)
                {
                    var expanded = ExpandSpecialMacro(define, token);
                    list.AddRange([expanded]);
                }
                else
                {
                    List<Token> expanded = ExpandTokens(def.Content);
                    list.AddRange(expanded);
                }
                
            }
            else
            {
                list.Add(token);
            }
        }

        return list;
    }

    /// <summary>
    /// Takes a list of tokens and expands them all if needed
    /// </summary>
    /// <param name="inputTokens"></param>
    /// <returns></returns>
    private List<Token> ExpandTokens(List<Token> inputTokens)
    {
        List<Token> tokens = new List<Token>();

        for (var currentIndex = 0; currentIndex < inputTokens.Count; currentIndex++)
        {
            tokens.AddRange(Evaluate(inputTokens, ref currentIndex));
        }

        return tokens;
    }

    private List<Token> Evaluate(List<Token> list, ref int currentIndex)
    {
        var token = list[currentIndex];

        var output = new List<Token>();

        if (_definedMacros.TryGetValue(token.Value as string, out Macro define))
        {
            if (define.IsFunction)
            {
                // Parse arguments
                var args = EvalCollectArguments(list, ref currentIndex, define);

                List<Token> expanded = ExpandMacroFunction(define, args);
                if (args.Count != define.Arguments.Count)
                    ThrowPreprocessorError(token, "Not enough arguments in macro");

                output.AddRange(expanded);
            }
            else if (define.IsSpecialMacro)
            {
                var expanded = ExpandSpecialMacro(define, token);
                output.AddRange([expanded]);
                currentIndex++;
            }
            else
            {
                int idx = 0;
                output.AddRange(Evaluate(define.Content, ref idx));
            }
        }
        else
        {
            output.Add(token);
        }

        return output;
    }

    /// <summary>
    /// Collects all the arguments of a macro call
    /// </summary>
    /// <param name="define"></param>
    /// <returns></returns>
    private Dictionary<string, List<Token>> CollectArguments(Macro define)
    {
        Expect("(");
        NextToken();

        var args = new Dictionary<string, List<Token>>();

        for (var i = 0; i < define.Arguments.Count; i++)
        {
            if (_state.Lookahead.Value as string == ")")
            {
                NextToken();
                break;
            }

            List<Token> argTokens = CollectArgument();
            args.Add(define.Arguments[i].Name, argTokens);

            if ((string)_state.Lookahead.Value != ",")
                break;

            NextToken();
        }

        return args;
    }

    /// <summary>
    /// Collects all the arguments of a macro call
    /// </summary>
    /// <param name="define"></param>
    /// <returns></returns>
    private Dictionary<string, List<Token>> EvalCollectArguments(List<Token> list, ref int currentIndex, Macro define)
    {
        var token = list[currentIndex++];
        if (token.Type == TokenType.Identifier)
        {
            if (_definedMacros.TryGetValue(token.Value as string, out Macro n))
            {
                return EvalCollectArguments(list, ref currentIndex, define);
            }
        }

        if (token.Value as string != "(")
            ThrowPreprocessorError(token, "Expected '('");

        var args = new Dictionary<string, List<Token>>();
        for (var i = 0; i < define.Arguments.Count; i++)
        {
            List<Token> argTokens = EvalCollectArgument(list, ref currentIndex);
            args.Add(define.Arguments[i].Name, argTokens);

            token = list[currentIndex];
            if ((string)token.Value != ",")
                break;

            ++currentIndex;
        }

        if (token.Value as string == ",")
            ThrowPreprocessorError(token, "Too many arguments to macro call");

        return args;
    }

    /// <summary>
    /// Collects a single argument of a macro call
    /// </summary>
    /// <returns></returns>
    private List<Token> CollectArgument()
    {
        List<Token> tokens = new List<Token>();
        int depth = 0;

        while (true)
        {
            if (_state.Lookahead.Type == TokenType.EOF)
                ThrowPreprocessorError(_state.Lookahead, "unexpected EOF after macro function definition");

            if ((string)_state.Lookahead.Value == ")" && depth == 0)
                break;

            if ((string)_state.Lookahead.Value == "," && depth == 0)
                break;

            if ((string)_state.Lookahead.Value == "(")
                depth++;
            else if ((string)_state.Lookahead.Value == ")")
                depth--;

            tokens.Add(_state.Lookahead);

            NextToken();
        }

        return tokens;
    }

    /// <summary>
    /// Collects a single argument of a macro call
    /// </summary>
    /// <returns></returns>
    private List<Token> EvalCollectArgument(List<Token> tokensToEval, ref int currentIndex)
    {
        List<Token> tokens = new List<Token>();
        int depth = 0;

        var token = tokensToEval[currentIndex];
        while (true)
        {
            if ((string)token.Value == ")" && depth == 0)
                break;

            if ((string)token.Value == "," && depth == 0)
                break;

            if ((string)token.Value == "(")
                depth++;
            else if ((string)token.Value == ")")
                depth--;

            tokens.Add(token);

            token = tokensToEval[++currentIndex];
        }

        return tokens;
    }

    private void Expect(string val)
    {
        if (_state.Lookahead.Value as string != val)
            ThrowPreprocessorError(_state.Lookahead, $"Unexpected '{_state.Lookahead.Value as string}'");
    }

    private void Write(string str)
    {
        if (!_state.Writing)
            return;

        _writer.Write(str);
    }

    private void WriteLine()
    {
        if (!_state.Writing)
            return;

        _writer.WriteLine();
    }

    private void WriteLine(string str)
    {
        if (!_state.Writing)
            return;

        _writer.Write(str);
    }

    private void WriteTokens(IEnumerable<Token> tokens)
    {
        if (!_state.Writing)
            return;

        foreach (var token in tokens)
            Write(token.Value as string);
    }

    /// <summary>
    /// From internal representation to an external structure
    /// </summary>
    private string GetTokenRaw(Token token)
    {
        return _state.TokenScanner.Source.Slice(token.Start, token.End);
    }

    private void NextToken()
    {
        int prevIndex = _state.TokenScanner.Index;
        CollectComments();

        Write(_state.TokenScanner.Source.Substring(prevIndex, _state.TokenScanner.Index - prevIndex));

        var token = _state.TokenScanner.Lex();
        Token t = new Token { Type = token.Type, Value = GetTokenRaw(token), Start = token.Start, End = token.End };

        var start = new Position(token.LineNumber, token.Start - _state.TokenScanner.LineStart);
        var end = new Position(_state.TokenScanner.LineNumber, _state.TokenScanner.Index - _state.TokenScanner.LineStart);

        t.Location = t.Location.WithPosition(start, end);

        _state.Lookahead = t;
    }

    private void CollectComments()
    {
        if (true /*!_config.Comment*/)
        {
            _state.TokenScanner.ScanComments();
        }
        else
        {
            var comments = _state.TokenScanner.ScanComments();

            if (comments.Count > 0)
            {
                for (var i = 0; i < comments.Count; ++i)
                {
                    var e = comments[i];
                    var node = new Comment();
                    node.Type = e.MultiLine ? CommentType.Block : CommentType.Line;
                    node.Value = _state.TokenScanner.Source.Slice(e.Slice[0], e.Slice[1]);
                    node.Start = e.Start;
                    node.End = e.End;
                    node.Loc = e.Loc;
                }
            }
        }
    }

    private void Warn(Token token, string message)
    {
        Logger.Warn($"{message} at {_state.CurrentFileName}:{token.Location.Start.Line}");
    }

    private void ThrowPreprocessorError(Token token, string message)
    {
        throw new PreprocessorException(message, _state.CurrentFileName, token);
    }
}

public class Macro
{
    /// <summary>
    /// Name of the macro.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Token declaring the name of this macro.
    /// </summary>
    public Token NameToken { get; set; }

    /// <summary>
    /// Whether this is macro is a function.
    /// </summary>
    public bool IsFunction { get; set; }

    /// <summary>
    /// Whether this macro is built-in, cannot be replaced.
    /// </summary>
    public bool IsBuiltin { get; set; }

    /// <summary>
    /// Whether this macro is special. That means that the preprocessor will dynamic provide its contents.
    /// </summary>
    public bool IsSpecialMacro { get; set; }

    /// <summary>
    /// Argument names (if macro function).
    /// </summary>
    public List<MacroArgument> Arguments { get; set; } = new List<MacroArgument>();

    /// <summary>
    /// Tokens for the macro's content.
    /// </summary>
    public List<Token> Content { get; set; } = new List<Token>();
}

public class MacroArgument
{
    public string Name { get; set; }
    public List<Token> Arguments { get; set; } = new List<Token>();

    public override string ToString()
    {
        return Name;
    }
}
