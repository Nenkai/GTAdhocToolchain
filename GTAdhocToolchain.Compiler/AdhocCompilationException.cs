//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Compiler;

public class AdhocCompilationException : Exception
{
    public AdhocCompilationException(string message, Exception innerException)
        : base(message, innerException)
    {

    }

    public AdhocCompilationException(string message)
        : base(message)
    {

    }
}
