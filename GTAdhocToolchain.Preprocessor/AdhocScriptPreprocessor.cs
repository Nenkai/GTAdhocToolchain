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

                    if ((string)_lookahead.Value == "define")
                    {
                        ParseDefine();
                        continue;
                    }
                }
                else if (_lookahead.Type == TokenType.Identifier)
                {
                    ProcessIdentifier(_lookahead);
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

            bool parsedArguments = false;

            var define = new DefineMacro();
            while (true)
            {
                NextToken();
                if (!parsedArguments && (string)_lookahead.Value == "(")
                {
                    ParseMacroFunctionParameters(define);
                    parsedArguments = true;
                    continue;
                }

                if (_lookahead.Location.Start.Line != name.Location.Start.Line)
                    break;

                define.Content.Add(_lookahead);
            }

            if (!_defines.TryAdd((string)name.Value, define))
            {
                Console.WriteLine(@$"Warning: Redefinition of macro '{name.Value}'");
                _defines[(string)name.Value] = define;
            }
        }

        private void ProcessIdentifier(Token token)
        {
            if (_defines.TryGetValue(token.Value as string, out DefineMacro define))
            {
                if (define.IsFunctionMacro)
                {
                    // Parse arguments
                    NextToken();
                    var args = CollectArguments(define);

                    if (args.Count != define.Arguments.Count)
                        throw new Exception("Not enough arguments in macro");
                }
                else
                {
                    foreach (var t in define.Content)
                        ProcessIdentifier(t);
                }
            }
            else
            {
                Write(token.Value as string);
            }
        }

        private List<List<Token>> CollectArguments(DefineMacro define)
        {
            Expect("(");
            NextToken();

            var args = new List<List<Token>>();

            for (var i = 0; i < define.Arguments.Count; i++)
            {
                List<Token> argTokens = CollectArgument();
                args.Add(argTokens);

                if ((string)_lookahead.Value != ",")
                    break;

                NextToken();
            }

            return args;
        }

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
