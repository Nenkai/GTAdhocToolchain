// Copyright (c) 2026 Nenkai
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Variables
{
    public class Variable
    {
        public int StackIndex { get; set; } = -1;
        public AdhocSymbol Symbol { get; set; }

        public override string ToString()
        {
            return $"Variable: {Symbol}";
        }
    }
}
