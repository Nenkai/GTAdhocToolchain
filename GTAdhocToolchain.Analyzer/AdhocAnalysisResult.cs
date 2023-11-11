using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Analyzer
{
    public class AdhocAnalysisResult
    {
        public IEnumerable<Scope> Scopes { get; set; }

        public List<Symbol> GetAllDefinedVariables(int endPos)
        {
            var list = new List<Symbol>();

            foreach (var scope in Scopes)
            {
                foreach (var variable in scope.DefinedVariables)
                {
                    if (endPos > variable.Node.Range.Start)
                        list.Add(variable);
                }
                
            }

            return list;
        }

        public List<Symbol> GetAllDefinedStatics()
        {
            var list = new List<Symbol>();

            foreach (var scope in Scopes)
            {
                list.AddRange(scope.DefinedStatics);
            }

            return list;
        }

        public List<Symbol> GetAllDefinedAttributes()
        {
            var list = new List<Symbol>();

            foreach (var scope in Scopes)
            {
                list.AddRange(scope.DefinedAttributes);
            }

            return list;
        }

        public List<Symbol> GetAllDefinedModules()
        {
            var list = new List<Symbol>();

            foreach (var scope in Scopes)
            {
                list.AddRange(scope.DefinedModules);
            }

            return list;
        }

        public List<Symbol> GetAllDefinedMethods()
        {
            var list = new List<Symbol>();

            foreach (var scope in Scopes)
            {
                list.AddRange(scope.DefinedMethods);
            }

            return list;
        }

        public List<Symbol> GetAllDefinedFunctions()
        {
            var list = new List<Symbol>();

            foreach (var scope in Scopes)
            {
                list.AddRange(scope.DefinedFunctions);
            }

            return list;
        }
    }
}
