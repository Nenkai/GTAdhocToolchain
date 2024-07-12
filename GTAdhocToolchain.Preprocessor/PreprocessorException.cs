using Esprima;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Preprocessor;

public class PreprocessorException : Exception
{
    public string FileName { get; }
    public Token Token { get; }

    public PreprocessorException(string message, string fileName, Token token)
        : base(message)
    {
        FileName = fileName;
        Token = token;
    }
}
