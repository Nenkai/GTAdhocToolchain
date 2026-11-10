//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

using Esprima;

using System;

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
