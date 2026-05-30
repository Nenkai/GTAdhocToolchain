// Copyright (c) 2026 Nenkai
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Variables
{
    public class StaticVariable : Variable
    {
        public override string ToString()
        {
            return $"Static: {Symbol}";
        }
    }
}
