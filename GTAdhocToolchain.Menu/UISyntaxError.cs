// Copyright (c) 2026 Nenkai
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Menu;

public class UISyntaxError : Exception
{
    public UISyntaxError(string message)
        : base(message) { }
}
