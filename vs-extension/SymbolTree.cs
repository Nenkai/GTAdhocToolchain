using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdhocLanguage
{
    public class SymbolNode
    {
        /// <summary>
        /// Name of this symbol
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Modules within this node
        /// </summary>
        public List<SymbolNode> Modules { get; set; } = new List<SymbolNode>();

        /// <summary>
        /// Attributes within this node
        /// </summary>
        public List<SymbolNode> Attributes { get; set; } = new List<SymbolNode>();

        /// <summary>
        /// Parameters if this is a function or method
        /// </summary>
        public List<SymbolNode> Parameters { get; set; } = new List<SymbolNode>();
    }
}
