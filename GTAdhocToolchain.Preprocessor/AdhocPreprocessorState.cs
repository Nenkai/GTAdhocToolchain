using Esprima;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Preprocessor;

/// <summary>
/// Represents the state of a preprocessing unit.
/// </summary>
public class AdhocPreprocessorUnit
{
    public string BaseDirectory;
    public string CurrentFileName;
    public string CurrentIncludePath;
    public Scanner TokenScanner;
    public Token Lookahead;
    public DateTime FileTimeStamp;
    public bool Writing = true;

    public AdhocPreprocessorUnit()
    {
        
    }

    public void SetCode(string code)
    {
        TokenScanner = new Scanner(code);
        if (!string.IsNullOrEmpty(CurrentFileName))
            TokenScanner.SetFileName(CurrentFileName);
    }
}
