using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using System.Security.Cryptography.X509Certificates;

using Esprima;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Imaging;
using System.Diagnostics;

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

            SnapshotPoint initialCodeStart = triggerPoint;
            SnapshotPoint autoCompleteStart = triggerPoint;

            // Get current token by looking backwards until semicolon
            while ((initialCodeStart - 1).GetChar() != ';')
                initialCodeStart -= 1;

            while ((initialCodeStart - 1).GetChar() != ';' && char.IsLetter((autoCompleteStart - 1).GetChar()))
                autoCompleteStart -= 1;

            string currentToken = CleanExpression(snapshot.GetText(new SnapshotSpan(initialCodeStart, triggerPoint)));
            ITrackingSpan applicableTo = snapshot.CreateTrackingSpan(new SnapshotSpan(autoCompleteStart, triggerPoint), SpanTrackingMode.EdgeInclusive);

            if (!FindCompletions(currentToken, applicableTo, completionSets))
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

        private bool FindCompletions(string code, ITrackingSpan applicableTo, IList<CompletionSet> completionSets)
        {
            Debug.WriteLine("Finding completions");

            List<Token> tokens = new List<Token>();
            List<Completion> allCompletions = new List<Completion>();
            var s = new Scanner(code);
            
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

            if (tokens.Count == 1)
            {
                // Check keywords
                RegisterKeywords(allCompletions);
            }

            // Filters are the icons at the bottom which lets you filter the current options
            var filters = new List<IIntellisenseFilter>()
            {
                new IntellisenseFilter(KnownMonikers.Namespace, "Modules", "a", "c"),
                new IntellisenseFilter(KnownMonikers.IntellisenseKeyword, "Keywords", "b", "d"),
            };

            // Add sets
            var allSet = new AdhocCompletionSet("All", "All", applicableTo, allCompletions, Enumerable.Empty<Completion>(), filters);
            if (allCompletions.Count > 0)
            {
                completionSets.Add(allSet);
            }

            return allCompletions.Count > 0;
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

