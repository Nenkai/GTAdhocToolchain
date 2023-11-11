using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel.Composition;
using System.Collections.ObjectModel;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Imaging;

using Esprima;
using Esprima.Ast;

using GTAdhocToolchain.Analyzer;

namespace AdhocLanguage.Intellisense.Completions
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("adhoc")]
    [Name("adhocCompletion")]
    class AdhocCompletionSourceProvider : ICompletionSourceProvider
    {
        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new AdhocCompletionSource(textBuffer);

        }
    }

    public class AdhocCompletionSource : ICompletionSource
    {
        private IntellisenseProvider _provider;

        private ITextBuffer _buffer;
        private bool _disposed = false;

        public SymbolNode Tree { get; set; } = new SymbolNode()
        {
            Modules = new List<SymbolNode>
            {
                new SymbolNode()
                {
                    Name = "main",
                    Modules = new List<SymbolNode>()
                    {
                        new SymbolNode()
                        {
                            Name = "pdistd", Modules = new List<SymbolNode>()
                            {
                                 new SymbolNode() { Name = "MActivateEvent" },
                                 new SymbolNode() { Name = "MBbs2" },
                                 new SymbolNode() { Name = "MBLob" },
                                 new SymbolNode() { Name = "MCourse" },
                                 new SymbolNode() { Name = "MGpb" },
                                 new SymbolNode() { Name = "MSqlite" },
                                 new SymbolNode() { Name = "MXml" },
                            }
                        },


                        new SymbolNode() { Name = "pdiext" },

                    },
                },
            },
        };

        public AdhocCompletionSource(ITextBuffer buffer)
        {
            _buffer = buffer;
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (_disposed)
                throw new ObjectDisposedException("AdhocCompletionSource");

            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            var triggerPoint = (SnapshotPoint)session.GetTriggerPoint(snapshot);

            if (triggerPoint == null)
                return;

            IntellisenseProvider.Refresh(snapshot.GetText(), force: true);

            var scopes = IntellisenseProvider.GetAtPosition(triggerPoint.Position);

            SnapshotPoint currentTokenStart = triggerPoint;
            SnapshotPoint fullTokensStart = triggerPoint;

            // Get current token by looking backwards until semicolon
            while (currentTokenStart.Position != 0)
            {
                char prev = (currentTokenStart - 1).GetChar();
                if (char.IsWhiteSpace(prev) || prev == ':' || prev == '}' || prev == '{' || prev == ';' || prev == '(')
                    break;

                currentTokenStart -= 1;
            }

            while (fullTokensStart.Position != 0)
            {
                char prev = (fullTokensStart - 1).GetChar();
                if (char.IsWhiteSpace(prev) || prev == '{' || prev == '}' || prev == ';' || prev == '(')
                    break;

                fullTokensStart -= 1;
            }

            string currentToken = CleanExpression(snapshot.GetText(new SnapshotSpan(currentTokenStart, triggerPoint)));
            string fullToken = snapshot.GetText(new SnapshotSpan(fullTokensStart, triggerPoint));

            // Snapshot span should character at start of token, to end of current token
            ITrackingSpan applicableTo = snapshot.CreateTrackingSpan(new SnapshotSpan(currentTokenStart, triggerPoint), SpanTrackingMode.EdgeInclusive);

            if (!FindCompletions(currentToken, fullToken, triggerPoint, applicableTo, completionSets, scopes))
            {
                session.Dismiss();
            } 
        }

        private string CleanExpression(string inputStr)
        {
            StringBuilder sb = new StringBuilder(inputStr.Length);
            foreach (char c in inputStr)
            {
                if (char.IsWhiteSpace(c) || c == '\r' || c == '\n' || c == '\t')
                    continue;

                sb.Append(c);
            }

            return sb.ToString();
        }

        private bool FindCompletions(string currentToken, string allTokens, SnapshotPoint triggerPoint, ITrackingSpan applicableTo, 
            IList<CompletionSet> completionSets, AdhocAnalysisResult analysis)
        {
            Debug.WriteLine("Finding completions");

            List<Token> tokens = new List<Token>();
            List<Completion> allCompletions = new List<Completion>();
            var s = new Scanner(allTokens, new ParserOptions() { ErrorHandler = new AdhocErrorHandler() { Tolerant = true } });
            
            Token token;
            do
            {
                token = s.Lex();
                if (token.Type != TokenType.EOF)
                    tokens.Add(token);
            }
            while (token.Type != TokenType.EOF);

            bool inCall = false;

            SymbolNode target = Tree;
            for (int i = 0; i < tokens.Count; i++)
            {
                Token t = tokens[i];
                bool isLast = i == tokens.Count - 1;

                // Is '::' navigation?
                if (t.Type == TokenType.Punctuator)
                {
                    if (t.Value as string == "::")
                    {
                        if (isLast)
                        {
                            // Return all statics of path
                            RegisterAllFromSymbol(allCompletions, target);
                        }
                    }
                    else if (t.Value as string == "(")
                    {
                        inCall = true;
                        ;
                    }
                }
                else if (t.Type == TokenType.Identifier)
                {
                    if (isLast)
                    {
                        RegisterAllFromSymbol(allCompletions, target);
                    }
                    else
                    {
                        var symb = HasSymbol(target, t.Value as string);
                        if (symb is null)
                        {
                            if (isLast)
                            {
                                var currentSymbols = FindSymbol(Tree, t.Value as string);
                            }
                            else
                            {
                                // Symbol not found
                                target = null;
                                break;
                            }
                        }
                        else
                        {
                            target = symb;
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            if (tokens.Count >= 1)
            {
                // Check keywords
                CheckVariables(tokens.Last(), triggerPoint, allCompletions, analysis);
                RegisterKeywords(allCompletions);
            }

            
            // Filters are the icons at the bottom which lets you filter the current options
            var filters = new List<IIntellisenseFilter>()
            {
                new IntellisenseFilter(KnownMonikers.LocalVariable, "Variables", "a", "c"),
                new IntellisenseFilter(KnownMonikers.Field, "Statics", "c", "d"),
                new IntellisenseFilter(KnownMonikers.Property, "Attributes", "g", "h"),
                new IntellisenseFilter(KnownMonikers.Module, "Modules", "i", "j"),
                new IntellisenseFilter(KnownMonikers.MethodInstance, "Methods", "k", "l"),
                new IntellisenseFilter(KnownMonikers.Method, "Function", "m", "n"),
                new IntellisenseFilter(KnownMonikers.IntellisenseKeyword, "Keywords", "o", "p"),
            };

            // Add sets
            var allSet = new AdhocCompletionSet("All", "All", applicableTo, allCompletions, Enumerable.Empty<Completion>(), filters);
            if (allCompletions.Count > 0)
            {
                completionSets.Add(allSet);
            }

            return allCompletions.Count > 0;
        }

        private void CheckVariables(Token token, SnapshotPoint start, List<Completion> completions, AdhocAnalysisResult result)
        {
            if (result.Scopes.Count() == 0)
                throw new Exception("No scopes?");

            var variables = result.GetAllDefinedVariables(start.Position);
            foreach (var variable in variables)
            {
                if (token.Type == TokenType.Identifier)
                {
                    var completion = new Completion4(variable.Name, variable.Name, "Variable", KnownMonikers.LocalVariable);
                    completions.Add(completion);
                }
            }

            var statics = result.GetAllDefinedStatics();
            foreach (var staticVar in statics)
            {
                if (token.Type == TokenType.Identifier)
                {
                    var completion = new Completion4(staticVar.Name, staticVar.Name, "Static", KnownMonikers.Field);
                    completions.Add(completion);
                }
            }

            var attributes = result.GetAllDefinedAttributes();
            foreach (var attribute in attributes)
            {
                if (token.Type == TokenType.Identifier)
                {
                    var completion = new Completion4(attribute.Name, attribute.Name, "Attribute", KnownMonikers.Property);
                    completions.Add(completion);

                }
            }

            var methods = result.GetAllDefinedMethods();
            foreach (var method in methods)
            {
                if (token.Type == TokenType.Identifier)
                {
                    var completion = new Completion4(method.Name, method.Name, "Method", KnownMonikers.MethodInstance);
                    completions.Add(completion);

                }
            }

            var funcs = result.GetAllDefinedFunctions();
            foreach (var func in funcs)
            {
                if (token.Type == TokenType.Identifier)
                {
                    var completion = new Completion4(func.Name, func.Name, "Method", KnownMonikers.Method);
                    completions.Add(completion);

                }
            }

            var modules = result.GetAllDefinedModules();
            foreach (var module in modules)
            {
                if (token.Type == TokenType.Identifier)
                {
                    var completion = new Completion4(module.Name, module.Name, "Module", KnownMonikers.Module);
                    completions.Add(completion);
                }
            }
        }

        private void RegisterKeywords(List<Completion> completions)
        {
            foreach (var i in test)
            {
                var completion = new Completion4(i, i, "Keyword", KnownMonikers.IntellisenseKeyword);
                completions.Add(completion);
            }
        }

        public List<string> test = new List<string>()
        {
            "if",
            "do",
            "var",
            "for",
            "foreach",
            "static",
            "attribute",
            "new",
            "try",
            "let",
            "self",
            "else",
            "case",
            "void",
            "while",
            "break",
            "catch",
            "throw",
            "const",
            "yield",
            "class",
            "module", // ADHOC
            "return",
            "switch",
            "import",
            "default",
            "finally",
            "extends",
            "function",
            "method", // ADHOC
            "continue",
            "instanceof",
            "undef", // ADHOC
            "ctor", // ADHOC
            "print", // ADHOC (GT4 etc)
        };

        public void RegisterAllFromSymbol(List<Completion> completions, SymbolNode tree)
        {
            foreach (var sym in tree.Modules)
            {
                var completion = new Completion4(sym.Name, sym.Name, sym.Name, KnownMonikers.Namespace);
                completions.Add(completion);
            }
        }

        public SymbolNode HasSymbol(SymbolNode tree, string name)
        {
            foreach (var entry in tree.Modules)
            {
                if (entry.Name.Equals(name))
                {
                    return entry;
                }
            }

            return null;
        }

        public List<SymbolNode> FindSymbol(SymbolNode tree, string name)
        {
            List<SymbolNode> symbols = new List<SymbolNode>();

            foreach (var entry in tree.Modules)
            {
                if (entry.Name.Contains(name))
                {
                    symbols.Add(entry);
                }
            }

            return symbols;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}

