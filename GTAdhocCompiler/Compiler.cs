
using Esprima;
using Esprima.Ast;

using Esprima.Compiler.Instructions;

namespace Esprima.Compiler
{
    public class Compiler
    {
        public List<InstructionBase> Instructions { get; set; } = new();

        public void Compile(Script script)
        {
            var body = script.Body;
            for (var i = 0; i < body.Count; i++)
            {
                var node = body[i];
                if (node is FunctionDeclaration funcDecl)
                {
                    ProcessFunction(script, funcDecl);
                }
            }
        }

        public void ProcessFunction(Script script, FunctionDeclaration decl)
        {
            var funcInst = new FunctionConst();
            Instructions.Add(funcInst);
            funcInst.LineNumber = decl.Location.Start.Line;

            var funcBody = decl.Body;
            
        }
    }
}
