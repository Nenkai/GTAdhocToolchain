using Esprima;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace GTAdhocToolchain.Preprocessor
{
    public class AdhocScriptPreprocessor
    {
        private Dictionary<string, DefineMacro> _defines { get; set; } = new Dictionary<string, DefineMacro>();

        private Scanner _scanner;
        private Token _lookahead;

        private StreamWriter _writer;

        public AdhocScriptPreprocessor()
        {
            _writer = new StreamWriter(new MemoryStream(), Encoding.UTF8);

            foreach (var def in BuiltinDefines.CompilerProvidedConstants)
            {
                var define = new DefineMacro();
                _defines.Add(def.Key, new DefineMacro()
                {
                    Content = new List<Token>()
                    {
                        new Token() { Value = def.Value },
                    }
                });
            }
        }

        public void Preprocess(string code)
        {
            _scanner = new Scanner(code);
            NextToken();
            while (true) 
            {
                if (_lookahead.Type == TokenType.EOF)
                    break;

                if (_lookahead.Type == TokenType.Punctuator && (string)_lookahead.Value == "#")
                {
                    NextToken();

                    switch (_lookahead.Value)
                    {
                        case "define":
                            ParseDefine(); break;
                    }

                    continue;
                }
                else if (_lookahead.Type == TokenType.Identifier)
                {
                    ProcessSourceIdentifier(_lookahead);
                }
                else
                {
                    Write(_lookahead.Value as string);
                }

                NextToken();
            }
        }

        /// <summary>
        /// Parses #define and adds it to the macro table
        /// </summary>
        private void ParseDefine()
        {
            NextToken();
            Token name = _lookahead;

            int count = 0;

            var define = new DefineMacro();

            while (true)
            {
                NextToken();
                if (count == 0 && (string)_lookahead.Value == "(")
                {
                    ParseMacroFunctionParameters(define);
                }
                else
                {
                    if (_lookahead.Location.Start.Line != name.Location.Start.Line)
                        break;

                    define.Content.Add(_lookahead);
                }

                count++;
            }

            if (!_defines.TryAdd((string)name.Value, define))
            {
                Console.WriteLine(@$"Warning: Redefinition of macro '{name.Value}'");
                _defines[(string)name.Value] = define;
            }
        }

        /// <summary>
        /// Parses a macro define's parameters
        /// </summary>
        /// <param name="define"></param>
        private void ParseMacroFunctionParameters(DefineMacro define)
        {
            Expect("(");
            NextToken();

            define.IsFunctionMacro = true;

            while ((string)_lookahead.Value != ")")
            {
                var arg = new DefineMacroArgument();
                arg.Name = _lookahead.Value as string;
                define.Arguments.Add(arg);

                NextToken();

                if ((string)_lookahead.Value == ",")
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
            if (_defines.TryGetValue(token.Value as string, out DefineMacro define))
            {
                if (define.IsFunctionMacro)
                {
                    // Parse arguments
                    NextToken();
                    var args = CollectArguments(define);

                    List<Token> expanded = ExpandMacroFunction(define, args);
                    if (args.Count != define.Arguments.Count)
                        throw new Exception("Not enough arguments in macro function");

                    WriteTokens(expanded);
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
        
        private List<Token> ExpandMacroFunction(DefineMacro define, Dictionary<string, List<Token>> callArgs)
        {
            var list = new List<Token>();

            int i = 0;
            foreach (var token in define.Content)
            {
                if (token.Type != TokenType.Identifier)
                {
                    list.Add(token);
                    continue;
                }

                if (callArgs.TryGetValue(token.Value as string, out List<Token> replacements))
                {
                    list.AddRange(ExpandTokens(replacements));
                }
                else
                {
                    ;
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

            if (_defines.TryGetValue(token.Value as string, out DefineMacro define))
            {
                if (define.IsFunctionMacro)
                {
                    // Parse arguments
                    token = list[++currentIndex];
                    var args = EvalCollectArguments(list, ref currentIndex, define);

                    List<Token> expanded = ExpandMacroFunction(define, args);
                    if (args.Count != define.Arguments.Count)
                        throw new Exception("Not enough arguments in macro");

                    output.AddRange(expanded);
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
        private Dictionary<string, List<Token>> CollectArguments(DefineMacro define)
        {
            Expect("(");
            NextToken();

            var args = new Dictionary<string, List<Token>>();

            for (var i = 0; i < define.Arguments.Count; i++)
            {
                List<Token> argTokens = CollectArgument();
                args.Add(define.Arguments[i].Name, argTokens);

                if ((string)_lookahead.Value != ",")
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
        private Dictionary<string, List<Token>> EvalCollectArguments(List<Token> list, ref int currentIndex, DefineMacro define)
        {
            var token = list[currentIndex++];
            if (token.Value as string != "(")
                throw new Exception("Expected");

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
                if ((string)_lookahead.Value == ")" && depth == 0)
                    break;

                if ((string)_lookahead.Value == "," && depth == 0)
                    break;

                if ((string)_lookahead.Value == "(")
                    depth++;
                else if ((string)_lookahead.Value == ")")
                    depth--;

                tokens.Add(_lookahead);

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
            if (_lookahead.Value as string != val)
                throw new Exception("Unexpected");
        }

        private void Save()
        {
            _writer.Flush();
            File.WriteAllBytes("test.bin", (_writer.BaseStream as MemoryStream).ToArray());
        }

        private void Write(string str)
        {
            _writer.Write(str);
        }

        private void WriteTokens(IEnumerable<Token> tokens)
        {
            foreach (var token in tokens)
                _writer.Write(token.Value as string);
        }

        /// <summary>
        /// From internal representation to an external structure
        /// </summary>
        private string GetTokenRaw(Token token)
        {
            return _scanner.Source.Slice(token.Start, token.End);
        }

        private void NextToken()
        {
            int prevIndex = _scanner.Index;
            CollectComments();

            _writer.Write(_scanner.Source.Substring(prevIndex, _scanner.Index - prevIndex));

            var token = _scanner.Lex();

            Token t;

            t = new Token { Type = token.Type, Value = GetTokenRaw(token), Start = token.Start, End = token.End };

            var start = new Position(token.LineNumber, token.Start - _scanner.LineStart);
            var end = new Position(_scanner.LineNumber, _scanner.Index - _scanner.LineStart);

            t.Location = t.Location.WithPosition(start, end);

            _lookahead = t;
        }

        private void CollectComments()
        {
            if (true /*!_config.Comment*/)
            {
                _scanner.ScanComments();
            }
            else
            {
                var comments = _scanner.ScanComments();

                if (comments.Count > 0)
                {
                    for (var i = 0; i < comments.Count; ++i)
                    {
                        var e = comments[i];
                        var node = new Comment();
                        node.Type = e.MultiLine ? CommentType.Block : CommentType.Line;
                        node.Value = _scanner.Source.Slice(e.Slice[0], e.Slice[1]);
                        node.Start = e.Start;
                        node.End = e.End;
                        node.Loc = e.Loc;
                    }
                }
            }
        }
    }

    public class DefineMacro
    {
        public bool IsFunctionMacro { get; set; }

        public List<DefineMacroArgument> Arguments { get; set; } = new List<DefineMacroArgument>();
        public List<Token> Content { get; set; } = new List<Token>();
    }

    public class DefineMacroArgument
    {
        public string Name { get; set; }
        public List<Token> Arguments { get; set; } = new List<Token>();

        public override string ToString()
        {
            return Name;
        }
    }
}
