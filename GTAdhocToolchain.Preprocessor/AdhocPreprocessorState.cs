//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

using Esprima;

using System;

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
    public DateTime FileTimeStamp = DateTime.Now;
    public bool Writing = true;

    public AdhocPreprocessorUnit()
    {
        
    }

    public void SetCode(string code)
    {
        TokenScanner = new Scanner(code, CurrentFileName);
    }
}
