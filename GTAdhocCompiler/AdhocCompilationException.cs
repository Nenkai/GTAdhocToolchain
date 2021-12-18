using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler
{
    public class AdhocCompilationException : Exception
    {
        public AdhocCompilationException(string message)
            : base(message)
        {

        }
    }
}
