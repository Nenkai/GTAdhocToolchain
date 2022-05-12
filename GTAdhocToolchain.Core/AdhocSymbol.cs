using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core
{
    public class AdhocSymbol
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool HasHexEscape { get; set; }

        public AdhocSymbol(int id, string name, bool isHexSequence = false)
        {
            Id = id;
            Name = name;
            HasHexEscape = isHexSequence;
        }

        public AdhocSymbol(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"({Id}) {Name}";
        }
    }
}
