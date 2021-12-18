using System;
using System.Collections.Generic;
using System.IO;

using Esprima;
using Esprima.Utils;

using Newtonsoft.Json;

using GTAdhocCompiler;

namespace GTAdhocCompiler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
                return;

            var source = File.ReadAllText(args[0]);
            var parser = new AdhocAbstractSyntaxTree(source);
            var program = parser.ParseScript();


            var compiler = new AdhocScriptCompiler();
            compiler.Compile(program);
        }
    }
}
