// Copyright (c) 2026 Nenkai
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Compiler;

public class AdhocCompilationException : Exception
{
    public AdhocCompilationException(string message)
        : base(message)
    {

    }
}
