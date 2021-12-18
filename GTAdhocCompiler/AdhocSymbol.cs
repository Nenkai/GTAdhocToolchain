using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler
{
    public class AdhocSymbol
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public AdhocSymbol(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return $"({Id}) {Name}";
        }
    }
}
