using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using AdhocLanguage.Intellisense.Completions;

using Esprima.Ast;
using Esprima;

using GTAdhocToolchain.Analyzer;

namespace AdhocLanguage.Intellisense
{
    public class IntellisenseProvider
    {
        public IntellisenseProvider()
        {

        }

        public static DateTime LastParsed { get; set; }

        public static AdhocScriptAnalyzer LastParsedResult { get; set; }
        private static Object thisLock = new Object();

        public static void Refresh(string code, bool force = false)
        {
            lock (thisLock)
            {
                if (!force && (DateTime.Now - LastParsed).TotalSeconds < 0.5)
                    return;

                LastParsed = DateTime.Now;


                var errHandler = new AdhocErrorHandler();
                AdhocAbstractSyntaxTree tree = new AdhocAbstractSyntaxTree(code, new ParserOptions()
                {
                    ErrorHandler = errHandler
                });

                Script script = tree.ParseScript();

                LastParsedResult = new AdhocScriptAnalyzer(tree);
                LastParsedResult.ErrorHandler = errHandler;
                LastParsedResult.ParseScript(script);
            }
        }

        public static AdhocAnalysisResult GetAtPosition(int pos)
        {
            if (LastParsedResult is null)
                return null;

            return LastParsedResult.GetScopesFromPosition(pos);
        }
    }
}
